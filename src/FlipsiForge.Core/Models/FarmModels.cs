// SPDX-License-Identifier: GPL-3.0-or-later
// FlipsiForge.Core — Druckerfarm-Modelle (Cluster, Batch, Schedule, Settings)
// Teil des TechFlipsi-Ökosystems (https://techflipsi.kirchweger.de)
// Autor: TechFlipsi (Fabian Kirchweger)

namespace FlipsiForge.Core.Models;

// === Farm Enums ===

/// <summary>Typ eines Drucker-Clusters (steuert Scheduling-Verhalten).</summary>
public enum ClusterType
{
    /// <summary>Produktions-Cluster (hohe Zuverlässigkeit, echte Aufträge).</summary>
    Production,
    /// <summary>Test-Cluster (für Probendrucke, Kalibrierung).</summary>
    Testing,
    /// <summary>Prototyp-Cluster (Iteration, schnelle Durchläufe).</summary>
    Prototype,
    /// <summary>Reserviert (manuell gesperrt, kein Auto-Scheduling).</summary>
    Reserved
}

/// <summary>Priorität eines Druck-Batches. Höherer Wert = wichtiger.</summary>
public enum BatchPriority
{
    Low = 1,
    Normal = 5,
    High = 10,
    Urgent = 20
}

/// <summary>Lebenszyklus-Status eines Druck-Batches.</summary>
public enum BatchStatus
{
    /// <summary>Erstellt, wartet auf Slicing/Zuweisung.</summary>
    Pending,
    /// <summary>Wird gerade gesliced (G-Code-Generierung läuft).</summary>
    Slicing,
    /// <summary>Gesliced und bereit für Druck.</summary>
    Ready,
    /// <summary>Mindestens ein Item wird gerade gedruckt.</summary>
    Printing,
    /// <summary>Alle Items erfolgreich gedruckt.</summary>
    Completed,
    /// <summary>Batch fehlgeschlagen (kritischer Fehler).</summary>
    Failed,
    /// <summary>Vom User abgebrochen.</summary>
    Cancelled
}

/// <summary>Status eines einzelnen Batch-Items.</summary>
public enum BatchItemStatus
{
    /// <summary>Noch kein Drucker zugewiesen.</summary>
    Pending,
    /// <summary>Drucker zugewiesen, Druck noch nicht gestartet.</summary>
    Assigned,
    /// <summary>Wird gerade gedruckt.</summary>
    Printing,
    /// <summary>Erfolgreich abgeschlossen.</summary>
    Completed,
    /// <summary>Druck fehlgeschlagen.</summary>
    Failed
}

/// <summary>Status eines Farm-Schedule-Eintrags (Zeitplan).</summary>
public enum FarmScheduleStatus
{
    /// <summary>Geplant, wartet auf Start.</summary>
    Scheduled,
    /// <summary>Druck läuft aktuell auf diesem Drucker.</summary>
    Running,
    /// <summary>Erfolgreich abgeschlossen.</summary>
    Completed,
    /// <summary>Fehlgeschlagen.</summary>
    Failed,
    /// <summary>Übersprungen (z.B. wegen Failover oder manueller Stornierung).</summary>
    Skipped
}

// === Farm Modelle ===

/// <summary>
/// Ein Cluster aus Druckern das für automatisches Scheduling als Einheit behandelt wird.
/// Cluster helfen, Drucke auf verwandte Drucker zu verteilen (gleiche Hersteller, gleiche Filamente).
/// </summary>
public class PrinterCluster
{
    /// <summary>Primärschlüssel.</summary>
    public int Id { get; set; }

    /// <summary>Anzeigename des Clusters (z.B. "Bambu-Farm", "Test-Ecke").</summary>
    public string Name { get; set; } = "";

    /// <summary>Optionale Beschreibung.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// IDs der Drucker in diesem Cluster. Wird als CSV-String in der DB gespeichert
    /// (siehe DbContext-Erweiterung am Ende dieser Datei).
    /// </summary>
    public List<int> PrinterIds { get; set; } = new();

