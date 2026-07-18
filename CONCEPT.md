# FlipsiForge — Project Concept (18.07.2026)

## Ursprungsidee (Sir's Beschreibung)

Ein PC-Programm passend zur TechFlipsi Homepage. Schwerpunkt: 3D-Drucker-Management mit mehreren Hauptbereichen:

1. **Datei-Manager** — Selbstständige Suche über kompletten PC und alle Laufwerke nach 3D-Druck-Dateien (STL etc., alle Formate). Vorschaubild für jede Datei. Umschaltbar zwischen Raster-Ansicht und Listen-Ansicht.
2. **Drucker-Verwaltung** — 3D-Drucker einbinden und direkt verwalten (Druck starten, hochladen etc.). Anzeige von Drucker-Daten: Temperaturen, Druck-Status, Fortschrittsanzeige.
3. **Filament-Verwaltung** — Eigenes Spoolman-Äquivalent. Spulen-Inventar, Verbrauch, Bestand.

## Sir's Entscheidungen (18.07.2026)

### Grundlegendes

| # | Frage | Antwort |
|---|-------|---------|
| 1 | Framework | **Avalonia UI (.NET 10)** — neuer TechFlipsi Software-Standard. Windows + Linux. **Portable Version** = neuer Standard für alle TechFlipsi Apps! |
| 2 | Drucker-Protokoll | **5 Protokolle für alle Hersteller**: (1) Klipper/Moonraker — Snapmaker, Elegoo, Voron, Qidi, Anycubic. (2) Marlin USB-serial — Creality, Anycubic, Artillery, legacy. (3) Bambu Lab MQTT+FTP — Bambu X1/P1/A1. (4) PrusaLink REST — Prusa MK3/MK4/MK3.5/SL1. (5) OctoPrint REST — jeder Drucker mit OctoPrint host. Ziel: JEEDER 3D-Drucker auf dem Markt einbindbar. |
| 3 | Plattform | **Windows + Linux** |
| 4 | Packaging | **Installer + Portable** — neuer Standard! |
| 5 | Cloud-Sync | **Optional, Default = Lokal.** Nextcloud = Priorität 1. Auch Google Drive, OneDrive, Dropbox. Use-Cases: Einstellungen sync, Filament-Stand auf allen PCs, 3D-Druck-Dateien zentral. |
| 6 | Filament-System | **Eigenes System** (nicht Spoolman). Anpassbar auf Community-Wünsche. |
| 7 | Community | **Öffentlich** — Projekt lebt von der 3D-Druck-Community. |
| 8 | Name | **FlipsiForge** — bestätigt. |

### Server

| # | Frage | Antwort |
|---|-------|---------|
| 9 | Server-Variante | **Eingebaut, nicht separat.** Desktop-App ist immer der Client. Server-Modus optional. UI identisch ob lokal oder server-verbunden. Server abschaltbar → lokales SQLite. |
| 10 | Server-Zugang | **Zwei Wege**: (1) Gateway API (REST + WebSocket) für Desktop-App. (2) Web-UI für Browser (Handy/Tablet ohne App). |
| 11 | Auto-Discovery | **mDNS/Bonjour** — App und Server finden sich selbst. Keine manuelle IP. Manuelle Verbindung für Remote/VPN möglich. |
| 12 | Server-Hardware | **Jeder Linux-Server** — nicht nur Raspberry Pi. NUC, VPS, alter Laptop, NAS mit Docker. ARM64 + x64. |
| 13 | Remote-Zugang | **Tailscale/WireGuard/Reverse Proxy** — Server von unterwegs erreichbar. |

### Features (Runde 2)

