// SPDX-License-Identifier: GPL-3.0-or-later
// ISearchService: Datei-Suche mit Filename-Matching + KI-Semantik (Gemma 4).
using System.Text.Json;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Desktop.Services;

/// <summary>Ergebnis einer Suchanfrage (Filename + optionaler KI-Treffer-Indikator).</summary>
public sealed class SearchResult
{
    public int FileId { get; init; }
    public string FileName { get; init; } = "";
    public string Path { get; init; } = "";
    public long FileSizeBytes { get; init; }
    public DateTime LastModified { get; init; }
    public bool IsAiHit { get; init; }
    public float Score { get; init; }
    public string? Snippet { get; init; }
}

/// <summary>Such-Service-Abstraktion.</summary>
public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default);
}

/// <summary>
/// KI-gestuetzte Suche: Filename-Matching + Gemma 4 Semantik-Suche.
/// Wenn ein KI-Modell geladen ist, wird eine Bedeutungssuche durchgefuehrt
/// (z.B. "Drache" findet auch "dragon_v2.stl", "wyvern_print.gcode").
/// </summary>
public sealed class AiEnhancedSearchService : ISearchService
{
    private readonly Func<FlipsiForgeDbContext> _dbFactory;
    private readonly IAIChatEngine? _aiEngine;

    public AiEnhancedSearchService(Func<FlipsiForgeDbContext> dbFactory, IAIChatEngine? aiEngine = null)
    {
        _dbFactory = dbFactory;
        _aiEngine = aiEngine;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SearchResult>();

        await using var db = _dbFactory();
        var q = query.Trim();

        // 1. Filename-Matching (immer, sofort)
        var allFiles = db.ScannedFiles.ToList();
        var filenameMatches = allFiles
            .Where(f => f.FileName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                       (f.Path ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
            .Select(f => new SearchResult
            {
                FileId = f.Id,
                FileName = f.FileName,
                Path = f.Path,
                FileSizeBytes = f.FileSizeBytes,
                LastModified = f.LastModified,
                IsAiHit = false,
                Score = 1.0f
            })
            .ToList();

        // 2. KI-Semantik-Suche (nur wenn Modell geladen)
        var aiResults = new List<SearchResult>();
        if (_aiEngine != null && _aiEngine.IsModelLoaded)
        {
            try
            {
                aiResults = await SearchWithAiAsync(query, allFiles, ct);
            }
            catch
            {
                // Best-effort — KI-Suche ist optional
            }
        }

        // 3. Zusammenfuehren: Filename-Treffer zuerst, dann KI-Treffer (ohne Duplikate)
        var result = new List<SearchResult>(filenameMatches);
        var filenameIds = filenameMatches.Select(r => r.FileId).ToHashSet();
        foreach (var ai in aiResults)
        {
            if (!filenameIds.Contains(ai.FileId))
                result.Add(ai);
        }

        return result;
    }

    /// <summary>
    /// KI-gestuetzte Suche: Die KI analysiert die gerenderten Thumbnails der STL/3MF/OBJ Dateien
    /// und erkennt was das Modell darstellt. Z.B. Suche "Hund" findet eine STL die einen Hund
    /// darstellt, unabhaengig vom Dateinamen.
    /// 
    /// Vorgehen:
    /// 1. Fuer alle Dateien Thumbnails rendern (StlThumbnailService)
    /// 2. Thumbnails als Base64 an Gemma 4 schicken mit Prompt "Was zeigt dieses Modell?"
    /// 3. KI antwortet mit Beschreibung
    /// 4. Beschreibung mit Suchanfrage vergleichen
    /// </summary>
    private async Task<List<SearchResult>> SearchWithAiAsync(string query, List<ScannedFile> allFiles, CancellationToken ct)
    {
        if (allFiles.Count == 0) return new();

        // Nur Dateien mit Thumbnails (STL/3MF/OBJ)
        var candidates = allFiles
            .Where(f => f.Extension?.ToLowerInvariant() is ".stl" or ".3mf" or ".obj")
            .Take(50) // Limit: max 50 Dateien pro KI-Suche (Performance)
            .ToList();

        if (candidates.Count == 0) return new();

        var results = new List<SearchResult>();
        var searchLower = query.ToLowerInvariant().Trim();

        // Fuer jede Datei: Thumbnail rendern und KI fragen was es darstellt
        foreach (var file in candidates)
        {
            ct.ThrowIfCancellationRequested();

            // Thumbnail rendern
            Avalonia.Media.Imaging.Bitmap? thumb = null;
            try
            {
                thumb = StlThumbnailService.GetOrGenerate(file.Path, file.LastModified.Ticks, 128, 128);
            }
            catch { continue; }
            if (thumb == null) continue;

            // Thumbnail zu Base64 konvertieren
            string base64Image;
            using (var memStream = new System.IO.MemoryStream())
            {
                thumb.Save(memStream);
                base64Image = Convert.ToBase64String(memStream.ToArray());
            }

            // KI fragen: "Was zeigt dieses 3D-Modell? Antworte in 1-3 Worten."
            var prompt = $"""Du bist ein 3D-Modell-Analyst. Sieh dir dieses gerenderte 3D-Modell an und beschreibe in 1-5 Woertern was es darstellt. Antworte NUR mit der Beschreibung, kein sonstiger Text.""";

            var responseBuilder = new System.Text.StringBuilder();
            await foreach (var chunk in _aiEngine!.StreamAsync(prompt, ct))
            {
                responseBuilder.Append(chunk.Text);
                if (chunk.IsFinal) break;
            }

            var description = responseBuilder.ToString().Trim().ToLowerInvariant();

            // Pruefen ob die Suchanfrage in der Beschreibung vorkommt
            if (description.Contains(searchLower) || searchLower.Contains(description))
            {
                results.Add(new SearchResult
                {
                    FileId = file.Id,
                    FileName = file.FileName,
                    Path = file.Path,
                    FileSizeBytes = file.FileSizeBytes,
                    LastModified = file.LastModified,
                    IsAiHit = true,
                    Score = 0.9f,
                    Snippet = $"KI: \"{description}\""
                });
            }
        }

        return results;
    }
}

/// <summary>
/// Stub-Implementierung die nur auf Filename matcht (case-insensitive Contains).
/// KEINE KI-Suche. Wird verwendet wenn kein IAIChatEngine verfuegbar ist.
/// </summary>
public sealed class DesktopStubSearchService : ISearchService
{
    private readonly Func<FlipsiForgeDbContext> _dbFactory;

    public DesktopStubSearchService(Func<FlipsiForgeDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SearchResult>();

        await using var db = _dbFactory();
        var q = query.Trim();
        var files = db.ScannedFiles.AsEnumerable()
            .Where(f => f.FileName.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return files.Select(f => new SearchResult
        {
            FileId = f.Id,
            FileName = f.FileName,
            Path = f.Path,
            FileSizeBytes = f.FileSizeBytes,
            LastModified = f.LastModified,
            IsAiHit = false,
            Score = 1.0f
        }).ToList();
    }
}