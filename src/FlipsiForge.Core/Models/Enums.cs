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