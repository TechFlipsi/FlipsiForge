using System.Net.Http;
using System.Text.Json;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Core.Services.Printing;

/// <summary>
/// Moonraker-Verbindung (Klipper-Firmware).
/// Endpoints:
///   GET /printer/info                       — Status
///   GET /printer/objects/query?extruder&amp;heater_bed — Temperaturen
///   GET /printer/objects/query?virtual_sdcard&amp;print_stats  — Job-Info
///   POST /printer/print_start?filename=…    — Druck starten
///   POST /printer/print_pause               — pausieren
///   POST /printer/print_resume              — fortsetzen
///   POST /printer/print_cancel              — abbrechen
/// Datei-Upload via /server/files/vfs/{path} (POST multipart).
/// Siehe: https://moonraker.readthedocs.io
/// </summary>
public sealed class MoonrakerConnection : HttpPrinterConnectionBase
{
    /// <summary>
    /// Erzeugt eine Moonraker-Verbindung.
    /// </summary>
    /// <param name="http">HttpClient.</param>
    /// <param name="baseUrl">z.B. http://192.168.1.50:7125.</param>
    /// <param name="apiKey">Optional Moonraker API-Key (X-Api-Key Header).</param>
    public MoonrakerConnection(HttpClient http, string baseUrl, string? apiKey = null)
        : base(http, baseUrl, apiKey) { }

    /// <inheritdoc />
    public override async Task<PrinterStatus> GetStatusAsync()
    {
        var json = await GetJsonAsync<JsonElement>("printer/info").ConfigureAwait(false);
        if (json.ValueKind == JsonValueKind.Undefined) return PrinterStatus.Offline;
        var state = JsonHelper.GetString(json, "result", "state");
        return state switch
        {
            "ready" => PrinterStatus.Idle,
            "printing" => PrinterStatus.Printing,
            "paused" => PrinterStatus.Paused,
            "error" => PrinterStatus.Error,
            "startup" => PrinterStatus.Heating,
            "shutdown" => PrinterStatus.Offline,
            _ => PrinterStatus.Idle
        };
    }

    /// <inheritdoc />
    public override async Task<PrinterTemps> GetTemperaturesAsync()
    {
        var json = await GetJsonAsync<JsonElement>(
            "printer/objects/query?extruder&heater_bed").ConfigureAwait(false);
        if (json.ValueKind == JsonValueKind.Undefined)
            return new PrinterTemps { Hotend = 0, Bed = 0 };

        try
        {
            var temps = json.GetProperty("result").GetProperty("status");
            decimal hotend = 0, bed = 0;
            decimal? chamber = null;

            if (temps.TryGetProperty("extruder", out var ext) &&
                ext.TryGetProperty("temperature", out var extT))
                hotend = extT.GetDecimal();
            if (temps.TryGetProperty("heater_bed", out var b) &&
                b.TryGetProperty("temperature", out var bT))
                bed = bT.GetDecimal();
            if (temps.TryGetProperty("chamber", out var ch) &&
                ch.TryGetProperty("temperature", out var chT) && chT.ValueKind == JsonValueKind.Number)
                chamber = chT.GetDecimal();

            return new PrinterTemps { Hotend = hotend, Bed = bed, Chamber = chamber };
        }
        catch
        {
            return new PrinterTemps { Hotend = 0, Bed = 0 };
        }
    }

    /// <inheritdoc />
    public override async Task<PrinterJobInfo?> GetCurrentJobAsync()
    {
        var json = await GetJsonAsync<JsonElement>(
            "printer/objects/query?virtual_sdcard&print_stats").ConfigureAwait(false);
        if (json.ValueKind == JsonValueKind.Undefined) return null;

        try
        {
            var status = json.GetProperty("result").GetProperty("status");
            var stats = status.GetProperty("print_stats");
            var vsc = status.GetProperty("virtual_sdcard");

            var fileName = JsonHelper.GetString(stats, "filename") ?? "";
            var progress = JsonHelper.GetDecimal(vsc, "progress") ?? 0m;
            var total = JsonHelper.GetInt(stats, "total_duration") ?? 0;
            var printDur = JsonHelper.GetInt(stats, "print_duration") ?? 0;
            var remaining = total > 0 && progress > 0
                ? (int)(printDur / (double)progress - printDur)
                : 0;

            return new PrinterJobInfo
            {
                FileName = fileName,
                ProgressPercent = progress * 100m,
                ElapsedSec = printDur,
                RemainingSec = Math.Max(0, remaining)
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
        // Moonraker: Datei muss in virtual_sdcard liegen. Upload + dann print_start.
        // Für Stub: nur print_start mit Filename. Echter Upload via multipart folgt in v0.3.
        var fileName = Uri.EscapeDataString(Path.GetFileName(filePath));
        var ok = await PostAsync($"printer/print_start?filename={fileName}").ConfigureAwait(false);
        return ok && !requireConfirmation;
    }

    /// <inheritdoc />
    public override Task<bool> PauseAsync() => PostAsync("printer/print_pause");

    /// <inheritdoc />
    public override Task<bool> ResumeAsync() => PostAsync("printer/print_resume");

    /// <inheritdoc />
    public override Task<bool> CancelAsync() => PostAsync("printer/print_cancel");
}