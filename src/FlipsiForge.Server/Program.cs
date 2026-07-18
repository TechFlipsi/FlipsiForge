using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Server-Modus aus Konfiguration (Full oder Lite)
var serverMode = builder.Configuration.GetValue<ServerMode>("Server:Mode", ServerMode.Full);
var aiEnabled = builder.Configuration.GetValue<bool>("Server:AI", serverMode == ServerMode.Full);
var webUiEnabled = builder.Configuration.GetValue<bool>("Server:WebUI", serverMode == ServerMode.Full);

builder.Services.AddDbContext<FlipsiForgeDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Datenbank initialisieren + Filament-DB seeden
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlipsiForgeDbContext>();
    db.Database.EnsureCreated();
    await FilamentDbSeeder.SeedAsync(db);
}

app.MapOpenApi();

// === Gateway API (immer verfügbar — Full und Lite) ===

// Health-Check
app.MapGet("/api/health", () => new
{
    status = "ok",
    version = "0.1.0-pre",
    mode = serverMode.ToString(),
    ai = aiEnabled,
    webui = webUiEnabled,
    timestamp = DateTime.UtcNow
});

// Drucker
app.MapGet("/api/printers", async (FlipsiForgeDbContext db) =>
    await db.Printers.Where(p => p.IsActive).ToListAsync());

app.MapPost("/api/printers", async (FlipsiForgeDbContext db, Printer printer) =>
{
    printer.IsActive = true;
    db.Printers.Add(printer);
    await db.SaveChangesAsync();
    return Results.Created($"/api/printers/{printer.Id}", printer);
});

app.MapDelete("/api/printers/{id}", async (FlipsiForgeDbContext db, int id) =>
{
    var printer = await db.Printers.FindAsync(id);
    if (printer is null) return Results.NotFound();
    printer.IsActive = false;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Filament-Spulen
app.MapGet("/api/spools", async (FlipsiForgeDbContext db) =>
    await db.Spools.Where(s => s.Status == SpoolStatus.Active).ToListAsync());

app.MapPost("/api/spools", async (FlipsiForgeDbContext db, Spool spool) =>
{
    spool.Status = SpoolStatus.Active;
    db.Spools.Add(spool);
    await db.SaveChangesAsync();
    return Results.Created($"/api/spools/{spool.Id}", spool);
});

// Filament-Marken-Datenbank
app.MapGet("/api/filament-brands", async (FlipsiForgeDbContext db) =>
    await db.FilamentBrandSpecs.ToListAsync());

app.MapGet("/api/filament-brands/{brand}", async (FlipsiForgeDbContext db, string brand) =>
    await db.FilamentBrandSpecs.Where(b => b.Brand == brand).ToListAsync());

// Druck-Jobs (Queue)
app.MapGet("/api/print-jobs", async (FlipsiForgeDbContext db) =>
    await db.PrintJobs.Where(j => j.Status == PrintJobStatus.Queued || j.Status == PrintJobStatus.Confirmed)
        .ToListAsync());

app.MapPost("/api/print-jobs", async (FlipsiForgeDbContext db, PrintJob job) =>
{
    job.Status = PrintJobStatus.Queued;
    job.QueuedAt = DateTime.UtcNow;
    db.PrintJobs.Add(job);
    await db.SaveChangesAsync();
    return Results.Created($"/api/print-jobs/{job.Id}", job);
});

// Druck-Historie
app.MapGet("/api/print-history", async (FlipsiForgeDbContext db) =>
    await db.PrintHistory.OrderByDescending(h => h.StartedAt).Take(100).ToListAsync());

// Statistiken
app.MapGet("/api/statistics", async (FlipsiForgeDbContext db) =>
{
    var totalPrints = await db.PrintHistory.CountAsync();
    var successfulPrints = await db.PrintHistory.CountAsync(h => h.Success);
    var totalFilament = await db.PrintHistory.SumAsync(h => (decimal?)h.FilamentUsedG) ?? 0;
    var totalCost = await db.PrintHistory.SumAsync(h => (decimal?)h.CostEur) ?? 0;

    return new
    {
        totalPrints,
        successfulPrints,
        successRate = totalPrints > 0 ? (double)successfulPrints / totalPrints * 100 : 0,
        totalFilamentG = totalFilament,
        totalCostEur = totalCost
    };
});

// === Web-UI (nur im Full-Modus) ===
if (webUiEnabled && serverMode == ServerMode.Full)
{
    // TODO: Web-UI in v0.2.0 (Blazor oder statische HTML+JS)
    app.MapGet("/", () => Results.Text(
        $"""
        <html><body style="background:#050507;color:#ff6600;font-family:sans-serif;padding:40px">
        <h1>🔥 FlipsiForge Server v0.1.0-pre</h1>
        <p>Modus: <b>{serverMode}</b></p>
        <p>KI: {(aiEnabled ? "✅ aktiv" : "❌ deaktiviert")}</p>
        <p>Web-UI folgt in v0.2.0</p>
        <p>API: <a href="/api/health" style="color:#ff6600">/api/health</a> |
               <a href="/api/printers" style="color:#ff6600">/api/printers</a> |
               <a href="/api/spools" style="color:#ff6600">/api/spools</a> |
               <a href="/api/filament-brands" style="color:#ff6600">/api/filament-brands</a></p>
        </body></html>
        """, "text/html"));
}

app.Run();