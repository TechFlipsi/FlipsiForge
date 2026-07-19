# 🧪 Testing Guide

Danke dass du FlipsiForge testen möchtest! Dieser Guide erklärt wie du loslegst — **mit oder ohne echten 3D-Drucker**.

## Schnellstart (ohne echten Drucker)

Du brauchst keinen echten Drucker um FlipsiForge zu testen. Wir haben einen eingebauten Mock-Server der einen Klipper/Moonraker-Drucker simuliert.

### Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git
- Optional: Ein echter 3D-Drucker (Moonraker/Klipper, Bambu Lab, PrusaLink, OctoPrint)

### Schritt 1: Repo klonen

```bash
git clone https://github.com/TechFlipsi/FlipsiForge.git
cd FlipsiForge
```

### Schritt 2: Mock-Drucker starten

```bash
dotnet run --project src/FlipsiForge.MockMoonraker -- --scenario printing
```

Das startet einen simulierten Drucker auf `http://localhost:7125`. Der Mock antwortet wie ein echter Moonraker — FlipsiForge kann nicht unterscheiden.

### Schritt 3: FlipsiForge starten

```bash
dotnet run --project src/FlipsiForge.Desktop
```

In FlipsiForge: Drucker hinzufügen → URL: `http://localhost:7125` → Protokoll: Klipper/Moonraker.

## Mock-Server Szenarien

| Szenario | Befehl | Was passiert |
|----------|--------|-------------|
| Demo | `--scenario demo` | Drucker ready, idle, Raumtemperatur (25°C) |
| Druck läuft | `--scenario printing` | Druck bei 35%, Hotend 210°C, Bed 60°C, fortschreitend |
| Pausiert | `--scenario paused` | Druck pausiert bei 50%, Hotend 180°C |
| Fehler | `--scenario error` | Thermistor-Fehler, Temperaturen 0°C |
| Offline | `--scenario offline` | Drucker offline/shutdown |

```bash
# Beispiel: Fehler-Szenario auf Port 8080
dotnet run --project src/FlipsiForge.MockMoonraker -- --port 8080 --scenario error
```

## Mock-Server API

Der Mock-Server unterstützt folgende Endpoints:

| Endpoint | Methode | Beschreibung |
|----------|---------|-------------|
| `/` | GET | Health check |
| `/printer/info` | GET | Drucker-Status |
| `/printer/objects/query?extruder&heater_bed&print_stats` | GET | Temperaturen + Job-Info |
| `/server/files/list` | GET | Simulierte G-Code-Dateiliste |
| `/printer/print_start?filename=benchy.gcode` | POST | Druck starten |
| `/printer/print_pause` | POST | Druck pausieren |
| `/printer/print_resume` | POST | Druck fortsetzen |
| `/printer/print_cancel` | POST | Druck abbrechen |
| `/printer/gcode/script` | POST | G-Code-Befehl senden |
| `/websocket` | WS | WebSocket für Live-Updates (alle 2s) |

## Mit echtem Drucker testen

### Klipper/Moonraker

1. FlipsiForge starten
2. Drucker hinzufügen → IP:Port deines Druckers (z.B. `http://192.168.1.50:7125`)
3. Protokoll: Klipper/Moonraker
4. Verbinden → Status, Temperaturen und Druck-Fortschritt sollten erscheinen

### Bambu Lab

1. Drucker hinzufügen → IP deines Druckers
2. Protokoll: Bambu Lab
3. Access Code als API-Key eingeben

### PrusaLink / OctoPrint

1. Drucker hinzufügen → IP:Port
2. Protokoll: PrusaLink oder OctoPrint
3. API-Key eingeben (in OctoPrint unter Settings → API)

## Was solltest du testen?

### Grundfunktionen
- [ ] Drucker hinzufügen (Mock + echt)
- [ ] Drucker-Status wird korrekt angezeigt
- [ ] Temperaturen werden aktualisiert
- [ ] Druck starten/pausieren/fortsetzen/abbrechen
- [ ] Datei-Liste wird angezeigt
- [ ] WebSocket-Updates kommen live an

### Edge Cases
- [ ] Drucker offline → FlipsiForge zeigt "Offline" ohne Absturz
- [ ] Fehler-Szenario → Fehlermeldung wird angezeigt
- [ ] Verbindung trennen/wiederherstellen
- [ ] Mehrere Drucker gleichzeitig
- [ ] Sehr lange Dateinamen
- [ ] Unicode in Dateinamen

### UI/UX
- [ ] Dark Mode lesbar
- [ ] Fenster skalieren (klein/groß)
- [ ] Navigation zwischen Tabs
- [ ] Responsive Layout

## Bug-Report erstellen

Wenn du einen Bug findest, erstelle ein [Issue](https://github.com/TechFlipsi/FlipsiForge/issues/new) mit:

```markdown
## Bug: [Kurze Beschreibung]

**Szenario:** printing
**OS:** Windows 11 / Ubuntu 24.04 / ...
**Drucker:** Mock / Echter Drucker (Modell)

### Was passiert ist
[Beschreibung + ggf. Screenshot]

### Was ich erwartet hätte
[Beschreibung]

### Schritte zum Reproduzieren
1. ...
2. ...
3. ...
```

## Discord

Für schnelle Fragen und direkten Austausch: [discord.gg/zHPhQ7EaqH](https://discord.gg/zHPhQ7EaqH)

---

Danke fürs Testen! Jeder Bug-Report hilft uns weiter. 🧪