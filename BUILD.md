# Building FlipsiForge

## Prerequisites

- .NET 10.0.300 SDK (`dotnet --version` → 10.0.300+)
- Git
- Optional: Docker (für Server-Images)

## Build (Linux)

```bash
# Clone
git clone https://github.com/TechFlipsi/FlipsiForge.git
cd FlipsiForge

# Restore + build solution
dotnet restore FlipsiForge.slnx
dotnet build FlipsiForge.slnx

# Nur Server
dotnet build src/FlipsiForge.Server/FlipsiForge.Server.csproj

# Run Server (Full mode, default)
dotnet run --project src/FlipsiForge.Server --urls "http://localhost:5000"

# Run Server (Lite mode)
dotnet run --project src/FlipsiForge.Server --urls "http://localhost:5000" \
  -- --Server:Mode=Lite --Server:AI=false --Server:WebUI=false

# Oder via appsettings-Lite-Datei:
ASPNETCORE_ENVIRONMENT=Lite dotnet run --project src/FlipsiForge.Server --urls "http://localhost:5000"

# Run Desktop app (requires display)
dotnet run --project src/FlipsiForge.Desktop
```

## API Endpoints (Server v0.2.0)

### Health & Settings

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/health` | Health check + Server-Info (Version, Mode, AI, WebUI) |
| GET | `/api/settings` | AppSettings (Singleton-Zeile aus SQLite) |
| PUT | `/api/settings` | AppSettings überschreiben (Body=AppSettings JSON) |
| PATCH | `/api/settings/{field}` | Partial Update eines Feldes (Body=JSON-Wert) |

### Printers (CRUD + Live + Maintenance)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/printers?includeInactive=false` | Aktive Drucker (Optional alle) |
| GET | `/api/printers/{id}` | Einzelner Drucker |
| POST | `/api/printers` | Drucker hinzufügen |
| PUT | `/api/printers/{id}` | Drucker full-update |
| PATCH | `/api/printers/{id}/activate` | Drucker reaktivieren (IsActive=true) |
| DELETE | `/api/printers/{id}?keepHistory=true` | `keepHistory=true`=archivieren, `false`=hart löschen |
| GET | `/api/printers/{id}/maintenance` | Wartungs-Einträge für Drucker |
| POST | `/api/printers/{id}/maintenance` | Wartungs-Eintrag anlegen (Body: component, action, notes) |
| GET | `/api/printers/{id}/maintenance/recommendations?onlineMode=false` | Wartungs-Empfehlungen (online=modellspezifisch, offline=allgemein) |
| POST | `/api/printers/{id}/connect` | Verbindung testen (via IPrinterConnectionManager) |
| GET | `/api/printers/{id}/status` | Live-Status (PrinterLiveStatus) |
| GET | `/api/printers/{id}/temps` | Live-Temperaturen (PrinterTemperatures) |
| GET | `/api/printers/{id}/job` | Aktueller Druck-Job (PrinterJobInfo oder null) |

### Spools (CRUD + Status)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/spools?includeArchived=false` | Aktive Spulen (Optional alle) |
| GET | `/api/spools/{id}` | Einzelne Spule |
| POST | `/api/spools` | Spule hinzufügen |
| PUT | `/api/spools/{id}` | Spule full-update |
| PATCH | `/api/spools/{id}/status` | Status ändern (Body={status: Active\|Empty\|Drying\|Archived}) |
| DELETE | `/api/spools/{id}?keepHistory=true` | `true`=archivieren, `false`=hart löschen |

### Filament-DB

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/filament-brands` | Alle Specs (41 Einträge, 20 Marken) |
| GET | `/api/filament-brands/{brand}` | Filter nach Markenname |

### Print Jobs & History

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/print-jobs` | Queue (Queued + Confirmed) |
| POST | `/api/print-jobs` | Job zur Queue hinzufügen |
| GET | `/api/print-history` | Letzte 100 Druck-Historien-Einträge |

