# Changelog

All notable changes to FlipsiForge will be documented in this file.

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

### Not Yet Implemented (planned for v0.2.0+)
- KI-Chat (Gemma 4 ONNX Runtime GenAI integration)
- KI-Suche (semantic file search with embeddings)
- File scanning (auto-scan drives for STL/3MF/G-code)
- Printer protocol implementations (Moonraker, Marlin, Bambu, PrusaLink, OctoPrint)
- Web-UI for Server Full
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