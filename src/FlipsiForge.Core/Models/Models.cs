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

/// <summary>App-Einstellungen.</summary>
public class AppSettings
{
    public int Id { get; set; } = 1;
    public AiModelChoice AiModel { get; set; } = AiModelChoice.Auto;
    public bool AiEnabled { get; set; } = true;
    public ServerMode ServerMode { get; set; } = ServerMode.Full;
    public bool WebUiEnabled { get; set; } = true;
    public string? Language { get; set; } = "de";
    public string? TelegramBotToken { get; set; }
    public long? TelegramChatId { get; set; }
    public string? NextcloudUrl { get; set; }
    public string? NextcloudUser { get; set; }
    public string? NextcloudPassword { get; set; }
    public List<string> WatchFolders { get; set; } = new();
}