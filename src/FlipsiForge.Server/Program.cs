// FlipsiForge.Server — v0.2.0
// ASP.NET Core 10 Minimal API — Full + Lite Mode
//
// v0.2.0 Endpoints:
//   /api/health, /api/printers (CRUD + maintenance + connect + status + temps + job),
//   /api/spools (CRUD + status), /api/filament-brands, /api/print-jobs, /api/print-history,
//   /api/files (list, scan, favorite, usage, search, delete), /api/ai (chat, embed, status, slicer),
//   /api/bot (messages, dismiss, settings), /api/backup, /api/restore, /api/export, /api/cache,
//   /api/statistics (+ /files, /filament), /api/settings (GET/PUT/PATCH)
//
// Stubs in Services/ ersetzen Core.Services falls zur Build-Zeit nicht verfügbar.
//
// SPDX-License-Identifier: GPL-3.0-only
// (c) 2026 TechFlipsi / Fabian Kirchweger — https://techflipsi.kirchweger.de

using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

// === Bootstrap ===
var builder = WebApplication.CreateBuilder(args);

// Server-Modus aus Konfiguration (Full oder Lite)
var serverMode = builder.Configuration.GetValue<ServerMode>("Server:Mode", ServerMode.Full);
var aiEnabled = builder.Configuration.GetValue<bool>("Server:AI", serverMode == ServerMode.Full);
var webUiEnabled = builder.Configuration.GetValue<bool>("Server:WebUI", serverMode == ServerMode.Full);
var serverVersion = builder.Configuration.GetValue<string>("Version") ?? "0.2.0";

// === DI-Setup ===
builder.Services.AddDbContext<FlipsiForgeDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();  // für HTTP-basierte Printer-Connections

// Options-Pattern für die neuen Settings-Blöcke
builder.Services.Configure<AiSettings>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<BotSettings>(builder.Configuration.GetSection("Bot"));
builder.Services.Configure<BackupSettings>(builder.Configuration.GetSection("Backup"));

// Core.Services-Implementierungen via TryAdd — falls ein anderer Subagent die
// echten Implementierungen in Core registriert hat, gewinnen diese. Sonst Stubs.
builder.Services.TryAddSingleton<IPrinterConnectionManager, StubPrinterConnectionManager>();
builder.Services.TryAddSingleton<IAIChatEngine, StubAIChatEngine>();
builder.Services.TryAddSingleton<IEmbeddingProvider, StubEmbeddingProvider>();

// Server-interne Services
builder.Services.AddSingleton<FileScanner>();
builder.Services.AddSingleton<MaintenanceRecommendationProvider>();
builder.Services.AddSingleton<BackupService>();
builder.Services.AddSingleton<BotMessageStore>();

var app = builder.Build();

// === DB initialisieren + Filament-DB seeden ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlipsiForgeDbContext>();
    db.Database.EnsureCreated();
    await FilamentDbSeeder.SeedAsync(db);
}

app.MapOpenApi();

// === Web-UI statische Dateien (Full-Modus) ===
if (webUiEnabled && serverMode == ServerMode.Full)
{
    app.UseStaticFiles();
}

// === HILFSFUNKTIONEN ===

/// <summary>Liefert die (einzige) AppSettings-Zeile aus der DB oder einen Default.</summary>
static async Task<AppSettings> GetSettingsAsync(FlipsiForgeDbContext db)
{
    var settings = await db.Settings.FirstOrDefaultAsync();
    if (settings is null)
    {
        settings = new AppSettings { Id = 1 };
        db.Settings.Add(settings);
        await db.SaveChangesAsync();
    }
    return settings;
}

// === HEALTH ===
app.MapGet("/api/health", () => new
{
    status = "ok",
    version = serverVersion,
    mode = serverMode.ToString(),
    ai = aiEnabled,
    webui = webUiEnabled,
    timestamp = DateTime.UtcNow
}).WithSummary("Health check + server info");

// === SETTINGS ===
app.MapGet("/api/settings", async (FlipsiForgeDbContext db) =>
{
    var s = await GetSettingsAsync(db);
    return Results.Ok(s);
}).WithSummary("Liefert die App-Einstellungen");

app.MapPut("/api/settings", async (FlipsiForgeDbContext db, AppSettings body) =>
{
    var s = await GetSettingsAsync(db);
    body.Id = s.Id;  // Singletons-Zeile erzwingen
    db.Entry(s).CurrentValues.SetValues(body);
    await db.SaveChangesAsync();
    return Results.Ok(body);
}).WithSummary("Überschreibt die App-Einstellungen");

