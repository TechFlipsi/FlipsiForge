using System.Text;
using System.Text.Json;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Core.Services.AI;

/// <summary>
/// Gemeinsame Basis-Logik für alle Chat-Engine-Implementierungen.
/// Stellt den System-Prompt für den 3D-Druck-Experten bereit und
/// formatiert die Chat-History für das jeweilige Backend.
/// </summary>
public abstract class AiChatEngineBase : IAIChatEngine
{
    /// <inheritdoc />
    public abstract bool IsLoaded { get; }

    /// <inheritdoc />
    public abstract string ModelName { get; }

    /// <inheritdoc />
    public abstract Task InitializeAsync(string modelPath, AiModelChoice choice);

    /// <inheritdoc />
    // Hinweis (Server-Subagent, 19.07.2026): CancellationToken-Default hinzugefügt,
    // damit OnnxGenAiChatEngine das override mit [EnumeratorCancellation] sauber
    // definieren kann. Aufrufe ohne Token bleiben kompatibel (default).
    public abstract IAsyncEnumerable<string> StreamChatAsync(
        List<ChatMessage> history, string userMessage, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<string> CompleteChatAsync(List<ChatMessage> history, string userMessage);

    /// <summary>
    /// System-Prompt — 3D-Druck-Experte für FlipsiForge.
    /// Deutsch, technisch, auf FDM-Druck fokussiert (PLA/PETG/ABS/TPU).
    /// </summary>
    protected const string SystemPrompt3DPrinting =
        """
        Du bist der FlipsiForge-KI-Assistent — ein erfahrener 3D-Druck-Experte.
        Du hilfst Anwendern bei Fragen zu:
        - Filament-Auswahl und Druck-Einstellungen (PLA, PETG, ABS, ASA, TPU, PC, PA6, ...)
        - Slicer-Profilen (PrusaSlicer, OrcaSlicer, Cura)
        - Drucker-Problemen (Warping, Stringing, Under-extrusion, Layer-Shift, ...)
        - Wartung (Nozzle, Bed-Leveling, Belt-Tension, PTFE-Tube, ...)
        - Kostenschätzung und Druckzeit-Schätzung

        Antwort-Regeln:
        - Antworte in der Sprache des Users (default Deutsch).
        - Sei konkret und praxisnah — nutze echte Zahlen wo möglich.
        - Bei Unsicherheit: klar kennzeichnen und Empfehlung mit Begründung geben.
        - Bei kritischen Sicherheitsfragen (z.B. ABS-Dämpfe, Lithium-Batterien): Warnung aussprechen.
        - Keine Floskeln — direkt auf die Frage eingehen.
        - Wenn Du etwas nicht weißt: sag es ehrlich statt raten.
        """;

    /// <summary>
    /// Hinweis-String wenn kein Modell geladen ist.
    /// </summary>
    protected const string NotLoadedHint =
        "KI-Modell nicht geladen — bitte Modell im Einstellungen → KI-Assistent konfigurieren.";

    /// <summary>
    /// Baut den vollständigen Prompt aus System-Prompt + History + neue User-Nachricht.
    /// </summary>
    /// <param name="history">Chat-History.</param>
    /// <param name="userMessage">Neue User-Nachricht.</param>
    /// <returns>Kompletter Prompt-String.</returns>
    protected virtual string BuildFullPrompt(List<ChatMessage> history, string userMessage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<|system|>");
        sb.AppendLine(SystemPrompt3DPrinting);
        sb.AppendLine("<|end|>");

        foreach (var msg in history)
        {
            var roleTag = msg.Role?.ToLowerInvariant() switch
            {
                "assistant" => "<|assistant|>",
                "system" => "<|system|>",
                _ => "<|user|>"
            };
            sb.AppendLine(roleTag);
            sb.AppendLine(msg.Content);
            sb.AppendLine("<|end|>");
        }

        sb.AppendLine("<|user|>");
        sb.AppendLine(userMessage);
        sb.AppendLine("<|end|>");
        sb.AppendLine("<|assistant|>");
        return sb.ToString();
    }

    /// <summary>
    /// Konvertiert die Chat-History in eine Gemma-3-kompatible Message-Liste
    /// (für Tokenizer-ApplyChatTemplate falls das Backend das unterstützt).
    /// </summary>
    protected static IEnumerable<(string Role, string Content)> ToMessageTuples(
        List<ChatMessage> history, string userMessage)
    {
        yield return ("system", SystemPrompt3DPrinting);
        foreach (var m in history)
        {
            var role = m.Role?.ToLowerInvariant() switch
            {
                "assistant" => "assistant",
                "system" => "system",
                _ => "user"
            };
            yield return (role, m.Content);
        }
        yield return ("user", userMessage);
    }

    /// <summary>
    /// Stub-Streaming-Output wenn kein Modell geladen: liefert den Hinweis-String
    /// als einziges Token.
    /// </summary>
    protected static async IAsyncEnumerable<string> StubStream()
    {
        await Task.Yield();
        yield return NotLoadedHint;
    }
}