    /// <summary>Typ des Clusters (steuert ob Auto-Scheduling aktiv ist).</summary>
    public ClusterType ClusterType { get; set; } = ClusterType.Production;

    /// <summary>
    /// Wenn true, dürfen neue Batches automatisch auf diesem Cluster gescheduled werden.
    /// </summary>
    public bool AutoSchedule { get; set; } = true;

    /// <summary>
    /// Mindestanzahl gleichzeitig aktiver Drucker im Cluster (Failover-Steuerung).
    /// Wenn unterschritten, werden neue Assignments priorisiert.
    /// </summary>
    public int MinActivePrinters { get; set; }

    /// <summary>
    /// Maximalzahl gleichzeitig aktiver Drucker im Cluster (Begrenzung).
    /// 0 = unbegrenzt.
    /// </summary>
    public int MaxActivePrinters { get; set; } = 10;

    /// <summary>Zeitpunkt der Erstellung (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Optionale Notizen des Users.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Ein Batch aus mehreren zu druckenden Teilen, das als Einheit gescheduled wird.
/// Ein Batch kann mehrere Exemplare verschiedener ScannedFiles enthalten.
/// </summary>
public class PrintBatch
{
    /// <summary>Primärschlüssel.</summary>
    public int Id { get; set; }

    /// <summary>Anzeigename des Batches.</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Einzelne Teile des Batches (1:N-Relation via <see cref="BatchItem.BatchId"/>).
    /// </summary>
    public List<BatchItem> Items { get; set; } = new();

    /// <summary>Priorität des Batches (höher = früher gescheduled).</summary>
    public BatchPriority Priority { get; set; } = BatchPriority.Normal;

    /// <summary>Aktueller Lebenszyklus-Status.</summary>
    public BatchStatus Status { get; set; } = BatchStatus.Pending;

    /// <summary>
    /// Gesamtzahl der zu druckenden Teile (Summe über alle <see cref="BatchItem.Quantity"/>).
    /// Wird vom Scheduler gepflegt.
    /// </summary>
    public int TotalParts { get; set; }

    /// <summary>
    /// Bereits erfolgreich gedruckte Teile. Wird vom Scheduler gepflegt.
    /// </summary>
    public int CompletedParts { get; set; }

    /// <summary>Erstellungszeitpunkt (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Zeitpunkt an dem der erste Druck gestartet wurde.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Zeitpunkt an dem der Batch abgeschlossen wurde (alle Items fertig).</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Optionale Deadline für den Batch. Der Scheduler priorisiert Batches
    /// mit näherer Deadline höher.
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Zugewiesenes Cluster. Null = Auto-Assign (Scheduler wählt Cluster).
    /// </summary>
    public int? AssignedClusterId { get; set; }

    /// <summary>Optionale Notizen des Users.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Einzelnes zu druckendes Teil innerhalb eines <see cref="PrintBatch"/>.
/// Ein BatchItem referenziert eine <see cref="ScannedFile"/> und kann mehrfach
/// gedruckt werden (<see cref="Quantity"/>).
/// </summary>
public class BatchItem
{
    /// <summary>Primärschlüssel.</summary>
    public int Id { get; set; }

    /// <summary>Fremdschlüssel zum Eltern-<see cref="PrintBatch"/>.</summary>
    public int BatchId { get; set; }

    /// <summary>Verweis auf die gescannte Quelldatei (STL/3MF/etc.).</summary>
    public int ScannedFileId { get; set; }

    /// <summary>Anzahl der zu druckenden Exemplare dieses Teils.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Bereits erfolgreich gedruckte Exemplare.</summary>
    public int PrintedQuantity { get; set; }

    /// <summary>Zugewiesener Drucker (null = noch nicht zugewiesen).</summary>
    public int? AssignedPrinterId { get; set; }

    /// <summary>Zugewiesene Filament-Spule (null = noch nicht gewählt).</summary>
    public int? SpoolId { get; set; }

    /// <summary>Aktueller Status dieses Items.</summary>
    public BatchItemStatus Status { get; set; } = BatchItemStatus.Pending;