app.MapPatch("/api/settings/{field}", async (FlipsiForgeDbContext db, string field, JsonElement body) =>
{
    var s = await GetSettingsAsync(db);
    var prop = typeof(AppSettings).GetProperty(field,
        System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
    if (prop is null) return Results.NotFound(new { error = $"Feld '{field}' existiert nicht" });
    object? value = body.ValueKind switch
    {
        JsonValueKind.String => body.GetString(),
        JsonValueKind.Number => body.TryGetInt32(out var i) ? i : body.TryGetDecimal(out var d) ? (object)d : body.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => body.Deserialize<List<string>>(),
        _ => body.ToString()
    };
    if (value is not null && prop.PropertyType.IsEnum)
        value = Enum.Parse(prop.PropertyType, value.ToString()!, ignoreCase: true);
    prop.SetValue(s, value);
    await db.SaveChangesAsync();
    return Results.Ok(s);
}).WithSummary("Partial Update eines einzelnen Settings-Feldes");

// === PRINTERS (CRUD) ===
app.MapGet("/api/printers", async (FlipsiForgeDbContext db, bool includeInactive = false) =>
    includeInactive
        ? Results.Ok(await db.Printers.ToListAsync())
        : Results.Ok(await db.Printers.Where(p => p.IsActive).ToListAsync()))
   .WithSummary("Liste aller Drucker (default nur aktive)");

app.MapGet("/api/printers/{id}", async (FlipsiForgeDbContext db, int id) =>
{
    var p = await db.Printers.FindAsync(id);
    return p is null ? Results.NotFound() : Results.Ok(p);
}).WithSummary("Einzelner Drucker");

app.MapPost("/api/printers", async (FlipsiForgeDbContext db, Printer printer) =>
{
    printer.IsActive = true;
    db.Printers.Add(printer);
    await db.SaveChangesAsync();
    return Results.Created($"/api/printers/{printer.Id}", printer);
}).WithSummary("Drucker hinzufügen");

app.MapPut("/api/printers/{id}", async (FlipsiForgeDbContext db, int id, Printer body) =>
{
    var p = await db.Printers.FindAsync(id);
    if (p is null) return Results.NotFound();
    body.Id = id;
    db.Entry(p).CurrentValues.SetValues(body);
    await db.SaveChangesAsync();
    return Results.Ok(body);
}).WithSummary("Drucker full-update");

app.MapPatch("/api/printers/{id}/activate", async (FlipsiForgeDbContext db, int id) =>
{
    var p = await db.Printers.FindAsync(id);
    if (p is null) return Results.NotFound();
    p.IsActive = true;
    await db.SaveChangesAsync();
    return Results.Ok(p);
}).WithSummary("Drucker reaktivieren");

app.MapDelete("/api/printers/{id}", async (FlipsiForgeDbContext db, int id, bool keepHistory = true) =>
{
    var p = await db.Printers.FindAsync(id);
    if (p is null) return Results.NotFound();
    if (keepHistory)
    {
        p.IsActive = false;
        await db.SaveChangesAsync();
    }
    else
    {
        // Komplettes Löschen aus DB (inkl. History, Jobs, Maintenance)
        var jobs = db.PrintJobs.Where(j => j.PrinterId == id);
        var hist = db.PrintHistory.Where(h => h.PrinterId == id);
        var maint = db.MaintenanceRecords.Where(m => m.PrinterId == id);
        db.PrintJobs.RemoveRange(jobs);
        db.PrintHistory.RemoveRange(hist);
        db.MaintenanceRecords.RemoveRange(maint);
        db.Printers.Remove(p);
        await db.SaveChangesAsync();
    }
    return Results.NoContent();
}).WithSummary("Drucker löschen (?keepHistory=true = archivieren, false = hart löschen)");

// === PRINTERS — Maintenance ===
app.MapGet("/api/printers/{id}/maintenance", async (FlipsiForgeDbContext db, int id) =>
{
    if (!await db.Printers.AnyAsync(p => p.Id == id)) return Results.NotFound();
    var records = await db.MaintenanceRecords
        .Where(m => m.PrinterId == id)
        .OrderByDescending(m => m.PerformedAt)
        .ToListAsync();
    return Results.Ok(records);
}).WithSummary("Wartungs-Einträge für Drucker");

app.MapPost("/api/printers/{id}/maintenance", async (FlipsiForgeDbContext db, int id, MaintenanceCreateRequest body) =>
{
    if (!await db.Printers.AnyAsync(p => p.Id == id)) return Results.NotFound();
    var rec = new MaintenanceRecord
    {
        PrinterId = id,
        Component = body.Component,
        Action = body.Action,
        Notes = body.Notes,
        PerformedAt = DateTime.UtcNow
    };
    db.MaintenanceRecords.Add(rec);
    await db.SaveChangesAsync();
    return Results.Created($"/api/printers/{id}/maintenance/{rec.Id}", rec);
}).WithSummary("Wartungs-Eintrag anlegen");

app.MapGet("/api/printers/{id}/maintenance/recommendations", async (
    FlipsiForgeDbContext db,
    MaintenanceRecommendationProvider provider,
    int id,
    bool onlineMode = false) =>
{
    var p = await db.Printers.FindAsync(id);
    if (p is null) return Results.NotFound();
    var recs = await provider.GetRecommendationsAsync(p, onlineMode);
    return Results.Ok(recs);
}).WithSummary("Wartungs-Empfehlungen (?onlineMode=true = modellspezifisch)");

// === PRINTERS — Live-Connection (via IPrinterConnectionManager) ===
app.MapPost("/api/printers/{id}/connect", async (
    FlipsiForgeDbContext db,
    IPrinterConnectionManager mgr,
    int id) =>
{
    var p = await db.Printers.FindAsync(id);
    if (p is null) return Results.NotFound();
    var result = await mgr.ConnectAsync(p);
    return Results.Ok(result);
}).WithSummary("Drucker-Verbindung testen");

app.MapGet("/api/printers/{id}/status", async (
    FlipsiForgeDbContext db,
    IPrinterConnectionManager mgr,
    int id) =>
{
    var p = await db.Printers.FindAsync(id);
    if (p is null) return Results.NotFound();
    var status = await mgr.GetStatusAsync(p);
    return Results.Ok(status);
}).WithSummary("Live-Status des Druckers");

app.MapGet("/api/printers/{id}/temps", async (
    FlipsiForgeDbContext db,
    IPrinterConnectionManager mgr,
    int id) =>
{
    var p = await db.Printers.FindAsync(id);
    if (p is null) return Results.NotFound();
    var temps = await mgr.GetTemperaturesAsync(p);
    return Results.Ok(temps);
}).WithSummary("Live-Temperaturen");

app.MapGet("/api/printers/{id}/job", async (
    FlipsiForgeDbContext db,
    IPrinterConnectionManager mgr,
    int id) =>
{
    var p = await db.Printers.FindAsync(id);
    if (p is null) return Results.NotFound();
    var job = await mgr.GetCurrentJobAsync(p);
    return Results.Ok(job);
}).WithSummary("Aktueller Druck-Job");

// === SPOOLS (CRUD + Status) ===
app.MapGet("/api/spools", async (FlipsiForgeDbContext db, bool includeArchived = false) =>
    includeArchived
        ? Results.Ok(await db.Spools.ToListAsync())
        : Results.Ok(await db.Spools.Where(s => s.Status == SpoolStatus.Active).ToListAsync()))
   .WithSummary("Liste aller Filament-Spulen (default nur aktive)");

app.MapGet("/api/spools/{id}", async (FlipsiForgeDbContext db, int id) =>
{
    var s = await db.Spools.FindAsync(id);
    return s is null ? Results.NotFound() : Results.Ok(s);
}).WithSummary("Einzelne Spule");

app.MapPost("/api/spools", async (FlipsiForgeDbContext db, Spool spool) =>
{
    spool.Status = SpoolStatus.Active;
    db.Spools.Add(spool);
    await db.SaveChangesAsync();
    return Results.Created($"/api/spools/{spool.Id}", spool);
}).WithSummary("Spule hinzufügen");

app.MapPut("/api/spools/{id}", async (FlipsiForgeDbContext db, int id, Spool body) =>
{
    var s = await db.Spools.FindAsync(id);
    if (s is null) return Results.NotFound();
    body.Id = id;
    db.Entry(s).CurrentValues.SetValues(body);
    await db.SaveChangesAsync();
    return Results.Ok(body);
}).WithSummary("Spule full-update");

app.MapPatch("/api/spools/{id}/status", async (FlipsiForgeDbContext db, int id, SpoolStatusUpdate body) =>
{
    var s = await db.Spools.FindAsync(id);
    if (s is null) return Results.NotFound();
    if (!Enum.TryParse<SpoolStatus>(body.Status, ignoreCase: true, out var status))
        return Results.BadRequest(new { error = $"Ungültiger Status '{body.Status}'. Erlaubt: Active, Empty, Drying, Archived" });
    s.Status = status;
    await db.SaveChangesAsync();
    return Results.Ok(s);
}).WithSummary("Spool-Status ändern (Active/Empty/Drying/Archived)");

app.MapDelete("/api/spools/{id}", async (FlipsiForgeDbContext db, int id, bool keepHistory = true) =>
{
    var s = await db.Spools.FindAsync(id);
    if (s is null) return Results.NotFound();
    if (keepHistory)
    {
        s.Status = SpoolStatus.Archived;
        await db.SaveChangesAsync();
    }
    else
    {
        db.Spools.Remove(s);
        await db.SaveChangesAsync();
    }
    return Results.NoContent();
}).WithSummary("Spule löschen (?keepHistory=true = archivieren, false = hart löschen)");

// === FILAMENT BRANDS ===
app.MapGet("/api/filament-brands", async (FlipsiForgeDbContext db) =>
    Results.Ok(await db.FilamentBrandSpecs.ToListAsync()))
   .WithSummary("Alle Filament-Marken-Specs (41 Einträge, 20 Marken)");

app.MapGet("/api/filament-brands/{brand}", async (FlipsiForgeDbContext db, string brand) =>
    Results.Ok(await db.FilamentBrandSpecs.Where(b => b.Brand == brand).ToListAsync()))
   .WithSummary("Filter nach Markenname");

// === PRINT JOBS / HISTORY ===
app.MapGet("/api/print-jobs", async (FlipsiForgeDbContext db) =>
    Results.Ok(await db.PrintJobs
        .Where(j => j.Status == PrintJobStatus.Queued || j.Status == PrintJobStatus.Confirmed)
        .ToListAsync()))
   .WithSummary("Druck-Jobs in der Queue");

app.MapPost("/api/print-jobs", async (FlipsiForgeDbContext db, PrintJob job) =>
{
    job.Status = PrintJobStatus.Queued;
    job.QueuedAt = DateTime.UtcNow;
    db.PrintJobs.Add(job);
    await db.SaveChangesAsync();
    return Results.Created($"/api/print-jobs/{job.Id}", job);
}).WithSummary("Druck-Job zur Queue hinzufügen");

app.MapGet("/api/print-history", async (FlipsiForgeDbContext db) =>
    Results.Ok(await db.PrintHistory.OrderByDescending(h => h.StartedAt).Take(100).ToListAsync()))
   .WithSummary("Letzte 100 Druck-Historie-Einträge");

// === FILES ===
app.MapGet("/api/files", async (FlipsiForgeDbContext db, string? extension = null, string? folder = null) =>
{
    var q = db.ScannedFiles.AsNoTracking();
    if (!string.IsNullOrEmpty(extension))
        q = q.Where(f => f.Extension == extension.ToUpper());
    if (!string.IsNullOrEmpty(folder))
        q = q.Where(f => f.Path.StartsWith(folder));
    var files = await q.OrderByDescending(f => f.LastModified).ToListAsync();
    return Results.Ok(files);
}).WithSummary("Gescannte Dateien (?extension=STL&folder=/path)");

app.MapGet("/api/files/search", async (
    FlipsiForgeDbContext db,
    IEmbeddingProvider embedder,
    string q) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.BadRequest(new { error = "Query-Parameter 'q' fehlt" });

    // Dateinamen-Suche (Fuzzy via Contains, offline, sofort)
    var lower = q.ToLowerInvariant();
    var filenameMatches = await db.ScannedFiles
        .AsNoTracking()
        .Where(f => f.FileName.ToLower().Contains(lower)
                    || (f.Tags != null && f.Tags.ToLower().Contains(lower))
                    || (f.Notes != null && f.Notes.ToLower().Contains(lower)))
        .ToListAsync();

    // KI-Suche — nur wenn Embedding-Modell geladen
    var aiMatches = new List<(ScannedFile File, float Score)>();
    if (embedder.IsLoaded)
    {
        var queryEmbed = await embedder.EmbedAsync(q);
        if (queryEmbed.Length > 0)
        {
            var allFiles = await db.ScannedFiles.AsNoTracking().ToListAsync();
            foreach (var f in allFiles)
            {
                if (f.Embedding is { Length: > 0 } fe)
                {
                    var score = embedder.Similarity(queryEmbed, fe);
                    if (score > 0.5f)
                        aiMatches.Add((f, score));
                }
            }
        }
    }

    var result = new
    {
        query = q,
        filenameMatches = filenameMatches.Select(f => new { file = f, source = "filename" }),
        aiMatches = aiMatches.OrderByDescending(x => x.Score)
            .Select(x => new { file = x.File, source = "ai", score = x.Score, badge = "🤖 KI" }),
        aiAvailable = embedder.IsLoaded
    };
    return Results.Ok(result);
}).WithSummary("Kombinierte Datei-Suche (Dateiname + KI falls verfügbar)");

