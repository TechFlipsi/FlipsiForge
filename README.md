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
- **Format-Filter oben** — zeigt Anzahl pro Dateiformat als klickbare Badges oben in der Leiste:
  - `STL: 200` `3MF: 50` `G-code: 120` `OBJ: 30` `STEP: 15` `PLY: 8` `AMF: 3`
  - Klick auf ein Format → zeigt nur Dateien dieses Typs
  - Klick auf "Alle" → zeigt wieder alles
  - Live aktualisiert beim Scannen
- **Suche** — immer Dateinamen-Suche + KI-Suche kombiniert (nicht entweder/oder):
  - **Dateinamen-Suche** (Fuzzy, offline, sofort) — durchsucht Dateinamen, Tags, Notizen nach dem Suchbegriff
  - **KI-Suche** (lokal eingebettet, automatisch mit am Start) — versteht Bedeutung, findet Synonyme, andere Sprachen. "Drache" findet auch `dragon_v2.stl`, `mythical_creature.3mf`, `wyvern_print.gcode`
  - Beide laufen parallel, Ergebnisse werden zusammengeführt (Dateinamen-Treffer zuerst, dann KI-Treffer)
  - **KI-Treffer sind gekennzeichnet** — jedes KI-Ergebnis hat ein Badge/Icon "🤖 KI" damit User sofort sieht: das ist ein KI-Treffer, kein Dateinamen-Treffer
  - KI läuft **lokal eingebettet** (ONNX Runtime, kleines quantisiertes Modell — wie FlipsiSort/FlipsiColor). Kein Ollama, kein externer Service, kein Internet nötig
  - **Optional:** User kann in Einstellungen externe KI-Anbieter konfigurieren (OpenAI, Anthropic, etc.) — wenn nichts konfiguriert wird, lokale KI verwendet
  - User tippt "Drache" → sieht sofort Dateinamen-Treffer + KI-Treffer (gekennzeichnet) in einer Liste
- **STL Repair Check** — scan files for defects (non-manifold edges, holes) before printing. Warning when model is broken
- **G-code Visualizer** — load G-code file → 3D layer-by-layer visualization. See exactly what will be printed
- **Datei-Versionierung** — edited STLs keep old versions (v1, v2, v3 with dates). No one loses the original by accident
- **Projekt-Export** — STL + G-code + filament info + print settings as ZIP package. For local backup or transfer to another PC
- **Bulk-Aktionen** — select multiple files → send to printer / group as project / export
- **Duplikat-Erkennung** — same STL under different names → warning
- **Sortierung nach Drucker-Eignung** — "Fits Snapmaker U1 (235×235×275mm)" vs "Fits Neptune 4 Pro"
- **ZIP-Archiv-Scan** — search STLs inside ZIP archives without extracting. Many models ship as ZIP
- **OpenSCAD-Integration** — .scad files recognized, parameter editor, customizer. Adjust parametric models without coding
- **Datei-Kommentare/Notizen** — add notes and comments to any model file
- **Fuzzy Search** — typo-tolerant search across all files

### 2. 🖨️ Printer Management — Direct Control

- Connect to **all major 3D printer brands** via 5 protocols:
  - **Klipper/Moonraker** (Snapmaker, Elegoo Neptune, Voron, Qidi, Anycubic, custom) — HTTP REST API + WebSocket
  - **Marlin** (Creality, Anycubic, Artillery, legacy printers) — USB-serial (System.IO.Ports)
  - **Bambu Lab** (Bambu Lab X1/P1/A1 series) — MQTT + FTP API
  - **PrusaLink** (Prusa MK3/MK4/MK3.5/SL1) — REST API (OpenAPI spec from Prusa)
  - **OctoPrint** (any printer with OctoPrint host) — REST API + WebSocket
- Multiple printer profiles — any brand, any model
- **Add / Remove printers** — add new printers, remove old ones when sold/replaced. Removing a printer keeps its print history (optional) but disconnects all active connections. Settings/data can be fully purged or archived
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
  - Load/unload filament macros — with **filament selection from Filament tab** when loading