    /// <summary>Geschätzte Druckdauer aller Exemplare in Minuten.</summary>
    public decimal? EstimatedDurationMin { get; set; }

    /// <summary>Geschätzter Filament-Verbrauch aller Exemplare in Gramm.</summary>
    public decimal? EstimatedFilamentG { get; set; }

    /// <summary>Tatsächliche Druckdauer in Minuten (wird nach Druck gesetzt).</summary>
    public decimal? ActualDurationMin { get; set; }

    /// <summary>Tatsächlicher Filament-Verbrauch in Gramm.</summary>
    public decimal? ActualFilamentG { get; set; }

    /// <summary>
    /// Sortier-Reihenfolge innerhalb des Batches (niedriger = früher im Schedule).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>Navigations-Property zum Eltern-Batch (EF Core).</summary>
    public PrintBatch? Batch { get; set; }
}

/// <summary>
/// Ein Zeitplan-Eintrag: welches Batch-Item wird wann auf welchem Drucker gedruckt.
/// Pro Zuweisung (BatchItem × Printer) entsteht ein Schedule-Eintrag.
/// </summary>
public class FarmSchedule
{
    /// <summary>Primärschlüssel.</summary>
    public int Id { get; set; }

    /// <summary>Fremdschlüssel zum <see cref="PrintBatch"/>.</summary>
    public int BatchId { get; set; }

    /// <summary>Fremdschlüssel zum <see cref="Printer"/>.</summary>
    public int PrinterId { get; set; }

    /// <summary>Geplanter Start-Zeitpunkt (UTC).</summary>
    public DateTime ScheduledStart { get; set; } = DateTime.UtcNow;

    /// <summary>Geschätzter End-Zeitpunkt (basierend auf EstimatedDurationMin).</summary>
    public DateTime EstimatedEnd { get; set; } = DateTime.UtcNow;

    /// <summary>Tatsächlicher Start-Zeitpunkt (wird beim Druck-Start gesetzt).</summary>
    public DateTime? ActualStart { get; set; }

    /// <summary>Tatsächlicher End-Zeitpunkt (wird beim Druck-Ende gesetzt).</summary>
    public DateTime? ActualEnd { get; set; }

    /// <summary>Aktueller Status dieses Schedule-Eintrags.</summary>
    public FarmScheduleStatus Status { get; set; } = FarmScheduleStatus.Scheduled;

    /// <summary>
    /// Numerische Priorität dieses Schedule-Eintrags (von <see cref="PrintBatch.Priority"/> übernommen).
    /// Höherer Wert = wichtiger.
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// Globale Einstellungen für die Druckerfarm. Singleton-Modell: es existiert
/// immer nur eine Zeile mit <see cref="Id"/> = 1 (siehe DbContext-Seed).
/// </summary>
public class FarmSettings
{
    /// <summary>Primärschlüssel — IMMER 1 (Singleton).</summary>
    public int Id { get; set; } = 1;

    /// <summary>
    /// Maximalzahl gleichzeitig laufender Drucke farm-übergreifend.
    /// Verhindert Überlastung bei vielen Druckern.
    /// </summary>
    public int MaxConcurrentPrints { get; set; } = 10;

    /// <summary>
    /// Wenn true, weist <see cref="Services.Farm.AutoSchedulerService"/> automatisch
    /// Drucker zu wenn ein Batch gescheduled wird.
    /// </summary>
    public bool AutoAssignPrinters { get; set; } = true;

    /// <summary>
    /// Wenn true, bevorzugt der Scheduler Drucker aus demselben Cluster
    /// wie der zugewiesene Batch.
    /// </summary>
    public bool PreferSameCluster { get; set; } = true;

    /// <summary>
    /// Wenn true, wechselt der Scheduler bei einem Drucker-Fehler automatisch
    /// auf einen anderen verfügbaren Drucker (Failover).
    /// </summary>
    public bool FailoverOnError { get; set; } = true;

    /// <summary>
    /// Wenn true, werden fehlgeschlagene BatchItems automatisch neu zugewiesen.
    /// </summary>
    public bool AutoRescheduleFailed { get; set; } = true;

