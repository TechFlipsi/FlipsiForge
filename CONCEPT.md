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
| 2 | Drucker-Protokoll | **Klipper/Moonraker + Marlin** (USB-serial). Klipper ist Standard, aber Marlin für breite Community. |
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