// FlipsiForge.Server — v0.2.0
// Stub-Implementierungen für IAIChatEngine und IEmbeddingProvider.
// Liefert Platzhalter-Antworten und Null-Embeddings, wenn die echte
// Core.Services-Implementierung (ONNX Runtime + Gemma 4) nicht geladen ist.
//
// SPDX-License-Identifier: GPL-3.0-only
// (c) 2026 TechFlipsi / Fabian Kirchweger

using FlipsiForge.Core.Models;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace FlipsiForge.Server.Services;

/// <summary>
/// Stub-Implementierung von <see cref="IAIChatEngine"/>. Liefert eine kurze
/// Offline-Antwort. Die echte Implementierung in Core.Services nutzt
/// Microsoft.ML.GenAI + ONNX Runtime für Gemma 4 E4B/E2B.
/// </summary>
public sealed class StubAIChatEngine : IAIChatEngine
{
    private readonly AiSettings _aiSettings;
    private readonly ILogger<StubAIChatEngine> _logger;

    /// <summary>Konstruktor.</summary>
    public StubAIChatEngine(IOptions<AiSettings> aiSettings, ILogger<StubAIChatEngine> logger)
    {
        _aiSettings = aiSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsLoaded => false;

    /// <inheritdoc />
    public string ModelName => "(stub)";

    /// <inheritdoc />
    public string Choice => _aiSettings.ModelChoice switch
    {
        "E4B" => "E4B",
        "E2B" => "E2B",
        "E2BQat" => "E2BQat",
        "Off" => "Off",
        _ => "Off"  // Auto → Off bis echte Implementierung entscheidet
    };

    /// <inheritdoc />
    public Task<string> GenerateReplyAsync(string message, IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("StubAIChatEngine.GenerateReplyAsync für Nachricht: {Message}", message);
        var reply = "[KI deaktiviert — Stub-Modus] Core.Services nicht geladen. " +
                    "Diese Antwort stammt vom Server-Stub. Nachricht war: " +
                    (message.Length > 120 ? message[..120] + "…" : message);
        return Task.FromResult(reply);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamReplyAsync(string message, IReadOnlyList<ChatMessage> history, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var reply = await GenerateReplyAsync(message, history, cancellationToken);
        yield return reply;
    }
}

/// <summary>
/// Stub-Implementierung von <see cref="IEmbeddingProvider"/>. Liefert Null-Vektoren
/// und 0.0 Similarity. Die echte Implementierung in Core.Services nutzt
/// all-MiniLM-L6-v2 via ONNX Runtime (dim=384).
/// </summary>
public sealed class StubEmbeddingProvider : IEmbeddingProvider
{
    private readonly ILogger<StubEmbeddingProvider> _logger;

    /// <summary>Konstruktor.</summary>
    public StubEmbeddingProvider(ILogger<StubEmbeddingProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsLoaded => false;

    /// <inheritdoc />
    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("StubEmbeddingProvider.EmbedAsync für Text: {Text}", text);
        // Null-Embedding signalisiert dass kein KI-Modell geladen ist
        return Task.FromResult(Array.Empty<float>());
    }

    /// <inheritdoc />
    public float Similarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length) return 0f;
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        var denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denom == 0 ? 0f : dot / denom;
    }
}