| # | Idee | Antwort |
|---|-------|---------|
| 14 | STL Reparatur-Check | ✅ **Rein** — Dateien auf Defekte prüfen vor dem Druck |
| 15 | Slicer-Integration (OrcaSlicer/PrusaSlicer CLI) | ✅ **Rein** — wenn direkt hinzufügbar. Sir hat Bedenken wegen Original-Qualität, aber einbinden. |
| 16 | Projekt-Export (ZIP) | ✅ **Rein** — für komplett lokale User die dennoch auf anderen PCs arbeiten wollen |
| 17 | Druck-Queue | ✅ **Rein** — aber mit **Bestätigungspflicht** bevor nächster Druck startet (jemand muss Drucker leeren) |
| 18 | Timelapse | ✅ **Rein** — mit eigenem Speicher-Tab. **Server-Funktion** (nicht App, weil PC nicht bis Druckende an bleiben soll) |
| 19 | G-code Visualizer | ✅ **Rein** — Layer-für-Layer 3D Vorschau |
| 20 | Filament-Verwaltung (alle Ideen) | ✅ **Alles rein** — QR-Code, Verbrauchs-Vorhersage, Empfehlung, Preis-Tracking, Trocknungs-Log, Trocknungs-Timer, Material-Empfehlung, Verbrauch pro Kategorie |
| 21 | Server-Features (alle Ideen) | ✅ **Alles rein** — Multi-User/Rollen, Push-Notifications, Statistik-Export, REST API, Timelapse |
| 22 | Übergreifend (alle Ideen) | ✅ **Alles rein** — Backup/Restore, Druck-Profil-Templates, Datei-Versionierung |
| 23 | Makerspace-Support (Multi-User/Rollen) | ✅ **Rein** — Admin/User/Viewer Rollen |
| 24 | Push-Notifications | ✅ **Rein** — **Telegram bevorzugt**, alle anderen Möglichkeiten auch |
| 25 | Home Assistant Integration | ✅ **Pflicht** — HACS Custom Integration + HA Add-on um Server direkt in HA zu betreiben |
| 26 | Plugin-System | ✅ **Rein** — Community kann eigene Plugins schreiben |
| 27 | Statistik-Dashboard (4. Tab) | ✅ **Rein** — Diagramme, Verbrauch, Erfolgsrate, Kosten, Auslastung |
| 28 | Model-Repository (Online-Plattformen) | ✅ **Rein** — alle gängigen einbinden. **Unified Search** — nicht auswählen welche Plattform, alle gleichzeitig durchsuchen. Ergebnisse gesammelt anzeigen. Thingiverse, Printables, MakerWorld. |
| 29 | QR-Code Drucker-Zugang | ⚠️ **Optional** — Sir braucht es nicht (Dashboard reicht), aber als Option einbauen falls andere es wollen |
| 30 | Druck-Kosten-Rechner | ✅ **Must-Have** — bereits bestätigt |

## Vollständige Feature-Liste

### Tab 1: 📁 Datei-Manager
- Auto-Scan aller Laufwerke nach 3D-Druck-Dateien (STL, OBJ, 3MF, G-code, PLY, STEP, AMF, X3D)
- Vorschaubild (STL/3MF/OBJ gerendert, G-code Layer-Preview)
- Grid/List umschaltbar
- Watch-Folders (neue Dateien auto-erkannt)
- Duplikat-Erkennung
- Datei-Tags (benutzerdefiniert)
- Datei-Versionierung (v1, v2, v3 — alte Versionen behalten)
- STL Reparatur-Check (non-manifold, holes → Warnung)
- G-code Visualizer (3D Layer-für-Layer)
- 3MF Multi-Modell-Extraktion
- Sortierung nach Drucker-Eignung ("Passt auf Snapmaker U1")
- Bulk-Aktionen (mehrere Dateien → Drucker/Projekt/Export)
- Projekt-Export (STL + G-code + Filament + Settings als ZIP)
- Suchen & Filtern

### Tab 2: 🖨️ Drucker-Verwaltung
- Klipper/Moonraker (REST + WebSocket) + Marlin (USB-serial)
- Multi-Drucker Dashboard (alle Drucker side-by-side)
- Live-Daten: Temperaturen, Status, Fortschritt, Layer, Speed
- Webcam Live-Feed
- Temperatur-Kurven (Live-Graph)
- Druck-Historie (Dauer, Filament, Erfolg/Misserfolg)
- Firmware-Info (Klipper, MCU, Input Shaper)
- Direkt-Aktionen: Start, Pause, Cancel, Emergency Stop, Home, Move, Fans, Filament Macros
- G-Code Terminal (Raw-Konsole)
- Macro-Buttons (konfigurierbar)
- Druck-Queue (mit Bestätigungspflicht vor nächstem Druck)
- Slicer-Integration (OrcaSlicer/PrusaSlicer CLI → STL → G-code → Drucker)
- Druck-Profil-Templates (pro Drucker fertige Profile)
- Benachrichtigungen (Desktop-Notification bei Druckende/Error)

