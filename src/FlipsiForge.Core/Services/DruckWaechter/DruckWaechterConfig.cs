// SPDX-License-Identifier: GPL-3.0-or-later
// DruckWächter — Konfigurations-Modelle (global + pro Drucker).
// Auto-Detect: has_power_meter, shutdown_verfuegbar, licht_macros,
// filaments_macros, extruder_anzahl werden NICHT in der Config gespeichert —
// sie werden zur Laufzeit via Shelly/Moonraker-API erkannt (Sir's Anforderung:
// "so viel wie möglich AUTO-DETECTEN, nicht vom User einstellbar machen").

namespace FlipsiForge.Core.Services.DruckWaechter;

/// <summary>
/// Globale DruckWächter-Einstellungen — gelten für ALLE Drucker gleich.
/// Werte die pro Drucker unterschiedlich sein können gehören in
/// <see cref="DruckWaechterPrinterConfig"/>.
/// </summary>
public sealed class DruckWaechterGlobalConfig
{
    /// <summary>Strompreis in € pro kWh (für Stromkosten-Berechnung, nur relevant bei Shelly mit PM).</summary>
    public decimal Strompreis { get; set; } = 0.15m;

    /// <summary>
    /// Filament-Preis in € pro 1kg/330m-Rolle (Standard-Länge einer 1kg PLA-Spule bei 1.75mm).
    /// Wird genutzt um filamentkosten = (m / 330) × preis zu berechnen.
    /// </summary>
    public decimal FilamentPreis { get; set; } = 20.0m;

    /// <summary>
    /// Auto-Aus-Timer in Minuten: wenn ein Druck fertig ist und alle Köpfe abgekühlt sind
    /// und KEIN Telegram eingerichtet ist, wird der Drucker nach dieser Zeit automatisch
    /// ausgeschaltet — sofern kein neuer Druck gestartet wird (Default 15).
    /// </summary>
    public int AutoAusTimerMinuten { get; set; } = 15;

    /// <summary>
    /// Abkühl-Schwelle in °C: alle Extruder müssen unter diesem Wert sinken,
    /// bevor der Drucker ausgeschaltet wird. Verhindert thermischen Schock
    /// und gibt dem Hotend Zeit zum sicheren Abkühlen (Default 50).
    /// </summary>
    public int AbkuehlSchwelleC { get; set; } = 50;

    /// <summary>True wenn der Nacht-Modus aktiv ist (Drucker wird im Nacht-Zeitfenster ohne Nachfrage ausgeschaltet).</summary>
    public bool NachtModusAktiv { get; set; } = true;

    /// <summary>Start des Nacht-Modus (Default 00:00).</summary>
    public TimeOnly NachtModusVon { get; set; } = new(0, 0);

    /// <summary>Ende des Nacht-Modus (Default 06:00).</summary>
    public TimeOnly NachtModusBis { get; set; } = new(6, 0);

    /// <summary>True wenn Telegram-Benachrichtigungen aktiv sind.</summary>
    public bool TelegramAktiv { get; set; }

    /// <summary>Telegram Bot-Token (nur nötig wenn TelegramAktiv = true).</summary>
    public string? TelegramBotToken { get; set; }

    /// <summary>Telegram Chat-ID an die Nachrichten gesendet werden (nur nötig wenn TelegramAktiv = true).</summary>
    public long? TelegramChatId { get; set; }
}

/// <summary>
/// DruckWächter-Konfiguration für einen einzelnen Drucker.
/// Jeder Drucker bekommt seinen EIGENEN Shelly (Sir's Anforderung, 19.07.2026).
/// </summary>
public sealed class DruckWaechterPrinterConfig
{
    /// <summary>ID des Druckers (aus der FlipsiForge Printer-Tabelle).</summary>
    public int PrinterId { get; set; }

    /// <summary>IP-Adresse des Shelly-Geräts das diesen Drucker schaltet (z.B. "192.168.178.60"). Null = kein Shelly zugewiesen.</summary>
    public string? ShellyIp { get; set; }

    /// <summary>Shelly Switch-Kanal-ID (Shelly Plus 1PM = 0, default 0).</summary>
    public int ShellySwitchId { get; set; } = 0;

    /// <summary>True wenn dieser Drucker ein Graceful Shutdown via Moonraker /server/shutdown unterstützt. Wird i.d.R. auto-detected, kann aber manuell überschrieben werden (Default true).</summary>
    public bool ShutdownVerfuegbar { get; set; } = true;

    /// <summary>Verzögerung in Sekunden zwischen Moonraker-Shutdown und Shelly-Aus. Gibt dem Host Zeit zum Runterfahren (Default 60).</summary>
    public int ShutdownDelaySek { get; set; } = 60;

    /// <summary>Name des Moonraker GCode-Macros um das Licht einzuschalten (z.B. "FLASHLIGHT_ON"). Null/leer = kein Licht-Macro.</summary>
    public string? LichtMacroAn { get; set; }

    /// <summary>Name des Moonraker GCode-Macros um das Licht auszuschalten (z.B. "FLASHLIGHT_OFF"). Null/leer = kein Licht-Macro.</summary>
    public string? LichtMacroAus { get; set; }

    /// <summary>Name des Moonraker GCode-Macros um Filament zu laden (z.B. "LOAD_FILAMENT"). Null/leer = kein Filament-Macro.</summary>
    public string? FilamentMacroLaden { get; set; }

    /// <summary>Name des Moonraker GCode-Macros um Filament zu entladen (z.B. "UNLOAD_FILAMENT"). Null/leer = kein Filament-Macro.</summary>
    public string? FilamentMacroEntladen { get; set; }
}

/// <summary>
/// Komplette DruckWächter-Konfiguration (global + Liste der Drucker).
/// </summary>
public sealed class DruckWaechterConfig
{
    /// <summary>Globale Einstellungen die für alle Drucker gelten.</summary>
    public DruckWaechterGlobalConfig Global { get; set; } = new();

    /// <summary>Pro-Drucker-Konfiguration (jeder Eintrag = ein Drucker mit eigenem Shelly).</summary>
    public List<DruckWaechterPrinterConfig> Printers { get; set; } = new();
}