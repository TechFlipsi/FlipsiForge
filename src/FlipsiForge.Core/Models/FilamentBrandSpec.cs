namespace FlipsiForge.Core.Models;

/// <summary>
/// Filament-Marken-Datenbank — Hersteller-Empfehlungen für Druck-Einstellungen.
/// 79 vordefinierte Einträge, User kann eigene hinzufügen.
/// </summary>
public class FilamentBrandSpec
{
    public int Id { get; set; }
    public string Brand { get; set; } = "";
    public string ProductName { get; set; } = "";
    public MaterialType MaterialType { get; set; }

    // Temperaturen
    public int HotendMin { get; set; }
    public int HotendMax { get; set; }
    public int HotendOptimal { get; set; }
    public int BedMin { get; set; }
    public int BedMax { get; set; }
    public int BedOptimal { get; set; }

    // Speed
    public int SpeedMin { get; set; }
    public int SpeedMax { get; set; }
    public int SpeedOptimal { get; set; }
    public int OuterWallSpeed { get; set; }
    public int InfillSpeed { get; set; }

    // Druck-Einstellungen
    public int FanPercent { get; set; }
    public decimal RetractionMm { get; set; }
    public decimal? PressureAdvance { get; set; }
    public decimal LayerHeightMin { get; set; }
    public decimal LayerHeightMax { get; set; }
    public decimal LayerHeightOptimal { get; set; }

    // Filament-Eigenschaften
    public bool IsUVResistant { get; set; }
    public bool IsWeatherResistant { get; set; }
    public bool IsFoodSafe { get; set; }
    public bool IsFlexible { get; set; }
    public bool IsAbrasive { get; set; }
    public bool IsHeatResistant { get; set; }
    public int MaxServiceTempC { get; set; }
    public bool NeedsEnclosure { get; set; }
    public bool NeedsDirectDrive { get; set; }
    public bool NeedsDryingBeforePrint { get; set; }
    public int DryingTempC { get; set; }
    public int DryingDurationH { get; set; }
    public bool IsBiodegradable { get; set; }
    public bool IsRecyclable { get; set; }
    public bool WarpsEasily { get; set; }
    public bool StringsEasily { get; set; }
    public bool IsImpactResistant { get; set; }
    public decimal TensileStrengthMpa { get; set; }
    public decimal DensityGcm3 { get; set; }
    public string SuitableFor { get; set; } = "";
    public string NotSuitableFor { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool IsUserAdded { get; set; }
}