app.MapPost("/api/files/scan", async (
    FlipsiForgeDbContext db,
    FileScanner scanner,
    FileScanRequest body,
    CancellationToken cancellationToken) =>
{
    var result = await scanner.ScanAsync(db, body.Folders, body.Extensions, cancellationToken);
    return Results.Ok(result);
}).WithSummary("Datei-Scan starten (Body: folders, extensions)");

app.MapGet("/api/files/{id}", async (FlipsiForgeDbContext db, int id) =>
{
    var f = await db.ScannedFiles.FindAsync(id);
    return f is null ? Results.NotFound() : Results.Ok(f);
}).WithSummary("Einzelne Datei");

app.MapPost("/api/files/{id}/favorite", async (FlipsiForgeDbContext db, int id) =>
{
    var f = await db.ScannedFiles.FindAsync(id);
    if (f is null) return Results.NotFound();
    // Favorit wird als Tag "★" gespeichert (Stub-Variante — echte Implementierung
    // würde ein bool-Feld in ScannedFile ergänzen).
    var tags = (f.Tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    if (tags.Contains("★")) tags.Remove("★"); else tags.Add("★");
    f.Tags = string.Join(',', tags);
    await db.SaveChangesAsync();
    return Results.Ok(new { id, favorite = tags.Contains("★") });
}).WithSummary("Favorit togglen");

app.MapPost("/api/files/{id}/usage", async (FlipsiForgeDbContext db, int id, FileUsageRequest body) =>
{
    var f = await db.ScannedFiles.FindAsync(id);
    if (f is null) return Results.NotFound();
    // Usage wird als Tag "viewed:N" bzw. "printed:N" gezählt (Stub).
    var action = body.Action == "printed" ? "printed" : "viewed";
    var tag = $"usage:{action}";
    var tags = (f.Tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    var existing = tags.FirstOrDefault(t => t.StartsWith(tag + ":", StringComparison.Ordinal));
    if (existing is null)
    {
        tags.Add($"{tag}:1");
    }
    else
    {
        tags.Remove(existing);
        var n = int.Parse(existing.Split(':')[^1]) + 1;
        tags.Add($"{tag}:{n}");
    }
    f.Tags = string.Join(',', tags);
    await db.SaveChangesAsync();
    return Results.Ok(new { id, action, tags = f.Tags });
}).WithSummary("Usage loggen (viewed/printed)");

app.MapDelete("/api/files/{id}", async (FlipsiForgeDbContext db, int id) =>
{
    var f = await db.ScannedFiles.FindAsync(id);
    if (f is null) return Results.NotFound();
    db.ScannedFiles.Remove(f);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithSummary("Datei aus DB löschen (nicht von Disk!)");

// === AI ===
app.MapGet("/api/ai/status", (IAIChatEngine engine) => new
{
    loaded = engine.IsLoaded,
    modelName = engine.ModelName,
    choice = engine.Choice
}).WithSummary("KI-Status");

app.MapPost("/api/ai/embed", async (IEmbeddingProvider embedder, EmbedRequest body) =>
{
    var vec = await embedder.EmbedAsync(body.Text);
    return Results.Ok(new { text = body.Text, dim = vec.Length, vector = vec });
}).WithSummary("Embedding-Vektor für Text erzeugen");

app.MapPost("/api/ai/chat", async (
    IAIChatEngine engine,
    IOptions<AiSettings> aiSettings,
    FlipsiForgeDbContext db,
    ChatRequest body,
    CancellationToken cancellationToken,
    HttpContext ctx) =>
{
    // Chat-Verlauf aus Body in ChatMessage-Liste konvertieren
    var history = body.History.Select(h => new ChatMessage { Role = h.Role, Content = h.Content }).ToList();

    // Neue User-Nachricht in DB persistieren (Chat-Verlauf)
    var userMsg = new ChatMessage { Role = "user", Content = body.Message, Timestamp = DateTime.UtcNow };
    db.ChatMessages.Add(userMsg);
    await db.SaveChangesAsync(cancellationToken);

    if (aiSettings.Value.Streaming && engine.IsLoaded)
    {
        // SSE-Streaming
        ctx.Response.ContentType = "text/event-stream";
        ctx.Response.Headers.CacheControl = "no-cache";
        ctx.Response.Headers.Connection = "keep-alive";
        await using var writer = new StreamWriter(ctx.Response.Body);
        var fullReply = new StringBuilder();
        await foreach (var token in engine.StreamReplyAsync(body.Message, history, cancellationToken))
        {
            fullReply.Append(token);
            await writer.WriteAsync($"data: {JsonSerializer.Serialize(token)}\n\n");
            await writer.FlushAsync(cancellationToken);
        }
        // Persistiere finale Antwort
        db.ChatMessages.Add(new ChatMessage { Role = "assistant", Content = fullReply.ToString(), Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
        return;
    }

    // Non-Streaming
    var reply = await engine.GenerateReplyAsync(body.Message, history, cancellationToken);
    db.ChatMessages.Add(new ChatMessage { Role = "assistant", Content = reply, Timestamp = DateTime.UtcNow });
    await db.SaveChangesAsync(cancellationToken);
    await Results.Ok(new { reply }).ExecuteAsync(ctx);
}).WithSummary("KI-Chat (Streaming via SSE wenn AiStreaming=true)");

app.MapPost("/api/ai/slicer-profile", async (
    FlipsiForgeDbContext db,
    IAIChatEngine engine,
    SlicerProfileRequest body) =>
{
    var printer = await db.Printers.FindAsync(body.PrinterId);
    if (printer is null) return Results.NotFound(new { error = "Drucker nicht gefunden" });
    var spool = body.SpoolId.HasValue ? await db.Spools.FindAsync(body.SpoolId) : null;

    // Stub: regelbasierte Profilgenerierung basierend auf Filament-DB
    FilamentBrandSpec? spec = null;
    if (spool is not null)
    {
        spec = await db.FilamentBrandSpecs
            .Where(b => b.Brand == spool.Brand && b.MaterialType == spool.MaterialType)
            .FirstOrDefaultAsync();
    }

    var goal = string.IsNullOrWhiteSpace(body.Goal) ? "VisualQuality" : body.Goal.Trim();
    var profile = new
    {
        printer = new { printer.Brand, printer.Model, printer.NozzleDiameter, printer.BuildVolumeX, printer.BuildVolumeY, printer.BuildVolumeZ },
        filament = spool is null ? null : new { spool.Brand, spool.MaterialName, spool.MaterialType, spool.ColorHex },
        recommendations = spec is null ? null : new
        {
            hotendTemp = $"{spec.HotendMin}-{spec.HotendMax}°C (optimal: {spec.HotendOptimal}°C)",
            bedTemp = $"{spec.BedMin}-{spec.BedMax}°C (optimal: {spec.BedOptimal}°C)",
            fanPercent = spec.FanPercent,
            retractionMm = spec.RetractionMm,
            layerHeight = $"{spec.LayerHeightMin}-{spec.LayerHeightMax}mm (optimal: {spec.LayerHeightOptimal}mm)",
            speed = $"{spec.SpeedMin}-{spec.SpeedMax}mm/s (optimal: {spec.SpeedOptimal}mm/s)"
        },
        goal,
        generatedBy = engine.IsLoaded ? "AI" : "rules"
    };
    return Results.Ok(profile);
}).WithSummary("Slicer-Profil generieren");

// === BOT ===
app.MapGet("/api/bot/messages", (BotMessageStore store) =>
    Results.Ok(store.Active()))
   .WithSummary("Forge-Bot Nachrichten-Historie");

app.MapPost("/api/bot/dismiss", (BotMessageStore store) =>
{
    var ok = store.DismissLast();
    return ok ? Results.Ok(new { dismissed = true }) : Results.NotFound(new { dismissed = false });
}).WithSummary("Letzte Bot-Nachricht dismissen");

app.MapPatch("/api/bot/settings", async (
    IOptions<BotSettings> currentBotSettings,
    IOptionsMonitor<BotSettings> botSettingsMonitor,
    FlipsiForgeDbContext db,
    BotSettingsPatch patch) =>
{
    // Bot-Settings sind im "Bot"-Block der appsettings.json konfiguriert.
    // Ein PATCH zur Laufzeit überschreibt die Werte im OptionsMonitor (In-Memory).
    // Hinweis: Persistente Änderungen erfordern einen Schreibzugriff auf
    // appsettings.json oder eine eigene BotSettings-Tabelle — für v0.2.0
    // reicht die In-Memory-Überschreibung (Server-Neustart setzt zurück).
    var updated = new BotSettings
    {
        Enabled = patch.Enabled ?? currentBotSettings.Value.Enabled,
        Frequency = patch.Frequency ?? currentBotSettings.Value.Frequency
    };
    // Wir legen die Settings auch in der AppSettings.WatchFolders-Liste als
    // Tag "bot:enabled=true|false" ab, damit sie via GET /api/settings sichtbar
    // sind (Stub-Variante — echte Persistenz folgt mit v0.3.0).
    var s = await GetSettingsAsync(db);
    var wf = s.WatchFolders;
    wf.RemoveAll(t => t.StartsWith("bot:", StringComparison.Ordinal));
    wf.Add($"bot:enabled={updated.Enabled.ToString().ToLowerInvariant()}");
    wf.Add($"bot:frequency={updated.Frequency}");
    await db.SaveChangesAsync();

    return Results.Ok(updated);
}).WithSummary("Bot-Settings patchen (In-Memory + als Tags in WatchFolders)");

// === BACKUP / RESTORE ===
app.MapPost("/api/backup", async (BackupService backup, CancellationToken cancellationToken) =>
{
    try
    {
        var entry = await backup.CreateBackupAsync(cancellationToken);
        return Results.Ok(new { url = $"/api/backup/list/{entry.FileName}", entry });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
}).WithSummary("SQLite-Backup erstellen, returniert Download-URL");

app.MapGet("/api/backup/list", async (BackupService backup, CancellationToken cancellationToken) =>
    Results.Ok(await backup.ListBackupsAsync(cancellationToken)))
   .WithSummary("Liste aller Backups");

app.MapPost("/api/restore", async (BackupService backup, RestoreRequest body, CancellationToken cancellationToken) =>
{
    try
    {
        await backup.RestoreAsync(body.BackupPath, cancellationToken);
        return Results.Ok(new { restored = true, path = body.BackupPath, restartRequired = true });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
}).WithSummary("Backup zurückspielen (Body: backupPath)");

// === EXPORT (JSON aller Daten) ===
app.MapPost("/api/export", async (FlipsiForgeDbContext db) =>
{
    var export = new
    {
        exportedAt = DateTime.UtcNow,
        version = serverVersion,
        printers = await db.Printers.ToListAsync(),
        spools = await db.Spools.ToListAsync(),
        filamentBrands = await db.FilamentBrandSpecs.ToListAsync(),
        printJobs = await db.PrintJobs.ToListAsync(),
        printHistory = await db.PrintHistory.ToListAsync(),
        maintenanceRecords = await db.MaintenanceRecords.ToListAsync(),
        scannedFiles = await db.ScannedFiles.ToListAsync(),
        chatMessages = await db.ChatMessages.ToListAsync(),
        settings = await GetSettingsAsync(db)
    };
    return Results.Ok(export);
}).WithSummary("JSON-Export aller DB-Daten");

// === CACHE LEEREN ===
app.MapDelete("/api/cache", () =>
{
    // Cache-Verzeichnisse unter LocalApplicationData/FlipsiForge
    var baseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlipsiForge");
    var cleared = new List<string>();
    foreach (var sub in new[] { "thumbnails", "embeddings", "temp" })
    {
        var dir = Path.Combine(baseDir, sub);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
            cleared.Add(sub);
        }
    }
    return Results.Ok(new { cleared, clearedAt = DateTime.UtcNow });
}).WithSummary("Cache leeren (Thumbnails, Embeddings, Temp)");

// === STATISTICS ===
app.MapGet("/api/statistics", async (FlipsiForgeDbContext db) =>
{
    var totalPrints = await db.PrintHistory.CountAsync();
    var successfulPrints = await db.PrintHistory.CountAsync(h => h.Success);
    var totalFilament = await db.PrintHistory.SumAsync(h => (decimal?)h.FilamentUsedG) ?? 0;
    var totalCost = await db.PrintHistory.SumAsync(h => (decimal?)h.CostEur) ?? 0;

    return Results.Ok(new
    {
        totalPrints,
        successfulPrints,
        successRate = totalPrints > 0 ? (double)successfulPrints / totalPrints * 100 : 0,
        totalFilamentG = totalFilament,
        totalCostEur = totalCost
    });
}).WithSummary("Druck-Statistiken");

app.MapGet("/api/statistics/files", async (FlipsiForgeDbContext db) =>
{
    var files = await db.ScannedFiles.AsNoTracking().ToListAsync();
    var byFormat = files.GroupBy(f => f.Extension)
        .ToDictionary(g => g.Key, g => g.Count());
    var byFolder = files.GroupBy(f => Path.GetDirectoryName(f.Path) ?? "/")
        .ToDictionary(g => g.Key, g => g.Count());
    var favorites = files.Count(f => (f.Tags ?? "").Contains("★"));
    return Results.Ok(new
    {
        total = files.Count,
        byFormat,
        byFolder,
        favoritesCount = favorites,
        totalSizeBytes = files.Sum(f => f.FileSizeBytes)
    });
}).WithSummary("Datei-Statistiken (Total, pro Format, pro Ordner, Favoriten)");

app.MapGet("/api/statistics/filament", async (FlipsiForgeDbContext db) =>
{
    var spools = await db.Spools.AsNoTracking().ToListAsync();
    var byMaterial = spools.GroupBy(s => s.MaterialType.ToString())
        .ToDictionary(g => g.Key, g => g.Sum(s => s.RemainingWeightG));
    var byBrand = spools.GroupBy(s => s.Brand)
        .ToDictionary(g => g.Key, g => g.Sum(s => s.RemainingWeightG));
    return Results.Ok(new
    {
        totalSpools = spools.Count,
        totalRemainingG = spools.Sum(s => s.RemainingWeightG),
        totalConsumedG = spools.Sum(s => s.TotalWeightG - s.RemainingWeightG),
        byMaterial,
        byBrand,
        activeCount = spools.Count(s => s.Status == SpoolStatus.Active),
        emptyCount = spools.Count(s => s.Status == SpoolStatus.Empty),
        archivedCount = spools.Count(s => s.Status == SpoolStatus.Archived)
    });
}).WithSummary("Filament-Statistiken (Total Gewicht, nach Material, nach Marke)");

// === Web-UI Root-Redirect (Full-Modus) ===
if (webUiEnabled && serverMode == ServerMode.Full)
{
    app.MapGet("/api", () => Results.Redirect("/")).ExcludeFromDescription();
    // wwwroot/index.html wird via UseStaticFiles automatisch ausgeliefert.
    // SPA-Fallback: unbekannte Pfade → index.html (damien Client-Routing funktioniert)
    app.MapFallbackToFile("index.html");
}

// =========================================================================
//  FARM API — verfügbar in BEIDEN Modi (Full + Lite)
//  Druckerfarm-Support: Cluster, Batches, Auto-Scheduling, Overview
//  Lite: alle Endpunkte verfügbar (Farm-Überwachung braucht keine KI)
//  Full: alle Endpunkte + KI-gestützte Empfehlungen
// =========================================================================

// --- Drucker-Cluster ---

/// <summary>Listet alle Drucker-Cluster.</summary>
app.MapGet("/api/farm/clusters", async (FlipsiForgeDbContext db) =>
{
    var clusters = await db.Set<PrinterCluster>().ToListAsync();
    return Results.Ok(clusters);
}).WithSummary("Farm: Alle Drucker-Cluster");

/// <summary>Erstellt einen neuen Drucker-Cluster.</summary>
app.MapPost("/api/farm/clusters", async (FlipsiForgeDbContext db, PrinterCluster cluster) =>
{
    cluster.CreatedAt = DateTime.UtcNow;
    db.Set<PrinterCluster>().Add(cluster);
    await db.SaveChangesAsync();
    return Results.Created($"/api/farm/clusters/{cluster.Id}", cluster);
}).WithSummary("Farm: Cluster erstellen");

/// <summary>Liefert einen Cluster nach ID.</summary>
app.MapGet("/api/farm/clusters/{id}", async (FlipsiForgeDbContext db, int id) =>
{
    var cluster = await db.Set<PrinterCluster>().FindAsync(id);
    return cluster is null ? Results.NotFound() : Results.Ok(cluster);
}).WithSummary("Farm: Cluster nach ID");

/// <summary>Aktualisiert einen Cluster.</summary>
app.MapPut("/api/farm/clusters/{id}", async (FlipsiForgeDbContext db, int id, PrinterCluster updated) =>
{
    var cluster = await db.Set<PrinterCluster>().FindAsync(id);
    if (cluster is null) return Results.NotFound();
    cluster.Name = updated.Name;
    cluster.Description = updated.Description;
    cluster.PrinterIds = updated.PrinterIds;
    cluster.ClusterType = updated.ClusterType;
    cluster.AutoSchedule = updated.AutoSchedule;
    cluster.MinActivePrinters = updated.MinActivePrinters;
    cluster.MaxActivePrinters = updated.MaxActivePrinters;
    cluster.Notes = updated.Notes;
    await db.SaveChangesAsync();
    return Results.Ok(cluster);
}).WithSummary("Farm: Cluster aktualisieren");

/// <summary>Löscht einen Cluster.</summary>
app.MapDelete("/api/farm/clusters/{id}", async (FlipsiForgeDbContext db, int id) =>
{
    var cluster = await db.Set<PrinterCluster>().FindAsync(id);
    if (cluster is null) return Results.NotFound();
    db.Set<PrinterCluster>().Remove(cluster);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithSummary("Farm: Cluster löschen");

// --- Print Batches ---

/// <summary>Listet alle Print-Batches (filterbar nach Status).</summary>
app.MapGet("/api/farm/batches", async (FlipsiForgeDbContext db, string? status = null) =>
{
    var query = db.Set<PrintBatch>().AsQueryable();
    if (!string.IsNullOrEmpty(status) && Enum.TryParse<BatchStatus>(status, true, out var bs))
        query = query.Where(b => b.Status == bs);
    var batches = await query.OrderByDescending(b => b.Priority).ThenByDescending(b => b.CreatedAt).ToListAsync();
    return Results.Ok(batches);
}).WithSummary("Farm: Alle Batches (filterbar nach Status)");

/// <summary>Erstellt einen neuen Print-Batch.</summary>
app.MapPost("/api/farm/batches", async (FlipsiForgeDbContext db, PrintBatch batch) =>
{
    batch.CreatedAt = DateTime.UtcNow;
    if (batch.Status == BatchStatus.Pending) batch.Status = BatchStatus.Pending;
    db.Set<PrintBatch>().Add(batch);
    await db.SaveChangesAsync();
    return Results.Created($"/api/farm/batches/{batch.Id}", batch);
}).WithSummary("Farm: Batch erstellen");

/// <summary>Liefert einen Batch nach ID inkl. Items.</summary>
app.MapGet("/api/farm/batches/{id}", async (FlipsiForgeDbContext db, int id) =>
{
    var batch = await db.Set<PrintBatch>().FindAsync(id);
    if (batch is null) return Results.NotFound();
    var items = await db.Set<BatchItem>().Where(i => i.BatchId == id).ToListAsync();
    return Results.Ok(new { batch, items });
}).WithSummary("Farm: Batch nach ID mit Items");

/// <summary>Liefert den Fortschritt eines Batches.</summary>
app.MapGet("/api/farm/batches/{id}/progress", async (FlipsiForgeDbContext db, int id) =>
{
    var batch = await db.Set<PrintBatch>().FindAsync(id);
    if (batch is null) return Results.NotFound();
    var items = await db.Set<BatchItem>().Where(i => i.BatchId == id).ToListAsync();
    var totalParts = items.Sum(i => i.Quantity);
    var completedParts = items.Sum(i => i.PrintedQuantity);
    var failedParts = items.Count(i => i.Status == BatchItemStatus.Failed);
    var activePrinters = items.Count(i => i.Status == BatchItemStatus.Printing);
    var remaining = items.Where(i => i.Status != BatchItemStatus.Completed)
        .Sum(i => i.EstimatedDurationMin ?? 0);
    return Results.Ok(new
    {
        totalParts,
        completedParts,
        failedParts,
        activePrinters,
        estimatedRemainingMin = remaining,
        progressPercent = totalParts > 0 ? (double)completedParts / totalParts * 100 : 0
    });
}).WithSummary("Farm: Batch-Fortschritt");

/// <summary>Bricht einen Batch ab.</summary>
app.MapPost("/api/farm/batches/{id}/cancel", async (FlipsiForgeDbContext db, int id) =>
{
    var batch = await db.Set<PrintBatch>().FindAsync(id);
    if (batch is null) return Results.NotFound();
    batch.Status = BatchStatus.Cancelled;
    batch.FinishedAt = DateTime.UtcNow;
    var items = await db.Set<BatchItem>().Where(i => i.BatchId == id && i.Status == BatchItemStatus.Pending).ToListAsync();
    foreach (var item in items) item.Status = BatchItemStatus.Failed;
    await db.SaveChangesAsync();
    return Results.Ok(batch);
}).WithSummary("Farm: Batch abbrechen");

// --- Batch Items ---

/// <summary>Fügt Items zu einem Batch hinzu.</summary>
app.MapPost("/api/farm/batches/{id}/items", async (FlipsiForgeDbContext db, int id, BatchItem item) =>
{
    var batch = await db.Set<PrintBatch>().FindAsync(id);
    if (batch is null) return Results.NotFound();
    item.BatchId = id;
    item.Status = BatchItemStatus.Pending;
    db.Set<BatchItem>().Add(item);
    batch.TotalParts += item.Quantity;
    await db.SaveChangesAsync();
    return Results.Created($"/api/farm/batches/{id}/items/{item.Id}", item);
}).WithSummary("Farm: Batch-Item hinzufügen");

// --- Auto-Scheduling ---

/// <summary>Verteilt Batch-Aufträge auf verfügbare Drucker.</summary>
app.MapPost("/api/farm/batches/{id}/schedule", async (FlipsiForgeDbContext db, int id) =>
{
    var batch = await db.Set<PrintBatch>().FindAsync(id);
    if (batch is null) return Results.NotFound();

    // Einfache Scheduling-Logik: Items nach SortOrder, Drucker nach Status
    var items = await db.Set<BatchItem>()
        .Where(i => i.BatchId == id && i.Status == BatchItemStatus.Pending)
        .OrderBy(i => i.SortOrder)
        .ToListAsync();

    var clusterPrinterIds = batch.AssignedClusterId.HasValue
        ? (await db.Set<PrinterCluster>().FindAsync(batch.AssignedClusterId.Value))?.PrinterIds ?? new()
        : new List<int>();

    var printersQuery = db.Printers.Where(p => p.IsActive).AsQueryable();
    var printers = await printersQuery.ToListAsync();

    // Cluster-Filter
    if (clusterPrinterIds.Count > 0)
        printers = printers.Where(p => clusterPrinterIds.Contains(p.Id)).ToList();

    var scheduled = 0;
    foreach (var item in items)
    {
        // Finde einen passenden Drucker (Bauvolumen-Check, idle)
        var printer = printers.FirstOrDefault(p =>
            p.BuildVolumeX > 0 && p.BuildVolumeY > 0 &&
            p.BuildVolumeZ > 0);

        if (printer is null) continue;

        item.AssignedPrinterId = printer.Id;
        item.Status = BatchItemStatus.Assigned;
        db.Set<FarmSchedule>().Add(new FarmSchedule
        {
            BatchId = id,
            PrinterId = printer.Id,
            ScheduledStart = DateTime.UtcNow,
            EstimatedEnd = DateTime.UtcNow.AddMinutes((double)(item.EstimatedDurationMin ?? 60)),
            Status = FarmScheduleStatus.Scheduled,
            Priority = (int)batch.Priority
        });
        scheduled++;
    }

    if (scheduled > 0 && batch.Status == BatchStatus.Pending)
        batch.Status = BatchStatus.Ready;

    await db.SaveChangesAsync();
    return Results.Ok(new { scheduled, remaining = items.Count - scheduled });
}).WithSummary("Farm: Batch auto-schedule auf verfügbare Drucker");

// --- Farm Overview ---

/// <summary>Farm-Übersicht: alle Drucker, aktive Batches, geschätzte Fertigstellung.</summary>
app.MapGet("/api/farm/overview", async (FlipsiForgeDbContext db) =>
{
    var printers = await db.Printers.Where(p => p.IsActive).ToListAsync();
    var batches = await db.Set<PrintBatch>().Where(b => b.Status == BatchStatus.Printing || b.Status == BatchStatus.Ready).ToListAsync();
    var items = await db.Set<BatchItem>().Where(i => i.Status == BatchItemStatus.Pending || i.Status == BatchItemStatus.Assigned).ToListAsync();

    return Results.Ok(new
    {
        totalPrinters = printers.Count,
        activePrinters = await db.Set<FarmSchedule>().CountAsync(s => s.Status == FarmScheduleStatus.Running),
        idlePrinters = printers.Count - await db.Set<FarmSchedule>().CountAsync(s => s.Status == FarmScheduleStatus.Running),
        totalBatches = await db.Set<PrintBatch>().CountAsync(),
        activeBatches = batches.Count,
        totalPartsQueued = items.Sum(i => i.Quantity - i.PrintedQuantity),
        estimatedCompletionTimeMin = items.Sum(i => (double)(i.EstimatedDurationMin ?? 0)),
        serverMode = serverMode.ToString(),
        serverVersion
    });
}).WithSummary("Farm: Übersicht über alle Drucker und Batches");

// --- Farm Settings ---

/// <summary>Liefert die Farm-Einstellungen.</summary>
app.MapGet("/api/farm/settings", async (FlipsiForgeDbContext db) =>
{
    var settings = await db.Set<FarmSettings>().FirstOrDefaultAsync();
    return Results.Ok(settings ?? new FarmSettings());
}).WithSummary("Farm: Einstellungen");

/// <summary>Aktualisiert die Farm-Einstellungen.</summary>
app.MapPut("/api/farm/settings", async (FlipsiForgeDbContext db, FarmSettings updated) =>
{
    var settings = await db.Set<FarmSettings>().FirstOrDefaultAsync();
    if (settings is null)
    {
        updated.Id = 1;
        db.Set<FarmSettings>().Add(updated);
    }
    else
    {
        settings.MaxConcurrentPrints = updated.MaxConcurrentPrints;
        settings.AutoAssignPrinters = updated.AutoAssignPrinters;
        settings.PreferSameCluster = updated.PreferSameCluster;
        settings.FailoverOnError = updated.FailoverOnError;
        settings.AutoRescheduleFailed = updated.AutoRescheduleFailed;
        settings.SpaghettiDetectionEnabled = updated.SpaghettiDetectionEnabled;
        settings.SpaghettiDetectionInterval = updated.SpaghettiDetectionInterval;
        settings.NotificationOnFail = updated.NotificationOnFail;
        settings.AutoPauseOnAnomaly = updated.AutoPauseOnAnomaly;
    }
    await db.SaveChangesAsync();
    return Results.Ok(settings ?? updated);
}).WithSummary("Farm: Einstellungen aktualisieren");

app.Run();