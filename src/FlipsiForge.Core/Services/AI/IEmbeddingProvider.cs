using FlipsiForge.Core.Models;

namespace FlipsiForge.Core.Services.AI;

/// <summary>
/// Abstraktion eines Embedding-Providers (z.B. ONNX all-MiniLM-L6-v2, Ollama embeddings API).
/// Embeddings sind Vektoren für semantische / KI-gestützte Suche.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>True wenn ein Modell erfolgreich geladen wurde.</summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Lädt das Embedding-Modell vom angegebenen Pfad.
    /// Muss robust gegen DllNotFoundException / FileNotFoundException sein.
    /// </summary>
    Task InitializeAsync(string modelPath);

    /// <summary>
    /// Berechnet das Embedding (Vektor) für einen Text.
    /// Liefert null oder leeres Array wenn nicht geladen.
    /// </summary>
    Task<float[]> EmbedAsync(string text);
}