# Changelog

All notable changes to FlipsiForge will be documented in this file.

## [0.3.0] — 2026-08-09

### Added — DruckWächter
- **ShellyClient** — HTTP RPC Client for Shelly Gen2/3/4 (Switch.Set/Get, PM auto-detect)
- **DruckWaechterService** — Automation engine: status, control, costs, multi-extruder auto-detect
- **DruckWaechterConfig** — Global (electricity price, night mode, auto-off timer) + per-printer (Shelly IP, macros)
- **DruckWaechterView** — Bento cards, sliders (printer on/off, light on/off), filament buttons, multi-extruder popup, temperature bars
- **Auto-off at print end** — With Telegram (buttons + timeout) or without (15 min timer, configurable)
- **Cool-down wait** — All extruders must drop below threshold before shutdown
- **Night mode** — Auto-off without prompt in time window (e.g. 00:00–06:00)
- **Graceful shutdown** — Moonraker `/server/shutdown` → wait → Shelly off (fallback: direct Shelly off)
- **Cost calculator** — Electricity (if Shelly PM) + filament (always) — Shelly PM auto-detect
- **Design** — Bento cards in TechFlipsi Dark Void + Ember theme

### Added — KI-Integration
- **OnnxGenAiChatEngine** with `Microsoft.ML.OnnxRuntimeGenAI` 0.14.1
- **3-Tier model selection** by RAM:
  - E4B (~3.7GB) — Desktop ≥8GB
  - E2B (~2.6GB) — Mini-PC 4–8GB
  - E2B QAT (~1.3GB) — Raspberry Pi 4 2–4GB
- HuggingFace download links verified
- KI completely disableable in settings
- Robust against missing native libs → Stub mode (`IsLoaded=false`)

### Added — Windows Installer
- GitHub Actions build (dotnet publish + Inno Setup)
- `FlipsiForge-0.3.0-win-x64-setup.exe` available

## [0.2.0] — 2026-07-25

### Added — Server
- **Web-UI (Full mode)** — Browser dashboard with 6 tabs (Printers, Spools, Filament-DB, Statistics, Files, System). Dark Void + Ember theme. Vanilla JS, no build step, no Node dependency.
- **Settings-CRUD** — `GET /api/settings`, `PUT /api/settings`, `PATCH /api/settings/{field}` — partial updates via field name.
- **Printer-CRUD complete** — `PUT` (full update), `PATCH /activate` (reactivate), `DELETE?keepHistory=true|false` (archive vs hard delete).
- **Live printer connection** — `/connect`, `/status`, `/temps`, `/job` via `IPrinterConnectionManager` (Stub for offline, Core.Services for Moonraker/Marlin/Bambu/PrusaLink/OctoPrint).
- **Maintenance** — Create maintenance entries, dual-mode recommendations (`?onlineMode=true|false`) — model-specific or general tips, never "Internet required".
- **Files** — Scan trigger (`POST /api/files/scan` with folders+extensions), favorite toggle, usage log (viewed/printed), combined search (filename + AI if available, 🤖 AI badge for AI hits).
- **KI-Endpoints** — `/ai/chat` (streaming via SSE if `AI:Streaming=true`), `/ai/embed`, `/ai/status`, `/ai/slicer-profile` (rule-based via Filament-DB + optional AI).
- **Forge-Bot** — In-app companion (Eilik-style, Dark Void + Ember), `/api/bot/messages`, `/api/bot/dismiss`, `/api/bot/settings`.
- **Backup/Restore/Export/Cache** — SQLite VACUUM-INTO backup, restore with pre-restore backup, JSON export of all data, cache clearing (thumbnails/embeddings/temp).
- **Extended statistics** — `/statistics/files` (per format, per folder, favorite count), `/statistics/filament` (total weight, by material, by brand).
- **Core.Services-compatible** — Server compiles and runs standalone thanks to `Services/Stub*.cs` fallbacks. Real Core implementations swing in via `TryAddSingleton` automatically.

## [0.1.0-pre] — 2026-07-18

### Added
- **Project structure**: Solution with 3 projects (Core, Desktop, Server)
- **FlipsiForge.Core**: Models, DbContext, Filament-Marken-Datenbank (41 Einträge, 20 Marken)
- **FlipsiForge.Desktop**: Avalonia UI 12 with 7 tabs (Datei-Manager, Drucker, Filament, Model-Repo, Statistik, Kosten-Rechner, KI-Assistent)
- **FlipsiForge.Server**: ASP.NET Core Minimal API with Full/Lite mode support
- **Gateway API**: CRUD for printers, spools, print jobs, print history, statistics
- **Filament-Marken-Datenbank**: 41 entries covering 20 brands (eSUN, Prusament, Polymaker, Bambu Lab, Sunlu, Overture, Hatchbox, Elegoo, Creality, Inland, Fillamentum, ColorFabb, 3DXTech, Siraya Tech, Duramic, Eryone, MatterHackers, Atomic Filament, Fiberlogy, CookieCAD)
- **Dark Void + Ember theme**: TechFlipsi design language (#050507 + #ff6600)
- **Docker support**: Dockerfile for Server Full + Dockerfile.lite for Server Lite
- **System requirements** documented in README
- **3-tier AI system** documented (Gemma 4 E4B/E2B/E2B QAT)

### Implemented in v0.2.0–v0.3.0
- ✅ KI-Chat (Gemma 4 ONNX Runtime GenAI integration)
- ✅ KI-Endpoints (chat, embed, status, slicer-profile)
- ✅ Web-UI for Server Full
- ✅ Server-CRUD (printers, spools, settings, files, maintenance, statistics)
- ✅ DruckWächter (Shelly + Moonraker integration)

### Not Yet Implemented (planned for v0.4.0+)
- KI-Suche (semantic file search with embeddings — stub only)
- File scanning (auto-scan drives for STL/3MF/G-code — stub only)
- Printer protocol implementations (Moonraker, Marlin, Bambu, PrusaLink, OctoPrint — stub only)
- Push notifications (Telegram)
- Cloud sync (Nextcloud)
- Home Assistant HACS integration + Add-on
- NFC/QR code support
- Slicer integration (OrcaSlicer/PrusaSlicer CLI)
- 3D rendering (Silk.NET)
- G-code visualizer (SkiaSharp)
- Model repository search (Thingiverse/Printables/MakerWorld)
- Plugin system
- i18n (13 languages)