### Tab 3: 🧶 Filament-Verwaltung (Eigenes System)
- Spulen-Inventar (Marke, Typ, Farbe, Gewicht, Restbestand)
- Spool Lifecycle: neu → in-use → leer
- Kosten-Tracking (Preis/Spule, Preis/Gramm)
- Low-Stock Warnungen
- Material-Typen: PLA, PETG, TPU, ABS, ASA, PC, Nylon, etc.
- Farb-Tags mit visuellen Swatches
- Verbrauchs-Historie (welcher Druck, wie viel)
- Auto-Abzug bei Druck-Ende
- QR-Code pro Spule (generieren + scannen)
- Verbrauchs-Vorhersage ("reicht ~14 Tage")
- Filament-Empfehlung bei Datei-Auswahl
- Trocknungs-Log (wann, wie lange, welche Temperatur)
- Trocknungs-Timer mit Countdown
- Material-Empfehlung ("PLA/PETG, nicht TPU" basierend auf Geometrie)
- Verbrauch pro Kategorie (Diagramm: "2,3kg PLA vs 800g PETG")

### Tab 4: 🌐 Model-Repository
- Unified Search über Thingiverse, Printables, MakerWorld (und mehr)
- Eine Suche → alle Plattformen gleichzeitig → Ergebnisse gesammelt
- Download direkt in Datei-Manager
- Filter: kostenlos/kostenpflichtig, Kategorie, Druckzeit, Schwierigkeit, Bewertung
- "Makes" Gallery (was andere mit dem Modell gedruckt haben)
- Keine Plattform-Auswahl nötig — alle werden durchsucht

### Tab 5: 📊 Statistik-Dashboard
- Filament-Verbrauch über Zeit (Diagramm)
- Druck-Erfolgsrate
- Kosten pro Monat / pro Drucker
- Drucker-Auslastung
- Druck-Kosten-Rechner Gesamtansicht
- Export als Report (PDF/CSV)

### Übergreifend
- 🔥 **Druck-Kosten-Rechner** — Filament × Preis/g + Strom (Watt × Dauer × kWh-Preis) + Verschleiß = Gesamtkosten. Auto-filled aus Datei + Filament + Drucker. Manuell überschreibbar.
- **Cloud-Sync** (optional): Nextcloud (P1), Google Drive, OneDrive, Dropbox. Default = lokal.
- **Backup/Restore** — gesamte Datenbank als File exportieren/importieren
- **Plugin-System** — Community-Plugins (eigene Slicer, Statistiken, Integrationen)
- **Projekt-Gruppen** — Dateien + Filament + Drucker zu Projekt zusammenfassen
- **Dark/Light Theme** — Dark Void + Ember als Default
- **13 Sprachen i18n** — konsistent mit FlipsiColor/FlipsiSort
- **QR-Code Drucker-Zugang** (optional) — QR am Drucker → Handy scan → Web-UI für diesen Drucker

### Server-Only Features
- **Timelapse** — Webcam-Snapshots → Timelapse-Video. Eigener Speicher-Tab. Server-Seitig (PC aus = Video wird trotzdem fertig).
- **Push-Notifications** — Telegram (bevorzugt), Web-Push, Email, Desktop. Druckende/Error.
- **Multi-User mit Rollen** — Admin (alles), User (nur drucken), Viewer (nur gucken). Für Makerspaces.
- **REST API** — öffentliche API für Dritt-Tools. `GET /api/filaments`, `GET /api/printers/status`, etc.
- **Druck-Statistiken-Export** — Server sammelt alle Druck-Daten → Monatsbericht (Erfolgsrate, Kosten, Verbrauch).
- **Home Assistant Integration** — HACS Custom Integration + HA Add-on. Server direkt in HA betreiben. Sensoren für Filament, Druck-Status, Kosten. Volles Dashboard.

## Tech Stack (Final)

