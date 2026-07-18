# FlipsiForge 🔧

> **All-in-one 3D printing management software** — Part of the TechFlipsi ecosystem.

FlipsiForge is a cross-platform 3D printing management tool that handles files, printers, filament, model discovery, and print statistics in one unified application. Available as a **desktop app** (Windows + Linux) with an optional **headless server backend** (any Linux, Raspberry Pi to VPS).

## Status

🚧 **Concept Phase** — Repository created, development not yet started.

## Features

### 1. 📁 File Manager — Smart 3D Print File Discovery

- **Auto-scan** across the entire PC and all connected drives for 3D printable files
- Supported formats: `.stl`, `.obj`, `.3mf`, `.gcode`, `.gco`, `.ply`, `.step`, `.stp`, `.amf`, `.x3d`
- **Thumbnail preview** for each file (STL/3MF/OBJ rendered, G-code layer preview)
- **Grid view** (Raster) or **List view** (Listenansicht) toggle
- Categorization by type, date, size, material tags
- Search & filter across all discovered files
- Watch folders — automatically detect new files
- **STL Repair Check** — scan files for defects (non-manifold edges, holes) before printing. Warning when model is broken
- **G-code Visualizer** — load G-code file → 3D layer-by-layer visualization. See exactly what will be printed
- **Datei-Versionierung** — edited STLs keep old versions (v1, v2, v3 with dates). No one loses the original by accident
- **Projekt-Export** — STL + G-code + filament info + print settings as ZIP package. For local backup or transfer to another PC
- **Bulk-Aktionen** — select multiple files → send to printer / group as project / export
- **Duplikat-Erkennung** — same STL under different names → warning
- **Sortierung nach Drucker-Eignung** — "Fits Snapmaker U1 (235×235×275mm)" vs "Fits Neptune 4 Pro"

### 2. 🖨️ Printer Management — Direct Control

- Connect to Klipper/Moonraker printers (HTTP REST API) and Marlin printers (USB-serial)
- Multiple printer profiles (Snapmaker U1, Neptune 4 Pro, any Marlin printer)
- Real-time data display:
  - Bed temperature / Hotend temperature (per extruder)
  - Print status (idle, printing, paused, error)
  - Progress bar with ETA
  - Layer counter
  - Print speed / flow rate
- Direct actions:
  - Upload & start print from file manager
  - Pause / resume / cancel
  - Emergency stop
  - Home axes / move axes
  - Control fans
  - Load/unload filament macros
- **G-code Terminal** — raw console for manual commands
- **Macro-Buttons** — configurable buttons for frequent macros (e.g. "Load Filament T0", "Bed Mesh Calibrate")
- **Druck-Queue** — queue multiple files. Next print starts automatically after previous finishes, **but requires confirmation first** (someone needs to clear the printer bed)
- **Webcam-Preview** — live camera feed from printer (Moonraker camera endpoint)
- **Temperature Curves** — live graph for hotend/bed temperature over time
- **Druck-Historie** — past prints with duration, filament usage, success/failure
- **Firmware-Info** — Klipper version, MCU info, input shaper data
- **Slicer-Integration** — OrcaSlicer/PrusaSlicer CLI directly callable from the app. STL → Slice → G-code → Send to printer. One-click workflow
- **Druck-Profil-Templates** — pre-configured profiles per printer (Snapmaker U1: 60°C bed, 210°C hotend, PLA). New user selects printer → ready-made profile

### 3. 🧶 Filament Management — Custom Spool Tracking

- **Custom-built system** (not Spoolman — adaptable to community needs)
- Track filament inventory (brand, type, color, weight, remaining)
- Spool lifecycle: new → in-use → empty
- Cost tracking per spool (price paid, price per gram)
- Low-stock alerts
- Material types: PLA, PETG, TPU, ABS, ASA, PC, Nylon, etc.
- Color tags with visual swatches
- Usage history (which print used how much)
- Integration with printer tab (auto-deduct filament on print completion)
- **Spool QR-Code** — generate QR code per spool → scan with phone → instantly found in system
- **Verbrauchs-Vorhersage** — "At current usage, spool lasts ~14 days"
- **Filament-Empfehlung** — when selecting a file to print → "You have 3 matching spools (PLA Black, PLA Gray, PETG Black)"
- **Trocknungs-Log** — when/how long/at what temperature was spool dried. Warn about too-moist spools
- **Trocknungs-Timer** — mark spool as "in dryer" with countdown
- **Material-Empfehlung** — "For this model, PLA or PETG recommended, not TPU" based on geometry/wall thickness
- **Verbrauch pro Kategorie** — statistics: "This year 2.3kg PLA vs 800g PETG consumed" with charts

### 4. 🌐 Model Repository — Online Model Discovery

