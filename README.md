# FlipsiForge 🔧

> **3D Printer Management Desktop App** — Part of the TechFlipsi ecosystem.

FlipsiForge is a cross-platform desktop application for 3D printing enthusiasts who want one unified tool to manage their files, printers, and filament — without juggling five different tools.

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

### 2. 🖨️ Printer Management — Direct Control

- Connect to Klipper/Moonraker printers (HTTP REST API)
- Multiple printer profiles (Snapmaker U1, Neptune 4 Pro, etc.)
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

### 3. 🧶 Filament Management — Spool Tracking

- Track filament inventory (brand, type, color, weight, remaining)
- Spool lifecycle: new → in-use → empty
- Cost tracking per spool (price paid, price per gram)
- Low-stock alerts
- Material types: PLA, PETG, TPU, ABS, ASA, PC, Nylon, etc.
- Color tags with visual swatches
- Usage history (which print used how much)
- Integration with printer tab (auto-deduct filament on print completion)

## Tech Stack (Preliminary)

| Component | Choice | Reason |
|-----------|--------|--------|
| Framework | Avalonia UI 12 (.NET 10) | Cross-platform (Windows + Linux), proven with FlipsiColor.Avalonia |
| Language | C# 13 | Familiar, performant |
| File scanning | .NET filesystem watchers + background indexing | Non-blocking, incremental |
| STL rendering | OpenTK / Silk.NET for 3D preview thumbnails | Hardware-accelerated preview |
| Printer protocol | Moonraker REST API + WebSocket for live data | Standard for Klipper-based printers |
| Local storage | SQLite (LiteDB alternative for document-style) | Embedded, no server needed |
| i18n | JSON-based localization (like FlipsiColor/FlipsiSort) | Consistent with existing TechFlipsi apps |

## TechFlipsi Ecosystem Integration

- **Design language**: Dark Void + Ember theme (matching techflipsi.kirchweger.de)
- **Branding**: TechFlipsi family product
- **Website cross-link**: Will be featured on techflipsi.kirchweger.de/geraete.html

## Roadmap (Planned)

| Phase | Scope |
|-------|-------|
| v0.1.0 | File scanner + grid/list view + STL thumbnails |
| v0.2.0 | Printer tab (Moonraker connection, live data, basic controls) |
| v0.3.0 | Filament inventory tracking |
| v0.4.0 | Cross-tab integration (start print from file → deduct from spool) |
| v0.5.0 | Settings, multi-printer, profiles |
| v0.6.0 | i18n (13 languages like FlipsiColor/FlipsiSort) |
| v1.0.0 | Installer (Windows .exe + Linux .deb), portable version |

## License

MIT — same as other TechFlipsi projects.

## Author

**TechFlipsi** (Fabian Kirchweger) — [techflipsi.kirchweger.de](https://techflipsi.kirchweger.de)