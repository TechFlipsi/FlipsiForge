// SPDX-License-Identifier: GPL-3.0-or-later
// FlipsiForge.Core — Auto-Scheduling Engine für Druckerfarm
// Teil des TechFlipsi-Ökosystems (https://techflipsi.kirchweger.de)
// Autor: TechFlipsi (Fabian Kirchweger)

using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FlipsiForge.Core.Services.Farm;

/// <summary>
/// Auto-Scheduling Engine für die FlipsiForge-Druckerfarm.
/// Verteilt BatchItems auf verfügbare Drucker unter Berücksichtigung von
/// Priorität, Deadline, Bauvolumen, Filament-Kompatibilität und MaxConcurrentPrints.
/// </summary>
public sealed class AutoSchedulerService
{
    private readonly FlipsiForgeDbContext _db;

    /// <summary>
    /// Erzeugt eine neue Instanz der Auto-Scheduling Engine.
    /// </summary>
    /// <param name="db">Der FlipsiForge DbContext (enthält Farm-DbSets).</param>
    public AutoSchedulerService(FlipsiForgeDbContext db) => _db = db;

    /// <summary>
    /// Verteilt alle Pending-Items eines Batches auf verfügbare Drucker im Cluster.
    /// Berücksichtigt: Batch-Priorität, Deadline, Drucker-Verfügbarkeit (Idle),
    /// Filament-Kompatibilität, Bauvolumen-Check und MaxConcurrentPrints.
    /// </summary>
    /// <param name="batchId">ID des zu schedulenden Batches.</param>
    /// <param name="cancellationToken">Abbrechungs-Token.</param>
    /// <returns>Anzahl der erfolgreich zugewiesenen Items (0 bei Fehler oder nichts zu tun).</returns>
    public async Task<int> ScheduleBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Batch mit Items laden
            var batch = await _db.PrintBatches
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
                .ConfigureAwait(false);
            if (batch is null) return 0;

