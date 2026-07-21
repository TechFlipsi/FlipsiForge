namespace FlipsiForge.Core.Models;

/// <summary>Ein 3D-Drucker-Profil des Users.</summary>
public class Printer
{
    public int Id { get; set; }
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public string? FirmwareVersion { get; set; }
    public PrinterProtocol Protocol { get; set; }
    public string? IpAddress { get; set; }
    public string? UsbPort { get; set; }
    public decimal NozzleDiameter { get; set; } = 0.4m;
    public int BuildVolumeX { get; set; }
    public int BuildVolumeY { get; set; }
    public int BuildVolumeZ { get; set; }
    public bool IsEnclosed { get; set; }
    public bool IsDirectDrive { get; set; }
    public int MaxHotendTemp { get; set; } = 300;
    public int MaxBedTemp { get; set; } = 120;
    public bool IsActive { get; set; } = true;
    public DateTime? LastMaintenanceDate { get; set; }
    public decimal TotalPrintHours { get; set; }
    public string? Notes { get; set; }

    // === DruckWächter / Shelly Integration ===
    /// <summary>IP-Adresse des Shelly-Geräts das diesen Drucker schaltet (z.B. "192.168.178.60"). Null = kein Shelly zugewiesen.</summary>
    public string? ShellyIp { get; set; }
    /// <summary>Shelly Switch-Kanal-ID (Shelly Plus 1PM = 0, default 0).</summary>
    public int ShellySwitchId { get; set; } = 0;
}

