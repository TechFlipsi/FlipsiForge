using System.Collections.Concurrent;
using System.Security.Cryptography;
using FlipsiForge.Core.Models;
using FlipsiForge.Core.Services.AI;

namespace FlipsiForge.Core.Services.Scanner;

/// <summary>
/// Scanner für 3D-Druck-Dateien in einem oder mehreren Ordnern.
/// - Paralleles Directory.EnumerateFiles
/// - Generiert Embeddings für KI-Suche (via IEmbeddingProvider, falls geladen)
/// - Duplikat-Erkennung via SHA-256-Hash für Dateien mit gleichem Namen+Größe
/// </summary>
public sealed class FileScanner
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private static readonly string[] DefaultExtensions =
        { "STL", "3MF", "GCODE", "OBJ", "PLY", "STEP", "AMF", "X3D" };

    /// <summary>
    /// Erzeugt den Scanner.
    /// </summary>
    /// <param name="embeddingProvider">Optional Embedding-Provider für KI-Suche.
    /// Wenn nicht geladen, werden keine Embeddings generiert (nur Filename-Suche möglich).</param>
    public FileScanner(IEmbeddingProvider embeddingProvider)
    {
        _embeddingProvider = embeddingProvider;
    }

    /// <summary>
    /// Scannt eine Liste von Ordnern nach 3D-Druck-Dateien.
    /// </summary>
    /// <param name="folders">Liste der zu scannenden Ordner-Pfade.</param>
    /// <param name="extensions">Datei-Extensions ohne Punkt, z.B. ["STL","3MF"]. Null = Defaults.</param>
    /// <returns>Liste der gefundenen gescannten Dateien.</returns>
    public async Task<List<ScannedFile>> ScanAsync(List<string> folders, List<string>? extensions = null)
    {
        var exts = (extensions is null || extensions.Count == 0)
            ? DefaultExtensions.ToList()
            : extensions.Select(e => e.TrimStart('.').ToUpperInvariant()).Distinct().ToList();

        if (folders.Count == 0) return new List<ScannedFile>();

        // 1. Phase — Parallel Dateien enumerieren
        var allFiles = new ConcurrentBag<(string Path, string Name, string Ext, long Size, DateTime Modified)>();
        var parallelOpts = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

        await Parallel.ForEachAsync(folders, parallelOpts, async (folder, ct) =>
        {
            try
            {
                if (!Directory.Exists(folder)) return;
                // Top-level + rekursiv (1 Level tief, konfigurierbar später)
                await Task.Run(() =>
                {
                    var files = EnumerateFilesRecursive(folder, exts);
                    foreach (var f in files)
                    {
                        var fi = new FileInfo(f);
                        if (!fi.Exists) continue;
                        allFiles.Add((f, fi.Name, fi.Extension.TrimStart('.').ToUpperInvariant(),
                                      fi.Length, fi.LastWriteTimeUtc));
                    }
                }, ct).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException) { /* skip protected folders */ }
            catch (DirectoryNotFoundException) { /* skip missing */ }
            catch { /* ignore other errors per-folder */ }
        }).ConfigureAwait(false);

        // 2. Phase — Duplikat-Erkennung + Embeddings parallel
        var byNameSize = allFiles
            .GroupBy(f => (f.Name, f.Size))
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToList();

        var hashedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new ConcurrentBag<ScannedFile>();
        var counter = 1;

        await Parallel.ForEachAsync(allFiles, parallelOpts, async (file, ct) =>
        {
            try
            {
                var sf = new ScannedFile
                {
                    FileName = file.Name,
                    Path = file.Path,
                    Extension = file.Ext,
                    FileSizeBytes = file.Size,
                    LastModified = file.Modified
                };

                // Duplikat-Erkennung via SHA-256 nur für potentielle Duplikate (selber Name+Größe)
                if (byNameSize.Count > 0 && byNameSize.Any(f => f.Path == file.Path))
                {
                    sf.ContentHash = await ComputeHashAsync(file.Path).ConfigureAwait(false);
                    lock (hashedSet)
                    {
                        if (hashedSet.Contains(sf.ContentHash))
                        {
                            // Duplikat — Tag setzen
                            sf.Tags = "duplicate";
                        }
                        else
                        {
                            hashedSet.Add(sf.ContentHash);
                        }
                    }
                }

                // Embedding generieren (falls Provider geladen)
                if (_embeddingProvider.IsLoaded)
                {
                    // Embedding-Text = Dateiname (evtl. + Notes + Tags später)
                    var embeddingText = $"{file.Name} {Path.GetFileNameWithoutExtension(file.Name)}";
                    try
                    {
                        sf.Embedding = await _embeddingProvider.EmbedAsync(embeddingText).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Embedding-Fehler ist nicht fatal
                    }
                }

                // Id konsistent zuweisen (Thread-safe)
                int id;
                lock (results) { id = counter++; }
                sf.Id = id;

                results.Add(sf);
            }
            catch
            {
                // Per-File-Fehler nicht fatal
            }
        }).ConfigureAwait(false);

        return results.OrderBy(f => f.FileName).ToList();
    }

    /// <summary>
    /// Enumeriert Dateien rekursiv in einem Ordner, gefiltert nach Extensions.
    /// </summary>
    private static IEnumerable<string> EnumerateFilesRecursive(string folder, List<string> exts)
    {
        var opts = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            ReturnSpecialDirectories = false
        };

        foreach (var path in Directory.EnumerateFiles(folder, "*", opts))
        {
            var ext = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
            if (exts.Contains(ext))
                yield return path;
        }
    }

    /// <summary>
    /// Berechnet SHA-256-Hash einer Datei (streamend, für große Dateien).
    /// </summary>
    private static async Task<string> ComputeHashAsync(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81920, useAsync: true);
            using var sha = SHA256.Create();
            var hashBytes = await sha.ComputeHashAsync(stream).ConfigureAwait(false);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch
        {
            return "";
        }
    }
}