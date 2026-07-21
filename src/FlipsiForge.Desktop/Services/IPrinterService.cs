// SPDX-License-Identifier: GPL-3.0-or-later
// Desktop-Abstraktion fuer Drucker-Verbindungs-Checks und Auto-Detect.
// Stub gibt immer Offline zurueck, solange der echte PrinterService in Core.Services fehlt.
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Desktop.Services;

/// <summary>Status eines Verbindungs-Checks.</summary>
public enum ConnectionTestState
{
    Unknown,
    Testing,
    Online,
    Offline
}

/// <summary>
/// Ergebnis einer Drucker-Auto-Detect-Abfrage. Best-effort: Felder die nicht
/// erkannt wurden bleiben auf 0 / null / false. Der Aufrufer entscheidet welche
/// Werte er uebernehmen moechte.
/// </summary>
public sealed class PrinterAutoDetectResult
{
    public int BuildVolumeX { get; set; }
    public int BuildVolumeY { get; set; }
    public int BuildVolumeZ { get; set; }
    public int MaxHotendTemp { get; set; }
    public int MaxBedTemp { get; set; }
    public decimal NozzleDiameter { get; set; }
    public string? FirmwareVersion { get; set; }
    public bool IsEnclosed { get; set; }
    public bool IsDirectDrive { get; set; }

    /// <summary>true wenn mindestens ein Feld erkannt wurde (nicht alle null/0).</summary>
    public bool HasData =>
        BuildVolumeX > 0 || BuildVolumeY > 0 || BuildVolumeZ > 0
        || MaxHotendTemp > 0 || MaxBedTemp > 0 || NozzleDiameter > 0
        || !string.IsNullOrEmpty(FirmwareVersion);
}

/// <summary>Service fuer Drucker-Verbindungs-Checks, Druck-Status-Abfrage und Auto-Detect.</summary>
public interface IPrinterService
{
    /// <summary>Testet die Verbindung zu einem Drucker (async).</summary>
    Task<ConnectionTestState> TestConnectionAsync(Printer printer, CancellationToken ct = default);

    /// <summary>Liefert den aktuellen Status eines Druckers.</summary>
    Task<PrinterStatus> GetStatusAsync(Printer printer, CancellationToken ct = default);

    /// <summary>Liefert true, wenn irgendein Drucker gerade druckt.</summary>
    Task<bool> IsAnyPrinterPrintingAsync(CancellationToken ct = default);

    /// <summary>
    /// Fragt per HTTP die Drucker-Daten (Bauvolumen, Duese, Temperaturlimits, Firmware)
    /// automatisch ab. Best-effort — wirft keine Exceptions bei Timeouts oder
    /// nicht-erreichbaren Druckern, sondern liefert ein leeres Result.
    /// </summary>
    Task<PrinterAutoDetectResult> AutoDetectAsync(string ipAddress, PrinterProtocol protocol, CancellationToken ct = default);
}

/// <summary>Stub-Implementierung: Verbindungstests liefern Offline/Idle, aber Auto-Detect ist implementiert.</summary>
public sealed class StubPrinterService : IPrinterService
{
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    /// <inheritdoc />
    public async Task<ConnectionTestState> TestConnectionAsync(Printer printer, CancellationToken ct = default)
    {
        await Task.Delay(250, ct);
        return ConnectionTestState.Offline;
    }

    /// <inheritdoc />
    public async Task<PrinterStatus> GetStatusAsync(Printer printer, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return PrinterStatus.Offline;
    }

    /// <inheritdoc />
    public Task<bool> IsAnyPrinterPrintingAsync(CancellationToken ct = default)
        => Task.FromResult(false);

    /// <inheritdoc />
    public async Task<PrinterAutoDetectResult> AutoDetectAsync(string ipAddress, PrinterProtocol protocol, CancellationToken ct = default)
    {
        var result = new PrinterAutoDetectResult();
        if (string.IsNullOrWhiteSpace(ipAddress))
            return result;

        try
        {
            switch (protocol)
            {
                case PrinterProtocol.KlipperMoonraker:
                    await DetectKlipperMoonrakerAsync(ipAddress, result, ct);
                    break;
                case PrinterProtocol.OctoPrint:
                    await DetectOctoPrintAsync(ipAddress, result, ct);
                    break;
                case PrinterProtocol.PrusaLink:
                    await DetectPrusaLinkAsync(ipAddress, result, ct);
                    break;
                // Marlin, BambuLab: keine unterstuetzte Auto-Detect API (best-effort = leer)
                default:
                    break;
            }
        }
        catch
        {
            // Best-effort — niemals crashen, leeres Result zurueckgeben
        }

        return result;
    }