- **G-code Terminal** — raw console for manual commands
- **Macro-Buttons** — configurable buttons for frequent macros (e.g. "Load Filament T0", "Bed Mesh Calibrate")
- **Druck-Queue** — queue multiple files. Next print starts automatically after previous finishes, **but requires confirmation first** (someone needs to clear the printer bed)
- **Webcam-Preview** — live camera feed from USB webcam (Moonraker camera) **or RTSP IP cameras** (rtsp://)
- **Temperature Curves** — live graph for hotend/bed temperature over time. **Temperature Logger** — record history for later analysis
- **Druck-Historie** — past prints with duration, filament usage, success/failure
- **Firmware-Info** — Klipper version, MCU info, input shaper data
- **Input Shaper UI** — graphical input shaping configuration (no SSH needed). Configure X/Y resonance, choose shaper type, see frequency graph
- **Host-System-Status** — CPU, RAM, disk usage, temperature of the printer host (Raspberry Pi / server)
- **Wartungs-Tracker** — maintenance schedule per printer: HEPA filter, carbon filter, nozzle, belt replacement intervals with reminders
- **Slicer-Integration** — OrcaSlicer/PrusaSlicer CLI directly callable from the app. STL → Slice → G-code → Send to printer. One-click workflow
- **Druck-Profil-Templates** — pre-configured profiles per printer (Snapmaker U1: 60°C bed, 210°C hotend, PLA). New user selects printer → ready-made profile

### 3. 🧶 Filament Management — Custom Spool Tracking

- **Custom-built system** (not Spoolman — adaptable to community needs)
- **Add / Edit / Remove spools** — full CRUD. Add new spool, edit details, remove when empty/sold/trashed. Removing a spool keeps usage history (optional) or purges all data
- Track filament inventory (brand, type, color, weight, remaining)
- **Filament size specification** — diameter (1.75mm, 2.85mm, 3.00mm) + spool dimensions (width, outer diameter, inner diameter, hub diameter) for AMS/box compatibility
- **Weight tracking** — nominal weight (advertised, e.g. 1000g), actual weight (measured), consumed weight (auto-deducted on print), remaining weight (calculated). Empty spool weight for weigh-to-calculate-remaining
- Spool lifecycle: new → in-use → empty → archived
- Cost tracking per spool (price paid, price per gram, price per kg)
- Low-stock alerts (configurable threshold)
- Material types: PLA, PETG, TPU, ABS, ASA, PC, Nylon, etc.
- Color tags with visual swatches — **filter by color**
- Usage history (which print used how much)
- Integration with printer tab (auto-deduct filament on print completion)
- **Material-Datenbank mit Auto-Fill** — standard densities auto-populated (PLA=1.24, PETG=1.27, TPU=1.21, ABS=1.04, etc.). Nobody knows their spool's density
- **Filament-Bild** — store a photo per spool (shop photo or own photo) for quick recognition
- **Spool QR-Code** — generate QR code per spool → scan with phone → instantly found in system
- **NFC-Tag Support (OpenPrintTag)** — read AND write NFC tags for filament identification. OpenPrintTag is Prusa's new open NFC standard
- **Fuzzy Search** — typo-tolerant search across filament names AND file manager
- **Verbrauchs-Vorhersage** — "At current usage, spool lasts ~14 days"
- **Filament-Empfehlung** — when selecting a file to print → "You have 3 matching spools (PLA Black, PLA Gray, PETG Black)"
- **Trocknungs-Log** — when/how long/at what temperature was spool dried. Warn about too-moist spools
- **Trocknungs-Timer** — mark spool as "in dryer" with countdown
- **Material-Empfehlung** — "For this model, PLA or PETG recommended, not TPU" based on geometry/wall thickness
- **Verbrauch pro Kategorie** — statistics: "This year 2.3kg PLA vs 800g PETG consumed" with charts
- **Spool-Status** — active, in dryer, in storage, empty, archived. Filter by status
- **Multi-pack support** — add multiple identical spools at once (e.g. "5× Prusament PLA Galaxy Black, 1000g each")

### 4. 🌐 Model Repository — Online Model Discovery

- **Unified search** across all major platforms — no need to select which one
- Supported platforms: **Thingiverse**, **Printables**, **MakerWorld** (and more)
- Search once → results from all platforms shown together in a single list
- Download directly into file manager — no browser needed
- Filter by: free/paid, category, print time, difficulty, rating
- "Makes" gallery — see what others printed with the same model
- **Auto-Sync** — automatically sync new models from Printables/MakerWorld when they appear

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

### 7. 🤖 KI-Assistent — Filament, Settings, Wartung & Drucker-Chat

Drei KI-Funktionen in einer Integration:

**A) Filament & Settings Empfehlung** (wie oben beschrieben)
- Filament-Empfehlung mit Inventar-Check
- Druck-Einstellungs-Empfehlung (Temp, Speed, Layer, Fan, Retraction)
- Slicer-Profil-Generierung
- Ziel-Modus (Festigkeit, Speed, Qualität, Prototyp)
- Verwendungszweck-Eingabe ("Auto-Innenraum", "Außen", etc.)

