using FlipsiForge.Core.Models;

namespace FlipsiForge.Core.Services.Printing;

/// <summary>
/// Temperaturen eines Druckers (Hotend, Bed, optional Chamber).
/// </summary>
public sealed class PrinterTemps
{
    /// <summary>Hotend-Temperatur in °C.</summary>
    public decimal Hotend { get; init; }
    /// <summary>Druckbett-Temperatur in °C.</summary>
    public decimal Bed { get; init; }
    /// <summary>Kammer-Temperatur in °C (optional, nur bei Enclosed-Druckern).</summary>
    public decimal? Chamber { get; init; }
}

/// <summary>
/// Aktuell laufender Druck-Job.
/// </summary>
public sealed class PrinterJobInfo
{
    /// <summary>Dateiname des aktuellen Druck-Jobs.</summary>
    public string FileName { get; init; } = "";
    /// <summary>Fortschritt in Prozent (0–100).</summary>
    public decimal ProgressPercent { get; init; }
    /// <summary>Verstrichene Zeit in Sekunden.</summary>
    public int ElapsedSec { get; init; }
    /// <summary>Verbleibende Zeit in Sekunden.</summary>
    public int RemainingSec { get; init; }
}

/// <summary>
/// Abstraktion einer Drucker-Verbindung (Moonraker, Marlin, Bambu, PrusaLink, OctoPrint).
/// Jede Implementierung kapselt das jeweilige Protokoll (HTTP, Serial, MQTT).
/// Alle Methoden sind async und sollten niemals blockieren.
/// </summary>
public interface IPrinterConnection
{
    /// <summary>Aktueller Status des Druckers.</summary>
    Task<PrinterStatus> GetStatusAsync();

    /// <summary>Aktuelle Temperaturen (Hotend, Bed, Chamber).</summary>
    Task<PrinterTemps> GetTemperaturesAsync();

    /// <summary>Aktuell laufender Druck-Job oder null wenn Idle.</summary>
    Task<PrinterJobInfo?> GetCurrentJobAsync();

    /// <summary>
    /// Sendet eine G-code-Datei an den Drucker und startet ggf. den Druck.
    /// </summary>
    /// <param name="filePath">Lokaler Pfad zur G-code-Datei.</param>
    /// <param name="requireConfirmation">Wenn true, muss der User den Druck-Start manuell bestätigen.</param>
    /// <returns>true wenn erfolgreich gesendet.</returns>
    Task<bool> SendGcodeAsync(string filePath, bool requireConfirmation);

    /// <summary>Druck pausieren.</summary>
    Task<bool> PauseAsync();

    /// <summary>Druck fortsetzen.</summary>
    Task<bool> ResumeAsync();

    /// <summary>Druck abbrechen.</summary>
    Task<bool> CancelAsync();

    /// <summary>Verbindung zum Drucker aufbauen.</summary>
    Task<bool> ConnectAsync();

    /// <summary>Verbindung zum Drucker trennen.</summary>
    Task DisconnectAsync();
}