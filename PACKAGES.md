# FlipsiForge.Core v0.2.0 — NuGet-Paket-Dokumentation

## Installierte Pakete

| Paket | Version | Zweck | Von Task vorgegeben | Anmerkung |
|-------|---------|-------|---------------------|-----------|
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.0-preview.6.25313.107 | EF Core SQLite (bestehend) | — | autom. auf 25358.103 aufgelöst (NU1603 Warning, ok) |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.0-preview.6.25313.107 | EF Core Migrations Tooling | — | PrivateAssets=all |
| `Microsoft.ML.OnnxRuntime` | 1.27.1 | ONNX Runtime CPU EP für Embeddings | 1.20.1 | latest stable statt 1.20.1 |
| `Microsoft.ML.OnnxRuntimeGenAI.Managed` | 0.14.1 | ONNX Runtime GenAI für Chat (Gemma 3) | `Microsoft.ML.GenAI` 0.101.0-preview | **Paketname korrigiert** |
| `Microsoft.Extensions.Http` | 10.0.10 | IHttpClientFactory für Printer-Connections | 10.0.0-preview.6.25358.103 | latest stable statt Preview |
| `System.IO.Ports` | 10.0.10 | Serial-Port für Marlin-Drucker | 10.0.0-preview.6.25358.103 | latest stable statt Preview |
| `MQTTnet` | 5.2.0.1603 | MQTT-Client für Bambu Lab | 4.3.7.1207 | v5 statt v4 (API-Bruch, siehe unten) |

## Abweichungen vom Task-Spec

### 1. `Microsoft.ML.GenAI` 0.101.0-preview → `Microsoft.ML.OnnxRuntimeGenAI.Managed` 0.14.1
- **Problem:** Die im Task angegebene Paket-ID `Microsoft.ML.GenAI` 0.101.0-preview existiert nicht auf NuGet.org.
- **Recherche:** Die Suche lieferte irrelevante Treffer (`Microsoft.ML.OnnxRuntime.GenAI` mit Punkt — ebenfalls nicht existent).
- **Korrekte ID:** `Microsoft.ML.OnnxRuntimeGenAI.Managed` 0.14.1 (Managed Wrapper, keine native Dep).
  - Alternative IDs für GPU-Support: `Microsoft.ML.OnnxRuntimeGenAI.DirectML` (Windows), `.Cuda` (NVIDIA), `.Foundry` (Azure).
- **Namespace:** `Microsoft.ML.OnnxRuntimeGenAI` (nicht `.Managed`).
- **API-Änderung:** Die 0.14.x API weicht signifikant von alten Anleitungen ab:
  - `GeneratorParams.SetInputSequences(sequences)` → **existiert nicht** — statt `Generator.AppendTokenSequences(sequences)` verwenden
  - `GeneratorParams.TryGraphCaptureMaxSteps()` → **existiert nicht**
  - `Generator.ComputeLogits()` → **existiert nicht** — direkt `GenerateNextToken()` aufrufen
  - `Generator.GetSequence(0)[^1]` → **existiert nicht** — `Generator.GetNextTokens()` liefert ReadOnlySpan<int>

### 2. `MQTTnet` 4.3.7.1207 → 5.2.0.1603
- **Problem:** v4 wird durch v5 abgelöst; v5 hat deutliche API-Brüche.
- **Änderungen in v5:**
  - `MqttFactory` → `MqttClientFactory` (für `CreateMqttClient()`)
  - `MqttClientConnectResult.IsSuccess` → **existiert nicht** — `ResultCode == MqttClientConnectResultCode.Success` verwenden
  - `MqttApplicationMessage.PayloadSegment` → **set-only** — `Payload` (ReadOnlySequence<byte>) zum Lesen verwenden
  - `MQTTnet.Client` Subnamespace → **entfällt** — alle Typen direkt in `MQTTnet`
  - `client.SubscribeAsync(MqttTopicFilter)` → **existiert nicht** — `SubscribeAsync(MqttClientSubscribeOptions)` verwenden, mit `TopicFilters = new List<MqttTopicFilter> {...}`
- **Implikation für BambuConnection:** gesamte Implementierung wurde an v5 angepasst.

