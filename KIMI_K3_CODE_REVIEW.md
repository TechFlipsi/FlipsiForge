# Kimi K3 Code-Review: FlipsiForge
**Datum:** 27.07.2026
**Modell:** Kimi K3 (2.8T, Ollama Cloud)
**Scope:** 88 C#-Dateien, ~11.000 Zeilen
**Dauer:** 386 Sekunden

---

## 1. Bugs

### 🔴 Kritisch

**1.1 Culture-Bug bei Embedding-Serialisierung** — `FlipsiForgeDbContext.cs`, `OnModelCreating`
`string.Join(',', v)` und `float.Parse` laufen ohne `InvariantCulture`. Auf einem deutschen System wird `0.5f` zu `"0,5"` serialisiert — der gespeicherte String `"0,5,0,3"` wird beim Deserialisieren zu `[0, 5, 0, 3]` statt `[0.5, 0.3]`. **Alle KI-Embeddings werden auf de-DE-Systemen still korrumpiert**, die semantische Suche liefert Müll.
*Fix:* Serialisierung/Deserialisierung konsequent mit `CultureInfo.InvariantCulture` oder gleich als JSON-Array speichern.

**1.2 PrinterIds-JSON-Konversion wirft zur Laufzeit** — `FlipsiForgeDbContext.cs`, `OnModelCreating` (PrinterCluster)
`JsonSerializer.Serialize(v, (System.Type)null!)` wählt den Overload mit `inputType`-Parameter und übergibt `null` → `ArgumentNullException` beim ersten Speichern eines Clusters.
*Fix:* Den generischen Overload ohne Type-Parameter verwenden.

**1.3 `requireConfirmation` invertiert Rückgabewert** — `BambuConnection.SendGcodeAsync`, `MoonrakerConnection.SendGcodeAsync`, `PrusaLinkConnection.SendGcodeAsync`
`return ok && !requireConfirmation` — wenn der User eine Bestätigung verlangt (`requireConfirmation=true`), wird **immer `false` zurückgegeben, obwohl der Befehl erfolgreich gesendet wurde**.
*Fix:* Rückgabewert soll den Transport-Erfolg melden; der Bestätigungs-Status gehört in ein separates Ergebnis-Objekt.

**1.4 Marlin liest M27-Antwort nie** — `MarlinConnection.SendGcodeRawAsync` / `GetStatusAsync`
Die ReadLine-Schleife wartet nur auf Zeilen mit Prefix `"ok"` oder `"T:"`. Die eigentliche M27-Antwort wird übersprungen; zurückgegeben wird das abschließende `"ok"`. Folge: `GetStatusAsync` liefert bei verbundenem Drucker **immer `Printing`**.
*Fix:* Alle Antwortzeilen bis zum `"ok"` sammeln und die fachlich relevante Zeile auswerten.

**1.5 PrusaLink: Basic statt Digest-Auth** — `PrusaLinkConnection.ApplyAuth`
PrusaLink (MK4/XL, lokal) verlangt **HTTP Digest Authentication**, nicht Basic. Der aktuelle Header wird von echten Geräten mit 401 abgelehnt — die gesamte PrusaLink-Integration ist gegen echte Hardware funktionslos.
*Fix:* Digest-Auth über `HttpClientHandler.Credentials` mit vorkonfiguriertem `CredentialCache` implementieren.

**1.6 Bambu: MQTT-Credentials + TLS falsch** — `BambuConnection.ConnectAsync`
(a) Username wird als `"bblp:{serial}"` gesetzt — Bambu LAN erwartet als Username schlicht `"bblp"`. (b) Ohne abgeschwächte Zertifikatsvalidierung schlägt der TLS-Handshake fehl. Beides zusammen: **Verbindung zu echtem Bambu-Drucker im LAN-Modus unmöglich.**
*Fix:* Username korrigieren; TLS-Options mit expliziter Zertifikats-Akzeptanz für LAN-Szenario versehen.

**1.7 PrusaLink Job-Parsing greift auf falschen JSON-Pfad zu** — `PrusaLinkConnection.GetCurrentJobAsync`
`JsonHelper.GetString(json, "file_path", "name")` — `file_path` ist ein String, kein Objekt. `TryGetProperty` auf Nicht-Objekt wirft `InvalidOperationException` → catch → immer `null`.
*Fix:* Pfad korrigieren und `JsonHelper` robust machen.

