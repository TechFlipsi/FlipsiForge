// FlipsiForge.Server — v0.2.0
// FileScanner — durchsucht Ordner nach 3D-Druck-Dateien und indiziert sie
// in der SQLite-DB. Echte Core-Implementierung würde STLs parsen für
// Triangle-Count + Bounding-Box + Thumbnail-Generierung; diese Version
// nimmt nur Datei-Metadaten (Name, Pfad, Größe, Extension, LastModified).
//
// SPDX-License-Identifier: GPL-3.0-only
// (c) 2026 TechFlipsi / Fabian Kirchweger

using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlipsiForge.Server.Services;

/// <summary>
/// Scannt Verzeichnisse nach 3D-Druck-Dateien und indiziert sie in der DB.
/// Erkennt .stl, .obj, .3mf, .gcode, .gco, .ply, .step, .stp, .amf, .x3d.
/// </summary>
public sealed class FileScanner
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "STL", "OBJ", "3MF", "GCODE", "GCO", "PLY", "STEP", "STP", "AMF", "X3D"
    };

    private readonly ILogger<FileScanner> _logger;

    /// <summary>Konstruktor.</summary>
    public FileScanner(ILogger<FileScanner> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Scannt die gegebenen Ordner rekursiv nach 3D-Druck-Dateien.
    /// Gefundene Dateien werden in <see cref="FlipsiForgeDbContext.ScannedFiles"/>
    /// upserted (bestehende werden aktualisiert, neue hinzugefügt).
    /// </summary>
    /// <param name="db">DbContext für Upsert.</param>
    /// <param name="folders">Wurzelverzeichnisse (werden rekursiv gescannt).</param>
    /// <param name="extensions">Filter auf Endungen (ohne Punkt). Leer = alle unterstützten.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Ergebnis-Statistik.</returns>
    public async Task<FileScanResult> ScanAsync(
        FlipsiForgeDbContext db,
        IReadOnlyList<string> folders,
        IReadOnlyList<string>? extensions = null,
        CancellationToken cancellationToken = default)
    {
        var allowed = extensions is { Count: > 0 } allowedExts
            ? new HashSet<string>(allowedExts, StringComparer.OrdinalIgnoreCase)
            : SupportedExtensions;

        var result = new FileScanResult();
        if (folders is null || folders.Count == 0)
        {
            _logger.LogWarning("FileScanner.ScanAsync: keine Ordner angegeben");
            return result;
        }

        // Bestehende Dateien laden für Upsert per Pfad
        var existingByPath = await db.ScannedFiles
            .ToDictionaryAsync(f => f.Path, f => f, cancellationToken);

        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                _logger.LogWarning("FileScanner: Ordner existiert nicht: {Folder}", folder);
                result.SkippedFolders.Add(folder);
                continue;
            }

            result.ScannedFolders.Add(folder);
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException)
            {
                _logger.LogWarning(ex, "FileScanner: Zugriff verweigert auf {Folder}", folder);
                result.SkippedFolders.Add(folder);
                continue;
            }

            foreach (var path in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fi = new FileInfo(path);
                var ext = fi.Extension.TrimStart('.').ToUpperInvariant();
                if (!allowed.Contains(ext)) continue;

                if (existingByPath.TryGetValue(path, out var existing))
                {
                    existing.FileName = fi.Name;
                    existing.FileSizeBytes = fi.Length;
                    existing.LastModified = fi.LastWriteTimeUtc;
                    existing.Extension = ext;
                    result.UpdatedFiles++;
                }
                else
                {
                    var scanned = new ScannedFile
                    {
                        FileName = fi.Name,
                        Path = path,
                        Extension = ext,
                        FileSizeBytes = fi.Length,
                        LastModified = fi.LastWriteTimeUtc
                    };
                    db.ScannedFiles.Add(scanned);
                    existingByPath[path] = scanned;
                    result.NewFiles++;
                }
            }
        }

        if (result.NewFiles > 0 || result.UpdatedFiles > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "FileScanner: {New} neue, {Updated} aktualisierte Dateien in {Folders} Ordnern",
            result.NewFiles, result.UpdatedFiles, result.ScannedFolders.Count);
        return result;
    }

    /// <summary>Default-Liste der unterstützten Datei-Endungen.</summary>
    public static IReadOnlyCollection<string> GetSupportedExtensions() => SupportedExtensions;
}

/// <summary>Statistik über einen Scan-Lauf.</summary>
public sealed class FileScanResult
{
    public int NewFiles { get; set; }
    public int UpdatedFiles { get; set; }
    public List<string> ScannedFolders { get; set; } = new();
    public List<string> SkippedFolders { get; set; } = new();
}