// FlipsiForge.Server — v0.2.0
// Server-Stub-Interfaces und Fallback-Implementierungen für Core.Services
//
// Diese Datei definiert die Service-Verträge, die von FlipsiForge.Core.Services
// (parallel von einem anderen Subagenten gebaut) bereitgestellt werden.
// Falls die echten Core.Services-Implementierungen zur Build-Zeit nicht verfügbar
// sind, registriert Program.cs diese Stub-Implementierungen via TryAdd-Lifecycle,
// sodass das Server-Projekt eigenständig kompiliert und läuft.
//
// SPDX-License-Identifier: GPL-3.0-only
// (c) 2026 TechFlipsi / Fabian Kirchweger — https://techflipsi.kirchweger.de

using FlipsiForge.Core.Models;

namespace FlipsiForge.Server.Services;

/// <summary>
/// Verwaltet Live-Verbindungen zu 3D-Druckern über alle 5 Protokolle
/// (Moonraker, Marlin, Bambu, PrusaLink, OctoPrint). Die echte Implementierung
/// in FlipsiForge.Core.Services spricht Moonraker REST/WS, Bambu MQTT etc.
/// Diese Stub-Version liefert Offline-Werte, sodass der Server auch ohne
/// Drucker-Verbindung kompiliert und läuft.
/// </summary>
public interface IPrinterConnectionManager
{
    /// <summary>Testet ob der Drucker erreichbar ist.</summary>
    /// <param name="printer">Drucker-Profil aus der DB.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Verbindungs-Resultat mit Status und Fehlermeldung.</returns>
    Task<PrinterConnectionResult> ConnectAsync(Printer printer, CancellationToken cancellationToken = default);

    /// <summary>Liefert den aktuellen Live-Status eines Druckers.</summary>
    Task<PrinterLiveStatus> GetStatusAsync(Printer printer, CancellationToken cancellationToken = default);

    /// <summary>Liefert aktuelle Hotend-/Bed-Temperaturen.</summary>
    Task<PrinterTemperatures> GetTemperaturesAsync(Printer printer, CancellationToken cancellationToken = default);

    /// <summary>Liefert den aktuell laufenden Druck-Job (oder null).</summary>
    Task<PrinterJobInfo?> GetCurrentJobAsync(Printer printer, CancellationToken cancellationToken = default);
}

/// <summary>Resultat eines Verbindungsversuchs.</summary>
public sealed class PrinterConnectionResult
{
    /// <summary>True wenn der Drucker antwortet.</summary>
    public bool Connected { get; init; }
    /// <summary>Fehlermeldung falls <see cref="Connected"/> false ist.</summary>
    public string? Error { get; init; }
    /// <summary>Protokoll-spezifische Details (Firmware, Klipper-Version etc.).</summary>
    public string? Details { get; init; }
    /// <summary>Zeitpunkt der Abfrage.</summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Live-Status eines Druckers.</summary>
public sealed class PrinterLiveStatus
{
    public PrinterStatus Status { get; init; } = PrinterStatus.Offline;
    public float? ProgressPercent { get; init; }
    public int? CurrentLayer { get; init; }
    public int? TotalLayers { get; init; }
    public TimeSpan? EstimatedEta { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Aktuelle Temperaturen eines Druckers.</summary>
public sealed class PrinterTemperatures
{
    public float? HotendActual { get; init; }
    public float? HotendTarget { get; init; }
    public float? BedActual { get; init; }
    public float? BedTarget { get; init; }
    public float? ChamberActual { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Info über den aktuell laufenden Druck-Job eines Druckers.</summary>
public sealed class PrinterJobInfo
{
    public string? FileName { get; init; }
    public float? ProgressPercent { get; init; }
    public TimeSpan? EstimatedEta { get; init; }
    public int? CurrentLayer { get; init; }
    public int? TotalLayers { get; init; }
    public DateTime? StartedAt { get; init; }
}