**1.8 JsonHelper crasht bei Nicht-Objekt-Zwischenelementen** — `HttpPrinterConnectionBase.cs`, `JsonHelper`
`cur.TryGetProperty(key, out cur)` ohne vorherige `ValueKind == Object`-Prüfung. Sobald ein Zwischenelement String/Number/Array ist, fliegt `InvalidOperationException` statt `null`.
*Fix:* In allen drei Helpern vor `TryGetProperty` den `ValueKind` prüfen.

**1.9 OctoPrint: Pfade mit Unterordnern kaputt** — `OctoPrintConnection.SendGcodeAsync`
`Uri.EscapeDataString` auf den kompletten Pfad escaped auch `/` zu `%2F` — Dateien in OctoPrint-Unterordnern sind nicht mehr adressierbar.
*Fix:* Pfad segmentweise escapen und mit `/` wieder zusammensetzen.

**1.10 OctoPrint: Pause ist ein Toggle** — `OctoPrintConnection.PauseAsync`
`{"command":"pause"}` ohne `action` togglet bei OctoPrint zwischen Pause und Resume. Ein zweiter Pause-Aufruf **setzt den Druck versehentlich fort**.
*Fix:* Explizit `action: "pause"` senden.

**1.11 Fake-Streaming in der KI-Engine** — `OnnxGenAiChatEngine.StreamChatAsync`
Die Generierungs-Schleife sammelt alle Tokens in einer `Queue`, erst **nach Abschluss der kompletten Generierung** wird geyielded. Der Aufrufer sieht das erste Token erst, wenn alles fertig ist.
*Fix:* Echtes Streaming über `Channel<string>` (Producer = Generierungs-Task, Consumer = async iterator).

**1.12 MeanPool indexiert 3D-Tensor mit 2 Indizes** — `LocalEmbeddingProvider.MeanPool`
Der Output-Tensor hat Shape `[1, seq, dim]`, indexiert wird aber `tensor[s, d]` — bei einem 3-dimensionalen `Tensor<T>` führt das zu einer Exception, die vom äußeren catch geschluckt wird → **EmbedAsync liefert immer leeres Array**.
*Fix:* Korrekt mit drei Indizes arbeiten und über die Attention-Mask mitteln.

**1.13 FileScanner vergibt eigene IDs** — `Core/Services/Scanner/FileScanner.cs`
`sf.Id = counter++` setzt Primärschlüssel auf lokal hochgezählte Werte. EF interpretiert gesetzte IDs als existierende Datensätze → Update statt Insert oder PK-Konflikte.
*Fix:* `Id` unangetastet lassen (0) und der DB überlassen.

**1.14 Filter-Badges zählen immer 0** — `FileManagerViewModel.MatchesExt`
Verglichen wird `File.Extension` (vom Scanner als `"STL"` ohne Punkt gespeichert) gegen `".stl"` (mit Punkt) → nie gleich → alle Format-Badges zeigen 0.
*Fix:* Format normalisieren.

**1.15 Filter und Suche wirken nicht auf die Liste** — `FileManagerViewModel`
`ApplyFilter` setzt nur `SelectedFilter`, `SearchAsync` setzt nur `IsAiHit`-Badges — die `Files`-Collection wird weder gefiltert noch nach Score sortiert. Der User tippt eine Suche ein und sieht weiterhin alle Dateien.
*Fix:* Gefilterte/sortierte Projektion einführen.

**1.16 Shelly Energy-Einheit falsch** — `ShellyClient.SwitchGetStatusAsync`
Shelly liefert `aenergy.total` in **Wattstunden**, das Feld heißt aber `EnergyKWh`. Stromkosten-Rechnung um Faktor 1000 falsch.
*Fix:* Einheit korrigieren oder bei der Übernahme durch 1000 teilen.