    /// <summary>
    /// Spaghetti-Detection via Kamera-KI aktiviert.
    /// Hinweis: aktuell ein Stub (<see cref="Services.Farm.AutoSchedulerService.CheckSpaghettiAsync"/>
    /// gibt immer false zurück) — Platzhalter für zukünftige ONNX-basierte Bilderkennung.
    /// </summary>
    public bool SpaghettiDetectionEnabled { get; set; }

    /// <summary>
    /// Prüf-Intervall für Spaghetti-Detection in Sekunden.
    /// </summary>
    public int SpaghettiDetectionInterval { get; set; } = 300;

    /// <summary>Benachrichtigung senden wenn ein Druck fehlschlägt.</summary>
    public bool NotificationOnFail { get; set; } = true;

    /// <summary>
    /// Wenn true, wird ein Drucker bei einer erkannten Anomalie (z.B. Spaghetti)
    /// automatisch pausiert um Filament-Verlust zu minimieren.
    /// </summary>
    public bool AutoPauseOnAnomaly { get; set; }
}

/*
=====================================================================
  MANUELLE DbContext-ERWEITERUNGEN
  Füge folgende Zeilen in /root/FlipsiForge/src/FlipsiForge.Core/Data/FlipsiForgeDbContext.cs ein.
=====================================================================

  1) Neue DbSet-Properties (z.B. nach den v0.2.0 DbSets, um 19:50 Uhr):

     // === v0.4.0 Farm DbSets ===
     public DbSet<PrinterCluster> PrinterClusters => Set<PrinterCluster>();
     public DbSet<PrintBatch> PrintBatches => Set<PrintBatch>();
     public DbSet<BatchItem> BatchItems => Set<BatchItem>();
     public DbSet<FarmSchedule> FarmSchedules => Set<FarmSchedule>();
     public DbSet<FarmSettings> FarmSettings => Set<FarmSettings>();

  2) In OnModelCreating (vor `base.OnModelCreating(modelBuilder);` einfügen):

     // --- PrinterCluster.PrinterIds als CSV-String speichern (List<int> ↔ string) ---
     modelBuilder.Entity<PrinterCluster>()
         .Property(c => c.PrinterIds)
         .HasConversion(
             v => string.Join(',', v),
             v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(int.Parse).ToList());

     // --- BatchItem → PrintBatch (1:N mit Cascade-Löschung) ---
     modelBuilder.Entity<BatchItem>()
         .HasOne<PrintBatch>()
         .WithMany(b => b.Items)
         .HasForeignKey(i => i.BatchId)
         .OnDelete(DeleteBehavior.Cascade);

     // --- FarmSettings als Singleton (Id=1, Seed) ---
     modelBuilder.Entity<FarmSettings>().HasData(new FarmSettings { Id = 1 });

     // --- Performance-Indizes ---
     modelBuilder.Entity<BatchItem>().HasIndex(i => i.BatchId);
     modelBuilder.Entity<BatchItem>().HasIndex(i => i.Status);
     modelBuilder.Entity<BatchItem>().HasIndex(i => i.AssignedPrinterId);
     modelBuilder.Entity<FarmSchedule>().HasIndex(s => s.BatchId);
     modelBuilder.Entity<FarmSchedule>().HasIndex(s => s.PrinterId);
     modelBuilder.Entity<FarmSchedule>().HasIndex(s => s.Status);
     modelBuilder.Entity<PrintBatch>().HasIndex(b => b.Status);
     modelBuilder.Entity<PrintBatch>().HasIndex(b => b.AssignedClusterId);

  3) Hinweis zur SQLite Schema-Drift (bekannter Pitfall):
     Da EnsureCreated() keine neuen Spalten zu bestehenden Tabellen hinzufügt,
     muss beim ersten Deployment mit Farm-Support die bestehende DB-Datei
     gelöscht werden (~/.local/share/FlipsiForge/flipsiforge.db) ODER
     eine EF-Core-Migration erstellt werden:
       dotnet ef migrations add FarmSupport --project src/FlipsiForge.Core
       dotnet ef database update
=====================================================================
*/