# 🤝 Contributing Guide

Danke dass du bei FlipsiForge mitmachen möchtest! Dieser Guide erklärt wie du beitragen kannst — egal ob Code, Übersetzungen, Tests oder Doku.

## Projektübersicht

FlipsiForge ist eine **cross-platform 3D-Druck Management Software** (.NET 10 / Avalonia UI). Sie ist Teil des [TechFlipsi](https://github.com/TechFlipsi) Ökosystems und steht unter **GPL-3.0** Lizenz.

### Tech Stack

| Technologie | Verwendung |
|-------------|-----------|
| .NET 10 | Laufzeit |
| Avalonia UI 12 | Cross-Platform UI (Windows + Linux) |
| ASP.NET Core | Server (Full + Lite Modus) |
| CommunityToolkit.Mvvm 8.4 | MVVM-Pattern |
| ONNX Runtime | KI lokal (keine Cloud) |
| Serilog | Logging |
| SkiaSharp | G-Code-Visualisierung |
| Entity Framework Core | Datenbank (Server-Modus) |

### Projektstruktur

```
FlipsiForge/
├── src/
│   ├── FlipsiForge.Core/          # Gemeinsamer Kern (Models, Services, Drucker-Protokolle)
│   ├── FlipsiForge.Desktop/       # Avalonia UI Desktop-App
│   ├── FlipsiForge.Server/        # ASP.NET Core Server (Full/Lite)
│   └── FlipsiForge.MockMoonraker/ # Mock-Server für Tests/Demos
├── CONCEPT.md                     # Gesamtkonzept
├── IMPLEMENTATION.md              # Implementierungs-Details
├── TESTING.md                     # Test-Guide für Tester
└── CONTRIBUTING.md                # Diese Datei
```

## Wie fängst du an?

### Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git
- Ein Code-Editor (VS Code, Rider, Visual Studio)
- Optional: [Avalonia UI Extension](https://avaloniaui.net/) für deinen Editor

### Setup

```bash
git clone https://github.com/TechFlipsi/FlipsiForge.git
cd FlipsiForge
dotnet build
```

### Mock-Drucker für Tests

```bash
# Simulierten Drucker starten
dotnet run --project src/FlipsiForge.MockMoonraker -- --scenario printing
```

Siehe [TESTING.md](TESTING.md) für alle Szenarien und Details.

## Wo kannst du mitmachen?

### 🎨 UI/UX (Avalonia / AXAML)

**Was:** Views erstellen, Design verbessern, Dark Mode, Layout, Animationen

**Wo:**
- `src/FlipsiForge.Desktop/Views/` — AXAML Views
- `src/FlipsiForge.Desktop/ViewModels/` — ViewModels (MVVM)
- Theme: Dark Void `#050507` + Ember `#ff6600`

**Beispiel — neuer View:**
```axaml
<UserControl xmlns="https://github.com/avaloniaui">
  <StackPanel Margin="16">
    <TextBlock Text="Mein neuer View" FontSize="20" FontWeight="Bold"/>
  </StackPanel>
</UserControl>
```

### 💻 C# Backend / Core-Logik

**Was:** Core-Services, Drucker-Protokolle, Server-API, KI-Integration

**Wo:**
- `src/FlipsiForge.Core/Services/` — Geschäftslogik
- `src/FlipsiForge.Core/Services/Printing/` — Drucker-Protokolle
- `src/FlipsiForge.Server/` — API-Controller, Services

**Neues Drucker-Protokoll hinzufügen:**
1. Erstelle Klasse in `Core/Services/Printing/` die `IPrinterConnection` implementiert
2. Registriere in `PrinterConnectionManager`
3. Füge Protokoll zum `PrinterProtocol` Enum in `Core/Models/Enums.cs` hinzu
4. Teste mit Mock oder echtem Drucker

### 🔌 Drucker-Integration

**Was:** Neue Drucker-Protokolle (RepRapFirmware, Duet, Klipper-Extensions), Verbesserung bestehender

**Bestehende Protokolle:**
- `MoonrakerConnection` — Klipper/Moonraker (HTTP + WebSocket)
- `BambuConnection` — Bambu Lab (MQTT)
- `PrusaLinkConnection` — Prusa (HTTP)
- `OctoPrintConnection` — OctoPrint (HTTP)
- `MarlinConnection` — Marlin (Serial)

### 🌐 Übersetzungen (i18n)

**Was:** JSON-Sprachdateien erweitern und übersetzen

**Wie:** Siehe i18n-Ordner. Jede Sprache hat ein JSON-File. Keine Code-Erfahrung nötig — nur Muttersprache!

**Geplante Sprachen:** DE, EN, FR, ES, IT, PL, NL, RU, TR, AR, PT, ZH, JA

**Regel:** Wenn die Software auf Deutsch eingestellt ist, sollen **keine englischen Wörter** sichtbar sein. Gleiches gilt für jede andere Sprache.

### 🧪 Testing & Mock-Server

**Was:** Szenarien erweitern, Bug-Reports, Edge-Case-Tests

**Wo:** `src/FlipsiForge.MockMoonraker/` — Mock-Server

**Neues Szenario hinzufügen:**
```csharp
case "heating":
    _state = "startup";
    _hotendTemp = 150m;
    _targetHotendTemp = 210m;
    break;
```

### 📖 Dokumentation

**Was:** README, Setup-Guides, API-Doku, Benutzerhandbuch

**Wo:** Root-Verzeichnis und `docs/` (wenn vorhanden)

## Contribution Workflow

### 1. Issue finden oder erstellen

Schau in den [offenen Issues](https://github.com/TechFlipsi/FlipsiForge/issues) oder erstelle ein neues für deine Idee.

### 2. Fork & Branch

```bash
# Fork auf GitHub erstellen, dann:
git clone https://github.com/<dein-username>/FlipsiForge.git
cd FlipsiForge
git remote add upstream https://github.com/TechFlipsi/FlipsiForge.git

# Branch erstellen
git checkout -b feat/mein-feature
# oder
git checkout -b fix/mein-bugfix
```

### 3. Code

- Halte dich an den bestehenden Code-Stil
- Verwende `CommunityToolkit.Mvvm` für ViewModels (`[ObservableProperty]`, `[RelayCommand]`)
- Alle öffentlichen Methoden brauchen XML-Doku-Kommentare (`/// <summary>`)
- Fehler defensiv behandeln — kein `throw` in UI-Code

### 4. Testen

```bash
# Bauen
dotnet build

# Mock-Server starten
dotnet run --project src/FlipsiForge.MockMoonraker -- --scenario printing

# Desktop-App testen
dotnet run --project src/FlipsiForge.Desktop
```

### 5. Commit

```bash
git add -A
git commit -m "feat: kurze Beschreibung

Optional: ausführlichere Beschreibung"
```

**Commit-Format:**
- `feat:` — Neues Feature
- `fix:` — Bug-Fix
- `docs:` — Dokumentation
- `refactor:` — Refactoring
- `test:` — Tests
- `chore:` — Sonstiges

### 6. Pull Request

```bash
git push origin feat/mein-feature
```

Dann auf GitHub einen Pull Request erstellen mit:
- **Titel:** Kurze Beschreibung
- **Body:** Was hast du gemacht? Warum? Wie testen?
- **Issue verlinken:** `Fixes #123` oder `Relates to #456`

## Code-Stil

### C# Konventionen

```csharp
// ✅ Richtig
public sealed class MeinService
{
    private readonly ILogger<MeinService> _logger;

    /// <summary>
    /// Macht etwas Wichtiges.
    /// </summary>
    public async Task<bool> MacheEtwasAsync(string parameter)
    {
        // ...
    }
}

// ❌ Falsch — keine Doku, public field
public class meinService {
    public string Value;
}
```

### Regeln

- **Nullable** ist enabled — behandele `null` explizit
- **Async** für alle I/O-Operationen (HTTP, DB, File)
- **Sealed** für Klassen die nicht vererbt werden sollen
- **XML-Doku** für alle öffentlichen Members
- **Defensive Fehlerbehandlung** — keine ungecatchten Exceptions in UI-Code
- **Keine Magic Numbers** — Konstanten oder Enums verwenden

## Lizenz

Alle Beiträge stehen unter **GPL-3.0**. Mit dem Einreichen eines PR stimmst du zu dass deine Änderungen unter dieser Lizenz veröffentlicht werden.

## Verhaltenskodex

- Sei respektvoll und freundlich
- Keine Diskriminierung — egal wegen was
- Hilf Neulingen — jeder hat mal angefangen
- Kritik ist ok, aber sachlich und konstruktiv
- Frag wenn du etwas nicht verstehst — niemand beisst

## Kontakt

- **Discord:** [discord.gg/zHPhQ7EaqH](https://discord.gg/zHPhQ7EaqH)
- **Issues:** [GitHub Issues](https://github.com/TechFlipsi/FlipsiForge/issues)
- **Maintainer:** [TechFlipsi](https://github.com/TechFlipsi)

---

Danke für deinen Beitrag! Jeder hilft, FlipsiForge besser zu machen. 🤝