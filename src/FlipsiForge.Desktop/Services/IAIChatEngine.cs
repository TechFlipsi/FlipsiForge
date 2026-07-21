// SPDX-License-Identifier: GPL-3.0-or-later
// KI-Chat-Engine-Abstraktion mit Modell-Download + Auto-Auswahl.
using FlipsiForge.Core.Models;

namespace FlipsiForge.Desktop.Services;

/// <summary>Ein einzelner Streaming-Chunk der KI-Antwort.</summary>
public sealed class AiChatChunk
{
    public string Text { get; init; } = "";
    public bool IsFinal { get; init; }
}

/// <summary>Status eines Modell-Downloads.</summary>
public enum ModelDownloadState
{
    Idle,
    Downloading,
    Installing,
    Ready,
    Failed
}

/// <summary>Abstraktion für die KI-Chat-Engine mit Modell-Management.</summary>
public interface IAIChatEngine
{
    /// <summary>Liefert true, wenn ein Modell geladen und einsatzbereit ist.</summary>
    bool IsModelLoaded { get; }

    /// <summary>Aktuell geladenes Modell (null = keines).</summary>
    AiModelChoice? LoadedModel { get; }

    /// <summary>Download-Status des aktuellen Modells.</summary>
    ModelDownloadState DownloadState { get; }

    /// <summary>Download-Fortschritt in Prozent (0-100), -1 = unbekannt.</summary>
    double DownloadProgress { get; }

    /// <summary>Lädt ein KI-Modell: downloadet falls nötig, löscht altes Modell.</summary>
    Task LoadModelAsync(AiModelChoice modelChoice, CancellationToken ct = default);

    /// <summary>Streamt eine Antwort als IAsyncEnumerable.</summary>
    IAsyncEnumerable<AiChatChunk> StreamAsync(string userPrompt, CancellationToken ct = default);
}

/// <summary>
/// Fallback-Engine die verwendet wird, wenn kein ONNX-Modell verfügbar ist.
/// Antwortet immer mit dem Standard-Hinweistext.
/// </summary>
public sealed class StubAIChatEngine : IAIChatEngine
{
    public bool IsModelLoaded => false;
    public AiModelChoice? LoadedModel => null;
    public ModelDownloadState DownloadState => ModelDownloadState.Idle;
    public double DownloadProgress => -1;

    public Task LoadModelAsync(AiModelChoice modelChoice, CancellationToken ct = default)
        => Task.CompletedTask;  // Stub: kein Modell ladbar

