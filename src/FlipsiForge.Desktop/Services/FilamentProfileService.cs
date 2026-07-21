// SPDX-License-Identifier: GPL-3.0-or-later
// FilamentProfileService: Laedt die filaments.json, Export/Import in den Einstellungen.
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

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
    public string? Notes { get; set; }
}

/// <summary>Eine Marke mit ihren Material-Profilen.</summary>
public class FilamentBrandProfile
{
    public string Name { get; set; } = "";
    public List<FilamentProfile> Materials { get; set; } = new();
}

/// <summary>Eine Farbdefinition.</summary>
public class FilamentColor
{
    public string Name { get; set; } = "";
    public string Hex { get; set; } = "";
}

/// <summary>Die komplette Filament-Datenbank.</summary>
public class FilamentDatabase
{
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string LastUpdated { get; set; } = "";
    public List<FilamentBrandProfile> Brands { get; set; } = new();
    public List<FilamentColor> Colors { get; set; } = new();
}

/// <summary>Service fuer Filament-Profile: Laden, Export, Import.</summary>
public static class FilamentProfileService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    /// <summary>Pfad zur mitgelieferten filaments.json.</summary>
    public static string DefaultDbPath => Path.Combine(
        AppContext.BaseDirectory, "Data", "filaments.json");

    /// <summary>Pfad zur benutzerdefinierten filaments.json (ueberschreibt Default).</summary>
    public static string UserDbPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlipsiForge", "filaments.json");

    /// <summary>Liefert den Pfad zur aktiven Datenbank (User falls vorhanden, sonst Default).</summary>
    public static string ActiveDbPath => File.Exists(UserDbPath) ? UserDbPath : DefaultDbPath;

    /// <summary>Laedt die Filament-Datenbank.</summary>
    public static FilamentDatabase Load()
    {
        try
        {
            var path = ActiveDbPath;
            if (!File.Exists(path)) return new FilamentDatabase();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<FilamentDatabase>(json, JsonOpts) ?? new();
        }
        catch
        {
            return new FilamentDatabase();
        }
    }

    /// <summary>Sucht ein Profil fuer Marke + Material-Typ.</summary>
    public static FilamentProfile? FindProfile(string brand, string materialType)
    {
        var db = Load();
        var brandProfile = db.Brands.FirstOrDefault(b =>
            b.Name.Equals(brand, StringComparison.OrdinalIgnoreCase));
        if (brandProfile == null) return null;
        return brandProfile.Materials.FirstOrDefault(m =>
            m.Type.Equals(materialType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Liefert alle Marken-Namen.</summary>
    public static List<string> GetBrandNames()
    {
        var db = Load();
        return db.Brands.Select(b => b.Name).ToList();
    }

    /// <summary>Liefert alle Farben.</summary>
    public static List<FilamentColor> GetColors()
    {
        var db = Load();
        return db.Colors;
    }

    /// <summary>Exportiert die aktive Datenbank in eine Datei (Speichern-Dialog).</summary>
    public static void Export(string destPath)
    {
        var db = Load();
        var json = JsonSerializer.Serialize(db, JsonOpts);
        File.WriteAllText(destPath, json);
    }

    /// <summary>Importiert eine Datenbank aus einer Datei. Ueberschreibt die User-DB.</summary>
    public static void Import(string sourcePath)
    {
        if (!File.Exists(sourcePath)) return;
        var json = File.ReadAllText(sourcePath);
        var db = JsonSerializer.Deserialize<FilamentDatabase>(json, JsonOpts);
        if (db == null) return;
        var dir = Path.GetDirectoryName(UserDbPath);
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(UserDbPath, json);
    }

    /// <summary>Setzt die User-DB zurueck (loescht sie, Default wird wieder aktiv).</summary>
    public static void ResetToDefault()
    {
        if (File.Exists(UserDbPath))
            File.Delete(UserDbPath);
    }
}