/// <summary>Eine Filament-Spule im Inventar.</summary>
public class Spool
{
    public int Id { get; set; }
    public string Brand { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public MaterialType MaterialType { get; set; }
    public string ColorHex { get; set; } = "#000000";
    public string? ColorName { get; set; }
    public decimal DiameterMm { get; set; } = 1.75m;
    public decimal TotalWeightG { get; set; } = 1000;
    public decimal RemainingWeightG { get; set; } = 1000;
    public decimal DensityGcm3 { get; set; } = 1.24m;
    public decimal CostEur { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public SpoolStatus Status { get; set; } = SpoolStatus.Active;
    public string? QrCode { get; set; }
    public string? NfcTag { get; set; }
    public string? Notes { get; set; }

    // === v0.5.0 Erweiterungen: Druck-Einstellungen (Auto-Fill aus FilamentBrandSpec) ===
    /// <summary>Empfohlene Drucktemperatur in °C (Auto-Fill aus Marken-Profil, editierbar).</summary>
    public int RecommendedHotendTemp { get; set; }
    /// <summary>Empfohlene Bett-Temperatur in °C (Auto-Fill aus Marken-Profil, editierbar).</summary>
    public int RecommendedBedTemp { get; set; }
}

/// <summary>Gescannte 3D-Druck-Datei.</summary>
public class ScannedFile
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public string Path { get; set; } = "";
    public string Extension { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
    public int? TriangleCount { get; set; }
    public decimal? BoundingBoxX { get; set; }
    public decimal? BoundingBoxY { get; set; }
    public decimal? BoundingBoxZ { get; set; }
    public float[]? Embedding { get; set; }  // Für KI-Suche (gecacht)
    public string? ContentHash { get; set; }  // v0.2.0 — für Duplikat-Erkennung
    public int AccessCount { get; set; }       // v0.2.0 — Häufigkeit
}

/// <summary>Ein Druck-Job in der Queue.</summary>
public class PrintJob
{
    public int Id { get; set; }
    public int PrinterId { get; set; }
    public int? SpoolId { get; set; }
    public int FileId { get; set; }
    public string GcodePath { get; set; } = "";
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Queued;
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public decimal? EstimatedDurationMin { get; set; }
    public decimal? EstimatedFilamentG { get; set; }
    public decimal? EstimatedCostEur { get; set; }
    public bool RequiresConfirmation { get; set; } = true;
}

public enum PrintJobStatus
{
    Queued,
    Confirmed,
    Printing,
    Completed,
    Failed,
    Cancelled
}

/// <summary>Druck-Historie-Eintrag.</summary>
public class PrintHistory
{
    public int Id { get; set; }
    public int PrinterId { get; set; }
    public int? SpoolId { get; set; }
    public string FileName { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public bool Success { get; set; }
    public decimal? DurationHours { get; set; }
    public decimal? FilamentUsedG { get; set; }
    public decimal? CostEur { get; set; }
    public string? FailureReason { get; set; }
}

/// <summary>Wartungs-Eintrag.</summary>
public class MaintenanceRecord
{
    public int Id { get; set; }
    public int PrinterId { get; set; }
    public string Component { get; set; } = "";
    public string Action { get; set; } = "";
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public decimal? PrintHoursAtMaintenance { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Chat-Verlauf mit KI-Assistent.</summary>
public class ChatMessage
{
    public int Id { get; set; }
    public string Role { get; set; } = "user";  // user, assistant, system
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// === v0.2.0 Erweiterungen ===

/// <summary>Markiert eine gescannte Datei als Favorit.</summary>
public class FavoriteFile
{
    public int Id { get; set; }
    public int ScannedFileId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Protokolliert den Zugriff auf gescannte Dateien (für Häufigkeit-Sortierung).</summary>
public class FileUsageLog
{
    public int Id { get; set; }
    public int ScannedFileId { get; set; }
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    public string? Action { get; set; }
}

/// <summary>Wartungs-Empfehlung (online modellspezifisch oder offline generisch).</summary>
public class MaintenanceRecommendation
{
    public string Component { get; set; } = "";
    public string Action { get; set; } = "";
    public int IntervalHours { get; set; }
    public string? ModelSpecificNote { get; set; }
    public bool OfflineFallback { get; set; }
}

/// <summary>Generiertes Slicer-Profil (KI-gestützt oder Template-basiert).</summary>
public class SlicerProfile
{
    public string Name { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Forge-Bot Historie — gezeigte Bot-Nachrichten.</summary>
public class BotMessage
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public DateTime ShownAt { get; set; } = DateTime.UtcNow;
}

/// <summary>App-Einstellungen (Singleton, ID=1).</summary>
public class AppSettings
{
    public int Id { get; set; } = 1;

    // --- KI (v0.1.0) ---
    public AiModelChoice AiModel { get; set; } = AiModelChoice.Auto;
    public bool AiEnabled { get; set; } = true;

    // --- Server / Allgemein (v0.1.0) ---
    public ServerMode ServerMode { get; set; } = ServerMode.Full;
    public bool WebUiEnabled { get; set; } = true;
    public string? Language { get; set; } = "de";
    public string? TelegramBotToken { get; set; }
    public long? TelegramChatId { get; set; }
    public string? NextcloudUrl { get; set; }
    public string? NextcloudUser { get; set; }
    public string? NextcloudPassword { get; set; }
    public List<string> WatchFolders { get; set; } = new();

    // === v0.2.0 Erweiterungen ===

    // --- Allgemein ---
    /// <summary>Tab der beim Start angezeigt wird.</summary>
    public string StartTab { get; set; } = "FileManager";
    /// <summary>Währung für Kosten-Anzeige.</summary>
    public string Currency { get; set; } = "EUR";
    /// <summary>Datumsformat-Präferenz.</summary>
    public DateFormat DateFormat { get; set; } = DateFormat.EU;
    /// <summary>Ob das App in den Tray minimiert wird.</summary>
    public bool MinimizeToTray { get; set; }

    // --- Datei-Scan ---
    /// <summary>Ordner die nach 3D-Druck-Dateien gescannt werden.</summary>
    public List<string> ScanFolders { get; set; } = new();
    /// <summary>Dateiformate die beim Scan berücksichtigt werden.</summary>
    public List<string> ScanFormats { get; set; } = new() { "STL", "3MF", "GCODE", "OBJ", "PLY", "STEP", "AMF", "X3D" };
    /// <summary>Auto-Scan beim App-Start.</summary>
    public bool AutoScanOnStart { get; set; }
    /// <summary>Intervall für wiederholten Auto-Scan (Minuten).</summary>
    public int ScanIntervalMinutes { get; set; } = 30;
    /// <summary>Default-Anzeige-Modus der Datei-Liste.</summary>
    public ViewMode DefaultView { get; set; } = ViewMode.Grid;
    /// <summary>Default-Sortierung der Datei-Liste.</summary>
    public FileSortMode DefaultSort { get; set; } = FileSortMode.NameAsc;
    /// <summary>Thumbnails generieren beim Scan.</summary>
    public bool GenerateThumbnails { get; set; } = true;
    /// <summary>Duplikate bei Scan erkennen (Hash-Vergleich).</summary>
    public bool DetectDuplicates { get; set; } = true;

    // --- Drucker ---
    /// <summary>ID des Default-Druckers (null = keiner).</summary>
    public int? DefaultPrinterId { get; set; }
    /// <summary>Default-Drucker automatisch verbinden beim Start.</summary>
    public bool AutoConnectPrinter { get; set; }
    /// <summary>Druck-Start braucht User-Bestätigung.</summary>
    public bool RequirePrintConfirmation { get; set; } = true;
    /// <summary>Temperatureinheit ("C" oder "F").</summary>
    public string TemperatureUnit { get; set; } = "C";
    /// <summary>Aktualisierungs-Intervall für Webcam in Sekunden.</summary>
    public int WebcamRefreshSeconds { get; set; } = 5;

    // --- Filament ---
    /// <summary>Filament-Kosten-Tracking aktivieren.</summary>
    public bool FilamentCostTracking { get; set; } = true;
    /// <summary>Wie lange Drying-Logs behalten werden (Tage).</summary>
    public int DryingLogRetentionDays { get; set; } = 365;
    /// <summary>QR/NFC-Tags für Spulen aktivieren.</summary>
    public bool QrNfcEnabled { get; set; }

    // --- KI-Assistent ---
    /// <summary>KI-Chat aktiviert.</summary>
    public bool AiChat { get; set; } = true;
    /// <summary>KI-Suche aktiviert (Embedding-basiert).</summary>
    public bool AiSearch { get; set; } = true;
    /// <summary>KI-Wartungs-Empfehlungen aktiviert.</summary>
    public bool AiMaintenance { get; set; } = true;
    /// <summary>KI-Streaming aktiviert (Token-für-Token).</summary>
    public bool AiStreaming { get; set; } = true;
    /// <summary>External OpenAI API-Key (optional, eigener Key).</summary>
    public string? ExternalOpenAiKey { get; set; }
    /// <summary>External Anthropic API-Key (optional).</summary>
    public string? ExternalAnthropicKey { get; set; }
    /// <summary>External Ollama Server-URL (optional, lokal).</summary>
    public string? ExternalOllamaUrl { get; set; }
    /// <summary>Pfad für heruntergeladene KI-Modelle.</summary>
    public string? ModelDownloadPath { get; set; }

    // --- Forge-Bot ---
    /// <summary>Forge-Bot anzeigen.</summary>
    public bool BotEnabled { get; set; } = true;
    /// <summary>Nachrichten-Häufigkeit des Bot.</summary>
    public BotFrequency BotFrequency { get; set; } = BotFrequency.Normal;
    /// <summary>Beginn der Ruhezeit (Do Not Disturb).</summary>
    public TimeSpan? BotDndStart { get; set; }
    /// <summary>Ende der Ruhezeit (Do Not Disturb).</summary>
    public TimeSpan? BotDndEnd { get; set; }
    /// <summary>Position des Bot auf dem Bildschirm.</summary>
    public BotPosition BotPosition { get; set; } = BotPosition.BottomRight;
    /// <summary>Bot-Sprache = App-Sprache (sonst Englisch).</summary>
    public bool BotLanguageSameAsApp { get; set; } = true;

    // --- Server ---
    /// <summary>Port des lokalen ASP.NET Core Servers.</summary>
    public int ServerPort { get; set; } = 5000;
    /// <summary>HTTPS für Server aktivieren.</summary>
    public bool HttpsEnabled { get; set; }
    /// <summary>Pfad zum HTTPS-Zertifikat (falls HTTPS aktiviert).</summary>
    public string? HttpsCertPath { get; set; }
    /// <summary>API-Key für Server-Authentifizierung.</summary>
    public string? ApiKey { get; set; }

    // --- Push-Benachrichtigungen ---
    /// <summary>Push-Benachrichtigungen aktiviert.</summary>
    public bool PushNotifications { get; set; } = true;
    /// <summary>Benachrichtigung wenn Druck fertig.</summary>
    public bool NotifyPrintComplete { get; set; } = true;
    /// <summary>Benachrichtigung wenn Druck fehlgeschlagen.</summary>
    public bool NotifyPrintFailed { get; set; } = true;
    /// <summary>Schwellwert für Filament-Warnung (in Gramm).</summary>
    public decimal FilamentWarnThresholdG { get; set; } = 100;
    /// <summary>Wie viele Tage vor Wartungs-Termin erinnern.</summary>
    public int MaintenanceReminderDaysBefore { get; set; } = 7;
    /// <summary>Art der Push-Benachrichtigung.</summary>
    public NotificationKind NotificationKind { get; set; } = NotificationKind.InApp;

    // --- Backup & Debug ---
    /// <summary>Auto-Backup Intervall ("off", "daily", "weekly", "monthly").</summary>
    public string AutoBackupInterval { get; set; } = "off";
    /// <summary>Pfad für Auto-Backups.</summary>
    public string? BackupPath { get; set; }
    /// <summary>Log-Level ("Trace", "Debug", "Info", "Warning", "Error").</summary>
    public string LogLevel { get; set; } = "Info";
    /// <summary>Debug-Modus aktiviert (erweitertes Logging).</summary>
    public bool DebugMode { get; set; }

    // --- CRUD / Archiv ---
    /// <summary>Historie behalten wenn Eintrag gelöscht wird (Archiv-Option).</summary>
    public bool KeepHistoryOnDelete { get; set; } = true;
}