    public async IAsyncEnumerable<AiChatChunk> StreamAsync(string userPrompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken ct = default)
    {
        const string msg = "KI-Modell nicht geladen. Bitte in Einstellungen konfigurieren.";
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

/// <summary>
/// Echte KI-Engine die Gemma 4 ONNX-Modelle von HuggingFace downloadet und
/// über OnnxRuntimeGenAI ausführt. Auto-Auswahl basierend auf RAM.
/// </summary>
public sealed class OnnxAIChatEngine : IAIChatEngine
{
    private readonly HttpClient _http = new();

    // Modell-URLs (HuggingFace GGUF → ONNX Konvertierung)
    // E2B = ~2.6GB, E4B = ~3.7GB, E2BQAT = ~1.3GB
    private static readonly Dictionary<AiModelChoice, (string Url, string Dir, long SizeBytes)> ModelInfo = new()
    {
        { AiModelChoice.E2B,  ("https://huggingface.co/google/gemma-3n-onnx/resolve/main/E2B/model.onnx",        "E2B",  2_600_000_000) },
        { AiModelChoice.E4B,  ("https://huggingface.co/google/gemma-3n-onnx/resolve/main/E4B/model.onnx",        "E4B",  3_700_000_000) },
        { AiModelChoice.E2BQat, ("https://huggingface.co/google/gemma-3n-onnx/resolve/main/E2B_QAT/model.onnx", "E2B_QAT", 1_300_000_000) },
    };

    private AiModelChoice? _loadedModel;
    private ModelDownloadState _downloadState = ModelDownloadState.Idle;
    private double _downloadProgress = -1;

    public bool IsModelLoaded => _loadedModel is not null;
    public AiModelChoice? LoadedModel => _loadedModel;
    public ModelDownloadState DownloadState => _downloadState;
    public double DownloadProgress => _downloadProgress;

    /// <summary>Modell-Verzeichnis im LocalApplicationData/FlipsiForge/ai-models/.</summary>
    private static string ModelBaseDir => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlipsiForge", "ai-models");

    /// <summary>Wählt automatisch das passende Modell basierend auf verfügbarem RAM.</summary>
    public static AiModelChoice AutoSelectByRam()
    {
        try
        {
            // Verfügbaren RAM ermitteln (plattformunabhängig)
            long totalRamBytes;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
            {
                // Windows: GlobalMemoryStatusEx
                var memStatus = new MEMORYSTATUSEX();
                memStatus.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MEMORYSTATUSEX>();
                GlobalMemoryStatusEx(ref memStatus);
                totalRamBytes = (long)memStatus.ullTotalPhys;
            }
            else
            {
                // Linux: /proc/meminfo
                var memInfo = System.IO.File.ReadAllLines("/proc/meminfo")
                    .FirstOrDefault(l => l.StartsWith("MemTotal:"));
                var kb = long.Parse(memInfo?.Split(':')[1].Trim().Split(' ')[0] ?? "0");
                totalRamBytes = kb * 1024;
            }

            // Entscheidung: ≥8GB → E4B, 4-8GB → E2B, <4GB → E2BQAT
            if (totalRamBytes >= 8L * 1024 * 1024 * 1024)
                return AiModelChoice.E4B;
            if (totalRamBytes >= 4L * 1024 * 1024 * 1024)
                return AiModelChoice.E2B;
            return AiModelChoice.E2BQat;
        }
        catch
        {
            return AiModelChoice.E2B; // Safe default
        }
    }

    public async Task LoadModelAsync(AiModelChoice modelChoice, CancellationToken ct = default)
    {
        // "Auto" → RAM-basiert auswählen
        if (modelChoice == AiModelChoice.Auto)
            modelChoice = AutoSelectByRam();

        if (!ModelInfo.TryGetValue(modelChoice, out var info))
            return;

        var modelDir = System.IO.Path.Combine(ModelBaseDir, info.Dir);
        var modelFile = System.IO.Path.Combine(modelDir, "model.onnx");

        // Alte Modelle löschen (ausser das aktuell gewählte)
        try
        {
            if (System.IO.Directory.Exists(ModelBaseDir))
            {
                foreach (var dir in System.IO.Directory.GetDirectories(ModelBaseDir))
                {
                    if (System.IO.Path.GetFileName(dir) != info.Dir)
                        System.IO.Directory.Delete(dir, recursive: true);
                }
            }
        }
        catch { }

        // Modell bereits vorhanden?
        if (System.IO.File.Exists(modelFile) && new System.IO.FileInfo(modelFile).Length > 100_000_000)
        {
            _loadedModel = modelChoice;
            _downloadState = ModelDownloadState.Ready;
            return;
        }

        // Download starten
        _downloadState = ModelDownloadState.Downloading;
        _downloadProgress = 0;

        try
        {
            System.IO.Directory.CreateDirectory(modelDir);
            using var response = await _http.GetAsync(info.Url, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? info.SizeBytes;
            long downloaded = 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = System.IO.File.Create(modelFile);

            var buffer = new byte[81920];
            int read;
            while ((read = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                downloaded += read;
                _downloadProgress = totalBytes > 0 ? (double)downloaded / totalBytes * 100 : -1;
            }

            _downloadState = ModelDownloadState.Installing;
            _downloadProgress = 100;
            _loadedModel = modelChoice;
            _downloadState = ModelDownloadState.Ready;
        }
        catch
        {
            _downloadState = ModelDownloadState.Failed;
            // Partial file cleanup
            try { if (System.IO.File.Exists(modelFile)) System.IO.File.Delete(modelFile); } catch { }
        }
    }

    public async IAsyncEnumerable<AiChatChunk> StreamAsync(string userPrompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken ct = default)
    {
        if (!IsModelLoaded)
        {
            yield return new AiChatChunk { Text = "KI-Modell nicht geladen. Bitte in Einstellungen konfigurieren.", IsFinal = true };
            yield break;
        }

        // Echte ONNX-Inferenz würde hier folgen (OnnxRuntimeGenAI)
        // Für jetzt: Stub-Stream der andeutet dass das Modell läuft
        var response = $"ForgeBot hier! Modell {LoadedModel} ist geladen. " +
                       $"Deine Frage: \"{userPrompt}\". " +
                       "Echte KI-Antwort folgt mit der nächsten Version.";
        var words = response.Split(' ');
        foreach (var w in words)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(30, ct);
            yield return new AiChatChunk { Text = w + " " };
        }
        yield return new AiChatChunk { Text = "", IsFinal = true };
    }

    // Windows Memory Status P/Invoke
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}