**B) Drucker-Wartungs-Empfehlungen** (online + offline)
- Mit Internet: modellspezifisch (bekannte Probleme, Ersatzteile, Firmware)
- Ohne Internet: allgemeine Wartungs-Tipps für alle Drucker

**C) Drucker-Assistent Chat** — User kann direkt in der Software schreiben
- **Chat-Interface** in der App — User stellt Fragen, KI antwortet
- Beispiele:
  - "Warum löst sich mein Druck vom Bett?" → KI: "Bett zu kalt, Filament-Typ prüfen, Bett reinigen, Brim verwenden..."
  - "Welche Temperatur für PETG auf dem Neptune 4 Pro?" → KI: nutzt Filament-DB, antwortet mit Hersteller-Empfehlung
  - "Mein Druck hat Stringing, was kann ich tun?" → KI: "Retraction erhöhen, Temperatur senken, PETG stringing-Anfällig..."
  - "Wie reinige ich die Düse?" → KI: Schritt-für-Schritt Anleitung
  - "Was ist Pressure Advance?" → KI: Erklärung + wie man es kalibriert
  - "Warum rattert mein Drucker?" → KI: Mögliche Ursachen (Riemen, Geschwindigkeit, Input Shaping)
- **Kontext-bewusst** — KI hat Zugriff auf alle Software-Daten:
  - User's Drucker (Marke, Modell, Firmware, Enclosed)
  - User's Filament-Inventar (was hat er, wie viel)
  - Druck-Historie (was hat er gedruckt, Erfolgsrate)
  - Filament-Marken-Datenbank (Hersteller-Empfehlungen)
  - Slicer-Einstellungs-Datenbank (Optimierungs-Tipps)
  - Material-Standard-DB (Temperaturen, Eigenschaften)
- **Lokal eingebettet** — Gemma 4 E4B (Desktop, ~3.7GB) oder E2B (Server/Raspberry Pi, ~2.6GB) via ONNX Runtime. Kein Ollama, kein externer Service, kein Internet nötig. App wählt automatisch das passende Modell basierend auf verfügbarem RAM
- **Optional:** Externe KI-Anbieter in Einstellungen konfigurierbar (OpenAI, Anthropic, etc.)
- **Chat-Verlauf** — Gespräche werden gespeichert, User kann später weitermachen
- **Schnell-Aktionen** — KI kann direkt Aktionen vorschlagen:
  - "Diese Spule zum Drucker laden" (aus Chat → Drucker-Tab)
  - "Diese Einstellungen als Slicer-Profil exportieren"
  - "Wartung als Erinnerung speichern"

- **Verwendungszweck (optional, Text-Eingabe)** — User beschreibt wofür das Modell gedacht ist:
  - "Auto-Innenraum" → hitzebeständig, UV-stabil → ASA oder ABS
  - "Außenbereich" → wetterfest, UV-resistent → ASA oder PETG
  - "Dekoration im Haus" → optische Qualität, keine Belastung → PLA
  - "Funktionsbauteil mit Gewinde" → fest, verschleißfest → PETG oder PA
  - "Flexible Dichtung" → flexibel → TPU
  - "Protoyp zum Testen" → schnell, billig → PLA
  - Frei eingebbbar — KI interpretiert den Text