### Files

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/files?extension=STL&folder=/path` | Gescannte Dateien (optional gefiltert) |
| GET | `/api/files/search?q=...` | Kombinierte Suche (Dateiname + KI falls verfügbar) |
| POST | `/api/files/scan` | Datei-Scan starten (Body={folders:[...], extensions:[...]}) |
| GET | `/api/files/{id}` | Einzelne Datei |
| POST | `/api/files/{id}/favorite` | Favorit togglen (★) |
| POST | `/api/files/{id}/usage` | Usage loggen (Body={action: viewed\|printed}) |
| DELETE | `/api/files/{id}` | Datei aus DB löschen (nicht von Disk!) |

### KI

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/ai/status` | {loaded, modelName, choice} |
| POST | `/api/ai/chat` | Chat-Antwort (Streaming via SSE falls AI:Streaming=true) |
| POST | `/api/ai/embed` | Embedding-Vektor (Body={text}) |
| POST | `/api/ai/slicer-profile` | Slicer-Profil generieren (Body={printerId, spoolId, goal}) |

### Bot (Forge-Bot)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/bot/messages` | Aktive Bot-Nachrichten |
| POST | `/api/bot/dismiss` | Letzte Nachricht dismissen |
| PATCH | `/api/bot/settings` | Bot-Settings patchen (Body={enabled?, frequency?}) |

### Backup / Restore / Export / Cache

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/backup` | SQLite-Backup erstellen (VACUUM INTO) |
| GET | `/api/backup/list` | Liste aller Backups |
| POST | `/api/restore` | Backup einspielen (Body={backupPath}) |
| POST | `/api/export` | JSON-Export aller DB-Daten |
| DELETE | `/api/cache` | Cache leeren (thumbnails, embeddings, temp) |

### Statistics

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/statistics` | Druck-Statistik (totalPrints, successRate, totalFilamentG, totalCostEur) |
| GET | `/api/statistics/files` | Datei-Statistik (Total, pro Format, pro Ordner, Favoriten) |
| GET | `/api/statistics/filament` | Filament-Statistik (Total Gewicht, nach Material, nach Marke) |

### Web-UI (nur Full-Modus)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | Dashboard (wwwroot/index.html) — Tabs: Drucker, Spulen, Filament-DB, Statistik, Files, System |

## Docker

```bash
# Server Full (mit KI + Web-UI)
docker build -t flipsiforge/server:full -f Dockerfile .
docker run -d -p 5000:5000 flipsiforge/server:full

# Server Lite (reine Überwachung)
docker build -t flipsiforge/server:lite -f Dockerfile.lite .
docker run -d -p 5000:5000 flipsiforge/server:lite
```

Beide Images basieren auf `mcr.microsoft.com/dotnet/sdk:10.0.300` (Build) und
`mcr.microsoft.com/dotnet/aspnet:10.0-noble` (Runtime). Das Binary wird als
**Single-File-Publish für linux-x64** erstellt (`PublishSingleFile=true`).

## Project Structure

