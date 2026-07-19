namespace FlipsiForge.Core.Models;

/// <summary>Drucker-Protokolle die FlipsiForge unterstützt.</summary>
public enum PrinterProtocol
{
    KlipperMoonraker,
    Marlin,
    BambuLab,
    PrusaLink,
    OctoPrint
}

/// <summary>Status eines Druckers.</summary>
public enum PrinterStatus
{
    Idle,
    Printing,
    Paused,
    Error,
    Offline,
    Heating,
    Cooling
}

/// <summary>Status einer Filament-Spule.</summary>
public enum SpoolStatus
{
    Active,
    Empty,
    Archived,
    Drying
}

/// <summary>Ziel-Modus für KI-Empfehlungen.</summary>
public enum PrintGoal
{
    MaximumStrength,
    FastPrint,
    VisualQuality,
    Prototype
}

/// <summary>Material-Typen für Filament.</summary>
public enum MaterialType
{
    PLA,
    PETG,
    ABS,
    ASA,
    TPU,
    PC,
    PA6,
    PVA,
    POM,
    HIPS,
    Other
}

/// <summary>Server-Modus (Full oder Lite).</summary>
public enum ServerMode
{
    Full,
    Lite
}

/// <summary>KI-Modell-Auswahl.</summary>
public enum AiModelChoice
{
    Auto,
    E4B,
    E2B,
    E2BQat,
    Off
}

// === v0.2.0 Erweiterungen ===

/// <summary>Häufigkeit der Forge-Bot-Nachrichten.</summary>
public enum BotFrequency
{
    Rare,
    Normal,
    Often
}

/// <summary>Position des Forge-Bot auf dem Bildschirm.</summary>
public enum BotPosition
{
    BottomRight,
    BottomLeft,
    TopRight,
    TopLeft
}

/// <summary>Art der Push-Benachrichtigung.</summary>
public enum NotificationKind
{
    InApp,
    System,
    Email,
    Webhook
}

/// <summary>Sortier-Modi für die Datei-Liste.</summary>
public enum FileSortMode
{
    NameAsc,
    NameDesc,
    DateAsc,
    DateDesc,
    SizeAsc,
    SizeDesc,
    Frequency,
    FavoritesFirst
}

/// <summary>Anzeige-Modus der Datei-Liste (Raster oder Liste).</summary>
public enum ViewMode
{
    Grid,
    List
}

/// <summary>Datumsformat-Einstellung (regional).</summary>
public enum DateFormat
{
    EU,
    US,
    Iso
}