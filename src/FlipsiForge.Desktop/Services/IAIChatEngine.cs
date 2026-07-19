// SPDX-License-Identifier: GPL-3.0-or-later
// Desktop-interne KI-Chat-Engine-Abstraktion (Stub-Fallback).
namespace FlipsiForge.Desktop.Services;

/// <summary>Ein einzelner Streaming-Chunk der KI-Antwort.</summary>
public sealed class AiChatChunk
{
    public string Text { get; init; } = "";
    public bool IsFinal { get; init; }
}

/// <summary>
/// Abstraktion für die KI-Chat-Engine. Im Desktop existiert ein
/// <see cref="StubAIChatEngine"/> der konstant "nicht geladen" antwortet,
/// sobald der Core.Services-Teil (der echte LlamaEdge-Adapter) fehlt.
/// </summary>
public interface IAIChatEngine
{
    /// <summary>Liefert true, wenn ein Modell geladen und einsatzbereit ist.</summary>
    bool IsModelLoaded { get; }

    /// <summary>Streamt eine Antwort als <see cref="IAsyncEnumerable{T}"/> (C# 8).</summary>
    IAsyncEnumerable<AiChatChunk> StreamAsync(string userPrompt, CancellationToken ct = default);
}

/// <summary>
/// Fallback-Engine die verwendet wird, wenn kein Modell geladen ist oder
/// Core.Services (noch) nicht verfügbar ist. Antwortet immer mit dem
/// Standard-Hinweistext.
/// </summary>
public sealed class StubAIChatEngine : IAIChatEngine
{
    /// <inheritdoc />
    public bool IsModelLoaded => false;

    /// <inheritdoc />
    public async IAsyncEnumerable<AiChatChunk> StreamAsync(string userPrompt, [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken ct = default)
    {
        const string msg = "KI-Modell nicht geladen. Bitte in Einstellungen konfigurieren.";
        // Simuliere Streaming: Wort für Wort
        var words = msg.Split(' ');
        foreach (var w in words)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(20, ct);
            yield return new AiChatChunk { Text = w + " " };
        }
        yield return new AiChatChunk { Text = "", IsFinal = true };
    }
}