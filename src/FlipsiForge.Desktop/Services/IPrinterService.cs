// SPDX-License-Identifier: GPL-3.0-or-later
// Desktop-Abstraktion für Drucker-Verbindungs-Checks. Stub gibt immer Offline
// zurück, solange der echte PrinterService in Core.Services fehlt.
using FlipsiForge.Core.Models;

namespace FlipsiForge.Desktop.Services;

/// <summary>Status eines Verbindungs-Checks.</summary>
public enum ConnectionTestState
{
    Unknown,
    Testing,
    Online,
    Offline
}

/// <summary>Service für Drucker-Verbindungs-Checks und Druck-Status-Abfrage.</summary>
public interface IPrinterService
{
    /// <summary>Testet die Verbindung zu einem Drucker (async).</summary>
    Task<ConnectionTestState> TestConnectionAsync(Printer printer, CancellationToken ct = default);

    /// <summary>Liefert den aktuellen Status eines Druckers.</summary>
    Task<PrinterStatus> GetStatusAsync(Printer printer, CancellationToken ct = default);

    /// <summary>Liefert true, wenn irgendein Drucker gerade druckt.</summary>
    Task<bool> IsAnyPrinterPrintingAsync(CancellationToken ct = default);
}

/// <summary>Stub-Implementierung: liefert immer Offline / Idle.</summary>
public sealed class StubPrinterService : IPrinterService
{
    /// <inheritdoc />
    public async Task<ConnectionTestState> TestConnectionAsync(Printer printer, CancellationToken ct = default)
    {
        await Task.Delay(250, ct);
        return ConnectionTestState.Offline;
    }

    /// <inheritdoc />
    public async Task<PrinterStatus> GetStatusAsync(Printer printer, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return PrinterStatus.Offline;
    }

    /// <inheritdoc />
    public Task<bool> IsAnyPrinterPrintingAsync(CancellationToken ct = default)
        => Task.FromResult(false);
}