- **Filament-Empfehlung mit Inventar-Check** — KI hat Zugriff auf das komplette Filament-Inventar:
  - "Ideal wäre PETG, aber du hast keins → nimm ABS (hast du 2 Spulen) → PLA ist ungeeignet für Außenbereich"
  - "Du hast 3 passende Spulen: Prusament PETG Orange, eSUN PETG Schwarz, Polymaker PETG Grau"
  - "Kein passendes Filament im Inventar → PETG kaufen (empfohlen für dieses Modell)"
- **Druck-Einstellungs-Empfehlung** — basierend auf Datei + Filament + Drucker + Verwendungszweck:
  - Hotend-Temperatur (z.B. PLA 200-220°C, PETG 230-245°C, TPU 220-240°C)
  - Bett-Temperatur (z.B. PLA 50-60°C, PETG 70-90°C)
  - Layer-Höhe (0.12mm Fine / 0.16mm Optimal / 0.24mm Draft)
  - Druckgeschwindigkeit (Fine Detail → langsam, Simple Geometrie → schnell)
  - Retraction-Distanz, Cooling-Fan %, Flow Rate
  - Infill-Dichte & Pattern (basierend auf Zweck: Prototyp vs. Funktionsbauteil)
- **Slicer-Profil-Generierung** — KI generiert komplettes Slicer-Profil (OrcaSlicer/PrusaSlicer) basierend auf Datei + Filament + Drucker + Zweck. User kann direkt starten
- **Fehler-Prävention** — warnt vor Problemen: "Hotend-Temperatur zu hoch für dieses Filament", "Bett-Temperatur außerhalb des empfohlenen Bereichs", "Layer-Höhe zu groß für 0.2mm Düse", "ABS braucht geschlossenen Drucker — deiner ist offen"
- **Drucker-Wartungs-Empfehlungen** — KI gibt Wartungs-Tipps in zwei Modi: **Mit Internet:** modellspezifisch (bekannte Probleme, Ersatzteile, Firmware-Updates, Community-Tipps für den genauen Drucker). **Ohne Internet:** allgemeine Wartungs-Empfehlungen die für alle Drucker gelten (Düse nach 300-500h, Riemen nach 1000h, Lager nach 1500h, etc.) — basierend auf Druckstunden und Standard-Verschleißteilen
- **Ziel-Modus** — User wählt Ziel: "Maximale Festigkeit", "Schneller Druck", "Optische Qualität", "Prototyp". KI passt Einstellungen entsprechend an
- **Erklärung** — KI erklärt WARUM sie jede Einstellung empfiehlt. Nicht nur Werte, sondern Begründung
- **Drucker-Datenbank (offline)** — KEINE fixierte Drucker-Liste (es gibt Tausende). Stattdessen: **Filament-Marken-Datenbank** (eSUN, Prusament, Polymaker, Bambu, Sunlu, Overture — mit Hersteller-Empfehlungen für Temp/Speed/Fan/Retraction pro Produkt) + **Slicer-Einstellungs-Datenbank** (OrcaSlicer/PrusaSlicer Optimierungs-Tipps: FineDetail, Strength, Speed, OverhangSupport, TPU, ABS/ASA). Drucker-Info kommt vom User-Profil (Build-Volume, Düse, Enclosed — User gibt an ob seine Haube montiert ist)
- **Vollständiger Daten-Zugriff** — KI hat Zugriff auf: Filament-Inventar, Drucker-Profile, Druck-Historie, Material-Datenbank, Drucker-Datenbank. Alles wird in den Prompt injiziert
- **Lokales Modell** — läuft offline, kein Cloud-Zwang. Entweder kleines fein-getuntes Modell oder regelbasiertes System mit LLM als Fallback

