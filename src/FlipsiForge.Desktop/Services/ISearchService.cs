// SPDX-License-Identifier: GPL-3.0-or-later
// Desktop-interne Service-Abstraktionen (Stubs/Fallbacks) für den Fall,
// dass FlipsiForge.Core.Services vom Parallel-Subagenten noch nicht
// bereit steht. Die ViewModel-Schicht spricht nur gegen diese Interfaces,
// so dass das Projekt auch ohne Core.Services kompiliert.
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

/// <summary>
/// Such-Service-Abstraktion. Im Desktop existiert ein
/// <see cref="DesktopStubSearchService"/> der nur Filename-Matches liefert.
/// Sobald Core.Services.CombinedSearchService bereitsteht, kann ein
/// Adapter implementiert werden der diese Abstraktion nutzt.
/// </summary>
public interface ISearchService
{
    /// <summary>Sucht Dateien mittels Filename + optionaler KI-Semantik.</summary>
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default);
}

/// <summary>
/// Stub-Implementierung die nur auf Filename matcht (case-insensitive Contains).
/// KEINE KI-Suche. Dient als Fallback, wenn der Core.Services CombinedSearchService
/// zur Build-Zeit noch nicht vorhanden ist.
/// </summary>
public sealed class DesktopStubSearchService : ISearchService
{
    private readonly Func<FlipsiForge.Core.Data.FlipsiForgeDbContext> _dbFactory;

    public DesktopStubSearchService(Func<FlipsiForge.Core.Data.FlipsiForgeDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
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