// FlipsiForge.Server — v0.2.0
// BackupService — erstellt SQLite-Backups (Kopie der DB-Datei), listet
// vorhandene Backups, und führt Restore durch. Die DB-Datei wird via
// EF Core SQLite "Data Source=..." Konfiguration gefunden.
//
// SPDX-License-Identifier: GPL-3.0-only
// (c) 2026 TechFlipsi / Fabian Kirchweger

using FlipsiForge.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlipsiForge.Server.Services;

/// <summary>
/// Erstellt und verwaltet SQLite-Backups. Backups werden als Datei-Kopie
/// der DB-Datei im Backup-Verzeichnis abgelegt.
/// </summary>
public sealed class BackupService
{
    private readonly BackupSettings _settings;
    private readonly ILogger<BackupService> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Konstruktor.</summary>
    public BackupService(
        IOptions<BackupSettings> settings,
        ILogger<BackupService> logger,
        IServiceProvider serviceProvider)
    {
        _settings = settings.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>Erstellt ein Backup und gibt dessen Pfad zurück.</summary>
    public async Task<BackupEntry> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        var dbPath = await ResolveDbPathAsync(cancellationToken);
        if (!File.Exists(dbPath))
            throw new FileNotFoundException("SQLite-Datei nicht gefunden", dbPath);

        var backupDir = GetBackupDir();
        Directory.CreateDirectory(backupDir);

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var backupFile = Path.Combine(backupDir, $"flipsiforge-{stamp}.db");
        // Atomic-ish copy — für laufende SQLite-DB ist VACUUM INTO besser,
        // aber für Stub reicht File.Copy mit geöffneter Connection.
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlipsiForgeDbContext>();
        await db.Database.ExecuteSqlRawAsync($"VACUUM INTO '{EscapeSqlPath(backupFile)}';", cancellationToken);

        var fi = new FileInfo(backupFile);
        _logger.LogInformation("Backup erstellt: {Path} ({Size} bytes)", backupFile, fi.Length);
        return new BackupEntry
        {
            FileName = fi.Name,
            Path = backupFile,
            SizeBytes = fi.Length,
            CreatedAt = fi.CreationTimeUtc
        };
    }

    /// <summary>Listet alle Backups im Backup-Verzeichnis.</summary>
    public Task<IReadOnlyList<BackupEntry>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var backupDir = GetBackupDir();
        if (!Directory.Exists(backupDir))
            return Task.FromResult<IReadOnlyList<BackupEntry>>(Array.Empty<BackupEntry>());

        var entries = Directory.EnumerateFiles(backupDir, "flipsiforge-*.db")
            .Select(p => new FileInfo(p))
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f => new BackupEntry
            {
                FileName = f.Name,
                Path = f.FullName,
                SizeBytes = f.Length,
                CreatedAt = f.CreationTimeUtc
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<BackupEntry>>(entries);
    }

    /// <summary>Stellt ein Backup wieder her — überschreibt die aktuelle DB-Datei.</summary>
    public async Task RestoreAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup-Datei nicht gefunden", backupPath);

        var dbPath = await ResolveDbPathAsync(cancellationToken);
        var backupDir = GetBackupDir();
        Directory.CreateDirectory(backupDir);

        // Aktuelle DB sichern bevor Restore
        if (File.Exists(dbPath))
        {
            var preRestoreBackup = Path.Combine(backupDir, $"pre-restore-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db");
            File.Copy(dbPath, preRestoreBackup, overwrite: true);
        }

        // Restore durch Kopie — App muss danach neu gestartet werden für sauber EF-State
        File.Copy(backupPath, dbPath, overwrite: true);
        _logger.LogInformation("Restore durchgeführt von {Source}", backupPath);
    }

    private string GetBackupDir()
    {
        if (!string.IsNullOrWhiteSpace(_settings.Path))
            return _settings.Path;
        var defaultDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlipsiForge", "backups");
        return defaultDir;
    }

    private async Task<string> ResolveDbPathAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlipsiForgeDbContext>();
        var conn = db.Database.GetConnectionString();
        if (string.IsNullOrEmpty(conn))
            throw new InvalidOperationException("SQLite-Connection-String fehlt");
        // Parse "Data Source=/path/to/flipsiforge.db"
        var idx = conn.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) throw new InvalidOperationException("Unerwarteter Connection-String: " + conn);
        var path = conn[(idx + "Data Source=".Length)..].Trim();
        // Strip semicolon-suffix
        var semi = path.IndexOf(';');
        if (semi >= 0) path = path[..semi];
        return path;
    }

    private static string EscapeSqlPath(string path) => path.Replace("'", "''");
}