    // === KlipperMoonraker ===
    // GET /printer/objects/query?configfile=bed_size + /printer/objects/query?toolhead
    private static async Task DetectKlipperMoonrakerAsync(string ip, PrinterAutoDetectResult result, CancellationToken ct)
    {
        var baseUri = $"http://{ip}";

        // 1. Bauvolumen aus configfile
        try
        {
            var cfgJson = await _httpClient.GetStringAsync($"{baseUri}/printer/objects/query?configfile=bed_size", ct);
            using var doc = JsonDocument.Parse(cfgJson);
            if (doc.RootElement.TryGetProperty("result", out var r)
                && r.TryGetProperty("status", out var status)
                && status.TryGetProperty("configfile", out var cfg)
                && cfg.TryGetProperty("settings", out var settings)
                && settings.TryGetProperty("bed_size", out var bedSize))
            {
                if (bedSize.ValueKind == JsonValueKind.Number)
                {
                    var v = bedSize.GetInt32();
                    result.BuildVolumeX = v;
                    result.BuildVolumeY = v;
                }
            }
        }
        catch { /* best-effort */ }

        // 2. Toolhead-Infos (max_temp etc. sind nicht direkt in toolhead, aber firmware_version steht in printer.info)
        try
        {
            var infoJson = await _httpClient.GetStringAsync($"{baseUri}/printer/info", ct);
            using var doc = JsonDocument.Parse(infoJson);
            if (doc.RootElement.TryGetProperty("result", out var r)
                && r.TryGetProperty("state_message", out _))
            {
                // Moonraker liefert keine direkten Temp/Duese-Werte via /printer/info.
                // Firmware-Version extrahieren falls vorhanden.
            }
            if (doc.RootElement.TryGetProperty("result", out r)
                && r.TryGetProperty("software_version", out var fw))
            {
                result.FirmwareVersion = fw.GetString();
            }
        }
        catch { /* best-effort */ }

        // 3. Bauvolumen Z aus configfile.settings.z_position (best-effort)
        try
        {
            var cfgJson = await _httpClient.GetStringAsync($"{baseUri}/printer/objects/query?configfile=settings", ct);
            using var doc = JsonDocument.Parse(cfgJson);
            if (doc.RootElement.TryGetProperty("result", out var r)
                && r.TryGetProperty("status", out var status)
                && status.TryGetProperty("configfile", out var cfg)
                && cfg.TryGetProperty("settings", out var settings))
            {
                // z_position_min / z_position_max existieren mancherorts
                if (settings.TryGetProperty("z_position_max", out var zMax) && zMax.ValueKind == JsonValueKind.Number)
                    result.BuildVolumeZ = zMax.GetInt32();
                // extruder.nozzle_diameter ist in settings.extruder verschachtelt
                if (settings.TryGetProperty("extruder", out var extr)
                    && extr.TryGetProperty("nozzle_diameter", out var nd)
                    && nd.ValueKind == JsonValueKind.Number)
                {
                    result.NozzleDiameter = (decimal)nd.GetDouble();
                }
                if (settings.TryGetProperty("extruder", out var extruder)
                    && extruder.TryGetProperty("max_temp", out var mt)
                    && mt.ValueKind == JsonValueKind.Number)
                {
                    result.MaxHotendTemp = mt.GetInt32();
                }
                if (settings.TryGetProperty("heater_bed", out var bed)
                    && bed.TryGetProperty("max_temp", out var bmt)
                    && bmt.ValueKind == JsonValueKind.Number)
                {
                    result.MaxBedTemp = bmt.GetInt32();
                }
            }
        }
        catch { /* best-effort */ }
    }

    // === OctoPrint ===
    // GET /api/settings → printer_profile.volume
    private static async Task DetectOctoPrintAsync(string ip, PrinterAutoDetectResult result, CancellationToken ct)
    {
        var baseUri = $"http://{ip}";

        try
        {
            var json = await _httpClient.GetStringAsync($"{baseUri}/api/settings", ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // printer_profile.volume: { depth, height, width, formFactor }
            if (root.TryGetProperty("printer", out var printer)
                && printer.TryGetProperty("volume", out var vol))
            {
                if (vol.TryGetProperty("width", out var w) && w.ValueKind == JsonValueKind.Number)
                    result.BuildVolumeX = (int)w.GetDouble();
                if (vol.TryGetProperty("depth", out var d) && d.ValueKind == JsonValueKind.Number)
                    result.BuildVolumeY = (int)d.GetDouble();
                if (vol.TryGetProperty("height", out var h) && h.ValueKind == JsonValueKind.Number)
                    result.BuildVolumeZ = (int)h.GetDouble();
            }

            // printer_profile.extruder.nozzleDiameter (falls vorhanden)
            if (root.TryGetProperty("printer", out var pr)
                && pr.TryGetProperty("extruder", out var ex)
                && ex.TryGetProperty("nozzleDiameter", out var nd)
                && nd.ValueKind == JsonValueKind.Number)
            {
                result.NozzleDiameter = (decimal)nd.GetDouble();
            }

            // Firmware-Version / OctoPrint-Version
            if (root.TryGetProperty("server", out var server)
                && server.TryGetProperty("version", out var ver))
            {
                result.FirmwareVersion = $"OctoPrint {ver.GetString()}";
            }
        }
        catch { /* best-effort */ }
    }

    // === PrusaLink ===
    // GET /api/v1/info
    private static async Task DetectPrusaLinkAsync(string ip, PrinterAutoDetectResult result, CancellationToken ct)
    {
        var baseUri = $"http://{ip}";

        try
        {
            var json = await _httpClient.GetStringAsync($"{baseUri}/api/v1/info", ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("printer", out var printer))
            {
                // printer.beddiameter / printer.x_axis, y_axis (best-effort)
                if (printer.TryGetProperty("x_axis", out var x) && x.ValueKind == JsonValueKind.Number)
                    result.BuildVolumeX = (int)x.GetDouble();
                if (printer.TryGetProperty("y_axis", out var y) && y.ValueKind == JsonValueKind.Number)
                    result.BuildVolumeY = (int)y.GetDouble();
                if (printer.TryGetProperty("z_axis", out var z) && z.ValueKind == JsonValueKind.Number)
                    result.BuildVolumeZ = (int)z.GetDouble();
            }

            if (root.TryGetProperty("firmware", out var fw))
                result.FirmwareVersion = fw.GetString();

            // nozzleDiameter nicht direkt in /api/v1/info, bleibt 0
        }
        catch { /* best-effort */ }
    }
}