- **Unified search** across all major platforms — no need to select which one
- Supported platforms: **Thingiverse**, **Printables**, **MakerWorld** (and more)
- Search once → results from all platforms shown together in a single list
- Download directly into file manager — no browser needed
- Filter by: free/paid, category, print time, difficulty, rating
- "Makes" gallery — see what others printed with the same model

### 5. 📊 Statistics Dashboard — Print Analytics

- Filament consumption over time (chart)
- Print success/failure rate
- Cost per month / per printer
- Printer utilization (which printer runs most)
- Druck-Kosten-Rechner integration — total costs at a glance
- Export statistics as report (PDF/CSV)

### 6. 🔥 Druck-Kosten-Rechner — Print Cost Calculator

- Filament weight × price/gram
- + Electricity (printer wattage × duration × kWh price)
- + Wear / depreciation
- = **Total cost per print**
- Auto-filled from selected file + filament + printer profile
- Manual override possible

## Architecture — Shared Core + Built-in Server

FlipsiForge is **one app** with an optional server backend. The desktop app always provides the full GUI experience — whether running standalone (local data) or connected to a FlipsiForge Server (centralized data).

```
FlipsiForge.Core (shared business logic)
├── FileScanner        — drive scanning, file indexing, thumbnails, STL repair check
├── PrinterController  — Moonraker REST/WS + Marlin USB-serial, print queue, slicer CLI
├── FilamentManager    — spool inventory, consumption, cost tracking, drying log
├── CostCalculator     — print cost calculator (filament + power + wear)
├── ModelRepository    — Thingiverse / Printables / MakerWorld unified search
├── StatisticsEngine   — print analytics, consumption charts, success rates
├── PluginSystem       — community plugins (custom slicers, stats, integrations)
├── CloudSync          — Nextcloud (P1) / Google Drive / OneDrive / Dropbox
└── ServerClient       — connects to FlipsiForge Server (optional)

FlipsiForge (Desktop App)       — Avalonia UI 12, full GUI, always the client
FlipsiForge.Server (Optional)   — ASP.NET Core backend, any Linux server
```

### Mode 1: Standalone (Default)

- App runs locally on the user's PC
- All data stored in local SQLite database
- No server needed — full functionality out of the box
- Cloud-Sync (Nextcloud etc.) available as optional add-on

### Mode 2: Server-Connected

- User sets up a FlipsiForge Server (e.g. on a Raspberry Pi, NUC, VPS, or any Linux machine in the home network)
- **Auto-Discovery**: App and Server find each other automatically via mDNS/Bonjour (UDP broadcast). No manual IP entry needed — app shows "FlipsiForge Server found on 192.168.x.x" → click to connect.
- Manual connection also possible (Settings → Server → enter URL for remote/VPN setups)
- App switches to server mode — filaments, printer profiles, settings, and print history are fetched from the server
- Multiple PCs can connect to the same server — all stay in sync automatically
- UI is **identical** to standalone mode — the user doesn't notice the difference
- Server can be disabled anytime → app falls back to local SQLite

### Mode 3: Remote Access (Server from outside home)

- Server supports Tailscale / WireGuard VPN for secure remote access
- Reverse proxy support (nginx, Caddy) for domain-based access
- Optional authentication (username/password or API token)
- Same Desktop App connects to `https://flipsiforge.my-tailnet.ts.net` or public domain
- Print monitoring from work, on the road, etc.

### FlipsiForge.Server (Headless Backend)

- **ASP.NET Core** web server — runs on **any Linux server** (Raspberry Pi, NUC, VPS, old laptop, NAS with Docker). ARM64 + x64.
- **No GUI** — pure backend, manages data and printer connections
- **Two access methods:**
  1. **Gateway API** (REST + WebSocket) — the Desktop App connects to this. The app talks to the server like a client: fetch filaments, send print jobs, get live printer data, sync settings. Real-time WebSocket for live temperature/progress updates.
  2. **Web UI** — browser-based interface for devices without the desktop app (phone, tablet, guest PC). Same features, rendered in browser.