```
FlipsiForge/
├── src/
│   ├── FlipsiForge.Core/                 # Shared library (Models, DbContext, Filament specs)
│   │   ├── Models/                        # Enums, Models, FilamentBrandSpec
│   │   └── Data/                          # DbContext, FilamentDbSeeder
│   ├── FlipsiForge.Desktop/              # Avalonia UI desktop app
│   │   ├── Views/                         # Tab views (AXAML)
│   │   └── ViewModels/                    # MVVM view models
│   └── FlipsiForge.Server/               # ASP.NET Core server (Full/Lite)
│       ├── Program.cs                     # Minimal API — alle v0.2.0 Endpoints
│       ├── Services/                      # Server-Stub-Interfaces + Fallback-Implementierungen
│       │   ├── IPrinterConnectionManager.cs       # Vertrag + PrinterConnectionResult/Status/Temps/Job
│       │   ├── StubPrinterConnectionManager.cs    # Stub: liefert "Offline"
│       │   ├── IAIChatEngine.cs                    # Vertrag + IEmbeddingProvider
│       │   ├── StubAIChatEngine.cs                 # Stub-Antworten
│       │   ├── StubEmbeddingProvider.cs            # Null-Embeddings
│       │   ├── ServerSettings.cs                   # Options-Klassen (AI, Bot, Backup, Requests)
│       │   ├── FileScanner.cs                      # Datei-Scan-Logik (STL/3MF/...)
│       │   ├── MaintenanceRecommendationProvider.cs # Dual-Mode Empfehlungen
│       │   ├── BackupService.cs                    # SQLite VACUUM INTO Backup
│       │   └── BotMessageStore.cs                  # In-Memory Bot-Nachrichten
│       ├── wwwroot/                       # Statische Web-UI (Full-Modus)
│       │   ├── index.html
│       │   ├── css/app.css                # Dark Void + Ember Theme
│       │   └── js/
│       │       ├── api.js                 # Fetch-Wrapper für alle Endpoints
│       │       └── app.js                 # Dashboard-Logik, Tabs, CRUD
│       ├── appsettings.json               # Full-Modus Config
│       ├── appsettings.Lite.json          # Lite-Modus Config
│       └── appsettings.Development.json   # Dev-Config
├── Dockerfile                              # Server Full (.NET 10.0.300)
├── Dockerfile.lite                         # Server Lite (.NET 10.0.300)
├── README.md
├── BUILD.md
├── IMPLEMENTATION.md
├── CONCEPT.md
└── CHANGELOG.md
```

## Build-Konfiguration

### Single-File-Publish (Docker)

```
dotnet publish src/FlipsiForge.Server/FlipsiForge.Server.csproj \
  -c Release -r linux-x64 \
  --self-contained false \
  -p:PublishSingleFile=true \
  -p:IncludeContentFilesInSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -o /app
```

- `--self-contained false`: Nutzt die installierte .NET-Runtime im Runtime-Image (kleiner)
- `PublishSingleFile=true`: Alles in einer Binary
- `IncludeContentFilesInSingleFile=true`: wwwroot wird in die Binary gepackt

### Core.Services-Kompatibilität

Der Server registriert Core.Services-Implementierungen via `TryAddSingleton`.
Falls ein anderer Subagent die echten Klassen in Core baut und via DI registriert,
gewinnen diese — sonst greifen die Server-Stubs (`Services/Stub*.cs`):

- `IPrinterConnectionManager` → `StubPrinterConnectionManager` (Offline)
- `IAIChatEngine` → `StubAIChatEngine` (Platzhalter-Antwort)
- `IEmbeddingProvider` → `StubEmbeddingProvider` (Null-Vektoren)

Der Server kompiliert und läuft in beiden Fällen.

## Testen (manuell)

```bash
# Start
dotnet run --project src/FlipsiForge.Server --urls "http://localhost:5000"

# Health
curl http://localhost:5000/api/health

# Printer hinzufügen
curl -X POST http://localhost:5000/api/printers \
  -H "Content-Type: application/json" \
  -d '{"brand":"Snapmaker","model":"U1","protocol":"KlipperMoonraker","ipAddress":"192.168.1.50","buildVolumeX":235,"buildVolumeY":235,"buildVolumeZ":275,"nozzleDiameter":0.4}'

# Wartungs-Empfehlungen
curl http://localhost:5000/api/printers/1/maintenance/recommendations?onlineMode=true

# Datei-Scan
curl -X POST http://localhost:5000/api/files/scan \
  -H "Content-Type: application/json" \
  -d '{"folders":["/tmp/stl-test"],"extensions":[]}'

# Settings ändern
curl -X PATCH http://localhost:5000/api/settings/AiEnabled -H "Content-Type: application/json" -d 'false'

# Backup erstellen
curl -X POST http://localhost:5000/api/backup

# Web-UI
# Browser öffnen: http://localhost:5000/
```