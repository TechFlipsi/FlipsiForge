// SPDX-License-Identifier: GPL-3.0-or-later
// Desktop-seitige Settings-Klasse: Hält ALLE UI-Einstellungen für die
// SettingsView. Persistiert als JSON-Datei im LocalApplicationData-Ordner.
// Überlappende Felder (AiEnabled, AiModel, ServerMode, ...) werden zusätzlich
// in die Core.Models.AppSettings (DB) geschrieben, damit Core/Server
// synchron laufen. Damit bleibt das Desktop-Projekt unabhängig davon,
// ob andere Core-Services schon fertig sind.
using System.Text.Json;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Desktop.Services;

// BotFrequency, BotPosition, NotificationKind, FileSortMode, ViewMode, DateFormat
// kommen aus FlipsiForge.Core.Models — keine Desktop-Duplikate mehr (v0.2.0).

/// <summary>Ansicht für Datei-Manager.</summary>
public enum FileViewMode
{
    List,
    Grid
}

/// <summary>Temperatur-Einheit.</summary>
public enum TempUnit
{
    Celsius,
    Fahrenheit
}

/// <summary>Alle Desktop-UI-Einstellungen. Persistiert als JSON-Datei.</summary>
public sealed class DesktopSettings
{
    // === 1. Allgemein ===
    public string Language { get; set; } = "de";
    public string Theme { get; set; } = "Dark Void + Ember";
    public string StartTab { get; set; } = "Datei-Manager";
    public string Currency { get; set; } = "EUR";
    public string DateFormat { get; set; } = "dd.MM.yyyy";
    public bool MinimizeToTray { get; set; } = false;

    // === 2. Datei-Manager ===
    public List<string> ScanFolders { get; set; } = new();
    public bool ScanStl { get; set; } = true;
    public bool Scan3mf { get; set; } = true;
    public bool ScanGcode { get; set; } = true;
    public bool ScanObj { get; set; } = true;
    public bool AutoScan { get; set; } = true;
    public int ScanIntervalMinutes { get; set; } = 30;
    public FileViewMode DefaultView { get; set; } = FileViewMode.List;
    public FileSortMode DefaultSort { get; set; } = FileSortMode.NameAsc;
    public bool ShowThumbnails { get; set; } = true;
    public bool DetectDuplicates { get; set; } = true;

    // === 3. Drucker ===
    public int? DefaultPrinterId { get; set; }
    public bool PrinterAutoConnect { get; set; } = true;
    public bool RequirePrintConfirmation { get; set; } = true;
    public TempUnit TemperatureUnit { get; set; } = TempUnit.Celsius;
    public int WebcamRefreshSeconds { get; set; } = 5;

    // === 4. Filament ===
    public bool FilamentCostTracking { get; set; } = true;
    public int DryingLogRetentionDays { get; set; } = 90;
    public bool EnableQrNfc { get; set; } = true;

    // === 5. KI-Assistent ===
    public bool AiEnabled { get; set; } = true;
    public AiModelChoice AiModel { get; set; } = AiModelChoice.Auto;
    public bool AiSearchEnabled { get; set; } = true;
    public bool AiMaintenanceSuggestions { get; set; } = true;
    public bool AiStreaming { get; set; } = true;
    public string? OpenAiApiKey { get; set; }
    public string? AnthropicApiKey { get; set; }
    public string? OllamaUrl { get; set; }
    public string? ModelDownloadPath { get; set; }

    // === 6. Forge-Bot ===
    public bool BotEnabled { get; set; } = true;
    public BotFrequency BotFrequency { get; set; } = BotFrequency.Normal;
    public string BotDndStart { get; set; } = "22:00";
    public string BotDndEnd { get; set; } = "07:00";
    public BotPosition BotPosition { get; set; } = BotPosition.BottomRight;
    public string BotLanguage { get; set; } = "de";

    // === 7. Server & Netzwerk ===
    public ServerMode ServerMode { get; set; } = ServerMode.Full;
    public int ServerPort { get; set; } = 5000;
    public bool WebUiEnabled { get; set; } = true;
    public string ApiKey { get; set; } = "";
    public bool UseHttps { get; set; } = false;

