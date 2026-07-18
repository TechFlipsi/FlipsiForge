# Building FlipsiForge

## Prerequisites
- .NET 10 SDK (`dotnet --version` should show 10.x)
- Git

## Build (Linux)

```bash
# Clone
git clone https://github.com/TechFlipsi/FlipsiForge.git
cd FlipsiForge

# Restore + build all projects
dotnet restore FlipsiForge.slnx
dotnet build FlipsiForge.slnx

# Run Desktop app (requires display)
dotnet run --project src/FlipsiForge.Desktop

# Run Server (Full mode, default)
dotnet run --project src/FlipsiForge.Server --urls "http://localhost:5000"

# Run Server (Lite mode)
dotnet run --project src/FlipsiForge.Server --urls "http://localhost:5000" \
  -- --Server:Mode=Lite --Server:AI=false --Server:WebUI=false
```

## API Endpoints (Server)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/health` | Health check + server info |
| GET | `/api/printers` | List all active printers |
| POST | `/api/printers` | Add a printer |
| DELETE | `/api/printers/{id}` | Deactivate a printer |
| GET | `/api/spools` | List all active filament spools |
| POST | `/api/spools` | Add a spool |
| GET | `/api/filament-brands` | List all filament brand specs (41 entries) |
| GET | `/api/filament-brands/{brand}` | Filter by brand name |
| GET | `/api/print-jobs` | List queued print jobs |
| POST | `/api/print-jobs` | Add a print job |
| GET | `/api/print-history` | Last 100 print history entries |
| GET | `/api/statistics` | Print statistics summary |
| GET | `/` | Web-UI placeholder (Full mode only) |

## Docker

```bash
# Server Full
docker build -t flipsiforge/server:full -f Dockerfile .
docker run -p 8080:8080 flipsiforge/server:full

# Server Lite
docker build -t flipsiforge/server:lite -f Dockerfile.lite .
docker run -p 8080:8080 flipsiforge/server:lite
```

## Project Structure

```
FlipsiForge/
├── src/
│   ├── FlipsiForge.Core/          # Shared library (models, DB, filament specs)
│   │   ├── Models/                # Enums, Models, FilamentBrandSpec
│   │   └── Data/                  # DbContext, FilamentDbSeeder
│   ├── FlipsiForge.Desktop/       # Avalonia UI desktop app
│   │   ├── Views/                  # 7 tab views (AXAML)
│   │   └── ViewModels/            # MVVM view models
│   └── FlipsiForge.Server/        # ASP.NET Core server (Full/Lite)
│       └── Program.cs             # Minimal API
├── Dockerfile                      # Server Full
├── Dockerfile.lite                 # Server Lite
├── README.md
├── IMPLEMENTATION.md
├── CONCEPT.md
└── CHANGELOG.md
```