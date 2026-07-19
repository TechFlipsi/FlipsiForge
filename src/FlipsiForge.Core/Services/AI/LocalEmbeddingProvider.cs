using System.Text.Json;
using FlipsiForge.Core.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FlipsiForge.Core.Services.AI;

/// <summary>
/// Lokale Embedding-Implementierung via Microsoft.ML.OnnxRuntime.
/// Lädt all-MiniLM-L6-v2 (384-dim) als ONNX-Modell und berechnet Embeddings für semantische Suche.
///
/// PITFALL: native ONNX-Runtime ist auf dem Build-Server evtl. nicht installiert (DllNotFoundException).
/// In dem Fall: IsLoaded=false, EmbedAsync liefert leeres Array. Code KOMPILE aber trotzdem.
/// Echte Inferenz läuft nur wenn onnxruntime.so/dll vorhanden.
/// </summary>
public sealed class LocalEmbeddingProvider : IEmbeddingProvider
{
    private InferenceSession? _session;
    private string _modelPath = "";
    private readonly object _lock = new();
    private bool _loadAttempted;

    /// <inheritdoc />
    public bool IsLoaded
    {
        get { lock (_lock) { return _session is not null; } }
    }

    /// <inheritdoc />
    public async Task InitializeAsync(string modelPath)
    {
        _modelPath = modelPath;
        _loadAttempted = true;
        await Task.Yield();

        try
        {
            if (!File.Exists(modelPath))
            {
                // Warten auf evtl. asynchronen Download
                return;
            }

            lock (_lock)
            {
                // SessionOptions mit CPU-EP — GPU-EP nur wenn DirectML/CUDA vorhanden
                var options = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
                };
                _session = new InferenceSession(modelPath, options);
            }
        }
        catch (DllNotFoundException)
        {
            // onnxruntime native lib fehlt → Stub-Modus
            lock (_lock) { _session = null; }
        }
        catch (FileNotFoundException)
        {
            lock (_lock) { _session = null; }
        }
        catch (Exception)
        {
            // Jeder andere Fehler → Stub-Modus
            lock (_lock) { _session = null; }
        }
    }

    /// <inheritdoc />
    public async Task<float[]> EmbedAsync(string text)
    {
        if (!IsLoaded || string.IsNullOrWhiteSpace(text))
            return Array.Empty<float>();

        try
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_session is null) return Array.Empty<float>();

                    // Sehr vereinfachte Tokenisierung — für Produktion korrekten Tokenizer verwenden
                    // (z.B. SharpToken or Microsoft.ML.Tokenizers). Hier: Wort-Level-Tokenization als Stub.
                    var tokens = SimpleTokenize(text, maxTokens: 128);
                    var inputIds = new DenseTensor<long>(new long[128], new[] { 1, 128 });
                    var attentionMask = new DenseTensor<long>(new long[128], new[] { 1, 128 });
                    for (var i = 0; i < tokens.Length && i < 128; i++)
                    {
                        inputIds[i] = tokens[i];
                        attentionMask[i] = 1;
                    }

                    var inputs = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                        NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask)
                    };

                    using var results = _session.Run(inputs);
                    // Embedding-Output: tensor [1, seq_len, 384] — mean-pool über seq_len
                    var output = results.FirstOrDefault()?.AsTensor<float>();
                    if (output is null) return Array.Empty<float>();

                    return MeanPool(output).ToArray();
                }
            }).ConfigureAwait(false);
        }
        catch
        {
            return Array.Empty<float>();
        }
    }

    /// <summary>
    /// Mean-Pooling über Sequence-Dimension.
    /// </summary>
    private static IEnumerable<float> MeanPool<T>(Tensor<T> tensor) where T : unmanaged
    {
        // Tensor shape: [1, seq, dim] — wir mitteln über seq
        var dims = tensor.Dimensions;
        if (dims.Length != 3) yield break;
        int seq = dims[1], dim = dims[2];
        for (int d = 0; d < dim; d++)
        {
            float sum = 0;
            for (int s = 0; s < seq; s++)
            {
                if (tensor[s, d] is float f) sum += f;
            }
            yield return sum / seq;
        }
    }

    /// <summary>
    /// Sehr einfache Hash-basierte Tokenisierung als Platzhalter.
    /// Für Produktion: BPE-Tokenizer (SharpToken oder Microsoft.ML.Tokenizers) verwenden.
    /// </summary>
    private static long[] SimpleTokenize(string text, int maxTokens)
    {
        var words = text.ToLowerInvariant().Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' },
            StringSplitOptions.RemoveEmptyEntries);
        var tokens = new long[Math.Min(words.Length, maxTokens)];
        for (var i = 0; i < tokens.Length; i++)
        {
            // Deterministic Hash als Platzhalter-Tokens-ID
            tokens[i] = Math.Abs(words[i].GetHashCode()) % 30000;
        }
        return tokens;
    }
}