using FlipsiForge.Core.Models;
using FlipsiForge.Core.Services.AI;

namespace FlipsiForge.Core.Services.Search;

/// <summary>
/// Kombinierte Such-Service — IMMER Dateiname + KI parallel (Pitfall-Regel:
/// nur Dateiname ist nicht genug, nur KI ist zu langsam/unzuverlässig).
///
/// Dateiname-Match:
///   - Case-insensitive Substring (Score 1.0 bei perfektem Match)
///   - Fuzzy-Levenshtein-Score (0–1, normalisiert)
///
/// KI-Match (wenn useKI &amp; EmbeddingProvider geladen):
///   - Cosine-Similarity zwischen Query-Embedding und File-Embedding
///   - Threshold 0.3 für KI-Treffer
///
/// Beide Scores werden kombiniert (max), KI-Treffer mit IsAiHit=true markiert.
/// </summary>
public sealed class CombinedSearchService : ISearchService
{
    private readonly IEmbeddingProvider _embeddingProvider;

    /// <summary>
    /// Erzeugt den Combined-Search-Service.
    /// </summary>
    /// <param name="embeddingProvider">Embedding-Provider für KI-Semantik-Suche.</param>
    public CombinedSearchService(IEmbeddingProvider embeddingProvider)
    {
        _embeddingProvider = embeddingProvider;
    }

    /// <inheritdoc />
    public async Task<List<SearchResult>> SearchAsync(
        string query, List<ScannedFile> files, bool useKI)
    {
        if (string.IsNullOrWhiteSpace(query) || files.Count == 0)
            return new List<SearchResult>();

        var qLower = query.Trim().ToLowerInvariant();

        // KI-Suche parallel vorbereiten
        float[]? queryEmbedding = null;
        if (useKI && _embeddingProvider.IsLoaded)
        {
            try
            {
                queryEmbedding = await _embeddingProvider.EmbedAsync(query).ConfigureAwait(false);
            }
            catch
            {
                queryEmbedding = null;
            }
        }

        var results = new List<SearchResult>(files.Count);

        foreach (var file in files)
        {
            var fileNameLower = (file.FileName ?? "").ToLowerInvariant();
            var (fileScore, snippet) = ScoreFileNameMatch(qLower, fileNameLower);

            double aiScore = 0;
            var isAiHit = false;

            if (queryEmbedding is { Length: > 0 } && file.Embedding is { Length: > 0 } emb)
            {
                try
                {
                    aiScore = CosineSimilarity(queryEmbedding, emb);
                    if (aiScore >= 0.3)
                        isAiHit = true;
                }
                catch
                {
                    aiScore = 0;
                }
            }

            // Kombinieren: max(fileScore, aiScore). Beide Scores 0–1.
            var combined = Math.Max(fileScore, aiScore);
            if (combined <= 0) continue;

            results.Add(new SearchResult
            {
                File = file,
                IsAiHit = isAiHit,
                Score = combined,
                Snippet = snippet
            });
        }

        // Score absteigend sortieren
        return results.OrderByDescending(r => r.Score).ToList();
    }

    /// <summary>
    /// Bewertet Filename-Match: Substring (1.0) + Fuzzy-Levenshtein (0–1).
    /// </summary>
    private static (double score, string? snippet) ScoreFileNameMatch(string query, string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return (0, null);

        // Substring-Match
        if (fileName.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            // Perfekter Match (ganzer Name) → 1.0, sonst 0.85
            var score = fileName.Equals(query, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.85;
            return (score, fileName);
        }

        // Fuzzy-Match via Levenshtein
        var dist = Levenshtein(query, fileName);
        var maxLen = Math.Max(query.Length, fileName.Length);
        if (maxLen == 0) return (0, null);
        var normalized = 1.0 - ((double)dist / maxLen);
        // Threshold: nur ähnliche Treffer (>0.5)
        if (normalized <= 0.5) return (0, null);
        return (normalized * 0.7, fileName); // Fuzzy-Score etwas abwerten
    }

    /// <summary>
    /// Levenshtein-Distanz (Standard-Algorithmus).
    /// </summary>
    private static int Levenshtein(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b.Length;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++) prev[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1, prev[j] + 1),
                    prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[b.Length];
    }

    /// <summary>
    /// Cosine-Similarity zwischen zwei Vektoren.
    /// Liefert -1 bis 1, bei Embeddings typischerweise 0 bis 1.
    /// </summary>
    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0;

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0) return 0;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}