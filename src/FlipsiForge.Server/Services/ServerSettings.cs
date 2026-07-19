// FlipsiForge.Server — v0.2.0
// Settings-Options-Klassen für die neuen Config-Blöcke AI/Bot/Backup.
// Gebunden via builder.Services.Configure<...>(configuration.GetSection(...)).
//
// SPDX-License-Identifier: GPL-3.0-only
// (c) 2026 TechFlipsi / Fabian Kirchweger

namespace FlipsiForge.Server.Services;

/// <summary>
/// KI-Konfiguration aus appsettings.json Block "AI".
/// </summary>
public sealed class AiSettings
{
    /// <summary>"Auto" (RAM-basiert), "E4B", "E2B", "E2BQat" oder "Off".</summary>
    public string ModelChoice { get; set; } = "Auto";

    /// <summary>Manueller Pfad zu einem ONNX-Modell. Leer = Auto-Auswahl.</summary>
    public string ModelPath { get; set; } = "";

    /// <summary>True = Chat-Antworten via SSE streamen (Token-für-Token).</summary>
    public bool Streaming { get; set; } = true;
}

/// <summary>
/// Forge-Bot-Konfiguration aus Block "Bot".
/// </summary>
public sealed class BotSettings
{
    /// <summary>True = Forge-Bot aktiv (Tipps/Sprüche einblenden).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>"Rare", "Normal" oder "Often" — Häufigkeit der Tipps.</summary>
    public string Frequency { get; set; } = "Normal";
}

/// <summary>
/// Backup-Konfiguration aus Block "Backup".
/// </summary>
public sealed class BackupSettings
{
    /// <summary>"off", "daily", "weekly" — automatische Backup-Kadenz.</summary>
    public string AutoInterval { get; set; } = "off";

    /// <summary>Verzeichnis für Backups. Leer = Default (DB-Verzeichnis /backups).</summary>
    public string Path { get; set; } = "";
}

/// <summary>
/// Body-Objekt für POST /api/files/scan — startet einen Datei-Scan.
/// </summary>
public sealed class FileScanRequest
{
    /// <summary>Wurzelverzeichnisse die rekursiv gescannt werden.</summary>
    public List<string> Folders { get; set; } = new();

    /// <summary>Datei-Endungen die indiziert werden (z.B. ["STL","3MF","GCODE"]).</summary>
    public List<string> Extensions { get; set; } = new() { "STL", "OBJ", "3MF", "GCODE", "GCO", "PLY", "STEP", "STP", "AMF", "X3D" };
}

/// <summary>Body-Objekt für POST /api/files/{id}/usage.</summary>
public sealed class FileUsageRequest
{
    /// <summary>"viewed" oder "printed".</summary>
    public string Action { get; set; } = "viewed";
}

/// <summary>Body-Objekt für PATCH /api/spools/{id}/status.</summary>
public sealed class SpoolStatusUpdate
{
    /// <summary>"Active", "Empty", "Drying" oder "Archived".</summary>
    public string Status { get; set; } = "Active";
}

/// <summary>Body-Objekt für POST /api/ai/chat.</summary>
public sealed class ChatRequest
{
    /// <summary>User-Nachricht.</summary>
    public string Message { get; set; } = "";

    /// <summary>Bisheriger Chat-Verlauf.</summary>
    public List<ChatHistoryEntry> History { get; set; } = new();
}

/// <summary>Ein Eintrag im Chat-Verlauf (POST /api/ai/chat Body.History).</summary>
public sealed class ChatHistoryEntry
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = "";
}

/// <summary>Body-Objekt für POST /api/ai/embed.</summary>
public sealed class EmbedRequest
{
    public string Text { get; set; } = "";
}

/// <summary>Body-Objekt für POST /api/ai/slicer-profile.</summary>
public sealed class SlicerProfileRequest
{
    public int PrinterId { get; set; }
    public int? SpoolId { get; set; }
    public string Goal { get; set; } = "VisualQuality";
}

/// <summary>Body-Objekt für POST /api/printers/{id}/maintenance.</summary>
public sealed class MaintenanceCreateRequest
{
    public string Component { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Notes { get; set; }
}

/// <summary>Body-Objekt für POST /api/restore.</summary>
public sealed class RestoreRequest
{
    public string BackupPath { get; set; } = "";
}

/// <summary>Body-Objekt für PATCH /api/bot/settings.</summary>
public sealed class BotSettingsPatch
{
    public bool? Enabled { get; set; }
    public string? Frequency { get; set; }
}

/// <summary>Eintrag in der Bot-Message-Historie (In-Memory).</summary>
public sealed class BotMessage
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Dismissed { get; set; }
}

/// <summary>Eintrag in der Backup-Liste.</summary>
public sealed class BackupEntry
{
    public string FileName { get; set; } = "";
    public string Path { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Wartungs-Empfehlung vom MaintenanceRecommendationProvider.</summary>
public sealed class MaintenanceRecommendation
{
    public string Component { get; set; } = "";
    public string Action { get; set; } = "";
    public string Reason { get; set; } = "";
    public string? OnlineMode { get; set; }
    public int? IntervalHours { get; set; }
}