using FlipsiForge.Core.Models;

namespace FlipsiForge.Core.Services.Search;

/// <summary>
/// Ein Suchergebnis — eine gescannte Datei mit Score und ggf. KI-Treffer-Markierung.
/// </summary>
public sealed class SearchResult
{
    /// <summary>Die gefundene Datei.</summary>
    public required ScannedFile File { get; init; }

    /// <summary>True wenn dieser Treffer via KI-Semantik-Suche zustande kam.</summary>
    public bool IsAiHit { get; init; }

    /// <summary>Score 0–1 (1 = perfekte Match).</summary>
    public double Score { get; init; }

    /// <summary>Optional Snippet — bei KI-Treffern die matched-Text-Stelle.</summary>
    public string? Snippet { get; init; }
}

/// <summary>
/// Abstraktion der Datei-Suche.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Sucht nach Dateien anhand eines Query-Strings.
    /// </summary>
    /// <param name="query">Suchbegriff.</param>
    /// <param name="files">Liste der zu durchsuchenden gescannten Dateien.</param>
    /// <param name="useKI">Wenn true, zusätzlich KI-Semantik-Suche (Embeddings) verwenden.</param>
    /// <returns>Sortierte Treffer-Liste (Score absteigend).</returns>
    Task<List<SearchResult>> SearchAsync(string query, List<ScannedFile> files, bool useKI);
}