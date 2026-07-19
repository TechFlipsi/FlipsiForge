using System.Runtime.CompilerServices;
using FlipsiForge.Core.Models;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace FlipsiForge.Core.Services.AI;

/// <summary>
/// Chat-Engine via Microsoft.ML.OnnxRuntimeGenAI (Managed) v0.14.1.
/// Lädt Gemma 3 E4B / E2B / E2B-Quant als ONNX-Modell und macht lokale Inferenz.
///
/// API-Flow (OnnxRuntimeGenAI 0.14.1):
///   1. new Config(modelPath) → new Model(config) → new Tokenizer(model)
///   2. tokenizer.Encode(prompt) → Sequences
///   3. new GeneratorParams(model) → SetSearchOption("max_length", …)
///   4. new Generator(model, generatorParams)
///   5. generator.AppendTokenSequences(sequences)
///   6. while (!generator.IsDone()) { generator.GenerateNextToken(); var token = generator.GetNextTokens()[0]; stream.Decode(token); }
///
/// PITFALL — Native Deps:
///   - Benötigt onnxruntime.dll/so + onnxruntime-genai.dll/so
///   - Auf Build-Server NICHT vorhanden → DllNotFoundException zur Laufzeit
///   - Lösung: try/catch um ALLES, IsLoaded=false als Stub-Modus
///   - Code KOMPILE gegen das NuGet-Paket (Managed Wrapper), aber läuft nur
///     wenn native libs installiert sind.
///
/// Alle Public-Methoden sind robust gegen fehlende Dlls und fehlende Modell-Dateien.
/// </summary>
public sealed class OnnxGenAiChatEngine : AiChatEngineBase
{
    private Model? _model;
    private Tokenizer? _tokenizer;
    private string _modelPath = "";
    private AiModelChoice _choice = AiModelChoice.Auto;
    private readonly object _lock = new();

    /// <inheritdoc />
    public override bool IsLoaded
    {
        get { lock (_lock) { return _model is not null; } }
    }

    /// <inheritdoc />
    public override string ModelName
    {
        get
        {
            lock (_lock)
            {
                if (_model is null) return "n/a";
                if (!string.IsNullOrEmpty(_modelPath))
                    return Path.GetFileName(_modelPath.TrimEnd('/', '\\'));
                return _choice.ToString();
            }
        }
    }

    /// <inheritdoc />
    public override async Task InitializeAsync(string modelPath, AiModelChoice choice)
    {
        _modelPath = modelPath;
        _choice = choice;
        await Task.Yield();

        try
        {
            if (!Directory.Exists(modelPath))
            {
                lock (_lock) { _model = null; _tokenizer = null; }
                return;
            }

            lock (_lock)
            {
                // Config sucht model.onnx + config.json + tokenizer.json im Verzeichnis
                var config = new Config(modelPath);
                _model = new Model(config);
                _tokenizer = new Tokenizer(_model);
            }
        }
        catch (DllNotFoundException)
        {
            // onnxruntime-genai native lib fehlt
            lock (_lock) { _model = null; _tokenizer = null; }
        }
        catch (FileNotFoundException)
        {
            // Modell-Datei fehlt
            lock (_lock) { _model = null; _tokenizer = null; }
        }
        catch (DirectoryNotFoundException)
        {
            lock (_lock) { _model = null; _tokenizer = null; }
        }
        catch (Exception)
        {
            // Jeder andere Fehler → Stub-Modus
            lock (_lock) { _model = null; _tokenizer = null; }
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> StreamChatAsync(
        List<ChatMessage> history, string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsLoaded)
        {
            await foreach (var tok in StubStream().WithCancellation(cancellationToken).ConfigureAwait(false))
                yield return tok;
            yield break;
        }

        Tokenizer? tokenizer;
        Model? model;
        lock (_lock) { tokenizer = _tokenizer; model = _model; }
        if (tokenizer is null || model is null) yield break;

        // Alles in try/catch kapseln, aber yield muss außerhalb des catch-Blocks erfolgen.
        // Daher sammeln wir Fehler als out-Variable und yield-en nach dem try.
        string? errorMessage = null;
        var tokenQueue = new Queue<string>();

        try
        {
            var prompt = BuildFullPrompt(history, userMessage);
            using var generatorParams = new GeneratorParams(model);
            // Sampling-Optionen für Gemma 3
            generatorParams.SetSearchOption("max_length", 1024);
            generatorParams.SetSearchOption("temperature", 0.7);
            generatorParams.SetSearchOption("top_p", 0.9);

            var sequences = tokenizer.Encode(prompt);
            using var generator = new Generator(model, generatorParams);
            generator.AppendTokenSequences(sequences);

            using var stream = tokenizer.CreateStream();

            while (!generator.IsDone())
            {
                cancellationToken.ThrowIfCancellationRequested();
                generator.GenerateNextToken();
                var nextTokens = generator.GetNextTokens();
                if (nextTokens.Length == 0) break;
                var lastToken = nextTokens[0];
                var decoded = stream.Decode(lastToken);
                if (!string.IsNullOrEmpty(decoded))
                    tokenQueue.Enqueue(decoded);
            }
        }
        catch (DllNotFoundException)
        {
            lock (_lock) { _model = null; _tokenizer = null; }
            errorMessage = NotLoadedHint;
        }
        catch (OperationCanceledException)
        {
            // Abbruch durch Caller — ruhig beenden
        }
        catch (Exception ex)
        {
            errorMessage = $"[KI-Fehler: {ex.Message}]";
        }

        // Außerhalb des try-Blocks: Tokens yield-en
        while (tokenQueue.Count > 0)
        {
            yield return tokenQueue.Dequeue();
            await Task.Yield();
        }

        if (errorMessage is not null)
            yield return errorMessage;
    }

    /// <inheritdoc />
    public override async Task<string> CompleteChatAsync(List<ChatMessage> history, string userMessage)
    {
        if (!IsLoaded) return NotLoadedHint;

        var sb = new System.Text.StringBuilder();
        try
        {
            await foreach (var tok in StreamChatAsync(history, userMessage).ConfigureAwait(false))
            {
                sb.Append(tok);
            }
            return sb.ToString();
        }
        catch (DllNotFoundException)
        {
            lock (_lock) { _model = null; _tokenizer = null; }
            return NotLoadedHint;
        }
        catch (Exception ex)
        {
            return $"[KI-Fehler: {ex.Message}]";
        }
    }
}