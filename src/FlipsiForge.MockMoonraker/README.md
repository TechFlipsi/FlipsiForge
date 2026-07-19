# FlipsiForge Mock Moonraker

Mock-Server der die [Moonraker API](https://moonraker.readthedocs.io/) nachahmt — für Demos und Tests **ohne echten 3D-Drucker**.

## Verwendung

```bash
# Default (Demo-Modus, Port 7125)
dotnet run --project src/FlipsiForge.MockMoonraker

# Eigener Port + Szenario
dotnet run --project src/FlipsiForge.MockMoonraker -- --port 8080 --scenario printing
```

## Szenarien

| Szenario | Beschreibung |
|----------|-------------|
| `demo` | Drucker ready, idle, Raumtemperatur |
| `printing` | Druck läuft (35% Fortschritt, Hotend 210°C, Bed 60°C, fortschreitend) |
| `paused` | Druck pausiert bei 50% |
| `error` | Thermistor-Fehler, Temperaturen 0°C |
| `offline` | Drucker offline/shutdown |

## API Endpoints

| Endpoint | Methode | Beschreibung |
|----------|---------|-------------|
| `/` | GET | Health check → `{"result":"ok"}` |
| `/printer/info` | GET | Drucker-Status (state, version, hostname) |
| `/printer/objects/query?extruder&heater_bed&print_stats` | GET | Temperatur, Job-Info |
| `/server/files/list` | GET | Simulierte G-Code-Dateiliste |
| `/printer/print_start?filename=benchy.gcode` | POST | Druck starten |
| `/printer/print_pause` | POST | Druck pausieren |
| `/printer/print_resume` | POST | Druck fortsetzen |
| `/printer/print_cancel` | POST | Druck abbrechen |
| `/printer/gcode/script` | POST | G-Code-Befehl senden |
| `/websocket` | WS | WebSocket für Live-Updates (alle 2s) |

## WebSocket Events

Verbunden unter `ws://localhost:7125/websocket` — sendet alle 2 Sekunden:
- `webhooks` (state)
- `extruder` (temperature, target, power)
- `heater_bed` (temperature, target, power)
- `print_stats` (filename, progress, layer, duration)
- `virtual_sdcard` (progress, file_position)
- `display_status` (progress, message)

## Integration in FlipsiForge

In FlipsiForge Desktop → Drucker hinzufügen → URL: `http://localhost:7125` → Protokoll: Klipper/Moonraker.

Der Mock antwortet exakt wie ein echter Moonraker — FlipsiForge kann nicht unterscheiden.

## Keine Abhängigkeiten

Reines .NET 10, keine NuGet-Packages, keine Java, keine Docker nötig. Läuft überall wo .NET läuft.