- **Multi-User with Roles** — Admin (full access), User (print only), Viewer (read-only). For families, makerspaces, community spaces
- **Push Notifications** — print finished/error notifications via **Telegram** (preferred), plus Web-Push, email, and desktop notifications
- **Timelapse** — webcam snapshots at intervals → timelapse video of print. Stored in dedicated timelapse tab. Server-side feature (PC doesn't need to stay on)
- **Druck-Statistiken-Export** — server collects data across all prints → monthly report (success rate, costs, filament usage)
- **REST API for third parties** — public API so other tools can query filament data, print status, costs. `GET /api/filaments`, `GET /api/printers/status`, etc.
- **Home Assistant Integration** — HACS custom integration + HA Add-on to run FlipsiForge.Server directly inside Home Assistant. Sensors for filament stock, print status, costs. Full HA dashboard support
- Same SQLite database (or PostgreSQL for multi-user setups)
- Printer connections live on the server — server controls printers 24/7
- STL thumbnails generated server-side (software rendering — no GPU required)
- File scanning scans USB drives / network mounts on the server host
- Docker image for easy deployment (ARM64 + x64)
- .NET 10 runs natively on ARM64 (Raspberry Pi 4/5, 64-bit)

## Tech Stack

| Component | Choice | Reason |
|-----------|--------|--------|
| Framework | Avalonia UI 12 (.NET 10) | TechFlipsi software standard, cross-platform (Windows + Linux) |
| Language | C# 13 | Proven with FlipsiColor.Avalonia |
| File scanning | .NET filesystem watchers + background indexing | Non-blocking, incremental |
| STL rendering | OpenTK / Silk.NET for 3D preview + G-code visualizer | Hardware-accelerated preview |
| STL repair | Mesh analysis (non-manifold detection, hole finding) | Warn before printing broken models |
| Slicer integration | OrcaSlicer / PrusaSlicer CLI orchestration | One-click STL → G-code → print |
| Printer protocol (Klipper) | Moonraker REST API + WebSocket | Standard for Klipper-based printers |
| Printer protocol (Marlin) | USB-serial (System.IO.Ports) | For Arduino-based non-Klipper printers |
| Model repositories | Thingiverse / Printables / MakerWorld REST APIs | Unified search across all platforms |
| Cloud-Sync | Nextcloud WebDAV (P1) + Google Drive / OneDrive / Dropbox | Optional, default = local-only |
| Local storage | SQLite (PostgreSQL optional for multi-user server) | Embedded, no server needed |
| Server backend | ASP.NET Core + Docker (ARM64 + x64) | Runs on any Linux: Pi, NUC, VPS, NAS |
| Auto-Discovery | mDNS/Bonjour (UDP broadcast) | App finds server automatically, no manual IP |
| Push notifications | Telegram Bot API (preferred) + Web-Push + email | Print status alerts to phone |
| Home Assistant | HACS custom integration + HA Add-on | Run server inside HA, sensors for all data |
| Plugin system | .NET plugin loading (MEF or assembly load) | Community extensions |
| i18n | JSON-based localization (13 languages) | Consistent with FlipsiColor/FlipsiSort |
| Packaging | Installer (.exe + .deb) + Portable (.zip) | New TechFlipsi standard |
| License | GPL-3.0 | Consistent with all TechFlipsi projects |

## Optional Features (built but not forced)

- **QR-Code Drucker-Zugang** — QR code sticker on printer → scan with phone → web UI opens directly for that printer. Optional convenience feature, not required since dashboard shows all printers

## TechFlipsi Ecosystem Integration

- **Design language**: Dark Void + Ember theme (matching techflipsi.kirchweger.de)
- **Branding**: TechFlipsi family product
- **Software-Standard**: Avalonia UI + Installer/Portable + GPL-3.0 + 13 Sprachen i18n
- **Website cross-link**: Will be featured on techflipsi.kirchweger.de/geraete.html
- **Community**: Public repo, Issues/PRs welcome (like FlipsiColor)

## Roadmap (Planned)

| Phase | Scope |
|-------|-------|
| v0.1.0 | File scanner + grid/list view + STL thumbnails + STL repair check |
| v0.2.0 | Printer tab (Moonraker + Marlin, live data, basic controls) + G-code visualizer |
| v0.3.0 | Filament inventory tracking (custom system) + QR codes + drying log |
| v0.4.0 | Druck-Kosten-Rechner + Slicer-Integration (OrcaSlicer/PrusaSlicer CLI) |
| v0.5.0 | Model Repository tab (Thingiverse + Printables + MakerWorld unified search) |
| v0.6.0 | Statistics Dashboard + Druck-Queue with confirmation + Druck-Profil-Templates |
| v0.7.0 | Cloud-Sync (Nextcloud P1) + Datei-Versionierung + Projekt-Export |
| v0.8.0 | Plugin-System + i18n (13 languages) |
| v0.9.0 | Built-in Server: FlipsiForge.Server + App server-connect + Auto-Discovery + Web-UI + Docker |
| v0.10.0 | Server features: Timelapse + Push-Notifications (Telegram) + Multi-User/Roles + REST API |
| v0.11.0 | Home Assistant Integration (HACS + HA Add-on) |
| v0.12.0 | Cloud-Sync extension (Google Drive, OneDrive, Dropbox) + QR-Code Drucker-Zugang |
| v1.0.0 | Installer (Windows .exe + Linux .deb) + Portable (.zip) + Server Docker image (ARM64 + x64) |

## License

GPL-3.0 — same as all TechFlipsi projects.

## Author

**TechFlipsi** (Fabian Kirchweger) — [techflipsi.kirchweger.de](https://techflipsi.kirchweger.de)