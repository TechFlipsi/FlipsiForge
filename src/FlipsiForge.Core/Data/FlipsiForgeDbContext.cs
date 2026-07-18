using FlipsiForge.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FlipsiForge.Core.Data;

/// <summary>SQLite-Datenbank für FlipsiForge.</summary>
public class FlipsiForgeDbContext : DbContext
{
    public FlipsiForgeDbContext() { }
    public FlipsiForgeDbContext(DbContextOptions<FlipsiForgeDbContext> options) : base(options) { }

    public DbSet<Printer> Printers => Set<Printer>();
    public DbSet<Spool> Spools => Set<Spool>();
    public DbSet<ScannedFile> ScannedFiles => Set<ScannedFile>();
    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();
    public DbSet<PrintHistory> PrintHistory => Set<PrintHistory>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<FilamentBrandSpec> FilamentBrandSpecs => Set<FilamentBrandSpec>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();

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
        // ScannedFile.Embedding als JSON speichern
        modelBuilder.Entity<ScannedFile>()
            .Property(f => f.Embedding)
            .HasConversion(
                v => v != null ? string.Join(',', v) : null,
                v => v != null ? v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(float.Parse).ToArray() : null);

        // AppSettings als Singleton (nur eine Zeile)
        modelBuilder.Entity<AppSettings>().HasData(new AppSettings { Id = 1 });

        base.OnModelCreating(modelBuilder);
    }
}