| Component | Choice |
|-----------|--------|
| Framework | Avalonia UI 12 (.NET 10) |
| Language | C# 13 |
| File scanning | .NET filesystem watchers + background indexing |
| STL rendering | OpenTK / Silk.NET (3D preview + G-code visualizer) |
| STL repair | Mesh analysis (non-manifold, holes) |
| Slicer integration | OrcaSlicer / PrusaSlicer CLI orchestration |
| Printer (Klipper) | Moonraker REST API + WebSocket |
| Printer (Marlin) | USB-serial (System.IO.Ports) |
| Model repos | Thingiverse / Printables / MakerWorld REST APIs |
| Cloud-Sync | Nextcloud WebDAV (P1) + Google Drive / OneDrive / Dropbox |
| Local storage | SQLite (PostgreSQL optional für Multi-User) |
| Server backend | ASP.NET Core + Docker (ARM64 + x64) |
| Auto-Discovery | mDNS/Bonjour (UDP broadcast) |
| Push notifications | Telegram Bot API (P1) + Web-Push + email |
| Home Assistant | HACS custom integration + HA Add-on |
| Plugin system | .NET plugin loading (MEF oder assembly load) |
| i18n | JSON-based (13 Sprachen) |
| Packaging | Installer (.exe + .deb) + Portable (.zip) |
| License | GPL-3.0 |

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

## TechFlipsi Ecosystem Integration

- **Design language**: Dark Void + Ember theme (matching techflipsi.kirchweger.de)
- **Branding**: TechFlipsi family product
- **Software-Standard**: Avalonia UI + Installer/Portable + GPL-3.0 + 13 Sprachen i18n
- **Website cross-link**: techflipsi.kirchweger.de/geraete.html
- **Community**: Öffentlich, Issues/PRs willkommen (wie FlipsiColor)

## Referenz-Projekte — Was es gibt und was wir besser machen

### Drucker-Kontrolle (Klipper/Moonraker)

