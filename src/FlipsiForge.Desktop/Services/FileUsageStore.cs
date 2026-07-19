// SPDX-License-Identifier: GPL-3.0-or-later
// Desktop-seitige JSON-Persistenz für File-Usage-Statistiken (Häufigkeit & Favoriten).
// Core.Models hat mittlerweile ein eigenes FileUsageLog DB-Modell (DbSet<FileUsageLog>),
// das Zugriffe pro ScannedFile in der DB protokolliert. Diese Datei hier ist eine
// ergänzende Desktop-only JSON-Ablage für UI-spezifische Metadaten (OpenCount,
// IsFavorite), die unabhängig von der DB funktioniert. Um den Namenskonflikt mit
// FlipsiForge.Core.Models.FileUsageLog zu vermeiden, heißt die Klasse hier
// FileUsageStore (die Entry-Klasse heißt weiterhin FileUsageEntry).
using System.Text.Json;

namespace FlipsiForge.Desktop.Services;

/// <summary>Usage-Eintrag für eine Datei (Häufigkeit + Favorit).</summary>
public sealed class FileUsageEntry
{
    public int FileId { get; set; }
    public int OpenCount { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime? LastOpened { get; set; }
}

/// <summary>JSON-Persistenz für <see cref="FileUsageEntry"/> (Desktop-only Cache).</summary>
public sealed class FileUsageStore
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private static readonly object _lock = new();

    /// <summary>Liefert den Pfad der Log-Datei.</summary>
    public static string GetFilePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlipsiForge");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "file_usage.json");
    }

    /// <summary>Lädt alle Usage-Einträge als Dictionary (FileId → Entry).</summary>
    public static Dictionary<int, FileUsageEntry> LoadAll()
    {
        try
        {
            var path = GetFilePath();
            if (!File.Exists(path)) return new();
            var list = JsonSerializer.Deserialize<List<FileUsageEntry>>(File.ReadAllText(path), JsonOpts);
            if (list is null) return new();
            return list.ToDictionary(e => e.FileId, e => e);
        }
        catch
        {
            return new();
        }
    }

    /// <summary>Speichert das gesamte Dictionary als JSON.</summary>
    public static void SaveAll(Dictionary<int, FileUsageEntry> entries)
    {
        try
        {
            lock (_lock)
            {
                File.WriteAllText(GetFilePath(), JsonSerializer.Serialize(entries.Values.ToList(), JsonOpts));
            }
        }
        catch
        {
            // Best-effort
        }
    }

    /// <summary>Liefert einen Eintrag oder erzeugt einen neuen (nicht gespeichert).</summary>
    public static FileUsageEntry GetOrNew(Dictionary<int, FileUsageEntry> dict, int fileId)
    {
        if (dict.TryGetValue(fileId, out var e)) return e;
        e = new FileUsageEntry { FileId = fileId };
        dict[fileId] = e;
        return e;
    }

    /// <summary>Incrementiert den OpenCount für eine Datei und speichert sofort.</summary>
    public static void IncrementOpen(int fileId)
    {
        var dict = LoadAll();
        var e = GetOrNew(dict, fileId);
        e.OpenCount++;
        e.LastOpened = DateTime.UtcNow;
        SaveAll(dict);
    }

    /// <summary>Schaltet den Favorit-Status um und speichert sofort.</summary>
    public static void ToggleFavorite(int fileId)
    {
        var dict = LoadAll();
        var e = GetOrNew(dict, fileId);
        e.IsFavorite = !e.IsFavorite;
        SaveAll(dict);
    }
}