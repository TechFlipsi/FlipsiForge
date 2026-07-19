using System.IO.Ports;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Core.Services.Printing;

/// <summary>
/// Marlin-Verbindung über seriellen USB-Port.
/// G-code-Befehle:
///   M105 — Temperaturen abfragen (Antwort: "ok T:210.0 /210.0 B:60.0 /60.0")
///   M24 — Druck fortsetzen (resume)
///   M25 — Druck pausieren
///   M0  — Druck stoppen
///   M23 &lt;path&gt; — Datei auswählen; M24 — Druck starten
/// Native Abhängigkeit: System.IO.Ports (funktioniert nur wenn /dev/ttyUSB* oder COM-Port vorhanden).
/// Build-Server hat keine seriellen Ports → ConnectAsync gibt false zurück, alle anderen
/// Methoden liefern Offline / 0-Temps. Build KOMPILE aber trotzdem.
/// </summary>
public sealed class MarlinConnection : IPrinterConnection
{
    private SerialPort? _port;
    private readonly string _portName;
    private readonly int _baudRate;
    private readonly object _lock = new();
    private bool _isConnected;

    /// <summary>
    /// Erzeugt eine Marlin-Verbindung.
    /// </summary>
    /// <param name="portName">z.B. /dev/ttyUSB0 oder COM3.</param>
    /// <param name="baudRate">Typisch 115200.</param>
    public MarlinConnection(string portName, int baudRate = 115200)
    {
        _portName = portName;
        _baudRate = baudRate;
    }

    /// <summary>
    /// Sendet G-code an den Drucker und liefert die Antwort-Zeile.
    /// Liefert null bei Fehler/keine Verbindung.
    /// </summary>
    private async Task<string?> SendGcodeRawAsync(string command)
    {
        lock (_lock)
        {
            if (_port is null || !_port.IsOpen) return null;
        }

        try
        {
            // SerialPort-Zugriff ist blockierend — in Task.Run kapseln
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_port is null || !_port.IsOpen) return null;
                    _port.WriteLine(command);
                    // Marlin antwortet mit "ok ..." — wir warten bis zu 3 Sekunden
                    var deadline = DateTime.UtcNow.AddSeconds(3);
                    while (DateTime.UtcNow < deadline)
                    {
                        var line = _port.ReadLine();
                        if (line.StartsWith("ok", StringComparison.OrdinalIgnoreCase) ||
                            line.StartsWith("T:", StringComparison.OrdinalIgnoreCase))
                            return line;
                    }
                    return null;
                }
            }).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PrinterStatus> GetStatusAsync()
    {
        if (!_isConnected) return PrinterStatus.Offline;
        // Marlin hat keinen direkten Status-Befehl — M27 liefert SD-Status
        var reply = await SendGcodeRawAsync("M27").ConfigureAwait(false);
        if (reply is null) return PrinterStatus.Offline;
        if (reply.Contains("Not SD printing", StringComparison.OrdinalIgnoreCase))
            return PrinterStatus.Idle;
        return PrinterStatus.Printing;
    }

    /// <inheritdoc />
    public async Task<PrinterTemps> GetTemperaturesAsync()
    {
        var reply = await SendGcodeRawAsync("M105").ConfigureAwait(false);
        if (reply is null) return new PrinterTemps { Hotend = 0, Bed = 0 };

        // Parse: "ok T:210.0 /210.0 B:60.0 /60.0 @:0 B@:0"
        decimal hotend = 0, bed = 0;
        decimal? chamber = null;
        var parts = reply.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            if (p.StartsWith("T:", StringComparison.OrdinalIgnoreCase))
            {
                var v = p[2..].Split('/');
                if (v.Length > 0 && decimal.TryParse(v[0], out var t)) hotend = t;
            }
            else if (p.StartsWith("B:", StringComparison.OrdinalIgnoreCase))
            {
                var v = p[2..].Split('/');
                if (v.Length > 0 && decimal.TryParse(v[0], out var b)) bed = b;
            }
            else if (p.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
            {
                var v = p[2..].Split('/');
                if (v.Length > 0 && decimal.TryParse(v[0], out var c)) chamber = c;
            }
        }
        return new PrinterTemps { Hotend = hotend, Bed = bed, Chamber = chamber };
    }

    /// <inheritdoc />
    public async Task<PrinterJobInfo?> GetCurrentJobAsync()
    {
        // Marlin hat keine Job-Info via G-code — nur M27 "SD printing byte X/Y"
        var reply = await SendGcodeRawAsync("M27").ConfigureAwait(false);
        if (reply is null || reply.Contains("Not SD printing", StringComparison.OrdinalIgnoreCase))
            return null;
        // Stub: keine echte Job-Info von Marlin
        return new PrinterJobInfo { FileName = "", ProgressPercent = 0, ElapsedSec = 0, RemainingSec = 0 };
    }

    /// <inheritdoc />
    public async Task<bool> SendGcodeAsync(string filePath, bool requireConfirmation)
    {
        // M23 <filename> wählt Datei, M24 startet Druck
        var fileName = Path.GetFileName(filePath);
        await SendGcodeRawAsync($"M23 {fileName}").ConfigureAwait(false);
        if (requireConfirmation) return true; // Datei gewählt, User muss bestätigen
        var ok = await SendGcodeRawAsync("M24").ConfigureAwait(false);
        return ok != null;
    }

    /// <inheritdoc />
    public Task<bool> PauseAsync() => SendGcodeRawAsync("M25").ContinueWith(t => t.Result != null, TaskScheduler.Default);

    /// <inheritdoc />
    public Task<bool> ResumeAsync() => SendGcodeRawAsync("M24").ContinueWith(t => t.Result != null, TaskScheduler.Default);

    /// <inheritdoc />
    public Task<bool> CancelAsync() => SendGcodeRawAsync("M0").ContinueWith(t => t.Result != null, TaskScheduler.Default);

    /// <inheritdoc />
    public Task<bool> ConnectAsync()
    {
        try
        {
            // System.IO.Ports native lib fehlt auf Build-Server → Exception fangen
            _port = new SerialPort(_portName, _baudRate)
            {
                ReadTimeout = 3000,
                WriteTimeout = 3000,
                DtrEnable = true,
                RtsEnable = true
            };
            _port.Open();
            _isConnected = _port.IsOpen;
            return Task.FromResult(_isConnected);
        }
        catch
        {
            // Erwartet auf Build-Server (kein /dev/ttyUSB*)
            _isConnected = false;
            _port = null;
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync()
    {
        try
        {
            lock (_lock)
            {
                if (_port is not null && _port.IsOpen)
                    _port.Close();
                _port?.Dispose();
                _port = null;
            }
        }
        catch { /* ignore */ }
        _isConnected = false;
        return Task.CompletedTask;
    }
}