// FlipsiForge.Server — v0.2.0
// Stub-Implementierung von IPrinterConnectionManager.
// Liefert Offline-Default-Werte, wenn die echte Core.Services-Implementierung
// noch nicht verfügbar ist. Wird via DI TryAdd nur registriert, wenn noch
// keine andere Implementierung von IPrinterConnectionManager existiert.
//
// SPDX-License-Identifier: GPL-3.0-only
// (c) 2026 TechFlipsi / Fabian Kirchweger

using FlipsiForge.Core.Models;
using Microsoft.Extensions.Logging;

namespace FlipsiForge.Server.Services;

/// <summary>
/// Stub-Implementierung von <see cref="IPrinterConnectionManager"/>.
/// Antwortet immer mit "Offline" — der Server läuft ohne echte Drucker-Anbindung.
/// </summary>
public sealed class StubPrinterConnectionManager : IPrinterConnectionManager
{
    private readonly ILogger<StubPrinterConnectionManager> _logger;

    /// <summary>Konstruktor mit DI-Logger.</summary>
    public StubPrinterConnectionManager(ILogger<StubPrinterConnectionManager> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<PrinterConnectionResult> ConnectAsync(Printer printer, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("StubPrinterConnectionManager.ConnectAsync für Drucker {PrinterId} ({Brand} {Model})", printer.Id, printer.Brand, printer.Model);
        return Task.FromResult(new PrinterConnectionResult
        {
            Connected = false,
            Error = "Stub-Implementation: Core.Services nicht geladen",
            Details = printer.Protocol.ToString()
        });
    }

    /// <inheritdoc />
    public Task<PrinterLiveStatus> GetStatusAsync(Printer printer, CancellationToken cancellationToken = default)
        => Task.FromResult(new PrinterLiveStatus { Status = PrinterStatus.Offline });

    /// <inheritdoc />
    public Task<PrinterTemperatures> GetTemperaturesAsync(Printer printer, CancellationToken cancellationToken = default)
        => Task.FromResult(new PrinterTemperatures());

    /// <inheritdoc />
    public Task<PrinterJobInfo?> GetCurrentJobAsync(Printer printer, CancellationToken cancellationToken = default)
        => Task.FromResult<PrinterJobInfo?>(null);
}