using FlipsiForge.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FlipsiForge.Core.Data;

/// <summary>SQLite-Datenbank für FlipsiForge.</summary>
public class FlipsiForgeDbContext : DbContext
{
    public FlipsiForgeDbContext() { }
    public FlipsiForgeDbContext(DbContextOptions<FlipsiForgeDbContext> options) : base(options) { }

    // === v0.1.0 DbSets ===
    public DbSet<Printer> Printers => Set<Printer>();
    public DbSet<Spool> Spools => Set<Spool>();
    public DbSet<ScannedFile> ScannedFiles => Set<ScannedFile>();
    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();
    public DbSet<PrintHistory> PrintHistory => Set<PrintHistory>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<FilamentBrandSpec> FilamentBrandSpecs => Set<FilamentBrandSpec>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();

    // === v0.2.0 DbSets ===
    /// <summary>Favoriten-Markierungen für gescannte Dateien.</summary>
    public DbSet<FavoriteFile> FavoriteFiles => Set<FavoriteFile>();
    /// <summary>Zugriffs-Logs für gescannte Dateien (Häufigkeit-Sortierung).</summary>
    public DbSet<FileUsageLog> FileUsageLogs => Set<FileUsageLog>();
    /// <summary>Forge-Bot Historie.</summary>
    public DbSet<BotMessage> BotMessages => Set<BotMessage>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FlipsiForge", "flipsiforge.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ScannedFile.Embedding als JSON-kompatiblen String speichern
        modelBuilder.Entity<ScannedFile>()
            .Property(f => f.Embedding)
            .HasConversion(
                v => v != null ? string.Join(',', v) : null,
                v => v != null ? v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(float.Parse).ToArray() : null);

        // AppSettings als Singleton (nur eine Zeile)
        modelBuilder.Entity<AppSettings>().HasData(new AppSettings { Id = 1 });

        // v0.2.0: Indizes
        modelBuilder.Entity<FavoriteFile>()
            .HasIndex(f => f.ScannedFileId);

        modelBuilder.Entity<FileUsageLog>()
            .HasIndex(f => f.ScannedFileId);

        modelBuilder.Entity<ScannedFile>()
            .HasIndex(f => f.ContentHash);

        base.OnModelCreating(modelBuilder);
    }
}