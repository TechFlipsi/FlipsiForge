using System.Net.Http;
using System.Text.Json;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Core.Services.Maintenance;

/// <summary>
/// Wartungs-Empfehlungen für Drucker.
/// Online-Modus: modellspezifisch (Brand + Model) — braucht Internet (Stub reicht).
/// Offline-Modus: allgemeine Empfehlungen für alle Drucker.
/// </summary>
public sealed class MaintenanceAdvisor
{
    private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(8) };
    private const string OfflineApiUrl = "https://api.flipsiforge.tech/maintenance/v1/recommendations";

    /// <summary>
    /// Liefert Wartungs-Empfehlungen für einen Drucker.
    /// </summary>
    /// <param name="printer">Drucker-Profil.</param>
    /// <param name="onlineMode">Wenn true, versuche modellspezifische Empfehlungen via HTTP.
    /// Wenn der HTTP-Call fehlschlägt, fällt die Methode automatisch auf Offline-Empfehlungen zurück.</param>
    /// <returns>Liste der Empfehlungen.</returns>
    public async Task<List<MaintenanceRecommendation>> GetRecommendationsAsync(
        Printer printer, bool onlineMode)
    {
        if (onlineMode)
        {
            var online = await TryGetOnlineRecommendationsAsync(printer).ConfigureAwait(false);
            if (online.Count > 0)
                return online;
            // Fallback auf Offline wenn Online fehlschlägt
        }

        return GetOfflineRecommendations(printer);
    }

    /// <summary>
    /// Statische Liste allgemeiner Wartungs-Empfehlungen für alle Drucker.
    /// </summary>
    public static List<MaintenanceRecommendation> GetOfflineRecommendations(Printer printer)
    {
        var list = new List<MaintenanceRecommendation>
        {
            new()
            {
                Component = "Nozzle",
                Action = "Düse reinigen oder ersetzen (Bronze/Stahl bei abrasivem Filament)",
                IntervalHours = 200,
                ModelSpecificNote = GetNozzleNote(printer),
                OfflineFallback = true
            },
            new()
            {
                Component = "Druckbett",
                Action = "Druckbett reinigen (Isopropanol) und neu kalibrieren",
                IntervalHours = 50,
                ModelSpecificNote = printer.IsEnclosed ? "Enclosed: PEI-Sheet auf Beschädigung prüfen" : null,
                OfflineFallback = true
            },
            new()
            {
                Component = "Zahnriemen",
                Action = "Riemen-Spannung prüfen (X/Y-Achse) und bei Bedarf nachspannen",
                IntervalHours = 500,
                ModelSpecificNote = null,
                OfflineFallback = true
            },
            new()
            {
                Component = "PTFE-Tube",
                Action = "Bowden-Tube inspizieren (nur Bowden-Drucker, nicht bei Direct-Drive)",
                IntervalHours = 1000,
                ModelSpecificNote = printer.IsDirectDrive ? "Direct-Drive: kein PTFE-Tube, überspringen" : null,
                OfflineFallback = true
            },
            new()
            {
                Component = "Lüfter",
                Action = "Hotend-Fan + Part-Cooling-Fan prüfen (Drehzahl, Geräusch, Staub)",
                IntervalHours = 300,
                ModelSpecificNote = null,
                OfflineFallback = true
            },
            new()
            {
                Component = "Extruder-Zahnrad",
                Action = "Extruder-Gear reinigen (Filament-Staub) und Filament-Pfad prüfen",
                IntervalHours = 400,
                ModelSpecificNote = printer.IsDirectDrive ? "Direct-Drive: Extruder-Gear häufiger prüfen" : null,
                OfflineFallback = true
            },
            new()
            {
                Component = "Firmware",
                Action = "Firmware-Update prüfen (90 Tage Intervall)",
                IntervalHours = 2160, // 90 Tage * 24h
                ModelSpecificNote = GetFirmwareNote(printer),
                OfflineFallback = true
            }
        };

        return list;
    }

    /// <summary>
    /// Versucht modellspezifische Empfehlungen via Online-API zu laden.
    /// Gibt bei Netzwerkfehler eine leere Liste zurück (Caller fällt auf Offline zurück).
    /// </summary>
    private async Task<List<MaintenanceRecommendation>> TryGetOnlineRecommendationsAsync(Printer printer)
    {
        try
        {
            var brand = Uri.EscapeDataString(printer.Brand ?? "");
            var model = Uri.EscapeDataString(printer.Model ?? "");
            if (string.IsNullOrEmpty(brand) || string.IsNullOrEmpty(model))
                return new List<MaintenanceRecommendation>();

            var url = $"{OfflineApiUrl}?brand={brand}&model={model}";
            using var resp = await SharedHttp.GetAsync(url).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return new List<MaintenanceRecommendation>();

            using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var parsed = await JsonSerializer.DeserializeAsync<List<MaintenanceRecommendation>>(stream)
                .ConfigureAwait(false);
            return parsed ?? new List<MaintenanceRecommendation>();
        }
        catch
        {
            // Offline / DNS / Network-Fehler → leer
            return new List<MaintenanceRecommendation>();
        }
    }

    /// <summary>Modellspezifische Nozzle-Note basierend auf Brand.</summary>
    private static string? GetNozzleNote(Printer p) => p.Brand?.ToLowerInvariant() switch
    {
        "bambu lab" => "Bambu: Hotend-Einheit tauschen statt nur Nozzle (bei X1C/A1)",
        "prusa" => "Prusa MK4: Nozzle mit Hotend-Sockel zusammen tauschen",
        "creality" => "Creality Ender: Nozzle regelmäßig nachziehen (Gefahr Leck)",
        _ => null
    };

    /// <summary>Modellspezifische Firmware-Note.</summary>
    private static string? GetFirmwareNote(Printer p) => p.Protocol switch
    {
        PrinterProtocol.KlipperMoonraker => "Klipper: git pull + Klipper-Menü → Firmware restart",
        PrinterProtocol.BambuLab => "Bambu: OTA-Update via Bambu-Studio / Web-UI",
        PrinterProtocol.PrusaLink => "Prusa: PrusaLink-Web-UI → Settings → Firmware",
        PrinterProtocol.OctoPrint => "OctoPrint: OctoPrint Updater + Drucker-Firmware separat",
        _ => null
    };
}