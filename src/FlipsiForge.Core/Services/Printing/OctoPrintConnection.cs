using System.Net.Http;
using System.Text.Json;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Core.Services.Printing;

/// <summary>
/// OctoPrint-Verbindung via REST API.
/// Endpoints:
///   GET  /api/connection   — Verbindungs-Status
///   GET  /api/printer      — Temperaturen & State
///   GET  /api/job          — Job-Info
///   POST /api/files/local/&lt;path&gt;  — Druck starten (command: "select", print: true)
///   POST /api/job          — Job-Steuerung (command: pause/cancel/restart)
///   POST /api/connection   — Verbindung zum Drucker herstellen/trennen
/// Authentifizierung: X-Api-Key Header (oder Authorization: Bearer Token).
/// Siehe: https://docs.octoprint.org/en/master/api
/// </summary>
public sealed class OctoPrintConnection : HttpPrinterConnectionBase
{
    /// <summary>
    /// Erzeugt eine OctoPrint-Verbindung.
    /// </summary>
    /// <param name="http">HttpClient.</param>
    /// <param name="baseUrl">z.B. http://192.168.1.50:80.</param>
    /// <param name="apiKey">OctoPrint API-Key (unter User Settings → API Keys).</param>
    public OctoPrintConnection(HttpClient http, string baseUrl, string apiKey)
        : base(http, baseUrl, apiKey) { }

    /// <inheritdoc />
    public override async Task<PrinterStatus> GetStatusAsync()
    {
        var json = await GetJsonAsync<JsonElement>("api/printer").ConfigureAwait(false);
        if (json.ValueKind == JsonValueKind.Undefined) return PrinterStatus.Offline;
        try
        {
            // OctoPrint: { "state": { "text": "Printing", "flags": { "printing": true, ... } } }
            var state = json.GetProperty("state");
            var text = JsonHelper.GetString(state, "text") ?? "Offline";
            return text.ToLowerInvariant() switch
            {
                "printing" => PrinterStatus.Printing,
                "paused" => PrinterStatus.Paused,
                "operational" => PrinterStatus.Idle,
                "error" => PrinterStatus.Error,
                "closed" or "offline" => PrinterStatus.Offline,
                _ => PrinterStatus.Idle
            };
        }
        catch
        {
            return PrinterStatus.Offline;
        }
    }

    /// <inheritdoc />
    public override async Task<PrinterTemps> GetTemperaturesAsync()
    {
        var json = await GetJsonAsync<JsonElement>("api/printer").ConfigureAwait(false);
        if (json.ValueKind == JsonValueKind.Undefined)
            return new PrinterTemps { Hotend = 0, Bed = 0 };
        try
        {
            // { "temperature": { "tool0": { "actual": 210, "target": 210 }, "bed": { "actual": 60, "target": 60 } } }
            var temps = json.GetProperty("temperature");
            var hot = JsonHelper.GetDecimal(temps, "tool0", "actual") ?? 0;
            var bed = JsonHelper.GetDecimal(temps, "bed", "actual") ?? 0;
            // OctoPrint hat keine Chamber-Temp standardmäßig
            decimal? ch = JsonHelper.GetDecimal(temps, "chamber", "actual");
            return new PrinterTemps { Hotend = hot, Bed = bed, Chamber = ch };
        }
        catch
        {
            return new PrinterTemps { Hotend = 0, Bed = 0 };
        }
    }

    /// <inheritdoc />
    public override async Task<PrinterJobInfo?> GetCurrentJobAsync()
    {
        var json = await GetJsonAsync<JsonElement>("api/job").ConfigureAwait(false);
        if (json.ValueKind == JsonValueKind.Undefined) return null;
        try
        {
            var state = JsonHelper.GetString(json, "state") ?? "";
            if (state.Equals("Operational", StringComparison.OrdinalIgnoreCase)) return null;

            var file = json.GetProperty("job").GetProperty("file");
            var name = JsonHelper.GetString(file, "name") ?? "";
            var progress = json.GetProperty("progress");
            var pct = JsonHelper.GetDecimal(progress, "completion") ?? 0m;
            var elapsed = JsonHelper.GetInt(progress, "printTime") ?? 0;
            var remaining = JsonHelper.GetInt(progress, "printTimeLeft") ?? 0;

            return new PrinterJobInfo
            {
                FileName = name,
                ProgressPercent = pct,
                ElapsedSec = elapsed,
                RemainingSec = remaining
            };
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public override async Task<bool> SendGcodeAsync(string filePath, bool requireConfirmation)
    {
        // OctoPrint: POST /api/files/local/<path> { "command": "select", "print": !requireConfirmation }
        var relPath = Uri.EscapeDataString(filePath.TrimStart('/'));
        var body = JsonSerializer.Serialize(new { command = "select", print = !requireConfirmation });
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/files/local/{relPath}")
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };
            ApplyAuth(req);
            using var cts = new CancellationTokenSource(DefaultTimeout);
            using var resp = await Http.SendAsync(req, cts.Token).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public override async Task<bool> PauseAsync()
        => await PostJobCommandAsync("pause").ConfigureAwait(false);

    /// <inheritdoc />
    public override async Task<bool> ResumeAsync()
        => await PostJobCommandAsync("pause", action: "resume").ConfigureAwait(false);

    /// <inheritdoc />
    public override async Task<bool> CancelAsync()
        => await PostJobCommandAsync("cancel").ConfigureAwait(false);

    /// <summary>POST /api/job mit command.</summary>
    private async Task<bool> PostJobCommandAsync(string command, string? action = null)
    {
        try
        {
            var body = action is null
                ? JsonSerializer.Serialize(new { command })
                : JsonSerializer.Serialize(new { command, action });
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/job")
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };
            ApplyAuth(req);
            using var cts = new CancellationTokenSource(DefaultTimeout);
            using var resp = await Http.SendAsync(req, cts.Token).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}