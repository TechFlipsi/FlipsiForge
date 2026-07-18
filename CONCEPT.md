# FlipsiForge — Project Concept (18.07.2026)

## Ursprungsidee (Sir's Beschreibung)

Ein PC-Programm passend zur TechFlipsi Homepage. Schwerpunkt: 3D-Drucker-Management mit drei Hauptbereichen:

1. **Datei-Manager** — Selbstständige Suche über kompletten PC und alle Laufwerke nach 3D-Druck-Dateien (STL etc., alle Formate). Vorschaubild für jede Datei. Umschaltbar zwischen Raster-Ansicht und Listen-Ansicht.
2. **Drucker-Verwaltung** — 3D-Drucker einbinden und direkt verwalten (Druck starten, hochladen etc.). Anzeige von Drucker-Daten: Temperaturen, Druck-Status, Fortschrittsanzeige.
3. **Filament-Verwaltung** — Eigenes Spoolman-Äquivalent. Spulen-Inventar, Verbrauch, Bestand.

## Sir's Entscheidungen (18.07.2026)

| # | Frage | Antwort |
|---|-------|---------|
| 1 | Framework | **Avalonia UI (.NET 10)** — neuer TechFlipsi Software-Standard. Windows + Linux. **Portable Version** wird ebenfalls neuer Standard für alle TechFlipsi Apps! |
| 2 | Drucker-Protokoll | **Klipper/Moonraker + Marlin** (USB-serial). Klipper ist der neue Standard im 3D-Druck, aber Marlin-Zugang für breitere Community. |
| 3 | Plattform | **Windows + Linux** (siehe #1) |
| 4 | Packaging | **Installer + Portable** (siehe #1) — neuer Standard! |
| 5 | Cloud-Sync | **Optional, Default = Lokal.** Nutzer können auf Wunsch Cloud einbinden. **Nextcloud = Priorität 1** (Unabhängigkeit von großen Anbietern). Auch Google Drive, OneDrive, Dropbox. Use-Cases: Einstellungen sync (Software auf mehreren PCs ohne Neu-Setup), Filament-Stand auf allen PCs gleich, 3D-Druck-Dateien an einem Ort ablegen. |
| 6 | Filament-System | **Eigenes System** (nicht Spoolman). Anpassbar auf Community-Wünsche. Mehr Flexibilität als vorgegebenes Spoolman. |
| 7 | Community | **Öffentlich** — Projekt lebt von der 3D-Druck-Community. Issues/PRs willkommen. |
| 8 | Name | **FlipsiForge** — bestätigt. |

## J.A.R.V.I.S. Funktions-Ideen (zur Diskussion)

### Datei-Manager
- **Watch-Folders**: Ordner markieren die überwacht werden → neue Dateien automatisch im Index
- **Duplikat-Erkennung**: Selbe STL unter verschiedenen Namen → Warnung
- **Datei-Tags**: Benutzerdefinierte Tags (z.B. "Ersatzteil", "Geschenk", "Prototyp")
- **G-code Preview**: Layer-für-Layer Vorschau für .gcode Dateien (nicht nur STL)
- **3MF Support**: 3MF Dateien enthalten mehrere Modelle + Metadaten → diese extrahieren und anzeigen
- **Sortierung nach Drucker-Eignung**: "Passt auf Snapmaker U1 (235×235×275mm)" vs "Passt auf Neptune 4 Pro"
- **Bulk-Aktionen**: Mehrere Dateien markieren → an Drucker senden / als Projekt gruppieren / exportieren

### Drucker-Verwaltung
- **Multi-Drucker Dashboard**: Alle Drucker gleichzeitig im Überblick (Snapmaker U1 + Neptune 4 Pro Side-by-Side)
- **Webcam-Preview**: Live-Kamera-Feed vom Drucker (Moonraker camera endpoint)
- **Druck-Historie**: Vergangene Drucke mit Dauer, Filament-Verbrauch, Erfolg/Misserfolg
- **Temperature Curves**: Live-Graph für Hotend/Bed Temperatur über Zeit
- **Benachrichtigungen**: Desktop-Notification bei Druckende, Fehler, Temperatur-Anomalie
- **G-Code Terminal**: Raw-Konsole für manuelle Befehle
- **Macro-Buttons**: Häufige Makros als konfigurierbare Buttons (z.B. "Load Filament T0", "Bed Mesh Calibrate")
- **Firmware-Info**: Klipper-Version, MCU-Info, Input Shaper Daten anzeigen
- **Marlin-Support**: USB-serial Verbindung für Nicht-Klipper-Drucker (Arduino-basiert)

### Filament-Verwaltung
- **Spool-QR-Code**: QR-Code pro Spule generieren → mit Handy scannen → sofort im System gefunden
- **Verbrauchs-Vorhersage**: "Bei aktuellem Verbrauch reicht Spule ~14 Tage"
- **Filament-Empfehlung**: Wenn Datei zum Drucken ausgewählt → "Du hast 3 passende Spulen (PLA Schwarz, PLA Grau, PETG Schwarz)"
- **Preis-Tracking**: Was wurde für welche Spule bezahlt → Kosten-pro-Druck Berechnung
- **Trocknungs-Timer**: Spule als "im Trockner" markieren mit Countdown
- **Eigenes System**: Vollständig selbst gebaut, anpassbar auf Community-Feedback

### Übergreifend
- **🔥 Druck-Kosten-Rechner** (Sir bestätigt: MUSS rein!) — Filament-Gewicht × Preis/gram + Stromkosten (Drucker-Watt × Dauer × Strompreis) + Verschleiß = Gesamtkosten
- **Cloud-Sync** (optional): Nextcloud (Priorität 1), Google Drive, OneDrive, Dropbox. Sync von Einstellungen + Filament-Stand + Datei-Ablage. Default = lokal ohne Cloud.
- **Projekt-Gruppen**: Dateien + Filament + Drucker zu einem "Projekt" zusammenfassen
- **Export/Backup**: Gesamte Datenbank exportieren (JSON/CSV)
- **Dark/Light Theme**: Dark Void + Ember als Default (TechFlipsi-Style)
- **13 Sprachen i18n**: Konsistent mit FlipsiColor/FlipsiSort

## Tech Stack (Final)

| Component | Choice | Reason |
|-----------|--------|--------|
| Framework | Avalonia UI 12 (.NET 10) | Neuer TechFlipsi Software-Standard, cross-platform |
| Language | C# 13 | Performant, bewährt in FlipsiColor.Avalonia |
| File scanning | .NET filesystem watchers + background indexing | Non-blocking, incremental |
| STL rendering | OpenTK / Silk.NET for 3D preview thumbnails | Hardware-accelerated preview |
| Printer protocol (Klipper) | Moonraker REST API + WebSocket | Standard für Klipper-Drucker |
| Printer protocol (Marlin) | USB-serial (System.IO.Ports) | Für Arduino-based Drucker |
| Cloud-Sync | Nextcloud WebDAV (P1) + Google Drive/OneDrive/Dropbox APIs | Optional, default lokal |
| Local storage | SQLite | Embedded, keine Server-Abhängigkeit |
| i18n | JSON-based localization (13 Sprachen) | Konsistent mit FlipsiColor/FlipsiSort |
| Packaging | Installer (.exe + .deb) + Portable (.zip) | Neuer TechFlipsi-Standard |
| License | GPL-3.0 | Konsistent mit allen TechFlipsi-Projekten |

## Roadmap (Planned)

| Phase | Scope |
|-------|-------|
| v0.1.0 | File scanner + grid/list view + STL thumbnails |
| v0.2.0 | Printer tab (Moonraker + Marlin, live data, basic controls) |
| v0.3.0 | Filament inventory tracking (eigenes System) |
| v0.4.0 | Druck-Kosten-Rechner + Cross-tab integration |
| v0.5.0 | Cloud-Sync (Nextcloud P1) + Settings + Multi-PC |
| v0.6.0 | Multi-Drucker Dashboard + Webcam + Notifications |
| v0.7.0 | i18n (13 languages) |
| v0.8.0 | Cloud-Sync Erweiterung (Google Drive, OneDrive, Dropbox) |
| v1.0.0 | Installer (Windows .exe + Linux .deb) + Portable (.zip) |

## TechFlipsi Ecosystem Integration

- **Design language**: Dark Void + Ember theme (matching techflipsi.kirchweger.de)
- **Branding**: TechFlipsi family product
- **Software-Standard**: Avalonia UI + Installer/Portable + GPL-3.0 + 13 Sprachen i18n
- **Website cross-link**: Will be featured on techflipsi.kirchweger.de/geraete.html
- **Community**: Öffentlich, Issues/PRs willkommen (wie FlipsiColor)