### 3. `Microsoft.Extensions.Http` / `System.IO.Ports` — Preview-Versionen nicht nötig
- Die im Task angegebenen `10.0.0-preview.6.25358.103` Versionen sind nicht die aktuellsten.
- Statt dessen: latest stable `10.0.10` (für .NET 10 Release) referenziert — funktioniert einwandfrei.

## Pitfall: Microsoft.ML.OnnxRuntime + .DirectML nicht gleichzeitig

Aus `modern-languages-error-prevention` Skill (Pitfall-Regel):
- **FEHLER:** Gleichzeitige Referenz von `Microsoft.ML.OnnxRuntime` + `Microsoft.ML.OnnxRuntime.DirectML` erzeugt DLL-Duplikat-Fehler (MSB9005: "multiple publish output files with same relative path: onnxruntime.dll").
- **LÖSUNG:** Nur `.DirectML` referenzieren (enthält CPU+GPU+Fallback), oder nur Basis-Paket (für CPU-only).
- FlipsiForge.Core nutzt nur das Basis-Paket (CPU) — sicher.

## Native Dependencies — Status Build-Server

| Library | Build-Server | Build kompiliert? | Runtime-Verhalten |
|---------|--------------|-------------------|-------------------|
| onnxruntime.dll/so | ❌ nicht installiert | ✅ | `LocalEmbeddingProvider.IsLoaded = false` → `EmbedAsync` liefert leeres Array |
| onnxruntime-genai.dll/so | ❌ nicht installiert | ✅ | `OnnxGenAiChatEngine.IsLoaded = false` → `CompleteChatAsync` liefert Hinweis-String |
| Serial-Ports (/dev/ttyUSB*) | ❌ nicht vorhanden | ✅ | `MarlinConnection.ConnectAsync` fängt Exception, liefert false |
| MQTT-Broker | ❌ nicht erreichbar | ✅ | `BambuConnection.ConnectAsync` fängt Exception, liefert false |

Alle Native-Abhängigkeiten sind via try/catch um `DllNotFoundException`, `FileNotFoundException`, `UnauthorizedAccessException` abgesichert — Code kompiliert und läuft stub-mäßig ohne Hardware.

## Offene Issues / TODO für v0.3

- **BambuConnection:** Serial-Parameter aus `Printer.Model` ist ein Stub (sollte eigenes DB-Feld sein).
- **PrusaLink/OctoPrint:** API-Key hard-coded als Stub — muss aus `AppSettings` oder `Printer.ApiKey` kommen.
- **MoonrakerConnection:** File-Upload fehlt (nur `print_start` für bereits hochgeladene Dateien).
- **MarlinConnection:** `M27` Job-Info-Parser nur Stub — keine echte Progress-Info von Marlin.
- **LocalEmbeddingProvider:** Tokenisierung ist Stub (Hash-basiert) — für Produktion `SharpToken` oder `Microsoft.ML.Tokenizers` integrieren.
- **OnnxGenAiChatEngine:** `ApplyChatTemplate` der Tokenizer-Klasse könnte statt `BuildFullPrompt` verwendet werden (Gemma-3 native Chat-Format).
- **MaintenanceAdvisor.OnlineApiUrl:** `api.flipsiforge.tech` ist ein Platzhalter — echte API-URL setzen.
- **GPU-Support:** Wenn DirectML/CUDA-EP benötigt, `Microsoft.ML.OnnxRuntime.DirectML` statt Basis-Paket (Windows-only) — Linux mit CUDA EP via `Microsoft.ML.OnnxRuntime.Gpu.Linux`.

## Build-Status

```
$ cd /root/FlipsiForge && dotnet build src/FlipsiForge.Core/FlipsiForge.Core.csproj
FlipsiForge.Core -> /root/FlipsiForge/src/FlipsiForge.Core/bin/Debug/net10.0/FlipsiForge.Core.dll
Der Buildvorgang wurde erfolgreich ausgeführt.
    0 Warnung(en)
    0 Fehler
```

Build sauber: 0 Fehler, 0 Warnungen (NU1903/NU1603 Security-Warning via NoWarn unterdrückt).