    // === 8. Benachrichtigungen ===
    public bool NotifyPushEnabled { get; set; } = true;
    public bool NotifyPrintFinished { get; set; } = true;
    public bool NotifyPrintFailed { get; set; } = true;
    public int FilamentWarningPercent { get; set; } = 15;
    public bool NotifyMaintenanceReminder { get; set; } = true;
    public NotificationKind NotificationKind { get; set; } = NotificationKind.System;

    // === 9. Erweitert ===
    public int BackupIntervalDays { get; set; } = 7;
    public string BackupPath { get; set; } = "";
    public string LogLevel { get; set; } = "Info";
    public bool DebugMode { get; set; } = false;

    // === 10. DruckWächter ===
    /// <summary>Strompreis pro kWh in Euro.</summary>
    public decimal DwStrompreis { get; set; } = 0.15m;
    /// <summary>Filamentpreis pro 1kg Rolle (330m).</summary>
    public decimal DwFilamentPreis { get; set; } = 20.0m;
    /// <summary>Auto-Aus Timer in Minuten (nach Abkühlung, wenn kein Telegram).</summary>
    public int DwAutoAusTimerMinuten { get; set; } = 15;
    /// <summary>Abkühl-Schwelle in °C — alle Extruder müssen darunter.</summary>
    public int DwAbkuehlSchwelleC { get; set; } = 50;
    /// <summary>Nacht-Modus aktiv (Auto-Aus ohne Nachfrage).</summary>
    public bool DwNachtModusAktiv { get; set; } = true;
    /// <summary>Nacht-Modus Start (HH:mm).</summary>
    public string DwNachtModusVon { get; set; } = "00:00";
    /// <summary>Nacht-Modus Ende (HH:mm).</summary>
    public string DwNachtModusBis { get; set; } = "06:00";
    /// <summary>Telegram für DruckWächter-Benachrichtigungen aktiv.</summary>
    public bool DwTelegramAktiv { get; set; } = false;
    /// <summary>Telegram Bot Token.</summary>
    public string? DwTelegramBotToken { get; set; }
    /// <summary>Telegram Chat ID.</summary>
    public long? DwTelegramChatId { get; set; }

    // === Meta ===
    public DateTime LastSaved { get; set; } = DateTime.UtcNow;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    /// <summary>Liefert den Pfad der Settings-JSON.</summary>
    public static string GetFilePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlipsiForge");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "desktop_settings.json");
    }

    /// <summary>Lädt die Settings aus der JSON-Datei oder liefert Defaults.</summary>
    public static DesktopSettings Load()
    {
        var path = GetFilePath();
        if (!File.Exists(path))
            return new DesktopSettings();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<DesktopSettings>(json, JsonOpts) ?? new DesktopSettings();
        }
        catch
        {
            return new DesktopSettings();
        }
    }

    /// <summary>Speichert die Settings in JSON und synced überlappende Felder in die DB.</summary>
    public void Save()
    {
        LastSaved = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(this, JsonOpts);
        File.WriteAllText(GetFilePath(), json);

        // Sync in die DB-AppSettings (best-effort)
        try
        {
            using var db = new FlipsiForgeDbContext();
            var app = db.Settings.FirstOrDefault();
            if (app != null)
            {
                app.AiEnabled = AiEnabled;
                app.AiModel = AiModel;
                app.ServerMode = ServerMode;
                app.WebUiEnabled = WebUiEnabled;
                app.Language = Language;
                app.WatchFolders = ScanFolders;
                db.SaveChanges();
            }
        }
        catch
        {
            // Best-effort sync; Fehler nicht fatal für Desktop
        }
    }

    /// <summary>Setzt alle Felder auf Defaults zurück.</summary>
    public static DesktopSettings CreateDefaults() => new();

    /// <summary>Generiert einen neuen zufälligen API-Key (32 Zeichen hex).</summary>
    public static string GenerateApiKey()
    {
        var bytes = new byte[16];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}