            var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);

            // 2. Nur Pending-Items scheduling, sortiert nach SortOrder dann Deadline
            var pendingItems = batch.Items
                .Where(i => i.Status == BatchItemStatus.Pending)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => batch.Deadline ?? DateTime.MaxValue)
                .ToList();
            if (pendingItems.Count == 0) return 0;

            // Relevanten ScannedFiles und Spulen für Kompatibilitäts-Check laden
            var fileIds = pendingItems.Select(i => i.ScannedFileId).Distinct().ToList();
            var files = await _db.ScannedFiles
                .Where(f => fileIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, cancellationToken)
                .ConfigureAwait(false);

            var spoolIds = pendingItems
                .Where(i => i.SpoolId.HasValue)
                .Select(i => i.SpoolId!.Value)
                .Distinct()
                .ToList();
            var spools = spoolIds.Count > 0
                ? await _db.Spools
                    .Where(s => spoolIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, cancellationToken)
                    .ConfigureAwait(false)
                : new Dictionary<int, Spool>();

            // 3. Verfügbare Drucker finden
            var availablePrinters = await GetAvailablePrintersAsync(
                settings.PreferSameCluster ? batch.AssignedClusterId : null,
                cancellationToken)
                .ConfigureAwait(false);
            if (availablePrinters.Count == 0) return 0;

            // 4. Aktive Drucke farm-übergreifend zählen (für MaxConcurrentPrints)
            var activePrintCount = await CountActivePrintsAsync(cancellationToken).ConfigureAwait(false);

            // 5. Für jedes Item: passenden Drucker finden und zuweisen
            var assigned = 0;
            var usedPrinters = new HashSet<int>();

            foreach (var item in pendingItems)
            {
                // MaxConcurrentPrints Limit prüfen
                if (activePrintCount + usedPrinters.Count >= settings.MaxConcurrentPrints)
                    break;

                var printer = FindBestPrinter(item, availablePrinters, usedPrinters, files, spools);
                if (printer is null)
                    continue; // kein passender Drucker → Item bleibt Pending

                await AssignItemAsync(item.Id, printer.Id, cancellationToken).ConfigureAwait(false);
                usedPrinters.Add(printer.Id);
                assigned++;
            }

            // 6. Batch-Status aktualisieren wenn mindestens ein Item zugewiesen wurde
            if (assigned > 0 && batch.Status == BatchStatus.Pending)
            {
                batch.Status = BatchStatus.Ready;
                batch.StartedAt ??= DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return assigned;
        }
        catch
        {
            // Defensiv: bei DB-Fehler 0 zurückgeben statt Exception zu werfen
            return 0;
        }
    }

    /// <summary>
    /// Liefert alle verfügbaren (Idle) Drucker, optional gefiltert nach Cluster.
    /// Ein Drucker gilt als verfügbar wenn:
    /// - <see cref="Printer.IsActive"/> true ist,
    /// - kein aktiver <see cref="PrintJob"/> (Status Printing/Paused/Queued/Confirmed) existiert,
    /// - kein laufender/geplanter <see cref="FarmSchedule"/> existiert.
    /// Wenn <paramref name="clusterId"/> gesetzt ist und der Cluster existiert,
    /// werden nur Drucker zurückgegeben deren ID in <see cref="PrinterCluster.PrinterIds"/> steht.
    /// </summary>
    /// <param name="clusterId">Optionale Cluster-ID zum Filtern.</param>
    /// <param name="cancellationToken">Abbrechungs-Token.</param>
    /// <returns>Liste der verfügbaren Drucker (leer bei Fehler oder nichts verfügbar).</returns>
    public async Task<List<Printer>> GetAvailablePrintersAsync(
        int? clusterId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Drucker mit aktiven PrintJobs ausschließen.
            // PrintJobStatus kennt kein Paused (siehe Models.cs) — Queued/Confirmed/Printing blockieren.
            var busyPrinterIds = await _db.PrintJobs
                .Where(j => j.Status == PrintJobStatus.Printing
                         || j.Status == PrintJobStatus.Queued
                         || j.Status == PrintJobStatus.Confirmed)
                .Select(j => j.PrinterId)
                .Distinct()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Drucker mit laufenden/geplanten FarmSchedules ausschließen
            var scheduledPrinterIds = await _db.FarmSchedules
                .Where(s => s.Status == FarmScheduleStatus.Scheduled
                         || s.Status == FarmScheduleStatus.Running)
                .Select(s => s.PrinterId)
                .Distinct()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var excluded = new HashSet<int>(busyPrinterIds.Concat(scheduledPrinterIds));

            // Alle aktiven Drucker laden (client-side filtern wegen excluded + cluster)
            var printers = await _db.Printers
                .Where(p => p.IsActive)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var result = printers.Where(p => !excluded.Contains(p.Id));

            // Cluster-Filter (client-side, da PrinterIds als CSV gespeichert wird)
            if (clusterId.HasValue)
            {
                var cluster = await _db.PrinterClusters
                    .FirstOrDefaultAsync(c => c.Id == clusterId.Value, cancellationToken)
                    .ConfigureAwait(false);

                if (cluster is null || cluster.PrinterIds.Count == 0)
                    return new List<Printer>(); // Cluster nicht gefunden → leer

                var clusterPrinterIds = new HashSet<int>(cluster.PrinterIds);
                result = result.Where(p => clusterPrinterIds.Contains(p.Id));
            }

            return result.ToList();
        }
        catch
        {
            return new List<Printer>();
        }
    }

    /// <summary>
    /// Weist ein BatchItem einem Drucker zu und erstellt einen <see cref="FarmSchedule"/>-Eintrag.
    /// Setzt den BatchItem-Status auf <see cref="BatchItemStatus.Assigned"/> und legt einen
    /// neuen Schedule-Eintrag mit Status <see cref="FarmScheduleStatus.Scheduled"/> an.
    /// </summary>
    /// <param name="batchItemId">ID des zuzuweisenden BatchItems.</param>
    /// <param name="printerId">ID des Ziel-Druckers.</param>
    /// <param name="cancellationToken">Abbrechungs-Token.</param>
    /// <returns>true wenn die Zuweisung erfolgreich war, false sonst.</returns>
    public async Task<bool> AssignItemAsync(
        int batchItemId,
        int printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _db.BatchItems
                .FirstOrDefaultAsync(i => i.Id == batchItemId, cancellationToken)
                .ConfigureAwait(false);
            if (item is null) return false;

            var printer = await _db.Printers
                .FirstOrDefaultAsync(p => p.Id == printerId, cancellationToken)
                .ConfigureAwait(false);
            if (printer is null) return false;

            // Bauvolumen-Check (defensiv — kein File oder keine BoundingBox = kein Blocker)
            if (!await IsBuildVolumeSufficientAsync(item, printer, cancellationToken)
                .ConfigureAwait(false))
                return false;

            // Item aktualisieren
            item.AssignedPrinterId = printerId;
            item.Status = BatchItemStatus.Assigned;

            // Batch-Priorität für Schedule-Eintrag laden
            var batch = await _db.PrintBatches
                .FirstOrDefaultAsync(b => b.Id == item.BatchId, cancellationToken)
                .ConfigureAwait(false);
            var priority = (int)(batch?.Priority ?? BatchPriority.Normal);

            // Schedule-Eintrag erstellen
            var schedule = new FarmSchedule
            {
                BatchId = item.BatchId,
                PrinterId = printerId,
                ScheduledStart = DateTime.UtcNow,
                EstimatedEnd = item.EstimatedDurationMin.HasValue
                    ? DateTime.UtcNow.AddMinutes((double)item.EstimatedDurationMin.Value)
                    : DateTime.UtcNow.AddHours(1), // Fallback 1h
                Status = FarmScheduleStatus.Scheduled,
                Priority = priority
            };
            _db.FarmSchedules.Add(schedule);

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Plant ein fehlgeschlagenes BatchItem neu ein, wenn
    /// <see cref="FarmSettings.AutoRescheduleFailed"/> aktiv ist.
    /// Setzt das Item zurück auf <see cref="BatchItemStatus.Pending"/> und ruft
    /// <see cref="ScheduleBatchAsync"/> für den Eltern-Batch auf.
    /// </summary>
    /// <param name="batchItemId">ID des fehlgeschlagenen Items.</param>
    /// <param name="cancellationToken">Abbrechungs-Token.</param>
    /// <returns>true wenn neu eingeplant wurde, false wenn nicht aktiviert oder Fehler.</returns>
    public async Task<bool> RescheduleFailedAsync(
        int batchItemId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
            if (!settings.AutoRescheduleFailed) return false;

            var item = await _db.BatchItems
                .FirstOrDefaultAsync(i => i.Id == batchItemId, cancellationToken)
                .ConfigureAwait(false);
            if (item is null) return false;
            if (item.Status != BatchItemStatus.Failed) return false;

            // Item zurücksetzen (bereits gedruckte Exemplare behalten)
            item.Status = BatchItemStatus.Pending;
            item.AssignedPrinterId = null;
            // PrintedQuantity bleibt unverändert — schon fertige Exemplare nicht neu drucken

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Eltern-Batch neu schedulen (weist das Item automatisch neu zu)
            if (item.BatchId > 0)
            {
                await ScheduleBatchAsync(item.BatchId, cancellationToken).ConfigureAwait(false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Liefert den aktuellen Fortschritt eines Batches.
    /// </summary>
    /// <param name="batchId">ID des Batches.</param>
    /// <param name="cancellationToken">Abbrechungs-Token.</param>
    /// <returns>
    /// Tuple mit (totalParts, completedParts, failedParts, activePrinters, estimatedRemainingMin).
    /// Alle Werte 0 bei Fehler oder nicht gefundenem Batch.
    /// </returns>
    public async Task<(int totalParts, int completedParts, int failedParts, int activePrinters, decimal estimatedRemainingMin)> GetBatchProgressAsync(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var batch = await _db.PrintBatches
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
                .ConfigureAwait(false);
            if (batch is null) return (0, 0, 0, 0, 0);

            var total = batch.Items.Sum(i => i.Quantity);
            var completed = batch.Items
                .Where(i => i.Status == BatchItemStatus.Completed)
                .Sum(i => i.Quantity);
            var failed = batch.Items
                .Where(i => i.Status == BatchItemStatus.Failed)
                .Sum(i => i.Quantity);

            var activePrinters = await _db.FarmSchedules
                .Where(s => s.BatchId == batchId
                         && (s.Status == FarmScheduleStatus.Running
                          || s.Status == FarmScheduleStatus.Scheduled))
                .Select(s => s.PrinterId)
                .Distinct()
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            // Geschätzte verbleibende Zeit = Summe EstimatedDurationMin über Pending/Assigned-Items
            var remaining = batch.Items
                .Where(i => i.Status == BatchItemStatus.Pending
                         || i.Status == BatchItemStatus.Assigned)
                .Sum(i => i.EstimatedDurationMin ?? 0m);

            return (total, completed, failed, activePrinters, remaining);
        }
        catch
        {
            return (0, 0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Stub für zukünftige Kamera-KI-basierte Spaghetti-Detection.
    /// Aktuelle Implementierung gibt immer false zurück.
    /// Die echte Implementierung würde ein Webcam-Bild holen und ein ONNX-Modell
    /// (z.B. MobileNet-Variante) auswerten, um Druckfehler zu erkennen.
    /// </summary>
    /// <param name="printerId">ID des zu prüfenden Druckers.</param>
    /// <param name="cancellationToken">Abbrechungs-Token.</param>
    /// <returns>Immer false (Stub — noch nicht implementiert).</returns>
    public Task<bool> CheckSpaghettiAsync(int printerId, CancellationToken cancellationToken = default)
    {
        // Platzhalter — zukünftige Implementierung:
        //   1. Webcam-Bild vom Drucker holen (via IPrinterConnection.GetWebcamSnapshotAsync)
        //   2. Bild vorverarbeiten (Resize, Normalisierung)
        //   3. ONNX-Modell auswerten (Spaghetti-Klassifikator)
        //   4. Konfidenz-Schwelle prüfen
        //   5. Bei Detektion: NotificationOnFail + AutoPauseOnAnomaly auswerten
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }

    /// <summary>
    /// Liefert eine <see cref="FarmOverview"/>-Zusammenfassung der gesamten Farm.
    /// </summary>
    /// <param name="cancellationToken">Abbrechungs-Token.</param>
    public async Task<FarmOverview> GetFarmOverviewAsync(CancellationToken cancellationToken = default)
    {
        return await FarmOverview.BuildAsync(_db, cancellationToken).ConfigureAwait(false);
    }

    // === Private Helpers ===

    /// <summary>
    /// Findet den besten passenden Drucker für ein BatchItem aus der Liste der verfügbaren Drucker.
    /// Kriterien (alle müssen erfüllt sein):
    /// - Drucker noch nicht in <paramref name="used"/> (ein Drucker pro Schedule-Durchlauf)
    /// - Bauvolumen des Druckers >= BoundingBox des ScannedFiles
    /// - Filament-Kompatibilität: Material des Spools muss vom Drucker verarbeitbar sein
    /// </summary>
    private static Printer? FindBestPrinter(
        BatchItem item,
        List<Printer> available,
        HashSet<int> used,
        Dictionary<int, ScannedFile> files,
        Dictionary<int, Spool> spools)
    {
        foreach (var p in available)
        {
            if (used.Contains(p.Id)) continue;

            // Bauvolumen-Check (nur wenn BoundingBox bekannt)
            if (files.TryGetValue(item.ScannedFileId, out var file)
                && file.BoundingBoxX.HasValue
                && file.BoundingBoxY.HasValue
                && file.BoundingBoxZ.HasValue)
            {
                if (file.BoundingBoxX.Value > p.BuildVolumeX
                    || file.BoundingBoxY.Value > p.BuildVolumeY
                    || file.BoundingBoxZ.Value > p.BuildVolumeZ)
                {
                    continue;
                }
            }

            // Filament-Kompatibilität (nur wenn Spool zugewiesen)
            if (item.SpoolId.HasValue && spools.TryGetValue(item.SpoolId.Value, out var spool))
            {
                if (!IsFilamentCompatible(spool.MaterialType, p))
                    continue;
            }

            return p;
        }
        return null;
    }

    /// <summary>
    /// Prüft ob ein Material vom Drucker verarbeitbar ist.
    /// ABS/ASA/PC brauchen Enclosure und hohe Hotend-Temp (&gt;=260°C).
    /// PA6 braucht sehr hohe Hotend-Temp (&gt;=280°C).
    /// POM braucht moderate Hotend-Temp (&gt;=210°C).
    /// PLA/PETG/TPU/PVA/HIPS/Other funktionieren auf jedem Standard-Drucker.
    /// </summary>
    private static bool IsFilamentCompatible(MaterialType material, Printer printer)
    {
        return material switch
        {
            MaterialType.ABS or MaterialType.ASA or MaterialType.PC
                => printer.IsEnclosed && printer.MaxHotendTemp >= 260,
            MaterialType.PA6 => printer.MaxHotendTemp >= 280,
            MaterialType.POM => printer.MaxHotendTemp >= 210,
            // Standard-Materialien — jeder aktive Drucker kann diese
            MaterialType.PLA or MaterialType.PETG or MaterialType.TPU
                or MaterialType.PVA or MaterialType.HIPS or MaterialType.Other
                => true,
            _ => true
        };
    }

    /// <summary>
    /// Prüft ob das Bauvolumen des Druckers für das BatchItem ausreicht.
    /// Lädt das ScannedFile und vergleicht dessen BoundingBox gegen BuildVolume.
    /// Defensive: kein File oder keine BoundingBox → true (kein Blocker).
    /// </summary>
    private async Task<bool> IsBuildVolumeSufficientAsync(
        BatchItem item,
        Printer printer,
        CancellationToken cancellationToken)
    {
        try
        {
            var file = await _db.ScannedFiles
                .FirstOrDefaultAsync(f => f.Id == item.ScannedFileId, cancellationToken)
                .ConfigureAwait(false);
            if (file is null) return true; // kein File → kein Blocker
            if (!file.BoundingBoxX.HasValue
                || !file.BoundingBoxY.HasValue
                || !file.BoundingBoxZ.HasValue)
                return true; // BoundingBox unbekannt → kein Blocker

            return file.BoundingBoxX.Value <= printer.BuildVolumeX
                && file.BoundingBoxY.Value <= printer.BuildVolumeY
                && file.BoundingBoxZ.Value <= printer.BuildVolumeZ;
        }
        catch
        {
            return true; // defensiv: bei Fehler nicht blockieren
        }
    }

    /// <summary>
    /// Lädt die Farm-Einstellungen. Legt eine Default-Zeile an falls noch nicht vorhanden.
    /// </summary>
    private async Task<FarmSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var s = await _db.FarmSettings
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (s is not null) return s;

            // Fallback: Default-Settings anlegen wenn noch nicht geseedt
            s = new FarmSettings();
            _db.FarmSettings.Add(s);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return s;
        }
        catch
        {
            return new FarmSettings(); // sichere Defaults
        }
    }

    /// <summary>
    /// Zählt die aktuell laufenden/geplanten Drucke farm-übergreifend
    /// (distinct PrinterIds über aktive FarmSchedules).
    /// </summary>
    private async Task<int> CountActivePrintsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.FarmSchedules
                .Where(s => s.Status == FarmScheduleStatus.Running
                         || s.Status == FarmScheduleStatus.Scheduled)
                .Select(s => s.PrinterId)
                .Distinct()
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            return 0;
        }
    }
}