# FlipsiForge 🔧

> **3D Printer Management Software** — Part of the TechFlipsi ecosystem.

FlipsiForge is a cross-platform 3D printing management tool that handles files, printers, and filament in one unified application. Available as a **desktop app** (Windows + Linux) and a **headless server** (Raspberry Pi, mini PCs, any Linux server).

## Status

🚧 **Concept Phase** — Repository created, development not yet started.

## Vision

A single, well-structured desktop application that handles three core pillars of 3D printing:

### 1. 📁 File Manager — Smart 3D Print File Discovery

- **Auto-scan** across the entire PC and all connected drives for 3D printable files
- Supported formats: `.stl`, `.obj`, `.3mf`, `.gcode`, `.gco`, `.ply`, `.step`, `.stp`, `.amf`, `.x3d`
- **Thumbnail preview** for each file (STL/3MF/OBJ rendered, G-code layer preview)
- **Grid view** (Raster) or **List view** (Listenansicht) toggle
- Categorization by type, date, size, material tags
- Search & filter across all discovered files
- Watch folders — automatically detect new files

### Printer Management — Direct Control

- Connect to Klipper/Moonraker printers (HTTP REST API) and Marlin printers (USB-serial)
- Multiple printer profiles (Snapmaker U1, Neptune 4 Pro, any Marlin printer)
- ** Druck-Kosten-Rechner** — Filament weight × price/g + electricity (printer wattage × duration × kWh price) + wear = total cost per print
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
- G-code terminal (send raw commands)

### 3. 🧶 Filament Management — Spool Tracking (Eigenes System)

- Custom-built filament tracking (not Spoolman — adaptable to community needs)
- Track filament inventory (brand, type, color, weight, remaining)
- Spool lifecycle: new → in-use → empty
- Cost tracking per spool (price paid, price per gram)
- Low-stock alerts
- Material types: PLA, PETG, TPU, ABS, ASA, PC, Nylon, etc.
- Color tags with visual swatches
- Usage history (which print used how much)
- Integration with printer tab (auto-deduct filament on print completion)

## Architecture — Shared Core + Built-in Server

FlipsiForge is **one app** with an optional server backend. The desktop app always provides the full GUI experience — whether running standalone (local data) or connected to a FlipsiForge Server (centralized data).

```
FlipsiForge.Core (shared business logic)
├── FileScanner        — drive scanning, file indexing, thumbnails
├── PrinterController  — Moonraker REST/WS + Marlin USB-serial
├── FilamentManager    — spool inventory, consumption, cost tracking
├── CostCalculator     — print cost calculator (filament + power + wear)
├── CloudSync          — Nextcloud (P1) / Google Drive / OneDrive / Dropbox
└── ServerClient       — connects to FlipsiForge Server (optional)

FlipsiForge (Desktop App)       — Avalonia UI 12, full GUI, always the client
FlipsiForge.Server (Optional)   — ASP.NET Core backend, runs on Raspberry Pi / server
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
| STL rendering | OpenTK / Silk.NET for 3D preview thumbnails | Hardware-accelerated preview |
| Printer protocol (Klipper) | Moonraker REST API + WebSocket | Standard for Klipper-based printers |
| Printer protocol (Marlin) | USB-serial (System.IO.Ports) | For Arduino-based non-Klipper printers |
| Cloud-Sync | Nextcloud WebDAV (P1) + Google Drive / OneDrive / Dropbox | Optional, default = local-only |
| Local storage | SQLite | Embedded, no server needed |
| i18n | JSON-based localization (13 languages) | Consistent with FlipsiColor/FlipsiSort |
| Packaging | Installer (.exe + .deb) + Portable (.zip) | New TechFlipsi standard |
| Server | ASP.NET Core + Docker (ARM64 + x64) | Runs on any Linux: Raspberry Pi, NUC, VPS, NAS |
| Auto-Discovery | mDNS/Bonjour (UDP broadcast) | App finds server automatically, no manual IP |
| License | GPL-3.0 | Consistent with all TechFlipsi projects |

## TechFlipsi Ecosystem Integration

- **Design language**: Dark Void + Ember theme (matching techflipsi.kirchweger.de)
- **Branding**: TechFlipsi family product
- **Website cross-link**: Will be featured on techflipsi.kirchweger.de/geraete.html

## Roadmap (Planned)

| Phase | Scope |
|-------|-------|
| v0.1.0 | File scanner + grid/list view + STL thumbnails (Desktop) |
| v0.2.0 | Printer tab (Moonraker + Marlin, live data, basic controls) |
| v0.3.0 | Filament inventory tracking (custom system) |
| v0.4.0 | Druck-Kosten-Rechner + cross-tab integration |
| v0.5.0 | Cloud-Sync (Nextcloud P1) + settings + multi-PC |
| v0.6.0 | Multi-printer dashboard + webcam + notifications |
| v0.7.0 | i18n (13 languages) |
| v0.8.0 | Built-in Server: FlipsiForge.Server backend + App server-connect mode + Web-UI + Docker (Raspberry Pi) |
| v0.9.0 | Cloud-Sync extension (Google Drive, OneDrive, Dropbox) |
| v1.0.0 | Installer (Windows .exe + Linux .deb) + Portable (.zip) + Server Docker image (ARM64 + x64) |

## License

GPL-3.0 — same as all TechFlipsi projects.

## Author

**TechFlipsi** (Fabian Kirchweger) — [techflipsi.kirchweger.de](https://techflipsi.kirchweger.de)