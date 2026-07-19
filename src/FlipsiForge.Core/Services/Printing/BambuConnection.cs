using System.Text.Json;
using FlipsiForge.Core.Models;
using MQTTnet;
using MQTTnet.Packets;

namespace FlipsiForge.Core.Services.Printing;

/// <summary>
/// Bambu Lab-Verbindung via MQTT (Bambu Connect Protocol).
/// Topics:
///   device/&lt;serial&gt;/report  — Status-Updates vom Drucker (Push)
///   device/&lt;serial&gt;/request — Befehle an den Drucker
/// MQTT-Broker: bblt.bambu-lab.com:8883 (Cloud) oder lokaler LAN-Modus auf Drucker-IP:8883.
/// 
/// Native Abhängigkeit: MQTTnet 5.x. Auf Build-Server läuft kein MQTT-Broker →
/// ConnectAsync liefert false, alle anderen Methoden liefern Offline / 0-Werte.
/// Build kompiliert trotzdem.
/// </summary>
public sealed class BambuConnection : IPrinterConnection, IAsyncDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _serial;
    private readonly string _accessCode;
    private IMqttClient? _client;
    private readonly object _stateLock = new();
    private JsonElement? _lastState;
    private bool _isConnected;

    /// <summary>
    /// Erzeugt eine Bambu-Lab-Verbindung.
    /// </summary>
    /// <param name="host">MQTT-Broker-Host (z.B. 192.168.1.50 für LAN-Modus).</param>
    /// <param name="serial">Drucker-Seriennummer (Device-Topic).</param>
    /// <param name="accessCode">Access-Code aus Bambu-App (LAN-Access-Code, 8 Zeichen).</param>
    /// <param name="port">MQTT-Port — default 8883 (TLS).</param>
    public BambuConnection(string host, string serial, string accessCode, int port = 8883)
    {
        _host = host;
        _port = port;
        _serial = serial;
        _accessCode = accessCode;
    }

    /// <summary>Report-Topic auf dem der Drucker seine Status-Updates veröffentlicht.</summary>
    private string ReportTopic => $"device/{_serial}/report";

    /// <summary>Request-Topic auf dem wir Befehle an den Drucker senden.</summary>
    private string RequestTopic => $"device/{_serial}/request";

    /// <summary>
    /// Verarbeitet eine eingehende MQTT-Nachricht und cached den JSON-State.
    /// </summary>
    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            if (args.ApplicationMessage.Topic != ReportTopic) return Task.CompletedTask;
            // Hinweis (Server-Subagent, 19.07.2026): MQTTnet v5 — Payload ist
            // ReadOnlySequence<byte>; JsonDocument.Parse akzeptiert ReadOnlySpan<byte>.
            // Für Multi-Segment-Sequences manuell in Array kopieren.
            var payload = args.ApplicationMessage.Payload;
            var buffer = new byte[(int)payload.Length];
            var pos = 0;
            foreach (var seg in payload)
            {
                seg.Span.CopyTo(buffer.AsSpan(pos));
                pos += seg.Length;
            }
            using var doc = JsonDocument.Parse(buffer);
            lock (_stateLock)
            {
                _lastState = doc.RootElement.Clone();
            }
        }
        catch { /* ignore malformed payloads */ }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<PrinterStatus> GetStatusAsync()
    {
        lock (_stateLock)
        {
            if (!_isConnected || _lastState is null) return Task.FromResult(PrinterStatus.Offline);
            try
            {
                // Bambu JSON: { "print": { "gcode_state": "RUNNING|IDLE|PAUSE|FAILED" } }
                var state = JsonHelper.GetString(_lastState.Value, "print", "gcode_state");
                return Task.FromResult(state switch
                {
                    "RUNNING" => PrinterStatus.Printing,
                    "PAUSE" => PrinterStatus.Paused,
                    "FAILED" => PrinterStatus.Error,
                    "FINISH" => PrinterStatus.Idle,
                    "IDLE" => PrinterStatus.Idle,
                    _ => PrinterStatus.Idle
                });
            }
            catch
            {
                return Task.FromResult(PrinterStatus.Offline);
            }
        }
    }

    /// <inheritdoc />
    public Task<PrinterTemps> GetTemperaturesAsync()
    {
        lock (_stateLock)
        {
            if (!_isConnected || _lastState is null)
                return Task.FromResult(new PrinterTemps { Hotend = 0, Bed = 0 });
            try
            {
                // Bambu: { "print": { "nozzle_temper": 210, "bed_temper": 60, "chamber_temper": 35 } }
                var hot = JsonHelper.GetDecimal(_lastState.Value, "print", "nozzle_temper") ?? 0;
                var bed = JsonHelper.GetDecimal(_lastState.Value, "print", "bed_temper") ?? 0;
                var ch = JsonHelper.GetDecimal(_lastState.Value, "print", "chamber_temper");
                return Task.FromResult(new PrinterTemps { Hotend = hot, Bed = bed, Chamber = ch });
            }
            catch
            {
                return Task.FromResult(new PrinterTemps { Hotend = 0, Bed = 0 });
            }
        }
    }

    /// <inheritdoc />
    public Task<PrinterJobInfo?> GetCurrentJobAsync()
    {
        lock (_stateLock)
        {
            if (!_isConnected || _lastState is null) return Task.FromResult<PrinterJobInfo?>(null);
            try
            {
                var subtask = JsonHelper.GetString(_lastState.Value, "print", "subtask_name") ?? "";
                var pct = JsonHelper.GetInt(_lastState.Value, "print", "mc_percent") ?? 0;
                var remaining = JsonHelper.GetInt(_lastState.Value, "print", "mc_remaining_time") ?? 0;
                return Task.FromResult<PrinterJobInfo?>(new PrinterJobInfo
                {
                    FileName = subtask,
                    ProgressPercent = pct,
                    ElapsedSec = 0, // Bambu liefert nur Remaining
                    RemainingSec = remaining * 60 // remaining_time ist in Minuten
                });
            }
            catch
            {
                return Task.FromResult<PrinterJobInfo?>(null);
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendGcodeAsync(string filePath, bool requireConfirmation)
    {
        // Bambu: Befehl als JSON-Payload an Request-Topic
        // { "print": { "sequence_id": "0", "command": "project_file", "project_id": "0", "profile_id": "0", "url": "file:///path" } }
        if (!_isConnected || _client is null) return false;
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                print = new
                {
                    sequence_id = "0",
                    command = "project_file",
                    project_id = "0",
                    profile_id = "0",
                    url = $"file:///{filePath}"
                }
            });
            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(RequestTopic)
                .WithPayload(payload)
                .Build();
            await _client.PublishAsync(msg, CancellationToken.None).ConfigureAwait(false);
            return !requireConfirmation;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> PauseAsync() => await SendCommandAsync("pause").ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> ResumeAsync() => await SendCommandAsync("resume").ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> CancelAsync() => await SendCommandAsync("stop").ConfigureAwait(false);

    /// <summary>Sendet einen simplen Befehl an den Request-Topic.</summary>
    private async Task<bool> SendCommandAsync(string command)
    {
        if (!_isConnected || _client is null) return false;
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                print = new { sequence_id = "0", command }
            });
            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(RequestTopic)
                .WithPayload(payload)
                .Build();
            await _client.PublishAsync(msg, CancellationToken.None).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync()
    {
        try
        {
            // MQTTnet 5.x: MqttClientFactory (MqttFactory wurde in v5 umbenannt)
            var factory = new MqttClientFactory();
            _client = factory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_host, _port)
                .WithClientId($"flipsiforge_{_serial}")
                .WithCredentials($"bblp:{_serial}", _accessCode)
                .WithTlsOptions(o => o.UseTls()) // Bambu braucht TLS
                .Build();

            _client.ApplicationMessageReceivedAsync += OnMessageReceived;
            // Hinweis (Server-Subagent, 19.07.2026): MQTTnet v5 — MqttClientConnectResult
            // hat kein IsSuccess mehr, sondern ResultCode (enum MqttClientConnectResultCode).
            var result = await _client.ConnectAsync(options).ConfigureAwait(false);
            _isConnected = result.ResultCode == MqttClientConnectResultCode.Success;

            if (_isConnected)
            {
                var topicFilter = new MqttTopicFilterBuilder().WithTopic(ReportTopic).Build();
                var subOpts = new MqttClientSubscribeOptions
                {
                    TopicFilters = new List<MqttTopicFilter> { topicFilter }
                };
                await _client.SubscribeAsync(subOpts, CancellationToken.None).ConfigureAwait(false);
            }
            return _isConnected;
        }
        catch
        {
            // Erwartet auf Build-Server (kein MQTT-Broker)
            _isConnected = false;
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        try
        {
            if (_client is not null && _client.IsConnected)
                await _client.DisconnectAsync().ConfigureAwait(false);
        }
        catch { /* ignore */ }
        _isConnected = false;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        _client?.Dispose();
    }
}