using System.Net.Http;
using System.Text.Json;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Core.Services.Printing;

/// <summary>
/// PrusaLink-Verbindung (Prusa-Firmware ab MK3.9 / MK4 / XL).
/// Endpoints:
///   GET /api/v1/status — allgemeiner Status
///   GET /api/v1/job    — Job-Info
///   POST /api/v1/job   — Job-Steuerung (pause/resume/cancel via "command")
///   PUT  /api/v1/files/local/{path} — Upload ( multipart)
///   POST /api/v1/files/local/{filename} — Druck starten
/// Authentifizierung: HTTP Basic-Auth mit API-Key (RealNumber unter Settings → Network → API).
/// Siehe: https://connect.prusa3d.com/docs/prusalink
/// </summary>
public sealed class PrusaLinkConnection : HttpPrinterConnectionBase
{
    private readonly string _apiDigest;

    /// <summary>
    /// Erzeugt eine PrusaLink-Verbindung.
    /// </summary>
    /// <param name="http">HttpClient.</param>
    /// <param name="baseUrl">z.B. http://192.168.1.50:80.</param>
    /// <param name="apiKey">PrusaLink API-Key (RealNumber aus Drucker-Settings).</param>
    public PrusaLinkConnection(HttpClient http, string baseUrl, string apiKey)
        : base(http, baseUrl, apiKey)
    {
        // PrusaLink nutzt HTTP Basic Auth mit "api-key" als Passwort und leerem User
        _apiDigest = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{apiKey}"));
    }

    /// <inheritdoc />
    protected override void ApplyAuth(HttpRequestMessage req)
    {
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", _apiDigest);
    }

    /// <inheritdoc />
    public override async Task<PrinterStatus> GetStatusAsync()
    {
        var json = await GetJsonAsync<JsonElement>("api/v1/status").ConfigureAwait(false);
        if (json.ValueKind == JsonValueKind.Undefined) return PrinterStatus.Offline;
        try
        {
            var state = JsonHelper.GetString(json, "printer", "state") ?? "OFFLINE";
            return state.ToUpperInvariant() switch
            {
                "IDLE" => PrinterStatus.Idle,
                "PRINTING" => PrinterStatus.Printing,
                "PAUSED" => PrinterStatus.Paused,
                "BUSY" or "HEATING" => PrinterStatus.Heating,
                "ATTENTION" or "ERROR" => PrinterStatus.Error,
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
        var json = await GetJsonAsync<JsonElement>("api/v1/printer").ConfigureAwait(false);
        if (json.ValueKind == JsonValueKind.Undefined)
            return new PrinterTemps { Hotend = 0, Bed = 0 };
        try
        {
            var hot = JsonHelper.GetDecimal(json, "temp_nozzle") ?? 0;
            var bed = JsonHelper.GetDecimal(json, "temp_bed") ?? 0;
            decimal? ch = JsonHelper.GetDecimal(json, "temp_chamber");
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
        var json = await GetJsonAsync<JsonElement>("api/v1/job").ConfigureAwait(false);
        if (json.ValueKind == JsonValueKind.Undefined) return null;
        try
        {
            var state = JsonHelper.GetString(json, "state") ?? "";
            if (state.Equals("IDLE", StringComparison.OrdinalIgnoreCase)) return null;
            var name = JsonHelper.GetString(json, "file_path", "name") ?? "";
            var progress = JsonHelper.GetDecimal(json, "progress") ?? 0m;
            var elapsed = JsonHelper.GetInt(json, "time_printing") ?? 0;
            var remaining = JsonHelper.GetInt(json, "time_remaining") ?? 0;
            return new PrinterJobInfo
            {
                FileName = name,
                ProgressPercent = progress * 100m,
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
        // PrusaLink: POST /api/v1/files/local/<filename> mit "to_print": true
        var fileName = Uri.EscapeDataString(Path.GetFileName(filePath));
        // Echter Upload fehlt in Stub — Befehl zum Starten eines bereits hochgeladenen Files:
        var ok = await PostAsync($"api/v1/files/local/{fileName}?to_print=true").ConfigureAwait(false);
        return ok && !requireConfirmation;
    }

    /// <inheritdoc />
    public override async Task<bool> PauseAsync()
        => await PostJsonCommandAsync("api/v1/job", """{"command":"pause"}""").ConfigureAwait(false);

    /// <inheritdoc />
    public override async Task<bool> ResumeAsync()
        => await PostJsonCommandAsync("api/v1/job", """{"command":"resume"}""").ConfigureAwait(false);

    /// <inheritdoc />
    public override async Task<bool> CancelAsync()
        => await PostJsonCommandAsync("api/v1/job", """{"command":"cancel"}""").ConfigureAwait(false);

    /// <summary>POST mit JSON-Body.</summary>
    private async Task<bool> PostJsonCommandAsync(string path, string jsonBody)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{path.TrimStart('/')}")
            {
                Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
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