**Technischer Ansatz:**
- Kein speziell trainiertes 3D-Druck-Modell existiert (Stand Juli 2026)
- **Hybrid-System:** Regelbasierte Datenbank (Material-Typ → Standard-Temperaturen/Speed) + LLM für komplexe Empfehlungen (Geometrie-Analyse, Ziel-Modus, Inventar-Check, Verwendungszweck)
- **Referenz:** [Slicer Copilot](https://github.com/pfrankov/slicer-copilot) (11★, Apache-2.0) — nutzt LLM (GPT-4o) um .3mf-Projekte zu analysieren und Settings zu optimieren. Funktioniert mit lokalem LLM (OpenAI-compatible API)
- **Wissenschaft:** [LLM-3D Print](https://arxiv.org/abs/2408.14307) — Forschung zeigt dass LLMs ohne Fine-Tuning 3D-Druck-Parameter optimieren können
- **Datenquelle:** 3D-Druck-Settings-Datenbank (cnccode.com) mit Standardwerten für PLA/PETG/ABS/TPU für Temperatur/Speed/Retraction/Quality
- **Lokale Ausführung:** Kleines LLM (z.B. Gemma 4 12B oder Qwen3.5 14B via Ollama) als lokaler Empfehlungs-Engine. Cloud-LLM nur optional

## Architecture — Shared Core + Built-in Server

FlipsiForge is **one app** with an optional server backend. The desktop app always provides the full GUI experience — whether running standalone (local data) or connected to a FlipsiForge Server (centralized data).

```
FlipsiForge.Core (shared business logic)
├── FileScanner        — drive scanning, file indexing, thumbnails, STL repair check, ZIP scan, OpenSCAD
├── PrinterController  — Moonraker REST/WS + Marlin USB-serial + Bambu API, print queue, slicer CLI, input shaper, maintenance tracker
├── FilamentManager    — spool inventory, consumption, cost tracking, drying log, NFC/QR, material DB, fuzzy search
├── CostCalculator     — print cost calculator (filament + power + wear)
├── ModelRepository    — Thingiverse / Printables / MakerWorld unified search + auto-sync
├── StatisticsEngine   — print analytics, consumption charts, success rates
├── CameraManager      — USB webcam + RTSP IP camera + timelapse recording
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
- **Timelapse** — webcam snapshots at intervals → timelapse video of print. Stored in dedicated timelapse tab. Server-side feature (PC doesn't need to stay on). **Manual start** also possible (timelapse without active print). **Render abort** on print cancel
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
| Printer protocol (Klipper) | Moonraker REST API + WebSocket | Snapmaker, Elegoo, Voron, Qidi, Anycubic |
| Printer protocol (Marlin) | USB-serial (System.IO.Ports) | Creality, Anycubic, Artillery, legacy |
| Printer protocol (Bambu) | Bambu Lab MQTT + FTP API | Bambu Lab X1/P1/A1 series |
| Printer protocol (Prusa) | PrusaLink REST API (OpenAPI spec) | Prusa MK3/MK4/MK3.5/SL1 |
| Printer protocol (OctoPrint) | OctoPrint REST API + WebSocket | Any printer with OctoPrint host |
| Camera | USB webcam + RTSP IP camera support | Not everyone has USB — many use IP cameras |
| NFC | OpenPrintTag standard + generic NFC read/write | Prusa's open NFC standard for filament ID |
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

- **Design language**: Dark Void + Ember theme — identisch zur TechFlipsi Homepage (techflipsi.kirchweger.de)
- **Logo**: "TECH" weiß + "FLIPSI" orange — selbes Logo wie Homepage
- **Branding**: TechFlipsi family product
- **Software-Standard**: Avalonia UI + Installer/Portable + GPL-3.0 + 13 Sprachen i18n
- **Website cross-link**: techflipsi.kirchweger.de/geraete.html
- **Community**: Öffentlich, Issues/PRs willkommen (wie FlipsiColor)

### Design System (wie techflipsi.kirchweger.de)

| Element | Wert |
|---------|------|
| Hintergrund | `#050507` (Void) |
| Akzentfarbe | `#ff6600` (Ember) |
| Akzent-Glow | `rgba(255, 102, 0, 0.4)` |
| Akzent-Soft | `rgba(255, 102, 0, 0.15)` |
| Schrift Body | `Inter` |
| Schrift Headings | `Space Grotesk` |
| Karten/Surface | Dark surface mit Border + Hover-Glow |
| Logo | `logo-emblem.png` — 3D-Drucker-Extruder der einen Wireframe-Würfel druckt (Neon-Orange auf Schwarz, kreisförmig). Selbes Bild wie techflipsi.kirchweger.de |
| App-Icon | `apple-touch-icon.png` / `favicon.ico` — abgeleitet vom Logo-Emblem |
| Buttons | `.cta-button` mit Arrow SVG Icon |

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