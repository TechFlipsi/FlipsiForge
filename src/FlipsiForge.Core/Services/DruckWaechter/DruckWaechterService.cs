// SPDX-License-Identifier: GPL-3.0-or-later
// DruckWächter — Automation Engine: orchestriert Shelly + Moonraker
// für Auto-Aus, Nacht-Modus, Licht- und Filament-Steuerung, Kosten-Berechnung.
// Direkte Shelly-HTTP-API (kein HA) + Moonraker GCode-Macros (keine zweite Shelly für Licht).
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using FlipsiForge.Core.Models;
using FlipsiForge.Core.Services.Printing;

namespace FlipsiForge.Core.Services.DruckWaechter;

/// <summary>
/// Druck-Status-Erweiterung für den DruckWächter (um Offline/Heating/Cooling erweitert).
/// </summary>
public enum DruckWaechterPrinterState
{
    /// <summary>Drucker druckt gerade.</summary>
    Printing,
    /// <summary>Drucker ist bereit/idle.</summary>
    Idle,
    /// <summary>Druck ist pausiert.</summary>
    Paused,
    /// <summary>Drucker meldet Fehler.</summary>
    Error,
    /// <summary>Drucker/Shelly nicht erreichbar.</summary>
    Offline
}

/// <summary>
/// Zuständigkeits-Snapshot eines Druckers für die DruckWächter-UI.
/// Bündelt Status, Schalt-Zustand, Leistung, Temperaturen und Macro-Verfügbarkeit
/// in einem Call — die UI muss nicht mehrere Roundtrips machen.
/// </summary>
public readonly struct DruckWaechterStatus
{
    /// <summary>Drucker-Zustand (Printing/Idle/Paused/Error/Offline).</summary>
    public DruckWaechterPrinterState State { get; init; }

    /// <summary>True wenn der Shelly-Switch aktuell an ist (Strom am Drucker).</summary>
    public bool IsShellyOn { get; init; }

    /// <summary>Aktuelle Leistungsaufnahme in Watt (null wenn Shelly kein PM oder offline).</summary>
    public decimal? PowerW { get; init; }

    /// <summary>Alle Extruder mit Name + Ist-Temperatur in °C.</summary>
    public required List<(string Name, decimal Temp)> ExtruderTemps { get; init; }

    /// <summary>Alle Heizbetten mit Name + Ist-Temperatur in °C.</summary>
    public required List<(string Name, decimal Temp)> BedTemps { get; init; }

    /// <summary>True wenn ein Licht-Macro konfiguriert ist (Licht-Button sichtbar).</summary>
    public bool HasLightMacro { get; init; }

    /// <summary>True wenn ein Filament-Macro konfiguriert ist (Filament-Buttons sichtbar).</summary>
    public bool HasFilamentMacro { get; init; }

    /// <summary>Anzahl der Extruder (auto-detected via Moonraker /printer/objects/list).</summary>
    public int ExtruderCount { get; init; }
}

/// <summary>
/// Automation Engine für den DruckWächter — kapselt die Logik für
/// Auto-Aus, Nacht-Modus, Licht/Filament-Steuerung, Graceful Shutdown und Kosten-Berechnung.
/// </summary>
/// <remarks>
/// Konstruiert mit drei Abhängigkeiten:
/// 1. <see cref="HttpClient"/> für den direkten Shelly-Zugriff (via <see cref="ShellyClient"/>).
/// 2. <see cref="DruckWaechterConfig"/> mit globalen + pro-Drucker-Einstellungen.
/// 3. Eine Factory-Funktion die zu einer PrinterId eine <see cref="MoonrakerConnection"/> liefert
///    (vermeidet harte Abhängigkeit auf einen konkreten Connection-Manager in Core).
/// </remarks>
public sealed class DruckWaechterService
{
    private readonly HttpClient _http;
    private readonly DruckWaechterConfig _config;
    private readonly Func<int, MoonrakerConnection> _connectionFactory;
    private readonly ShellyClient _shelly;

