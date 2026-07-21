// SPDX-License-Identifier: GPL-3.0-or-later
// LoggerService: Thread-sicherer Datei-Logger fuer FlipsiForge Desktop.
// Schreibt in LocalApplicationData/FlipsiForge/logs/:
//   app.log    — alle Meldungen (Info, Warning, Error, Crash)
//   ki.log     — KI-spezifische Meldungen (gefiltert)
//   crash.log  — nur Crashes (LogCrash)
// Rotation: max 5MB pro Datei, danach umbenennen in .old (ueberschreibt bestehende .old).
using System.Text;

namespace FlipsiForge.Desktop.Services;

/// <summary>Log-Level fuer <see cref="LoggerService"/>.</summary>
public enum LogLevel
{
    Info,
    Warning,
    Error,
    Crash,
    Ki
}

/// <summary>
/// Thread-sicherer statischer Logger der in Dateien schreibt.
/// Verwendet ein Lock pro Datei, sodass parallele Schreibvorgaenge sicher sind.
/// Dateien werden in <c>LocalApplicationData/FlipsiForge/logs/</c> abgelegt.
/// Rotation bei 5MB: <c>app.log</c> → <c>app.log.old</c> (ueberschreibt bestehende .old).
/// </summary>
public static class LoggerService
{
    /// <summary>Maximale Dateigroesse vor Rotation (5 MB).</summary>
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    /// <summary>Basis-Verzeichnis fuer alle Log-Dateien.</summary>
    private static readonly string LogDirectory = InitializeLogDirectory();

    /// <summary>Vollstaendige Pfade der drei Log-Dateien.</summary>
    private static readonly string AppLogPath = Path.Combine(LogDirectory, "app.log");
    private static readonly string KiLogPath = Path.Combine(LogDirectory, "ki.log");
    private static readonly string CrashLogPath = Path.Combine(LogDirectory, "crash.log");

    /// <summary>Locks pro Datei (jeweils ein eigenes Lock → parallele Schreibvorgaenge in verschiedene Dateien sind moeglich).</summary>
    private static readonly object AppLock = new();
    private static readonly object KiLock = new();
    private static readonly object CrashLock = new();

    /// <summary>Initialisiert das Log-Verzeichnis und liefert den Pfad zurueck.</summary>
    private static string InitializeLogDirectory()
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FlipsiForge", "logs");
            Directory.CreateDirectory(dir);
            return dir;
        }
        catch
        {
            // Fallback: temp directory
            return Path.GetTempPath();
        }
    }

    /// <summary>Liefert das Log-Verzeichnis (fuer "Logs anzeigen" Button).</summary>
    public static string GetLogDirectory() => LogDirectory;

    /// <summary>Info-Meldung loggen (app.log).</summary>
    public static void LogInfo(string message, string? source = null)
        => WriteLog(AppLogPath, AppLock, "INFO", message, source);

    /// <summary>Warnung loggen (app.log).</summary>
    public static void LogWarning(string message, string? source = null)
        => WriteLog(AppLogPath, AppLock, "WARN", message, source);

    /// <summary>Fehler loggen (app.log).</summary>
    public static void LogError(string message, string? source = null, Exception? ex = null)
    {
        var full = ex != null ? $"{message}{Environment.NewLine}Exception: {ex}" : message;
        WriteLog(AppLogPath, AppLock, "ERROR", full, source);
    }

    /// <summary>Crash loggen (app.log + crash.log).</summary>
    public static void LogCrash(string message, Exception? ex = null, string? source = null)
    {
        var full = ex != null ? $"{message}{Environment.NewLine}Exception: {ex}" : message;
        WriteLog(AppLogPath, AppLock, "CRASH", full, source);
        WriteLog(CrashLogPath, CrashLock, "CRASH", full, source);
    }

    /// <summary>KI-spezifische Meldung loggen (app.log + ki.log).</summary>
    public static void LogKi(string message, string? source = null)
    {
        WriteLog(AppLogPath, AppLock, "KI", message, source);
        WriteLog(KiLogPath, KiLock, "KI", message, source);
    }

    /// <summary>Schreibt eine formatierte Zeile in die angegebene Datei (thread-safe).</summary>
    private static void WriteLog(string filePath, object lockObj, string level, string message, string? source)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var sourceStr = source != null ? $" [{source}]" : "";
            var line = $"{timestamp} [{level}]{sourceStr} {message}{Environment.NewLine}";

            lock (lockObj)
            {
                // Rotation: wenn Datei zu gross, zu .old umbenennen
                RotateIfNeeded(filePath);

                File.AppendAllText(filePath, line, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging darf nie die App zum Absturz bringen
        }
    }

    /// <summary>Prueft Dateigroesse und rotiert zu .old wenn noetig.</summary>
    private static void RotateIfNeeded(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return;
            var info = new FileInfo(filePath);
            if (info.Length < MaxFileSizeBytes) return;

            var oldPath = filePath + ".old";
            // Bestehende .old-Datei loeschen (ueberschreiben)
            if (File.Exists(oldPath))
                File.Delete(oldPath);
            File.Move(filePath, oldPath);
        }
        catch
        {
            // Rotation-Fehler nicht fatal
        }
    }

    /// <summary>Liefert die letzten N Zeilen einer Log-Datei (fuer Debug-Anzeige).</summary>
    public static string GetRecentLogLines(string fileName, int maxLines = 50)
    {
        try
        {
            var path = Path.Combine(LogDirectory, fileName);
            if (!File.Exists(path)) return "";
            var lines = File.ReadLines(path);
            return string.Join(Environment.NewLine, lines.TakeLast(maxLines));
        }
        catch
        {
            return "";
        }
    }
}