// SPDX-License-Identifier: GPL-3.0-or-later
// FlipsiForge.Core — Druckerfarm-Übersicht (Summary struct)
// Teil des TechFlipsi-Ökosystems (https://techflipsi.kirchweger.de)
// Autor: TechFlipsi (Fabian Kirchweger)

using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FlipsiForge.Core.Services.Farm;

/// <summary>
/// Zusammenfassung des aktuellen Farm-Status für Dashboard-Anzeige.
/// Wird von <see cref="AutoSchedulerService.GetFarmOverviewAsync"/> verwendet.
/// </summary>
public readonly struct FarmOverview
{
    /// <summary>Gesamtzahl aller aktiven Drucker in der Farm.</summary>
    public int TotalPrinters { get; init; }

    /// <summary>Anzahl der Drucker die aktuell einen Druck ausführen.</summary>
    public int ActivePrinters { get; init; }

    /// <summary>Anzahl der verfügbaren (Idle) Drucker.</summary>
    public int IdlePrinters { get; init; }

    /// <summary>
    /// Anzahl der Drucker deren letzter Druck fehlgeschlagen ist
    /// (Approximation über <see cref="PrintJobStatus.Failed"/>).
    /// </summary>
    public int ErrorPrinters { get; init; }

    /// <summary>Gesamtzahl aller Batches in der Datenbank.</summary>
    public int TotalBatches { get; init; }

    /// <summary>
    /// Anzahl der gerade aktiven Batches (Status Slicing, Ready oder Printing).
    /// </summary>
    public int ActiveBatches { get; init; }

    /// <summary>
    /// Anzahl der noch zu druckenden Teile (Summe Quantity über alle
    /// Pending/Assigned BatchItems).
    /// </summary>
    public int TotalPartsQueued { get; init; }

    /// <summary>
    /// Geschätzte Zeit bis die gesamte Farm leer ist (Minuten).
    /// Berechnet als (Summe verbleibende EstimatedDurationMin) ÷ (aktive Drucker).
    /// Null wenn keine pending Items vorhanden.
    /// </summary>
    public decimal? EstimatedCompletionTimeMin { get; init; }

    /// <summary>
    /// Erzeugt eine <see cref="FarmOverview"/> aus der aktuellen Datenbank.
    /// Defensiv: bei DB-Fehler wird eine leere Overview zurückgegeben
    /// (alle Werte 0 / null).
    /// </summary>
    /// <param name="db">Der FlipsiForge DbContext.</param>
    /// <param name="cancellationToken">Abbrechungs-Token.</param>
    public static async Task<FarmOverview> BuildAsync(
        FlipsiForgeDbContext db,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Gesamtzahl aktiver Drucker
            var totalPrinters = await db.Printers
                .Where(p => p.IsActive)
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            // Aktive Drucker = Drucker mit laufenden/geplanten FarmSchedules
            var activePrinters = await db.FarmSchedules
                .Where(s => s.Status == FarmScheduleStatus.Running
                         || s.Status == FarmScheduleStatus.Scheduled)
                .Select(s => s.PrinterId)
                .Distinct()
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            // Error-Drucker = Drucker mit fehlgeschlagenen PrintJobs (Approximation)
            var errorPrinters = await db.PrintJobs
                .Where(j => j.Status == PrintJobStatus.Failed)
                .Select(j => j.PrinterId)
                .Distinct()
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            var totalBatches = await db.PrintBatches
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            var activeBatches = await db.PrintBatches
                .Where(b => b.Status == BatchStatus.Printing
                         || b.Status == BatchStatus.Ready
                         || b.Status == BatchStatus.Slicing)
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            // Noch zu druckende Teile (Summe über Quantity der Pending/Assigned-Items)
            var totalPartsQueued = await db.BatchItems
                .Where(i => i.Status == BatchItemStatus.Pending
                         || i.Status == BatchItemStatus.Assigned)
                .SumAsync(i => i.Quantity, cancellationToken)
                .ConfigureAwait(false);

            // Geschätzte verbleibende Zeit (Minuten) = Summe EstimatedDurationMin
            // über alle Pending/Assigned-Items
            var totalRemainingMin = await db.BatchItems
                .Where(i => i.Status == BatchItemStatus.Pending
                         || i.Status == BatchItemStatus.Assigned)
                .Select(i => i.EstimatedDurationMin ?? 0m)
                .SumAsync(cancellationToken)
                .ConfigureAwait(false);

            // Teilen durch Anzahl aktiver Drucker (mindestens 1 um Division durch 0 zu vermeiden)
            var denominator = Math.Max(1, activePrinters);
            decimal? estimatedCompletion = totalRemainingMin > 0
                ? totalRemainingMin / denominator
                : null;

            // Idle = was übrig bleibt nach Active und Error (niemals negativ)
            var idlePrinters = Math.Max(0, totalPrinters - activePrinters - errorPrinters);

            return new FarmOverview
            {
                TotalPrinters = totalPrinters,
                ActivePrinters = activePrinters,
                IdlePrinters = idlePrinters,
                ErrorPrinters = errorPrinters,
                TotalBatches = totalBatches,
                ActiveBatches = activeBatches,
                TotalPartsQueued = totalPartsQueued,
                EstimatedCompletionTimeMin = estimatedCompletion
            };
        }
        catch
        {
            // Defensiv: bei DB-Fehler leere Overview
            return new FarmOverview();
        }
    }
}