| Projekt | Stars | Sprache | Lizenz | Was es kann | Was wir übernehmen |
|---------|-------|---------|--------|-------------|---------------------|
| **[Mainsail](https://github.com/mainsail-crew/mainsail)** | 2.180 | Vue | GPL-3.0 | Beliebtestes Klipper Web-UI. Dashboard, Macros, Temperature Curves, Webcam | UI/UX Patterns für Drucker-Dashboard, Macro-Verwaltung |
| **[Fluidd](https://github.com/fluidd-core/fluidd)** | 1.782 | Vue | GPL-3.0 | Zweites großes Klipper UI. Lightweight, responsive | Alternative UI-Patterns, responsive Design-Ideen |
| **[OctoPrint](https://github.com/OctoPrint/OctoPrint)** | 9.041 | Python | AGPL-3.0 | Das originale 3D-Drucker Web-Interface. Plugin-System, G-code viewer, Timelapse | Plugin-System Architektur, G-code Visualisierung, Timelapse-Pattern |
| **[KlipperScreen](https://github.com/jordanruthe/KlipperScreen)** | 1.363 | Python | AGPL-3.0 | Touchscreen-Interface für Klipper | Touch-optimized UI Patterns (relevant für Web-UI auf Tablet) |
| **[Moonraker](https://github.com/Arksine/moonraker)** | 1.427 | Python | GPL-3.0 | API Server für Klipper. REST + WebSocket | Unsere direkte API-Grundlage für Klipper-Drucker |

**Unser Vorteil**: Mainsail/Fluidd/OctoPrint sind reine Web-Interfaces. FlipsiForge bietet eine **native Desktop-App** mit Datei-Manager, Filament-Tracking und Model-Repository — alles in einem. Plus Server-Modus mit Auto-Discovery.

### Slicer

| Projekt | Stars | Sprache | Lizenz | Relevanz |
|---------|-------|---------|--------|----------|
| **[OrcaSlicer](https://github.com/OrcaSlicer/OrcaSlicer)** | 15.138 | C++ | AGPL-3.0 | Meistgenutzter Open-Source Slicer. Wir rufen die CLI davon auf |
| **[PrusaSlicer](https://github.com/prusa3d/PrusaSlicer)** | 9.179 | C++ | AGPL-3.0 | Zweite Slicer-Option für CLI-Integration |
| **[Cura](https://github.com/Ultimaker/Cura)** | 6.996 | Python | LGPL-3.0 | Dritte Option. Python-basiert, auch CLI-callable |

**Unser Ansatz**: Wir bauen keinen eigenen Slicer. Wir orchestrieren OrcaSlicer/PrusaSlicer/Cura via CLI — STL → Slice → G-code → Drucker, alles aus FlipsiForge.

### Filament-Management

| Projekt | Stars | Sprache | Lizenz | Was es kann | Was wir besser machen |
|---------|-------|---------|--------|-------------|----------------------|
| **[Spoolman](https://github.com/Donkie/Spoolman)** | 2.608 | Python | MIT | Beliebtestes Filament-Tracking. Web-Service, QR-Codes, Klipper/Moonraker-Integration, OctoPrint-Plugin | Wir bauen eigenes System: Desktop-App statt nur Web, Trocknungs-Log/Timer, Material-Empfehlung, Verbrauchs-Vorhersage, Kosten-Rechner integriert |
| **[SpoolEase](https://github.com/yanshay/SpoolEase)** | 528 | Rust | — | NFC-basiertes Filament-Management. Gewicht/Location-Tracking, Slicer-Integration | NFC als zusätzliche Option neben QR-Codes interessant |

**Unser Vorteil**: Spoolman ist nur Filament. Wir haben Filament + Drucker + Dateien + Kosten in einer App. Plus Trocknungs-Log und Material-Empfehlung die keines der beiden hat.

### 3D-Modell-Verwaltung (Datei-Manager)

| Projekt | Stars | Sprache | Lizenz | Was es kann | Was wir besser machen |
|---------|-------|---------|--------|-------------|----------------------|
| **[Manyfold](https://github.com/manyfold3d/manyfold)** | 2.083 | Ruby | AGPL-3.0 | Self-hosted 3D-Modell-Verwaltung. Docker, Tags, STL-Viewer im Browser, Metadata | Wir: Desktop-App statt nur Web, Auto-Scan aller Laufwerke (nicht nur ein Ordner), STL-Reparatur-Check, G-code Visualizer, Duplikat-Erkennung, Drucker-Eignung-Check |
| **[StlVault](https://github.com/rubenwe/StlVault)** | 230 | C# | MIT | 3D-Modell-Viewer und Organizer. "Lightroom für 3D-Druck" | C# — gleiche Sprache wie wir! Architektur als Referenz. Wir erweitern um Drucker/Filament/Kosten |
| **[STL Shelf](https://stl-shelf.com)** | — | — | — | Private STL-Bibliothek. Versioning, Katalogisierung | Datei-Versionierung als Inspiration |

**Unser Vorteil**: Manyfold und StlVault sind nur Datei-Manager. FlipsiForge verbindet Dateien mit Druckern (start print from file manager) und Filament (auto-deduct on print). Niemand hat das bisher in einer App.

### Model-Repository (Online-Suche)

| Projekt | Stars | Was es kann |
|---------|-------|-------------|
| **[3DScanner](https://github.com/MoeinAlz/3DScanner)** | 0 | Scrapt MakerWorld, Printables, Thingiverse gleichzeitig. "No more jumping between websites" |

**Das bestätigt unsere Idee**: Jemand hatte exakt denselben Gedanken — unified search über alle Plattformen. Aber 0 Stars, winzig. Wir bauen das richtig: in eine Desktop-App integriert, mit Download direkt in den Datei-Manager, Filter, "Makes" Gallery.

### STL-Reparatur

| Projekt | Stars | Sprache | Was es kann |
|---------|-------|---------|-------------|
| **[stlrepair](https://github.com/shanekirk/stlrepair)** | 6 | C++ | CLI-Tool: repariert broken STL headers, truncated data, non-triangle data |

**Insight**: STL-Reparatur ist ein Nischen-Feature. Wir bauen es direkt in die App ein als Pre-Print-Check — warnt before man druckt, nicht nur nachher.

### Kosten-Rechner

| Projekt | Stars | Was es kann |
|---------|-------|-------------|
| **[3D_Printer_Cost_Calculator](https://github.com/BahadrPoroy/3D_Printer_Cost_Calculator)** | 0 | Desktop, C++, Filament + Strom |
| **[FilamentCalculator](https://github.com/MKinG4ever/FilamentCalculator)** | 0 | Python, Filament + Strom + Verschleiß + Profit |

**Insight**: Kosten-Rechner gibt es als Standalone-Tools, aber niemand hat es in ein 3D-Druck-Management-Tool integriert. Bei uns ist es Teil des Workflows: Datei auswählen → Filament wählen → Kosten automatisch berechnet → Drucken.

### Timelapse

| Projekt | Stars | Was es kann |
|---------|-------|-------------|
| **[easy-timelapse](https://github.com/Mercuso/easy-timelapse)** | 9 | Webcam/Phone → Timelapse bei Layer-Change |
| **OctoPrint** (Plugin) | — | Built-in Timelapse via G-code hooks |

**Unser Ansatz**: Server-side Timelapse (PC muss nicht an bleiben). Moonraker hat camera endpoint + layer-change hooks. Snapshots → ffmpeg → MP4.

## Was FlipsiForge einzigartig macht

Kein existierendes Projekt kombiniert alle diese Bereiche:

```
┌─────────────────────────────────────────────────────────┐
│                    FlipsiForge                          │
│                                                         │
│  Datei-Manager  +  Drucker-Kontrolle  +  Filament-DB    │
│  (Manyfold)        (Mainsail/Fluidd)     (Spoolman)      │
│       +                +                      +          │
│  Model-Search    Slicer-Integration    Kosten-Rechner    │
│  (3DScanner)      (OrcaSlicer CLI)      (niemand hat)    │
│       +                +                      +          │
│  STL-Repair      Druck-Queue             Trocknungs-Log  │
│  (stlrepair)      (OctoPrint)             (niemand hat)  │
│                                                         │
│  = Alles in EINER App. Desktop + Server.                │
└─────────────────────────────────────────────────────────┘
```

**Bisher muss man 4-5 Tools kombinieren:** Mainsail (Drucker) + Spoolman (Filament) + Manyfold (Dateien) + OrcaSlicer (Slicing) + Web-Browser (Modelle suchen). FlipsiForge macht das alles.

## Community Issue-Analyse — Feature-Ideen aus GitHub Issues

Analysiert am 18.07.2026 — offene Issues der Referenz-Projekte, sortiert nach Community-Interest (Reactions).

### Aus Spoolman Issues (Filament-Management)

| Issue | Reactions | Idee | Für FlipsiForge |
|-------|-----------|------|-----------------|
| #776 | 👍28 | **OpenPrintTag** — offener NFC-Tag Standard von Prusa. NFC-Tags statt QR-Codes für Filament-Erkennung | ✅ NFC-Support neben QR-Codes. OpenPrintTag ist neu offener Standard |
| #217 | 👍27 | **Bambu Lab Integration** — Bambu Drucker direkt anbinden | ✅ Bambu Lab Drucker-Support (neben Klipper/Marlin) |
| #69 | 👍18 | **Auto-populate density** — Dichte-Feld automatisch ausfüllen. Niemand kennt die Dichte seiner Spule | ✅ Material-Datenbank mit Standard-Dichten (PLA=1.24, PETG=1.27, TPU=1.21, ABS=1.04, etc.). Auto-fill beim Anlegen |
| #552 | 👍11 | **Filament-Bild** — Bild pro Spule speichern (Shop-Foto zur Erkennung) | ✅ Bild pro Filament-Spule (Shop-Foto oder eigenes Foto) |
| #356 | 👍10 | **Fuzzy Search** — ungenaue Suche bei Filament-Namen | ✅ Fuzzy-Search im gesamten Datei-Manager UND Filament-Tab |
| #501 | 👍9 | **Auth/Multi-tenancy** — mehrere User mit Zugriffsrechten | ✅ Bereits geplant (Admin/User/Viewer Rollen) — bestätigt! |
| #739 | 👍8 | **Filter by Color** — Filamente nach Farbe filtern | ✅ Farb-Filter im Filament-Tab |
| #723 | 👍8 | **NFC-Tags erstellen** — nicht nur QR, auch NFC schreiben | ✅ NFC-Tag-Support (OpenPrintTag + generische NFC) |

### Aus Mainsail Issues (Drucker-UI)

| Issue | Reactions | Idee | Für FlipsiForge |
|-------|-----------|------|-----------------|
| #267 | 👍27 | **User Authentication** — Login bei externem Zugriff | ✅ Bereits geplant (Server Mode 3: Remote Access) — bestätigt! |
| #870 | 👍24 | **G-code Viewer auf Dashboard** — direkt neben Webcam | ✅ G-code Visualizer bereits geplant — auf Dashboard einblendbar |
| #1648 | 👍20 | **System Load Panel** — CPU/RAM des Drucker-Hosts anzeigen | ✅ Server-Host-Status (CPU, RAM, Disk, Temp) im Drucker-Tab |
| #990 | 👍7 | **Input Shaper UI** — Input Shaping grafisch konfigurieren statt SSH | ✅ Input Shaper Konfiguration im Drucker-Tab (Klipper) |
| #2376 | 👍5 | **User-organizable temperature tabs** — Temperaturen selbst gruppieren | ✅ Konfigurierbare Sensor-Gruppen im Drucker-Tab |

### Aus OctoPrint Issues (Drucker-Kontrolle)

| Issue | Reactions | Idee | Für FlipsiForge |
|-------|-----------|------|-----------------|
| #937 | 👍16 | **RTSP IP Camera Support** — nicht nur USB-Webcam sondern IP-Kameras (rtsp://) | ✅ RTSP-Kamera-Support (neben USB-Webcam). Viele haben IP-Kameras |
| #452 | 👍9 | **Timelapse without printing** — Timelapse auch manuell starten (nicht nur bei Druck) | ✅ Manueller Timelapse-Start (Server-Feature) |
| #1525 | 👍7 | **Temperature Logger** — Temperatur-Historie aufzeichnen | ✅ Temperature Curves bereits geplant — bestätigt! |
| #1526 | 👍6 | **Render abort on cancel** — Timelapse-Render abbrechen bei Druck-Abbruch | ✅ Timelapse-Render-Abbruch |

### Aus Fluidd Issues (Drucker-UI)

| Issue | Reactions | Idee | Für FlipsiForge |
|-------|-----------|------|-----------------|
| #292 | 👍6 | **Farm Status Display** — alle Drucker auf einer Übersichtsseite | ✅ Multi-Drucker Dashboard bereits geplant — bestätigt! |
| #1773 | 👍3 | **Maintenance Schedule** — Wartungsplan (HEPA-Filter, Carbon, Düsenwechsel) | ✅ Wartungs-Tracker pro Drucker: Filter/Nozzle/Belt-Austausch mit Erinnerung |
| #249 | 👍6 | **Palette 2 Integration** — Multi-Color-System anbinden | ⚠️ Niedrige Priorität — Nischen-Hardware |
| #753 | 👍5 | **Drucker als Tabs** — Drucker als Tab-Reiter oben | ✅ Multi-Drucker als Tabs ODER Side-by-Side (User-Wahl) |

### Aus Manyfold Issues (Datei-Manager)

| Issue | Reactions | Idee | Für FlipsiForge |
|-------|-----------|------|-----------------|
| #4530 | 👍13 | **Printables Sync** — Modelle von Printables automatisch synchronisieren | ✅ Printables API für Model-Repository + Auto-Sync Option |
| #4531 | 👍5 | **MakerWorld Sync** — dasselbe für MakerWorld | ✅ MakerWorld API für Model-Repository + Auto-Sync |
| #456 | 👍6 | **STLs in ZIP anzeigen** — Modelle in ZIP-Archiven durchsuchen ohne zu entpacken | ✅ ZIP-Archiv-Scan (Datei-Manager durchsucht auch ZIPs) |
| #2214 | 👍4 | **OpenSCAD Customizer** — OpenSCAD .scad Dateien mit parametrischen Werten anpassen | ✅ OpenSCAD-Integration: .scad Dateien erkannt, Parameter-Editor, Customizer |
| #764 | 👍4 | **Index all files in model directory** — alle Dateien indexieren | ✅ Auto-Scan bereits geplant — bestätigt! |
| #4254 | 👍3 | **Commenting** — Kommentare/Notizen an Modellen | ✅ Datei-Tags + Notizen pro Datei |

### Aus KlipperScreen Issues (Touch-UI)

| Issue | Reactions | Idee | Für FlipsiForge |
|-------|-----------|------|-----------------|
| #1264 | 👍9 | **Queue-Verwaltung** — Druck-Queue starten/stoppen | ✅ Druck-Queue bereits geplant (mit Bestätigung) — bestätigt! |
| #1554 | 👍5 | **Spoolman-Panel beim Filament-Load** — Spulen-Auswahl beim Laden | ✅ Beim "Load Filament" Macro → Filament-Auswahl aus Filament-Tab |
| #1097 | 👍3 | **QR-Code Scan Button** — QR scannen im UI | ✅ QR-Code-Scanner in App (Webcam des PCs/Handy) |
| #822 | 👍2 | **Running as desktop app** — jemand WILL es als Desktop-App | ✅ FlipsiForge IST eine Desktop-App — genau das was sie wollen! |

### Aus OrcaSlicer Issues (Slicer)

| Issue | Reactions | Idee | Für FlipsiForge |
|-------|-----------|------|-----------------|
| #6866 | 👍32 | **Connect to printer over USB** — direkter USB-Drucker-Zugriff | ✅ Marlin USB-serial Support bereits geplant |
| #7106 | 👍37 | **Different filament for walls/infill** — verschiedene Filamente pro Strukturbereich | ⚠️ Slicer-Feature, nicht unsere Zuständigkeit — aber Slicer-Profile-Konfiguration in FlipsiForge könnte das unterstützen |

## Neue Features aus Issue-Analyse (zusätzlich zu bisherigem Konzept)

1. **NFC-Tag Support (OpenPrintTag)** — neben QR-Codes. Prusa hat offenen NFC-Standard angekündigt. Spoolman #776 (28👍). NFC-Tags lesen UND schreiben.

2. **Bambu Lab Drucker-Support** — neben Klipper/Marlin. Spoolman #217 (27👍). Bambu Lab hat eigene API. Viele Bambu-User in der Community.

3. **Material-Datenbank mit Auto-Fill** — Standard-Dichten automatisch eintragen (PLA=1.24, PETG=1.27, TPU=1.21, etc.). Spoolman #69 (18👍). Niemand kennt die Dichte seiner Spule.

4. **Filament-Bild pro Spule** — Shop-Foto oder eigenes Foto. Spoolman #552 (11👍). Zur schnellen Erkennung.

5. **RTSP IP-Kamera Support** — nicht nur USB-Webcam sondern auch IP-Kameras (rtsp://). OctoPrint #937 (16👍). Viele nutzen Überwachungskameras.

6. **Host-System-Status** — CPU/RAM/Disk/Temperatur des Server-Hosts im Dashboard. Mainsail #1648 (20👍).

7. **Wartungs-Tracker pro Drucker** — HEPA-Filter, Carbon-Filter, Düse, Riemen — Austausch-Intervall mit Erinnerung. Fluidd #1773 (3👍).

8. **ZIP-Archiv-Scan** — STLs in ZIPs durchsuchen ohne zu entpacken. Manyfold #456 (6👍).

9. **OpenSCAD-Integration** — .scad Dateien erkennen, Parameter-Editor, Customizer. Manyfold #2214 (4👍). Parametrische Modelle anpassen ohne Coding.

10. **Fuzzy Search** — ungenaue Suche toleriert Tippfehler. Spoolman #356 (10👍). Über den gesamten Datei-Manager.

11. **Datei-Kommentare/Notizen** — Notizen/Notizen an Modellen. Manyfold #4254 (3👍).

12. **Printables/MakerWorld Auto-Sync** — Modelle automatisch synchronisieren wenn neue erscheinen. Manyfold #4530 (13👍) + #4531 (5👍). Blocked in Manyfold weil Printables keine öffentliche API hat — wir prüfen das zum Startzeitpunkt.

13. **Manueller Timelapse-Start** — Timelapse auch ohne laufenden Druck starten. OctoPrint #452 (9👍).

14. **Filament-Auswahl beim Load-Macro** — beim "Load Filament" Macro direkt Spule aus Filament-DB wählen. KlipperScreen #1554 (5👍).

15. **Input Shaper UI** — grafische Input-Shaping-Konfiguration statt SSH. Mainsail #990 (7👍).