using FlipsiForge.Core.Models;

namespace FlipsiForge.Core.Services.AI;

/// <summary>
/// Abstraktion des KI-Chat-Engines (z.B. OnnxRuntimeGenAI, Ollama, OpenAI-Proxy).
/// Alle Implementierungen MÜSSEN robust gegen fehlende native Libraries sein —
/// <see cref="IsLoaded"/> = false bedeutet Stub-Modus, Methoden liefern Hinweise-Strings.
/// </summary>
public interface IAIChatEngine
{
    /// <summary>True wenn ein Modell erfolgreich geladen wurde.</summary>
    bool IsLoaded { get; }

    /// <summary>Name des geladenen Modells (oder "n/a" wenn nicht geladen).</summary>
    string ModelName { get; }

    /// <summary>
    /// Lädt das KI-Modell vom angegebenen Pfad.
    /// Muss robust gegen DllNotFoundException / FileNotFoundException sein
    /// (Stub-Modus mit IsLoaded=false bei Fehler).
    /// </summary>
    /// <param name="modelPath">Verzeichnis mit model.onnx + config.json etc.</param>
    /// <param name="choice">Gewünschtes Modell (E4B, E2B, etc.).</param>
    Task InitializeAsync(string modelPath, AiModelChoice choice);

    /// <summary>
    /// Streaming-Chat — Token für Token via IAsyncEnumerable.
    /// </summary>
    /// <param name="history">Bisheriger Chat-Verlauf.</param>
    /// <param name="userMessage">Neue User-Nachricht.</param>
    /// <param name="cancellationToken">Abbruch-Token (optional).</param>
    /// <returns>Token-Stream.</returns>
    // Hinweis (Server-Subagent, 19.07.2026): CancellationToken-Default ergänzt,
    // damit AiChatEngineBase + OnnxGenAiChatEngine das override mit
    // [EnumeratorCancellation] sauber definieren können.
    IAsyncEnumerable<string> StreamChatAsync(
        List<ChatMessage> history, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Komplette Chat-Antwort (blockierend bis alles fertig).
    /// </summary>
    /// <param name="history">Bisheriger Chat-Verlauf.</param>
    /// <param name="userMessage">Neue User-Nachricht.</param>
    /// <returns>Antwort-Text.</returns>
    Task<string> CompleteChatAsync(List<ChatMessage> history, string userMessage);
}