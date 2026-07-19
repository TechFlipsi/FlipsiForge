// FlipsiForge.Server — v0.2.0
// KI-Service-Verträge und Stub-Implementierungen.
// Die echte Implementierung in FlipsiForge.Core.Services nutzt
// Microsoft.ML.GenAI + ONNX Runtime für Gemma 4 E4B/E2B Chat.
//
// SPDX-License-Identifier: GPL-3.0-only
// (c) 2026 TechFlipsi / Fabian Kirchweger

using FlipsiForge.Core.Models;

namespace FlipsiForge.Server.Services;

/// <summary>
/// KI-Chat-Engine für den Drucker-Assistenten. Läuft lokal via ONNX Runtime
/// (Gemma 4 E4B/E2B) in Core.Services. Diese Stub-Version gibt kurze
/// Offline-Antworten zurück, sodass der Server auch ohne Modell kompiliert.
/// </summary>
public interface IAIChatEngine
{
    /// <summary>True wenn das Modell erfolgreich geladen wurde.</summary>
    bool IsLoaded { get; }

    /// <summary>Name des geladenen Modells (z.B. "gemma-4-e4b-q4").</summary>
    string ModelName { get; }

    /// <summary>Aktuelle Modellauswahl: "E4B", "E2B", "E2BQat" oder "Off".</summary>
    string Choice { get; }

    /// <summary>Erzeugt eine Chat-Antwort (nicht-streaming).</summary>
    /// <param name="message">User-Nachricht.</param>
    /// <param name="history">Bisheriger Chat-Verlauf (user/assistant Messages).</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Antwort-Text.</returns>
    Task<string> GenerateReplyAsync(string message, IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streamt eine Chat-Antwort token-weise (für SSE-Streaming wenn
    /// <c>AppSettings.AiStreaming</c> true ist).
    /// </summary>
    /// <param name="message">User-Nachricht.</param>
    /// <param name="history">Bisheriger Chat-Verlauf.</param>
    /// <param name="onToken">Callback pro Token/Chunk.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    IAsyncEnumerable<string> StreamReplyAsync(string message, IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken = default);
}

/// <summary>Embedding-Provider für semantische Datei-Suche (all-MiniLM-L6-v2).</summary>
public interface IEmbeddingProvider
{
    /// <summary>True wenn das Embedding-Modell geladen wurde.</summary>
    bool IsLoaded { get; }

    /// <summary>Erzeugt einen Embedding-Vektor für den gegebenen Text.</summary>
    /// <param name="text">Text der embedded wird (Dateiname + Tags + Notizen).</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Float-Array (dim=384 bei MiniLM-L6).</returns>
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Berechnet Cosine-Similarity zwischen zwei Vektoren.</summary>
    float Similarity(float[] a, float[] b);
}