**1.17 Graceful-Shutdown-Code ist ungenutzt / hartes Ausschalten** — `DruckWaechterViewModel.ToggleShellyAsync`
Der Toggle schaltet den Shelly direkt aus — `ShutdownPrinterAsync` wird **nirgends aufgerufen**. Keine Prüfung ob gerade ein Druck läuft. Die Auto-Aus-Logik aus der Config hat **keine Implementierung**.
*Fix:* Ausschalt-Pfad über `ShutdownPrinterAsync` führen, Druck-läuft-Prüfung davor, Background-Service implementieren oder Config entfernen.

**1.18 FarmOverview: Error-Zählung ohne Zeitbezug** — `FarmOverview.BuildAsync`
`ErrorPrinters` zählt Drucker mit **jemals** fehlgeschlagenen Jobs. Ein Drucker, der vor Monaten einmal fehlschlug, bleibt ewig "Error".
*Fix:* Error über "letzter Job des Druckers ist Failed" oder Zeitfenster definieren.

**1.19 Server-Scheduling verteilt alles auf einen Drucker** — `Server/Program.cs`
Die Schleife sucht pro Item `printers.FirstOrDefault(...)` ohne zu merken, welche Drucker schon belegt wurden → **alle Items landen auf demselben Drucker**. Der fertige `AutoSchedulerService` aus Core wird serverseitig gar nicht registriert.
*Fix:* `AutoSchedulerService` in DI registrieren und den Endpoint darauf delegieren.

**1.20 Cancellation wird geschluckt** — `AutoSchedulerService`
Pauschales `catch { return 0/false; }` fängt auch `OperationCanceledException` — Abbrüche werden ignoriert.
*Fix:* `OperationCanceledException` durchreichen.

**1.21 Cluster-Regeln werden ignoriert** — `AutoSchedulerService.GetAvailablePrintersAsync`
`ClusterType.Reserved`, `AutoSchedule=false` und `MaxActivePrinters` werden nirgends ausgewertet. Reservierte Cluster bekommen trotzdem automatisch Aufträge.
*Fix:* Beim Cluster-Filter diese Flags prüfen.

