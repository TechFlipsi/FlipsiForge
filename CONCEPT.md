# FlipsiForge — Project Concept (18.07.2026)

## Ursprungsidee (Sir's Beschreibung)

Ein PC-Programm passend zur TechFlipsi Homepage. Schwerpunkt: 3D-Drucker-Management mit drei Hauptbereichen:

1. **Datei-Manager** — Selbstständige Suche über kompletten PC und alle Laufwerke nach 3D-Druck-Dateien (STL etc., alle Formate). Vorschaubild für jede Datei. Umschaltbar zwischen Raster-Ansicht und Listen-Ansicht.
2. **Drucker-Verwaltung** — 3D-Drucker einbinden und direkt verwalten (Druck starten, hochladen etc.). Anzeige von Drucker-Daten: Temperaturen, Druck-Status, Fortschrittsanzeige.
3. **Filament-Verwaltung** — Eigenes Spoolman-Äquivalent. Spulen-Inventar, Verbrauch, Bestand.

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

### Filament-Verwaltung
- **Spool-QR-Code**: QR-Code pro Spule generieren → mit Handy scannen → sofort im System gefunden
- **Verbrauchs-Vorhersage**: "Bei aktuellem Verbrauch reicht Spule ~14 Tage"
- **Filament-Empfehlung**: Wenn Datei zum Drucken ausgewählt → "Du hast 3 passende Spulen (PLA Schwarz, PLA Grau, PETG Schwarz)"
- **Preis-Tracking**: Was wurde für welche Spule bezahlt → Kosten-pro-Druck Berechnung
- **Trocknungs-Timer**: Spule als "im Trockner" markieren mit Countdown
- **Spoolman-Import**: Bestehende Spoolman-Daten importieren (falls bereits genutzt)

### Übergreifend
- **Druck-Kosten-Rechner**: Filament-Gewicht × Preis/gram + Stromkosten (Drucker-Watt × Dauer × Strompreis) + Verschleiß = Gesamtkosten
- **Projekt-Gruppen**: Dateien + Filament + Drucker zu einem "Projekt" zusammenfassen
- **Export/Backup**: Gesamte Datenbank exportieren (JSON/CSV)
- **Dark/Light Theme**: Dark Void + Ember als Default (TechFlipsi-Style)
- **13 Sprachen i18n**: Konsistent mit FlipsiColor/FlipsiSort

## Offene Fragen an Sir

1. **Technologie**: Avalonia UI (.NET 10, cross-platform) — wie bei FlipsiColor.Avalonia bewährt. Einverstanden, oder andere Präferenz?
2. **Drucker-Protokoll**: Nur Klipper/Moonraker (Ihre beiden Drucker), oder auch Marlin/RepRap (USB-serial)?
3. **Plattform**: Windows + Linux, oder nur Windows? (Ihr Stand-PC ist Windows-only, aber Linux-User in der Community gibt es)
4. **Installer vs. Portable**: Beides (wie FlipsiColor), oder nur eine Variante?
5. **Cloud-Sync**: Sollen Filament/Drucker-Daten zwischen PCs synchronisierbar sein (z.B. via Nextcloud), oder rein lokal?
6. **Spoolman-Integration**: Wollen Sie Spoolman direkt einbinden (Docker, API) falls OpenNept4une läuft, oder lieber eigenes System?
7. **Community**: Öffentlich (wie FlipsiColor, mit Issues/PRs), oder privat bis v1.0?
8. **Name**: "FlipsiForge" — gefällt der Name, oder etwas anderes?