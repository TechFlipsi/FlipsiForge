using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace FlipsiForge.MockMoonraker;

/// <summary>
/// HTTP + WebSocket Server der die Moonraker-API nachahmt.
/// Reines .NET, keine externen Abhängigkeiten (kein Kestrel, kein ASP.NET nötig).
/// Verwendet HttpListener für HTTP und TcpListener für WebSocket-Upgrade.
/// </summary>
public sealed class MockMoonrakerServer : IDisposable
{
    private readonly int _port;
    private readonly PrinterSimulator _sim;
    private HttpListener? _httpListener;
    private readonly List<WebSocket> _webSocketClients = new();
    private readonly object _wsLock = new();
    private Timer? _wsBroadcastTimer;

    public MockMoonrakerServer(int port, PrinterSimulator sim)
    {
        _port = port;
        _sim = sim;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://+:{_port}/");
        _httpListener.Start();

        // WebSocket Broadcast Timer — sendet alle 2 Sekunden Updates
        _wsBroadcastTimer = new Timer(_ => BroadcastStatus(), null,
            TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));

        Console.WriteLine($"✅ Mock Moonraker läuft auf Port {_port}");
        Console.WriteLine($"   HTTP:  http://localhost:{_port}/printer/info");
        Console.WriteLine($"   WS:    ws://localhost:{_port}/websocket");
        Console.WriteLine();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var ctx = await _httpListener.GetContextAsync().WaitAsync(cancellationToken);
                _ = Task.Run(() => HandleRequest(ctx), cancellationToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Listener: {ex.Message}");
            }
        }

        _wsBroadcastTimer?.Dispose();
        _httpListener?.Stop();
    }

    private async Task HandleRequest(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var resp = ctx.Response;
        var path = req.Url!.AbsolutePath;
        var query = req.Url.Query.TrimStart('?');

        try
        {
            // WebSocket Upgrade
            if (path == "/websocket" && req.IsWebSocketRequest)
            {
                var wsCtx = await ctx.AcceptWebSocketAsync(subProtocol: null);
                HandleWebSocket(wsCtx.WebSocket);
                return;
            }

            // Consume request body for POST requests (HttpListener requires Content-Length)
            if (string.Equals(req.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                try { _ = await req.InputStream.ReadAsync(new byte[1024]); }
                catch { }
            }

            resp.ContentType = "application/json";
            resp.Headers.Add("Access-Control-Allow-Origin", "*");

            var (body, status) = path switch
            {
                "/" or "" => ("{\"result\":\"ok\"}", 200),
                "/printer/info" => (_sim.GetPrinterInfo(), 200),
                "/printer/objects/query" => (_sim.GetObjectsQuery(query), 200),
                "/server/files/list" => (_sim.GetServerFilesList(), 200),
                "/printer/print_start" => HandlePrintStart(query),
                "/printer/print_pause" => PostResponse(_sim.PausePrint()),
                "/printer/print_resume" => PostResponse(_sim.ResumePrint()),
                "/printer/print_cancel" => PostResponse(_sim.CancelPrint()),
                "/printer/gcode/script" => await HandleGcodeScript(req),
                _ => ("{\"error\":\"not_found\"}", 404)
            };

            resp.StatusCode = status;
            var bytes = Encoding.UTF8.GetBytes(body);
            resp.ContentLength64 = bytes.Length;
            await resp.OutputStream.WriteAsync(bytes);
        }
        catch (Exception ex)
        {
            resp.StatusCode = 500;
            var err = JsonSerializer.Serialize(new { error = ex.Message });
            var bytes = Encoding.UTF8.GetBytes(err);
            resp.ContentLength64 = bytes.Length;
            await resp.OutputStream.WriteAsync(bytes);
        }
        finally
        {
            resp.Close();
        }
    }

    private static (string, int) BoolResponse(bool ok)
        => (JsonSerializer.Serialize(new { result = ok ? "ok" : "error" }), ok ? 200 : 400);

    private static (string, int) PostResponse(bool ok)
    {
        // HttpListener requires Content-Length header for POST, curl without body fails.
        // Return same format as BoolResponse but always 200 for mock.
        return (JsonSerializer.Serialize(new { result = ok ? "ok" : "error" }), ok ? 200 : 400);
    }

    private (string, int) HandlePrintStart(string query)
    {
        // ?filename=benchy.gcode
        var filename = System.Web.HttpUtility.ParseQueryString(query).Get("filename") ?? "print.gcode";
        var ok = _sim.StartPrint(filename);
        return PostResponse(ok);
    }

    private static async Task<(string, int)> HandleGcodeScript(HttpListenerRequest req)
    {
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        // Body: {"script": "M104 S200"} or query: ?script=M104 S200
        var script = "";
        if (!string.IsNullOrEmpty(body))
        {
            try
            {
                var json = JsonDocument.Parse(body);
                script = json.RootElement.GetProperty("script").GetString() ?? "";
            }
            catch { }
        }
        if (string.IsNullOrEmpty(script))
        {
            script = System.Web.HttpUtility.ParseQueryString(req.Url!.Query).Get("script") ?? "";
        }
        return BoolResponse(true); // Always succeed in mock
    }

    private void HandleWebSocket(WebSocket ws)
    {
        lock (_wsLock) _webSocketClients.Add(ws);
        Console.WriteLine($"  [WS] Client verbunden ({_webSocketClients.Count} aktiv)");

        _ = Task.Run(async () =>
        {
            var buffer = new byte[4096];
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    // Ignore incoming — mock only sends
                }
            }
            catch { }
            finally
            {
                lock (_wsLock) _webSocketClients.Remove(ws);
                try { ws.Dispose(); } catch { }
                Console.WriteLine($"  [WS] Client getrennt ({_webSocketClients.Count} aktiv)");
            }
        });
    }

    private void BroadcastStatus()
    {
        var status = _sim.GetObjectsQuery("webhooks&extruder&heater_bed&print_stats&virtual_sdcard&display_status");

        List<WebSocket> clients;
        lock (_wsLock) clients = _webSocketClients.ToList();

        foreach (var ws in clients)
        {
            try
            {
                if (ws.State != WebSocketState.Open) continue;
                var bytes = Encoding.UTF8.GetBytes(status);
                ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch { }
        }
    }

    public void Dispose()
    {
        _wsBroadcastTimer?.Dispose();
        _httpListener?.Close();
        lock (_wsLock)
        {
            foreach (var ws in _webSocketClients)
            {
                try { ws.Dispose(); } catch { }
            }
            _webSocketClients.Clear();
        }
    }
}