**1.22 Reschedule ignoriert Teilmengen und hinterlässt verwaiste Schedules** — `AutoSchedulerService.RescheduleFailedAsync**
(a) Der alte `FarmSchedule`-Eintrag wird nicht auf Failed/Skipped gesetzt. (b) `PrintedQuantity >= Quantity` wird beim Scheduling nie geprüft.
*Fix:* Alte Schedule-Einträge beim Fail/Reschedule schließen; Pending-Filter um `PrintedQuantity < Quantity` ergänzen.

**1.23 ForgeBot Auto-Hide-Race** — `ForgeBotViewModel.ShowMessage`
Das Auto-Hide läuft als fire-and-forget `Task.Delay().ContinueWith()`. Erscheint innerhalb der 5–10s eine zweite Nachricht, blendet die Continuation der ersten die zweite vorzeitig aus.
*Fix:* Pro Nachricht ein `CancellationTokenSource` führen und bei neuer Nachricht canceln.

**1.24 Settings-Getter mappen Codes nicht zurück auf Display-Strings** — `SettingsViewModel.Language` / `BotLanguage`
Gespeichert wird `"de"`/`"en"`, die Options-Liste enthält `"Deutsch"`/`"English"` — `_idx(...)` findet nie einen Treffer → die Combobox zeigt nach Reload immer den Default.
*Fix:* Symmetrische Code→Display-Mapping-Funktion.

**1.25 PATCH /api/settings erlaubt Typ-Fehler und Id-Überschreibung** — `Server/Program.cs`
(a) Reflection ohne Whitelist: `Id` kann überschrieben werden. (b) Zahlen werden als `int` geliefert; `SetValue` auf `decimal`/`long`/nullable Properties ohne Konvertierung → `ArgumentException` → 500.
*Fix:* Id ausschließen, `Convert.ChangeType` verwenden, Fehler als 400 statt 500 melden.

**1.26 MockMoonraker: parallele WebSocket-Sends** — `MockMoonrakerServer.BroadcastStatus`
`ws.SendAsync(...)` wird im Timer nicht awaited; bei langsamen Clients können zwei Sends auf demselben Socket überlappen → `InvalidOperationException`.
*Fix:* Sends serialisieren, fehlende Endpunkte nachziehen.

### 🟡 Mittel

**1.27 `ConnectAsync` der HTTP-Basis pingt `/`** — Moonraker/OctoPrint liefern auf `/` nicht zwingend 2xx → Verbindungstest meldet Offline, obwohl der Drucker erreichbar ist.
*Fix:* Protokoll-spezifischen Health-Endpoint abfragen.

**1.28 `GetConnection` hat eine Race Condition** — `PrinterConnectionManager.GetConnection`
TryGetValue → Create → Set ist nicht atomar. Zwei Threads können gleichzeitig Verbindungen erzeugen; eine wird überschrieben und leakt.
*Fix:* `GetOrAdd` mit Factory.

**1.29 `DisconnectAll()` ist sync-over-async** — `.GetAwaiter().GetResult()` im UI-Kontext ist ein klassisches Deadlock-Risiko.

**1.30 FileUsageStore: Read-Modify-Write nicht atomar** — Load → Modify → Save ohne übergreifende Sperre; parallele Zugriffe verlieren Updates.

**1.31 Server-FileScanner: Iteration außerhalb des try** — `Directory.EnumerateFiles(...)` ist lazy — `UnauthorizedAccessException` tritt erst während der foreach-Iteration auf, die nicht mehr im try steht → 500.

**1.32 Löschen hinterlässt verwaiste Referenzen** — Beim harten Löschen eines Druckers werden FarmSchedules/BatchItems nicht mitbereinigt. SQLite erzwingt FKs per Default nicht → stille Datenkorruption.
*Fix:* Cascade oder Soft-Delete, `PRAGMA foreign_keys=ON`.

**1.33 DruckWächter: kein Refresh, falsche Capability-Anzeige** — (a) Kein Polling-Timer. (b) `HasShelly` default `true` obwohl `ShellyIp = null`. (c) Licht-/Filament-Macros hardcoded. (d) Port fehlt.
*Fix:* Polling-Timer, Capabilities aus echter Config ableiten, Port-Default ergänzen.

**1.34 `AiAssistantViewModel`: kein Abbruch, UI-Flut** — Kein `CancellationToken` beim Streaming. Pro Token ein `Dispatcher.UIThread.InvokeAsync` — bei schnellen Modellen hunderte Invokes/Sekunde.
*Fix:* CancellationTokenSource pro Generierung; UI-Updates throttlen.

**1.35 `PrinterConnectionManager`: Credentials als Hardcode-Stubs** — Bambu `accessCode: "00000000"`, PrusaLink `apiKey: *** OctoPrint `apiKey: *** Funktional tot.
*Fix:* Credential-Felder am Printer-Modell einführen und durchreichen.

---

## 2. Sicherheit

### 🔴 Kritisch

**2.1 Keine Authentifizierung auf der gesamten Server-API** — `Server/Program.cs`
`AppSettings.ApiKey` existiert, wird aber **nirgends enforced**. Der Server lauscht standardmäßig im LAN und erlaubt jedem Netzwerk-Teilnehmer: Drucker steuern, Daten löschen, Backups einspielen, Chat-Verläufe lesen.
*Fix:* API-Key-Middleware vor allen `/api/*`-Routen.

**2.2 `/api/export` leakt alle Credentials** — Der Export enthält `settings` inkl. `ApiKey`, `TelegramBotToken`, `NextcloudPassword`, `ExternalOpenAiKey`, `ExternalAnthropicKey` — ungeschützt abrufbar.
*Fix:* Secrets beim Export maskieren.

**2.3 Restore-Path-Traversal / Arbitrary File Read** — `body.BackupPath` wird ungeprüft als Quelle für `File.Copy` verwendet. Ein Angreifer kann beliebige Server-Dateien als "Backup" einspielen und deren Inhalt anschließend über `/api/export` exfiltrieren.
*Fix:* Pfade auf das Backup-Verzeichnis einschränken.

### 🟠 Hoch

**2.4 Klartext-Credentials in JSON und SQLite** — Alle Secrets liegen unverschlüsselt in `%LocalAppData%/FlipsiForge/*.json` und `flipsiforge.db`.
*Fix:* Plattform-Schutz anbieten (Windows: DPAPI, Linux: Secret Service/keyring).

**2.5 Kein HTTPS trotz Setting** — `AppSettings.HttpsEnabled`/`HttpsCertPath` existieren, werden serverseitig nie ausgewertet.

### 🟡 Mittel

**2.6 Shelly ohne Auth** — `ShellyClient` unterstützt keine Geräte mit aktiviertem Passwort.
**2.7 Bambu TLS ohne Zertifikatsstrategie** — siehe 1.6.
**2.8 Fehlende Eingabevalidierung an mehreren Endpunkten** — Kein Rate-Limiting auf Scan/AI-Endpunkten.
**2.9 `VACUUM INTO`-Pfad** — SQL mit Pfad zusammenbauen sollte durch parametrisierte Variante ersetzt werden.

---

## 3. Architektur

**3.1 Dreifach definierte Service-Verträge** — Drei inkompatible `IAIChatEngine`-Interfaces mit gleichem Namen. Die Stubs können die echten Core-Implementierungen nie ersetzen.
*Fix:* Verträge ausschließlich in Core definieren.

**3.2 Zwei Settings-Wahrheiten mit garantiertem Drift** — `DesktopSettings` (JSON) vs `AppSettings` (DB). Sync deckt nur 6 Felder ab. Server missbraucht `WatchFolders` als Key-Value-Müllhalde.
*Fix:* Eine Source of Truth (DB).

**3.3 Tote DbSets und Parallel-Persistenz** — `FavoriteFiles`, `FileUsageLogs`, `BotMessages` existieren als Tabellen, werden aber nirgends geschrieben/gelesen. Drei konkurrierende Mechanismen für dieselbe Information.
*Fix:* Persistenz auf die DB-Tabellen konsolidieren.

**3.4 Langlebige DbContexts in ViewModels** — DbContext ist nicht thread-sicher und sammelt über den Change-Tracker unbegrenzt Entities an. Zusätzlich synchrone DB-Zugriffe auf dem UI-Thread.
*Fix:* Kurzlebige Contexts pro Operation.

**3.5 Duplizierte Fachlogik Core ↔ Server** — FileScanner (2×), Maintenance (2×), Scheduling (2×), Slicer-Profile (2×).
*Fix:* Logik in Core konsolidieren.

**3.6 Reflection-Hack statt API** — `DruckWaechterService.ExtractBaseUrl` liest das protected Feld `BaseUrl` per Reflection. Bricht still bei jedem Refactoring.

**3.7 View-Instanziierung bei jedem Tab-Wechsel** — Jeder Switch erzeugt View + ViewModel neu: State-Verlust, wiederholte DB-Loadss, nicht-disposed Ressourcen.
*Fix:* Views/ViewModels cachen.

**3.8 ServiceLocator als statische God-Registry** — Nicht thread-sicher, erschwert Testbarkeit.

**3.9 DruckWächter: Anforderung "Auto-Detect" nicht umgesetzt** — Config-Dokumentation beschreibt Verhalten, das nicht existiert (Auto-Aus-Timer, Abkühl-Schwelle, Telegram-Versand — alles ohne Implementierung).
*Fix:* Implementieren oder Config/Doku auf den Ist-Stand reduzieren.

**3.10 Versions-Chaos** — Desktop `v0.4.0`, Server `0.2.0`, AssemblyInfo `0.2.0`. `obj/`-Artefakte eingecheckt.
*Fix:* Version zentral generieren; `obj/`/`bin/` aus dem Repo entfernen.

---

## 4. Best Practices (.NET / Avalonia)

**4.1 Async-Missbrauch** — `App.axaml.cs`: `.GetAwaiter().GetResult()` beim Startup. `MarlinConnection`: `.ContinueWith(t => t.Result)`. `ForgeBotViewModel.OnTick`: `async void`. `DruckWaechterViewModel`: fire-and-forget ohne Beobachtung.

**4.2 IDisposable-Leaks** — `OnnxGenAiChatEngine`: Model/Tokenizer/Sequences nie disposed. `MarlinConnection`: SerialPort bei Exception undisposed. `DruckWaechterViewModel._httpClient`: nie disposed.

**4.3 `new Random()` an drei Stellen** — bei schnell aufeinanderfolgenden Aufrufen identische Seeds. `Random.Shared` verwenden.

**4.4 FindControl-Stringly-Typing** — ~40 `FindControl<T>("Name")`-Aufrufe in Dialogen. Tippfehler schlagen erst zur Laufzeit zu.

**4.5 Enum-Mapping über SelectedIndex** — bricht bei Enum-Umordnung.

**4.6 Inkonsistente CancellationToken-Verkettung** — teils `cts.Token`, teils das äußere `ct`.

**4.7 Leere Catch-Blöcke ohne jegliches Logging** — flächendeckend. Für eine Hardware-nahe App ist stilles Versagen beim Debuggen vor Ort fatal.

**4.8 `ConfigureModel` ist ein toter Command** — Button ohne Wirkung.

**4.9 `ResetToDefaults` ohne Bestätigung** — Kommentar behauptet Confirm-Dialog, keiner vorhanden.

**4.10 Inkonsistentes Save-Verhalten in Settings** — Manche Commands persistieren sofort, Property-Änderungen erst bei "Speichern".

---

## 5. Verbesserungen (keine Bugs, aber besser)

1. **Seeder-Aktualisierung** — Upsert-Strategie für FilamentDB, Bestandsnutzer bekommen neue Einträge nie.
2. **Embedding-Tokenizer** — `SimpleTokenize` (Hash-Modulo) produziert semantisch bedeutungslose Vektoren; KI-Suche ist faktisch Zufall.
3. **Levenshtein-Performance** — O(n·m) pro Datei bei jedem Such-Keystroke. Vorfilterung oder Suche debouncen.
4. **Duplikat-Erkennung O(n²)** — HashSet der Kandidaten-Pfade aufbauen.
5. **Rekursionstiefe** — Kommentar sagt "1 Level tief", Code macht unbegrenzte Rekursion.
6. **N+1 im Scheduler** — Batch-weise laden, ein `SaveChanges` am Ende.
7. **Marlin-Details** — 8.3-Dateinamen für SD, DTR-Reset-Wartezeit, Baudrate konfigurierbar.
8. **Moonraker-Upload fehlt** — `SendGcodeAsync` startet nur bereits vorhandene Dateien. Multipart-Upload priorisieren.
9. **Bambu Reconnect/Keepalive** — kein Disconnected-Handler, kein Reconnect.
10. **Kostenrechnung** — `CalculateCosts` mit 330m/kg nur für 1.75mm PLA korrekt; Durchmesser/Dichte einbeziehen.
11. **CORS-Policy** — Server setzt kein CORS.
12. **Chat-Historie wächst unbegrenzt** — ohne Retention.
13. **Server-Health erweitern** — DB-Erreichbarkeit, Modul-Status melden.
14. **`FarmSchedule.Priority` vs. Batch-Priority** — Redundanz ohne Sync-Garantie.
15. **Kommentar-Schulden abbauen** — 40-zeilige Anleitung in `FarmModels.cs` gehört in Docs/Migrations.

---

## Zusammenfassung — Top-Prioritäten

| # | Thema | Warum zuerst |
|---|-------|--------------|
| 1 | **Server-Auth + Export-Leak + Restore-Traversal** (2.1–2.3) | Offene LAN-API mit Credential-Exposition — aktiv gefährlich |
| 2 | **Embedding-Culture-Bug** (1.1) | Korrumpiert still sämtliche KI-Daten auf de-DE-Systemen |
| 3 | **Protokoll-Blocker** (1.4–1.6, 1.10) | Vier von fünf Drucker-Integrationen funktionieren gegen echte Hardware nicht |
| 4 | **Fake-Streaming + MeanPool-Index** (1.11, 1.12) | KI-Kernfeatures sind funktional gebrochen |
| 5 | **Vertrags-Duplikate + Settings-Doppelung + tote DbSets** (3.1–3.3) | Verhindert, dass die Stub-Strategie jemals durch echte Implementierungen ersetzbar wird |
| 6 | **DruckWächter: hartes Ausschalten, fehlende Auto-Aus-Logik** (1.17, 1.33) | Hardware-Sicherheitsrelevanz |
| 7 | **Server-Scheduling-Duplikat** (1.19) | Farm-Kernfeature verteilt falsch |

**Positiv:** defensive Programmierung gegen fehlende native Libs, saubere XML-Doku, klare Layer-Trennung Core/Desktop/Server, MockMoonraker als Teststrategie.