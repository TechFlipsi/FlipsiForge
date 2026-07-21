// SPDX-License-Identifier: GPL-3.0-or-later
// FilamentProfileService: Laedt Filament-Profile aus Ordnerstruktur.
// Jede Marke hat einen eigenen Ordner mit einer JSON-Datei.
// Nutzer koennen eigene Profile im "user" Ordner ablegen.
// Export/Import in den Einstellungen.
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace FlipsiForge.Desktop.Services;

/// <summary>Ein Filament-Profil (Marke + Material + Eigenschaften).</summary>
public class FilamentProfile
{
    public string Type { get; set; } = "";
    public string? Variant { get; set; }
    public double Density { get; set; }
    public int PrintTempMin { get; set; }
    public int PrintTempMax { get; set; }
    public int BedTempMin { get; set; }
    public int BedTempMax { get; set; }
    public double? Retraction { get; set; }
    public double? RetractionSpeed { get; set; }
    public double? MaxFlowRate { get; set; }
    public int? CoolingFanMin { get; set; }
    public int? CoolingFanMax { get; set; }
    public double? MaxPrintSpeed { get; set; }
    public string? Nozzle { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Eine Marke mit ihren Material-Profilen.</summary>
public class FilamentBrandProfile
{
    public string Brand { get; set; } = "";
    public string? Source { get; set; }
    public string? LastUpdated { get; set; }
    public List<FilamentProfile> Materials { get; set; } = new();
}

/// <summary>Eine Farbdefinition.</summary>
public class FilamentColor
{
    public string Name { get; set; } = "";
    public string Hex { get; set; } = "";
}

/// <summary>Die komplette Filament-Datenbank (alle Marken + Farben).</summary>
public class FilamentDatabase
{
    public string Version { get; set; } = "2.0.0";
    public string Description { get; set; } = "FlipsiForge Filament-Profil-Datenbank";
    public string LastUpdated { get; set; } = "";
    public List<FilamentBrandProfile> Brands { get; set; } = new();
    public List<FilamentColor> Colors { get; set; } = new();
}

/// <summary>Service fuer Filament-Profile: Laden aus Ordnerstruktur, Export, Import.</summary>
public static class FilamentProfileService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Basis-Pfad zu den mitgelieferten Filament-Profilen.</summary>
    private static string BuiltInDir => Path.Combine(
        AppContext.BaseDirectory, "Data", "filaments");

    /// <summary>Pfad zu den benutzerdefinierten Profilen.</summary>
    public static string UserDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlipsiForge", "filaments");

    /// <summary>Liefert den Pfad zur User-Import-Datei fuer eine Marke.</summary>
    public static string GetUserBrandPath(string brandFolder) =>
        Path.Combine(UserDir, brandFolder, $"{brandFolder}.json");

    /// <summary>Lädt alle Marken aus der Ordnerstruktur (BuiltIn + User).</summary>
    public static FilamentDatabase Load()
    {
        var db = new FilamentDatabase { LastUpdated = DateTime.Now.ToString("yyyy-MM-dd") };

        // 1. BuiltIn Marken laden (aus App-Verzeichnis)
        if (Directory.Exists(BuiltInDir))
        {
            foreach (var dir in Directory.GetDirectories(BuiltInDir))
            {
                var brandName = Path.GetFileName(dir);
                if (brandName == "user") continue; // User-Ordner separat behandeln

                var jsonPath = Path.Combine(dir, $"{brandName}.json");
                if (!File.Exists(jsonPath)) continue;

                try
                {
                    var json = File.ReadAllText(jsonPath);
                    var brand = JsonSerializer.Deserialize<FilamentBrandProfile>(json, JsonOpts);
                    if (brand?.Materials?.Count > 0)
                        db.Brands.Add(brand);
                }
                catch { /* Best-effort */ }
            }

            // Farben laden
            var colorsPath = Path.Combine(BuiltInDir, "colors.json");
            if (File.Exists(colorsPath))
            {
                try
                {
                    var json = File.ReadAllText(colorsPath);
                    var colorsData = JsonSerializer.Deserialize<FilamentColorsFile>(json, JsonOpts);
                    if (colorsData?.Colors?.Count > 0)
                        db.Colors = colorsData.Colors;
                }
                catch { }
            }
        }

        // 2. User-Profile laden (ueberschreiben BuiltIn bei gleicher Marke)
        if (Directory.Exists(UserDir))
        {
            foreach (var dir in Directory.GetDirectories(UserDir))
            {
                var brandName = Path.GetFileName(dir);
                var jsonPath = Path.Combine(dir, $"{brandName}.json");
                if (!File.Exists(jsonPath)) continue;

                try
                {
                    var json = File.ReadAllText(jsonPath);
                    var brand = JsonSerializer.Deserialize<FilamentBrandProfile>(json, JsonOpts);
                    if (brand?.Materials?.Count > 0)
                    {
                        // Bestehende Marke ersetzen oder hinzufuegen
                        var existing = db.Brands.FirstOrDefault(b =>
                            b.Brand.Equals(brand.Brand, StringComparison.OrdinalIgnoreCase));
                        if (existing != null)
                            db.Brands.Remove(existing);
                        db.Brands.Add(brand);
                    }
                }
                catch { }
            }

            // User-Farben laden
            var userColors = Path.Combine(UserDir, "colors.json");
            if (File.Exists(userColors))
            {
                try
                {
                    var json = File.ReadAllText(userColors);
                    var colorsData = JsonSerializer.Deserialize<FilamentColorsFile>(json, JsonOpts);
                    if (colorsData?.Colors?.Count > 0)
                        db.Colors = colorsData.Colors; // User-Farben ueberschreiben
                }
                catch { }
            }
        }

        return db;
    }

    /// <summary>Sucht ein Profil fuer Marke + Material-Typ (+ optionale Variante).</summary>
    public static FilamentProfile? FindProfile(string brand, string materialType, string? variant = null)
    {
        var db = Load();
        var brandProfile = db.Brands.FirstOrDefault(b =>
            b.Brand.Equals(brand, StringComparison.OrdinalIgnoreCase));
        if (brandProfile == null) return null;

        // Erst mit Variante suchen, dann ohne
        if (variant != null)
        {
            var exact = brandProfile.Materials.FirstOrDefault(m =>
                m.Type.Equals(materialType, StringComparison.OrdinalIgnoreCase) &&
                m.Variant?.Equals(variant, StringComparison.OrdinalIgnoreCase) == true);
            if (exact != null) return exact;
        }

        return brandProfile.Materials.FirstOrDefault(m =>
            m.Type.Equals(materialType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Liefert alle Marken-Namen.</summary>
    public static List<string> GetBrandNames()
    {
        var db = Load();
        return db.Brands.Select(b => b.Brand).OrderBy(n => n).ToList();
    }

    /// <summary>Liefert alle Farben.</summary>
    public static List<FilamentColor> GetColors()
    {
        var db = Load();
        return db.Colors;
    }

    /// <summary>Exportiert eine einzelne Marke in eine JSON-Datei.</summary>
    public static void ExportBrand(string brandName, string destPath)
    {
        var db = Load();
        var brand = db.Brands.FirstOrDefault(b =>
            b.Brand.Equals(brandName, StringComparison.OrdinalIgnoreCase));
        if (brand == null) return;
        var json = JsonSerializer.Serialize(brand, JsonOpts);
        File.WriteAllText(destPath, json);
    }

    /// <summary>Exportiert die gesamte Datenbank in eine JSON-Datei.</summary>
    public static void ExportAll(string destPath)
    {
        var db = Load();
        var json = JsonSerializer.Serialize(db, JsonOpts);
        File.WriteAllText(destPath, json);
    }

    /// <summary>Importiert eine Marken-JSON in den User-Ordner.</summary>
    public static void ImportBrand(string sourcePath)
    {
        if (!File.Exists(sourcePath)) return;
        var json = File.ReadAllText(sourcePath);
        var brand = JsonSerializer.Deserialize<FilamentBrandProfile>(json, JsonOpts);
        if (brand == null || string.IsNullOrEmpty(brand.Brand)) return;

        // Ordnername aus Markennamen generieren
        var folderName = brand.Brand.ToLowerInvariant()
            .Replace(" ", "-").Replace(".", "").Replace("/", "-");
        var destDir = Path.Combine(UserDir, folderName);
        Directory.CreateDirectory(destDir);
        File.WriteAllText(Path.Combine(destDir, $"{folderName}.json"), json);
    }

    /// <summary>Setzt die User-Profile zurueck (loescht den User-Ordner).</summary>
    public static void ResetToDefault()
    {
        if (Directory.Exists(UserDir))
            Directory.Delete(UserDir, recursive: true);
    }

    /// <summary>Hilfsklasse fuer Farben-JSON.</summary>
    private class FilamentColorsFile
    {
        public string? Description { get; set; }
        public List<FilamentColor> Colors { get; set; } = new();
    }
}