    /// <summary>
    /// Erzeugt den DruckWächter-Service.
    /// </summary>
    /// <param name="http">HttpClient für Shelly-Aufrufe (via IHttpClientFactory).</param>
    /// <param name="config">DruckWächter-Konfiguration (global + Drucker-Liste).</param>
    /// <param name="connectionFactory">Factory die zu einer PrinterId die passende Moonraker-Verbindung liefert.</param>
    public DruckWaechterService(
        HttpClient http,
        DruckWaechterConfig config,
        Func<int, MoonrakerConnection> connectionFactory)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _shelly = new ShellyClient(http);
    }

    /// <summary>
    /// Holt einen Status-Snapshot für einen Drucker — kombiniert Shelly-Status,
    /// Moonraker-Status und Temperaturen in einem Call.
    /// </summary>
    /// <param name="printerId">Drucker-ID (wie in <see cref="DruckWaechterPrinterConfig.PrinterId"/>).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Vollständigen Status; State=Offline wenn Drucker nicht konfiguriert/unerreichbar.</returns>
    public async Task<DruckWaechterStatus> GetPrinterStatusAsync(int printerId, CancellationToken ct = default)
    {
        var cfg = FindPrinter(printerId);
        if (cfg is null)
        {
            return new DruckWaechterStatus
            {
                State = DruckWaechterPrinterState.Offline,
                ExtruderTemps = new(),
                BedTemps = new()
            };
        }

        // --- Shelly-Status (parallel-sicher, eigener HTTP-Stack) ---
        ShellySwitchStatus shellyStatus = default;
        if (!string.IsNullOrWhiteSpace(cfg.ShellyIp))
            shellyStatus = await _shelly.SwitchGetStatusAsync(cfg.ShellyIp, cfg.ShellySwitchId, ct).ConfigureAwait(false);

        // --- Moonraker-Status + Temperaturen ---
        DruckWaechterPrinterState state;
        List<(string, decimal)> extruderTemps = new();
        List<(string, decimal)> bedTemps = new();
        int extruderCount = 0;
        try
        {
            var conn = _connectionFactory(printerId);
            var printerStatus = await conn.GetStatusAsync().ConfigureAwait(false);
            state = MapStatus(printerStatus);

            var (exts, beds) = await GetAllTempsAsync(printerId, ct).ConfigureAwait(false);
            extruderTemps = exts;
            bedTemps = beds;
            extruderCount = exts.Count;
        }
        catch
        {
            state = DruckWaechterPrinterState.Offline;
        }

        return new DruckWaechterStatus
        {
            State = state,
            IsShellyOn = shellyStatus.On,
            PowerW = shellyStatus.PowerW,
            ExtruderTemps = extruderTemps,
            BedTemps = bedTemps,
            HasLightMacro = !string.IsNullOrWhiteSpace(cfg.LichtMacroAn) || !string.IsNullOrWhiteSpace(cfg.LichtMacroAus),
            HasFilamentMacro = !string.IsNullOrWhiteSpace(cfg.FilamentMacroLaden) || !string.IsNullOrWhiteSpace(cfg.FilamentMacroEntladen),
            ExtruderCount = extruderCount
        };
    }

    /// <summary>
    /// Schaltet den Shelly des Druckers ein oder aus.
    /// </summary>
    /// <param name="printerId">Drucker-ID.</param>
    /// <param name="on">true = an, false = aus.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>true wenn erfolgreich geschaltet; false wenn kein Shelly konfiguriert oder Call fehlschlägt.</returns>
    public async Task<bool> SetShellyAsync(int printerId, bool on, CancellationToken ct = default)
    {
        var cfg = FindPrinter(printerId);
        if (cfg is null || string.IsNullOrWhiteSpace(cfg.ShellyIp)) return false;
        return await _shelly.SwitchSetAsync(cfg.ShellyIp, cfg.ShellySwitchId, on, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Schaltet das Licht des Druckers über das konfigurierte Moonraker-Macro ein/aus.
    /// KEIN Shelly für Licht — ausschließlich Moonraker GCode-Macros (Sir's Anforderung, 19.07.2026).
    /// </summary>
    /// <param name="printerId">Drucker-ID.</param>
    /// <param name="on">true = Licht-Macro "An", false = Licht-Macro "Aus".</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>true wenn das Macro gesendet wurde; false wenn kein Macro konfiguriert oder Senden fehlschlägt.</returns>
    public async Task<bool> SetLightAsync(int printerId, bool on, CancellationToken ct = default)
    {
        var cfg = FindPrinter(printerId);
        if (cfg is null) return false;
        var macro = on ? cfg.LichtMacroAn : cfg.LichtMacroAus;
        if (string.IsNullOrWhiteSpace(macro)) return false;
        return await SendMoonrakerScriptAsync(printerId, macro, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Lädt Filament in den angegebenen Extruder via Moonraker-Macro.
    /// Multi-Extruder: das Macro erhält den T-Parameter (z.B. "LOAD_FILAMENT T1").
    /// </summary>
    /// <param name="printerId">Drucker-ID.</param>
    /// <param name="extruderIndex">0-basierter Extruder-Index (T0, T1, ...).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>true wenn das Macro gesendet wurde; false wenn kein Macro konfiguriert oder Senden fehlschlägt.</returns>
    public async Task<bool> LoadFilamentAsync(int printerId, int extruderIndex, CancellationToken ct = default)
    {
        var cfg = FindPrinter(printerId);
        if (cfg is null || string.IsNullOrWhiteSpace(cfg.FilamentMacroLaden)) return false;
        var script = BuildMacroWithTool(cfg.FilamentMacroLaden, extruderIndex);
        return await SendMoonrakerScriptAsync(printerId, script, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Entlädt Filament aus dem angegebenen Extruder via Moonraker-Macro.
    /// </summary>
    /// <param name="printerId">Drucker-ID.</param>
    /// <param name="extruderIndex">0-basierter Extruder-Index (T0, T1, ...).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>true wenn das Macro gesendet wurde; false sonst.</returns>
    public async Task<bool> UnloadFilamentAsync(int printerId, int extruderIndex, CancellationToken ct = default)
    {
        var cfg = FindPrinter(printerId);
        if (cfg is null || string.IsNullOrWhiteSpace(cfg.FilamentMacroEntladen)) return false;
        var script = BuildMacroWithTool(cfg.FilamentMacroEntladen, extruderIndex);
        return await SendMoonrakerScriptAsync(printerId, script, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Fährt den Drucker kontrolliert herunter: sendet Moonraker /server/shutdown,
    /// wartet die konfigurierte <see cref="DruckWaechterPrinterConfig.ShutdownDelaySek"/>
    /// und schaltet dann den Shelly aus.
    /// Falls der Drucker kein Shutdown unterstützt (ShutdownVerfuegbar=false) wird
    /// direkt der Shelly ausgeschaltet (Fallback).
    /// </summary>
    /// <param name="printerId">Drucker-ID.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>true wenn der Shelly am Ende ausgeschaltet wurde.</returns>
    public async Task<bool> ShutdownPrinterAsync(int printerId, CancellationToken ct = default)
    {
        var cfg = FindPrinter(printerId);
        if (cfg is null || string.IsNullOrWhiteSpace(cfg.ShellyIp)) return false;

        // Graceful Shutdown via Moonraker, falls verfügbar
        if (cfg.ShutdownVerfuegbar)
        {
            try
            {
                var conn = _connectionFactory(printerId);
                // Moonraker: POST /server/shutdown — Host fährt runter (Klipper + OS).
                // HttpPrinterConnectionBase.PostAsync liefert true bei 2xx.
                await InvokeMoonrakerPostAsync(conn, "server/shutdown", ct).ConfigureAwait(false);
            }
            catch
            {
                // Fällt durch auf Shelly-Aus — kein harter Abbruch.
            }

            // Warte bis der Host runtergefahren ist, dann Shelly aus.
            try
            {
                var delay = TimeSpan.FromSeconds(Math.Max(1, cfg.ShutdownDelaySek));
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancelled während Wartezeit — trotzdem Shelly aus.
            }
        }

        return await _shelly.SwitchSetAsync(cfg.ShellyIp, cfg.ShellySwitchId, false, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Prüft ob der Drucker aktuell im Nacht-Modus-Zeitfenster liegt und deshalb
    /// automatisch (ohne Nachfrage) heruntergefahren werden darf.
    /// </summary>
    /// <param name="printerId">Drucker-ID (nicht zwingend nötig da Nacht-Modus global ist, aber Signatur-konsistent).</param>
    /// <returns>true wenn Nacht-Modus aktiv UND aktuelle Zeit im Fenster liegt; false sonst.</returns>
    public bool ShouldAutoShutdown(int printerId)
    {
        _ = printerId; // Signatur-Konsistenz; Nacht-Modus ist global.
        var g = _config.Global;
        if (!g.NachtModusAktiv) return false;

        var now = TimeOnly.FromDateTime(DateTime.Now);
        return IsInTimeWindow(now, g.NachtModusVon, g.NachtModusBis);
    }

    /// <summary>
    /// Berechnet Strom- und Filament-Kosten eines Drucks.
    /// Stromkosten nur wenn der Shelly ein PM hat (kwhUsed > 0).
    /// Filamentkosten = (m / 330) × rollenpreis, 330m = Standard-Länge 1kg PLA @ 1.75mm.
    /// </summary>
    /// <param name="kwhUsed">Verbrauchte Energie in kWh (0 oder negativ = kein PM).</param>
    /// <param name="filamentMeters">Verbrauchtes Filament in Metern.</param>
    /// <param name="strompreis">Strompreis in €/kWh (override; default aus Config).</param>
    /// <param name="filamentPreis">Rollenpreis in € pro 1kg/330m (override; default aus Config).</param>
    /// <returns>(stromKosten, filKosten, gesamt) — drei decimal-Werte.</returns>
    public (decimal stromKosten, decimal filKosten, decimal gesamt) CalculateCosts(
        decimal kwhUsed, decimal filamentMeters, decimal? strompreis = null, decimal? filamentPreis = null)
    {
        var sp = strompreis ?? _config.Global.Strompreis;
        var fp = filamentPreis ?? _config.Global.FilamentPreis;

        decimal stromKosten = kwhUsed > 0 ? kwhUsed * sp : 0m;
        decimal filKosten = filamentMeters > 0 ? (filamentMeters / 330m) * fp : 0m;
        return (stromKosten, filKosten, stromKosten + filKosten);
    }

    /// <summary>
    /// Zählt die Anzahl der Extruder des Druckers via Moonraker /printer/objects/list.
    /// Extruder heißen "extruder", "extruder1", "extruder2", ...
    /// </summary>
    /// <param name="printerId">Drucker-ID.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Anzahl der Extruder (mindestens 1 wenn der Drucker erreichbar ist; 0 bei Offline).</returns>
    public async Task<int> GetExtruderCountAsync(int printerId, CancellationToken ct = default)
    {
        var objects = await GetMoonrakerObjectsListAsync(printerId, ct).ConfigureAwait(false);
        if (objects is null) return 0;
        return CountByPrefix(objects, "extruder");
    }

    /// <summary>
    /// Zählt die Anzahl der Heizbetten des Druckers via Moonraker /printer/objects/list.
    /// Betten heißen "heater_bed", "heater_bed1", "heater_bed2", ...
    /// </summary>
    /// <param name="printerId">Drucker-ID.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Anzahl der Betten; 0 bei Offline.</returns>
    public async Task<int> GetBedCountAsync(int printerId, CancellationToken ct = default)
    {
        var objects = await GetMoonrakerObjectsListAsync(printerId, ct).ConfigureAwait(false);
        if (objects is null) return 0;
        return CountByPrefix(objects, "heater_bed");
    }

    /// <summary>
    /// Holt alle Extruder- und Bett-Temperaturen des Druckers.
    /// Nutzt Moonraker /printer/objects/query mit dynamisch gebauter Liste
    /// der erkannten Extruder/Betten.
    /// </summary>
    /// <param name="printerId">Drucker-ID.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>
    /// Tupel aus (extruderListe, bettListe). Jeder Eintrag = (Objekt-Name, Ist-Temperatur).
    /// Leere Listen bei Offline/Fehler.
    /// </returns>
    public async Task<(List<(string Name, decimal Temp)> extruders, List<(string Name, decimal Temp)> beds)> GetAllTempsAsync(
        int printerId, CancellationToken ct = default)
    {
        var objects = await GetMoonrakerObjectsListAsync(printerId, ct).ConfigureAwait(false);
        if (objects is null)
            return (new(), new());

        var extruderNames = objects.Where(o => o.StartsWith("extruder", StringComparison.Ordinal)).ToList();
        var bedNames = objects.Where(o => o.StartsWith("heater_bed", StringComparison.Ordinal)).ToList();

        var extruders = await QueryTempsAsync(printerId, extruderNames, ct).ConfigureAwait(false);
        var beds = await QueryTempsAsync(printerId, bedNames, ct).ConfigureAwait(false);
        return (extruders, beds);
    }

    // =========================================================================
    //  private helpers
    // =========================================================================

    private DruckWaechterPrinterConfig? FindPrinter(int printerId) =>
        _config.Printers.FirstOrDefault(p => p.PrinterId == printerId);

    /// <summary>
    /// Mappt den FlipsiForge-PrinterStatus auf den DruckWächter-Status (ohne Heating/Cooling
    /// da der DruckWächter nur die fünf Zustände Printing/Idle/Paused/Error/Offline kennt).
    /// </summary>
    private static DruckWaechterPrinterState MapStatus(PrinterStatus s) => s switch
    {
        PrinterStatus.Printing => DruckWaechterPrinterState.Printing,
        PrinterStatus.Paused => DruckWaechterPrinterState.Paused,
        PrinterStatus.Error => DruckWaechterPrinterState.Error,
        PrinterStatus.Offline => DruckWaechterPrinterState.Offline,
        PrinterStatus.Heating => DruckWaechterPrinterState.Printing, // Aufheizen = Druck in Arbeit
        PrinterStatus.Cooling => DruckWaechterPrinterState.Idle,
        _ => DruckWaechterPrinterState.Idle
    };

    /// <summary>
    /// Holt die Liste der verfügbaren Moonraker-Objekte via /printer/objects/list.
    /// Liefert null bei Offline/Fehler.
    /// </summary>
    private async Task<List<string>?> GetMoonrakerObjectsListAsync(int printerId, CancellationToken ct)
    {
        try
        {
            var conn = _connectionFactory(printerId);
            // HttpPrinterConnectionBase.GetJsonAsync<T>() ist protected — wir rufen
            // die öffentliche Status-API auf und parsen die Liste über einen Hilfs-Call
            // der die gleiche Connection nutzt. Da MoonrakerConnection die Methode nicht
            // öffentlich exponiert, greifen wir per reflection-freiem Helper zu: wir
            // bauen einen dedizierten GET auf printer/objects/list via HttpClient.
            var baseUrl = ExtractBaseUrl(conn);
            if (baseUrl is null) return null;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            using var resp = await _http.GetAsync($"{baseUrl}/printer/objects/list", cts.Token).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            var json = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: ct).ConfigureAwait(false);
            if (json.ValueKind != JsonValueKind.Object) return null;
            if (!json.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object) return null;
            if (!result.TryGetProperty("objects", out var objs) || objs.ValueKind != JsonValueKind.Array) return null;

            var list = new List<string>(objs.GetArrayLength());
            foreach (var item in objs.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var name = item.GetString();
                    if (!string.IsNullOrEmpty(name)) list.Add(name);
                }
            }
            return list;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fragt Temperaturen für eine Liste von Moonraker-Objekten ab
    /// (/printer/objects/query?extruder&extruder1&heater_bed&...).
    /// </summary>
    private async Task<List<(string Name, decimal Temp)>> QueryTempsAsync(
        int printerId, List<string> objectNames, CancellationToken ct)
    {
        if (objectNames.Count == 0) return new();

        try
        {
            var conn = _connectionFactory(printerId);
            var baseUrl = ExtractBaseUrl(conn);
            if (baseUrl is null) return new();

            var query = string.Join("&", objectNames);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            using var resp = await _http.GetAsync($"{baseUrl}/printer/objects/query?{query}", cts.Token).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return new();
            using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            var json = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: ct).ConfigureAwait(false);
            if (json.ValueKind != JsonValueKind.Object) return new();
            if (!json.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object) return new();
            if (!result.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.Object) return new();

            var temps = new List<(string, decimal)>(objectNames.Count);
            foreach (var name in objectNames)
            {
                if (status.TryGetProperty(name, out var obj) &&
                    obj.TryGetProperty("temperature", out var t) &&
                    t.ValueKind == JsonValueKind.Number)
                {
                    temps.Add((name, t.GetDecimal()));
                }
                else
                {
                    temps.Add((name, 0m));
                }
            }
            return temps;
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// Sendet ein GCode-Script an Moonraker (POST /printer/gcode/script?script=...).
    /// Wird für Licht- und Filament-Macros verwendet.
    /// </summary>
    private async Task<bool> SendMoonrakerScriptAsync(int printerId, string script, CancellationToken ct)
    {
        try
        {
            var conn = _connectionFactory(printerId);
            var baseUrl = ExtractBaseUrl(conn);
            if (baseUrl is null) return false;

            var escaped = Uri.EscapeDataString(script);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            using var resp = await _http.PostAsync($"{baseUrl}/printer/gcode/script?script={escaped}", content: null, cts.Token).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Führt einen POST auf Moonraker ohne Body aus (z.B. /server/shutdown).
    /// Nutzt die Basisklassen-PostAsync-Logik per direktem HttpClient-Call,
    /// da PostAsync protected ist.
    /// </summary>
    private async Task<bool> InvokeMoonrakerPostAsync(MoonrakerConnection conn, string relativePath, CancellationToken ct)
    {
        var baseUrl = ExtractBaseUrl(conn);
        if (baseUrl is null) return false;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            using var resp = await _http.PostAsync($"{baseUrl}/{relativePath.TrimStart('/')}", content: null, cts.Token).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extrahiert die BaseUrl aus einer HttpPrinterConnectionBase-Instanz per
    /// Reflection. Die Basisklasse hält BaseUrl als protected readonly field —
    /// wir greifen lesend zu (kein Setzen, kein State-Mutation).
    /// Alternative wäre eine öffentliche Eigenschaft in der Basisklasse, aber das
    /// wäre eine API-Änderung am bestehenden Code — Reflection ist für ein
    /// reines readonly-Feld das kleinere Übel.
    /// </summary>
    private static string? ExtractBaseUrl(MoonrakerConnection conn)
    {
        try
        {
            var baseUrlField = typeof(HttpPrinterConnectionBase)
                .GetField("BaseUrl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return baseUrlField?.GetValue(conn) as string;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Zählt wie viele Einträge in der Objekt-Liste mit dem gegebenen Prefix beginnen.
    /// "extruder" matcht "extruder" und "extruder1", aber NICHT "extruder_stepper".
    /// Die Moonraker-Namenskonvention ist: erster Kanal = prefix ohne Zahl,
    /// weitere = prefix + N (N >= 1). Wir akzeptieren beide Formen.
    /// </summary>
    private static int CountByPrefix(List<string> objects, string prefix)
    {
        int count = 0;
        foreach (var name in objects)
        {
            if (name == prefix) { count++; continue; }
            // prefix + Zahl (z.B. "extruder1", "heater_bed1")
            if (name.Length > prefix.Length &&
                name.StartsWith(prefix, StringComparison.Ordinal) &&
                char.IsDigit(name[prefix.Length]))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Baut den Macro-Aufruf mit T-Parameter für Multi-Extruder.
    /// "LOAD_FILAMENT" + extruderIndex 1 → "LOAD_FILAMENT T1".
    /// Bei Index 0 wird "T0" gesendet (Klipper-konform — nicht weglassen,
    /// weil das Macro T-Parameter erwartet).
    /// </summary>
    private static string BuildMacroWithTool(string macro, int extruderIndex)
    {
        var trimmed = macro.Trim();
        return $"{trimmed} T{extruderIndex.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Prüft ob eine TimeOnly-Zeit in einem Zeitfenster liegt (mit Mitternacht-Überlauf).
    /// Beispiel: 02:30 in [00:00, 06:00] → true. 23:30 in [22:00, 02:00] → true.
    /// </summary>
    private static bool IsInTimeWindow(TimeOnly now, TimeOnly from, TimeOnly to)
    {
        if (from == to) return false;
        if (from < to)
        {
            // Normales Fenster: 06:00–22:00
            return now >= from && now < to;
        }
        // Über-Mitternacht-Fenster: 22:00–06:00
        return now >= from || now < to;
    }
}