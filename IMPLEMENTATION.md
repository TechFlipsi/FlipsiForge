# FlipsiForge — Technische Umsetzungsdokumentation

> **Status:** 18.07.2026 — Recherche-Phase. Alle Technologien, APIs, Libraries und Patterns sind recherchiert und dokumentiert. Ready for Development.

---

## Inhaltsverzeichnis

1. [Projekt-Struktur](#1-projekt-struktur)
2. [Drucker-Protokolle (5)](#2-drucker-protokolle)
3. [Datei-Manager & 3D-Rendering](#3-datei-manager--3d-rendering)
4. [Filament-Management & Kosten-Rechner](#4-filament-management--kosten-rechner)
5. [Model-Repository APIs](#5-model-repository-apis)
6. [Cloud-Sync](#6-cloud-sync)
7. [Server & Auto-Discovery](#7-server--auto-discovery)
8. [Plugin-System](#8-plugin-system)
9. [i18n Internationalisierung](#9-i18n-internationalisierung)
10. [Home Assistant Integration](#10-home-assistant-integration)
11. [Push-Notifications](#11-push-notifications)
12. [NuGet Package Übersicht](#12-nuget-package-uebersicht)
13. [Bekannte Risiken & Mitigation](#13-bekannte-risiken--mitigation)

---

## 1. Projekt-Struktur

### Solution Layout

```
FlipsiForge/
├── FlipsiForge.Core/              # Gemeinsame Geschäftslogik (Class Library)
│   ├── FileScanner/               # Datei-Scan, Indexierung, Thumbnails, STL-Repair, ZIP-Scan, OpenSCAD
│   ├── PrinterController/         # Moonraker, Marlin, Bambu, PrusaLink, OctoPrint Protokolle
│   ├── FilamentManager/           # Spulen-Inventar, NFC/QR, Material-DB, Trocknungs-Log, Fuzzy Search
│   ├── CostCalculator/            # Druck-Kosten-Rechner (Filament + Strom + Verschleiß)
│   ├── ModelRepository/           # Thingiverse, Printables, MakerWorld unified search
│   ├── StatisticsEngine/          # Druck-Statistiken, Verbrauch, Erfolgsrate
│   ├── CameraManager/             # USB-Webcam + RTSP IP-Kamera + Timelapse
│   ├── CloudSync/                  # Nextcloud, Google Drive, OneDrive, Dropbox
│   ├── PluginSystem/              # Plugin Loading, MEF2 + AssemblyLoadContext
│   └── ServerClient/              # Verbindung zu FlipsiForge.Server (optional)
│
├── FlipsiForge/                   # Desktop App (Avalonia UI 12)
│   ├── ViewModels/                # MVVM ViewModels (ReactiveUI)
│   ├── Views/                      # AXAML Views (5 Tabs + Settings)
│   ├── Controls/                   # Custom Controls (STL-Viewer, G-code Visualizer, Webcam)
│   ├── Converters/                 # Value Converters
│   ├── Assets/                     # Icons, Images, Fonts
│   └── I18n/                       # 13 JSON Sprachdateien
│
├── FlipsiForge.Server/            # Headless Backend (ASP.NET Core)
│   ├── Controllers/                # REST API Controllers (Gateway API)
│   ├── Hubs/                       # SignalR WebSocket Hubs (live data)
│   ├── WebUI/                      # Blazor oder static SPA (browser access)
│   ├── Middleware/                 # Auth, Rate Limiting
│   └── Docker/                     # Dockerfile (ARM64 + x64)
│
├── FlipsiForge.Shared/            # Shared DTOs & Contracts (Client ↔ Server)
│   ├── Dtos/                       # Data Transfer Objects
│   └── Enums/                      # Shared Enums (MaterialType, PrinterType, UserRole)
│
├── FlipsiForge.HACS/              # Home Assistant HACS Integration (Python)
│   └── custom_components/flipsiforge/
│
├── FlipsiForge.Addon/             # Home Assistant Add-on (Docker)
│   ├── config.yaml
│   ├── Dockerfile
│   └── run.sh
│
├── tests/                          # Unit + Integration Tests
├── installer/                      # Installer builds (WiX for .exe, dpkg for .deb)
└── docs/                           # Documentation
```

### Target Frameworks

| Projekt | TFM | Begründung |
|---------|-----|------------|
| FlipsiForge.Core | `net10.0` | Cross-platform, keine UI-Abhängigkeit |
| FlipsiForge (Desktop) | `net10.0` | Avalonia UI 12 — Windows + Linux |
| FlipsiForge.Server | `net10.0` | ASP.NET Core — any Linux |
| FlipsiForge.Shared | `netstandard2.1` | Maximale Kompatibilität |
| FlipsiForge.HACS | Python 3.11+ | HA Integration |

### NuGet Packages — Core

```xml
<!-- FlipsiForge.Core.csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.9" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.9" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
<PackageReference Include="Raffinert.FuzzySharp" Version="5.0.3" />
<PackageReference Include="QRCoder" Version="1.8.0" />
<PackageReference Include="PCSC" Version="7.0.*" />
<PackageReference Include="PCSC.Iso7816" Version="7.0.*" />
<PackageReference Include="System.Composition" Version="10.0.*" />
<PackageReference Include="Serilog" Version="4.*" />
<PackageReference Include="Serilog.Sinks.File" Version="6.*" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.*" />
```

### NuGet Packages — Desktop

```xml
<!-- FlipsiForge.csproj (Desktop) -->
<!-- Avalonia 12.0.5 (stable, battle-tested in FlipsiColor v0.4.0+) -->
<PackageReference Include="Avalonia" Version="12.0.5" />
<PackageReference Include="Avalonia.Desktop" Version="12.0.5" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="12.0.5" />
<PackageReference Include="Avalonia.Fonts.Inter" Version="12.0.5" />
<PackageReference Include="Avalonia.Controls.DataGrid" Version="12.0.1" />

<!-- MVVM: CommunityToolkit.Mvvm (source generators, FlipsiColor pattern) -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />

<!-- i18n -->
<PackageReference Include="Lang.Avalonia" Version="12.1.0.1" />
<PackageReference Include="Lang.Avalonia.Json" Version="12.1.0.1" />

<!-- 3D Rendering: Silk.NET + Avalonia NativeControlHost (cross-platform) -->
<PackageReference Include="Silk.NET" Version="2.*" />
<PackageReference Include="Silk.NET.OpenGL" Version="2.*" />

<!-- G-code Visualization: SkiaSharp (simpler than GPU for 2D layer rendering) -->
<PackageReference Include="SkiaSharp" Version="2.88.*" />

<!-- Images -->
<PackageReference Include="SixLabors.ImageSharp" Version="3.*" />

<!-- USB-Serial (Marlin) -->
<PackageReference Include="System.IO.Ports" Version="9.0.*" />
```

### NuGet Packages — Server

```xml
<!-- FlipsiForge.Server.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <TargetFramework>net10.0</TargetFramework>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
  <PackageReference Include="Makaretu.Dns.Multicast" Version="1.6.*" />
  <PackageReference Include="Telegram.Bot" Version="22.*" />
  <PackageReference Include="MQTTnet" Version="4.*" />
</Project>
```

### MVVM Pattern (CommunityToolkit.Mvvm — FlipsiColor Pattern)

```csharp
// CommunityToolkit.Mvvm 8.4.2 — source generators, FlipsiColor uses this
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class PrinterTabViewModel : ObservableObject, IDisposable
{
    [ObservableProperty] private PrinterStatus _status;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "";

    // Computed property — re-notifies when dependencies change
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullStatus))]
    private int _printerCount;

    public string FullStatus => $"Printers: {PrinterCount} — {StatusText}";

    // Change callback (partial method, auto-generated)
    partial void OnStatusTextChanged(string value)
    {
        // side-effect on change
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync(CancellationToken token)
    {
        IsBusy = true;
        try { /* ... */ }
        finally { IsBusy = false; }
    }
    private bool CanConnect() => !IsBusy;

    [RelayCommand]
    private void SelectPrinter(Printer p) { /* parameter command */ }
}
```

### Multi-Tab App Structure (FlipsiColor Pattern)

FlipsiColor nutzt eine Sidebar + TabControl Architektur. FlipsiForge sollte dasselbe Pattern mit 5 Tabs verwenden:

```xml
<!-- MainWindow.axaml -->
<Grid>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="220"/>   <!-- Sidebar Navigation -->
    <ColumnDefinition Width="*"/>      <!-- Main Content -->
  </Grid.ColumnDefinitions>

  <!-- Sidebar with 5 Tab buttons -->
  <StackPanel Grid.Column="0" Classes="sidebar">
    <Button Content="📁 Dateien"  Command="{Binding NavigateCommand}" CommandParameter="Files"/>
    <Button Content="🖨️ Drucker"  Command="{Binding NavigateCommand}" CommandParameter="Printers"/>
    <Button Content="🧶 Filament" Command="{Binding NavigateCommand}" CommandParameter="Filament"/>
    <Button Content="🌐 Modelle"  Command="{Binding NavigateCommand}" CommandParameter="Models"/>
    <Button Content="📊 Statistik" Command="{Binding NavigateCommand}" CommandParameter="Stats"/>
  </StackPanel>

  <!-- Content area: ViewSwitcher -->
  <ContentControl Grid.Column="1" Content="{Binding CurrentView}"/>
</Grid>
```

### Key Avalonia 12 Pitfalls (from FlipsiColor/FlipsiSort experience)

- **`[ObservableProperty]` requires `partial` class** — source generator needs it
- **Use `{DynamicResource}` for theme brushes**, NOT `{ThemeResource}` (WPF-only)
- **`Dispatcher.UIThread.Post()`** not `Dispatcher.Invoke()` for UI thread
- **`StorageProvider.OpenFilePickerAsync()`** not `OpenFileDialog` (Avalonia 12 API)
- **Compiled bindings enabled by default** in v12
- **Version from assembly**: `Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)` — never hardcode
- **`ScrollBarViewer Padding="L,T,R,48"`** — bottom 48px so last section isn't clipped

---

## 2. Drucker-Protokolle

### 2.1 Moonraker (Klipper) — Snapmaker, Elegoo, Voron, Qidi

**Verbindung:** HTTP REST + WebSocket
**Default Port:** 80 (Snapmaker U1: 192.168.178.30, Neptune 4 Pro: 192.168.178.29)
**Doku:** https://moonraker.readthedocs.io/en/latest/external_api/

#### Wichtige REST Endpoints

| Method | Endpoint | Zweck |
|--------|----------|-------|
| `GET` | `/printer/info` | Klipper State, Version, Hostname, CPU |
| `POST` | `/printer/emergency_stop` | Not-Aus |
| `POST` | `/printer/restart` | Soft Restart |
| `POST` | `/printer/firmware_restart` | Firmware Restart |
| `GET` | `/printer/objects/list` | Verfügbare Klipper-Objekte |
| `GET` | `/printer/objects/query?extruder&heater_bed&print_stats` | Temperaturen, Status abfragen |
| `POST` | `/printer/print/start?filename=druck.gcode` | Druck starten |
| `POST` | `/printer/print/pause` | Pause |
| `POST` | `/printer/print/resume` | Resume |
| `POST` | `/printer/print/cancel` | Abbrechen |
| `POST` | `/printer/gcode/script?script=G28` | G-Code Befehl senden |
| `POST` | `/server/files/upload` | Datei hochladen (multipart) |
| `GET` | `/server/files/list` | Dateien listen |
| `DELETE` | `/server/files/delete?path=druck.gcode` | Datei löschen |
| `GET` | `/api/printer` | Legacy Status-Endpoint |

#### WebSocket (Live-Daten)

```
ws://192.168.178.30/websocket
```

**Subscribe Pattern:**
```json
{
    "jsonrpc": "2.0",
    "method": "printer.objects.subscribe",
    "params": {
        "objects": {
            "extruder": ["temperature", "target"],
            "heater_bed": ["temperature", "target"],
            "print_stats": ["state", "filename", "progress"],
            "virtual_sdcard": ["progress", "file_position"],
            "toolhead": ["position"],
            "gcode_move": ["speed", "speed_factor"]
        }
    },
    "id": 1
}
```

**Live Update Response:**
```json
{
    "jsonrpc": "2.0",
    "method": "notify_status_update",
    "params": [{
        "extruder": {"temperature": 210.5, "target": 210.0},
        "heater_bed": {"temperature": 60.1, "target": 60.0},
        "print_stats": {"state": "printing", "filename": "test.gcode", "progress": 0.45}
    }]
}
```

#### C# Implementation

```csharp
public interface IPrinterProtocol
{
    Task<PrinterStatus> GetStatusAsync();
    Task StartPrintAsync(string filename);
    Task PauseAsync();
    Task ResumeAsync();
    Task CancelAsync();
    Task EmergencyStopAsync();
    Task SendGcodeAsync(string command);
    Task UploadFileAsync(string localPath);
    IObservable<PrinterStatus> SubscribeToStatusUpdates();
    Task DisconnectAsync();  // Clean disconnect when removing a printer
}

public class MoonrakerClient : IPrinterProtocol
{
    private readonly HttpClient _http;
    private ClientWebSocket _ws;

    public MoonrakerClient(string host, int port = 80)
    {
        _http = new HttpClient { BaseAddress = new($"http://{host}:{port}") };
    }

    public async Task<PrinterStatus> GetStatusAsync()
    {
        var response = await _http.GetFromJsonAsync<MoonrakerResponse<PrinterStatus>>(
            "/printer/objects/query?extruder&heater_bed&print_stats&virtual_sdcard&toolhead");
        return response.Result;
    }

    public async Task StartPrintAsync(string filename)
    {
        await _http.PostAsync($"/printer/print/start?filename={Uri.EscapeDataString(filename)}", null);
    }

    public async Task EmergencyStopAsync()
    {
        await _http.PostAsync("/printer/emergency_stop", null);
    }

    public async Task SendGcodeAsync(string command)
    {
        await _http.PostAsync($"/printer/gcode/script?script={Uri.EscapeDataString(command)}", null);
    }

    public async Task UploadFileAsync(string localPath)
    {
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(localPath);
        form.Add(new StreamContent(fileStream), "file", Path.GetFileName(localPath));
        form.Add(new StringContent("/"), "root", "gcodes");
        await _http.PostAsync("/server/files/upload", form);
    }

    public IObservable<PrinterStatus> SubscribeToStatusUpdates()
    {
        return Observable.Create<PrinterStatus>(async observer =>
        {
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(new($"ws://{_http.BaseAddress.Host}/websocket"), CancellationToken.None);
            // Send subscribe request...
            // Read loop: parse notify_status_update → observer.OnNext(status)
        });
    }
}
```

### 2.2 Marlin (USB-Serial) — Creality, Anycubic, Artillery

**Verbindung:** USB-Serial (System.IO.Ports)

```csharp
public class MarlinClient : IPrinterProtocol
{
    private SerialPort _port;

    public MarlinClient(string portName, int baudRate = 115200)
    {
        _port = new SerialPort(portName, baudRate)
        {
            ReadTimeout = 5000,
            WriteTimeout = 5000,
            DtrEnable = true,
            RtsEnable = true
        };
        _port.DataReceived += OnDataReceived;
    }

    public async Task SendGcodeAsync(string command)
    {
        _port.WriteLine(command);
        // Wait for "ok" response
    }

    public async Task<PrinterStatus> GetStatusAsync()
    {
        // M105 → temperature, M27 → print status, M73 → progress
        await SendGcodeAsync("M105");
        // Parse: "ok T:210.5 /210.0 B:60.1 /60.0 @:64"
    }

    public async Task StartPrintAsync(string filename)
    {
        // M23 (select file) + M24 (start print) — for SD card
        await SendGcodeAsync($"M23 {filename}");
        await SendGcodeAsync("M24");
    }
}
```

**NuGet:** `System.IO.Ports` (eingebaut in .NET 10, separater NuGet für ältere Versionen)

**G-Code Cheatsheet:**
| G-Code | Zweck |
|--------|-------|
| `M105` | Temperaturen lesen |
| `M27` | Druck-Status |
| `M73 P25` | Fortschritt 25% setzen |
| `M24` | Druck starten (SD) |
| `M25` | Pause |
| `M0` | Pause (alternative) |
| `M112` | Not-Aus |
| `G28` | Home alle Achsen |
| `G28 X` | Home X |
| `M140 S60` | Bett-Temperatur setzen |
| `M104 S210` | Hotend-Temperatur setzen |
| `M106 S255` | Lüfter an |

### 2.3 Bambu Lab (MQTT + FTP) — X1/P1/A1 Series

**Verbindung:** MQTT (Port 8883 mit TLS) + FTP (Port 990 mit TLS)
**Doku:** https://bambutools.github.io/bambulabs_api/
**Reverse-engineered API:** https://github.com/Doridian/OpenBambuAPI/blob/main/mqtt.md

**MQTT Topics:**
- Subscribe: `device/{serial}/report` — Push-Nachrichten vom Drucker
- Publish: `device/{serial}/request` — Befehle senden

**MQTT Message Format (JSON):**
```json
{
    "print": {
        "command": "print_start",
        "param": "/mnt/sdcard/file.gcode"
    }
}
```

**C# Implementation:**
```csharp
// NuGet: MQTTnet
public class BambuClient : IPrinterProtocol
{
    private IMqttClient _mqtt;

    public BambuClient(string ip, string serial, string accessCode)
    {
        var factory = new MqttFactory();
        _mqtt = factory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(ip, 8883)
            .WithCredentials($"bblp:{accessCode}")
            .WithTls(t => t.UseSslProtocols(SslProtocols.Tls12))
            .Build();
    }

    public async Task StartPrintAsync(string filename)
    {
        var msg = new MqttApplicationMessageBuilder()
            .WithTopic($"device/{_serial}/request")
            .WithPayload(JsonSerializer.Serialize(new {
                print = new { command = "print_start", param = filename }
            }))
            .Build();
        await _mqtt.PublishAsync(msg);
    }
}
```

**NuGet:** `MQTTnet` (open-source, cross-platform MQTT client)

### 2.4 PrusaLink (REST API) — Prusa MK3/MK4/MK3.5/SL1

**Verbindung:** HTTP REST + API Key
**Doku:** https://prusa-link.hexdocs.pm/api-reference.html
**OpenAPI Spec:** https://github.com/prusa3d/Prusa-Link-Web/blob/master/spec/openapi.yaml

| Method | Endpoint | Zweck |
|--------|----------|-------|
| `GET` | `/api/v1/info` | API Version, Printer Info |
| `GET` | `/api/v1/status` | Printer Status |
| `GET` | `/api/v1/job` | Aktiver Druck-Job |
| `POST` | `/api/v1/job/pause` | Pause |
| `POST` | `/api/v1/job/resume` | Resume |
| `POST` | `/api/v1/job/stop` | Stop |
| `GET` | `/api/v1/storage` | Storage-Liste |
| `GET` | `/api/v1/files/{storage}/{path}` | Dateien listen |
| `POST` | `/api/v1/files/{storage}/{path}` | Datei hochladen / Druck starten |
| `GET` | `/api/v1/printer` | Temperaturen, Position |

**Auth:** HTTP Digest mit API Key (Prusa Link Web UI → Settings → API Key)

```csharp
public class PrusaLinkClient : IPrinterProtocol
{
    private readonly HttpClient _http;

    public PrusaLinkClient(string host, string apiKey)
    {
        _http = new HttpClient { BaseAddress = new($"http://{host}") };
        _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public async Task<PrinterStatus> GetStatusAsync()
    {
        return await _http.GetFromJsonAsync<PrinterStatus>("/api/v1/status");
    }
}
```

### 2.5 OctoPrint (REST + WebSocket) — Jeder Drucker mit OctoPrint

**Doku:** https://docs.octoprint.org/en/main/api/

| Method | Endpoint | Zweck |
|--------|----------|-------|
| `GET` | `/api/printer` | Temperaturen, Status |
| `POST` | `/api/printer/printhead` | Home, Jog |
| `POST` | `/api/printer/tool` | Tool-Temperatur |
| `POST` | `/api/printer/bed` | Bett-Temperatur |
| `POST` | `/api/job` | Start/Pause/Resume/Cancel |
| `GET` | `/api/files` | Dateien listen |
| `POST` | `/api/files/local/{path}` | Datei hochladen |
| `POST` | `/api/files/local/{path}` | Druck starten |
| `GET` | `/api/settings` | Drucker-Einstellungen |
| `POST` | `/api/connection` | Verbinden/Trennen |

**Auth:** `X-Api-Key` Header

**WebSocket:** `ws://{host}/sock` — live updates

### 2.6 RTSP IP-Kamera Support

Nicht jeder hat USB-Webcam. Viele haben IP-Kameras (Axis, Reolink, Tapo, etc.).

```csharp
// Option 1: LibVLC (cross-platform, spielt RTSP Streams)
// NuGet: LibVLCSharp.Avalonia
using LibVLCSharp;
using LibVLCSharp.Avalonia;

var core = LibVLC.Initialize();
var libVLC = new LibVLC();
var mediaPlayer = new MediaPlayer(libVLC);
using var media = new Media(libVLC, "rtsp://192.168.1.100/stream", FromType.FromLocation);
mediaPlayer.Play(media);

// In Avalonia:
// <vlc:VideoView MediaPlayer="{Binding MediaPlayer}" />
```

**NuGet:**
- `LibVLCSharp.Avalonia` — VLC Integration für Avalonia (RTSP, RTMP, HTTP streams)
- Alternative: `Ffmpeg.AutoGen` + custom rendering (mehr Kontrolle, komplexer)

---

## 3. Datei-Manager & 3D-Rendering

### 3.1 STL Parsing

STL Dateien gibt es in zwei Formaten: Binary und ASCII.

```csharp
public class StlParser
{
    public static Mesh Parse(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        // Binary STL: 80-byte header + 4-byte triangle count + triangles
        // ASCII STL: "solid ..." lines with "facet normal / vertex / endfacet"

        Span<byte> header = stackalloc byte[80];
        fs.Read(header);

        if (IsAsciiStl(header))
            return ParseAscii(filePath);
        else
            return ParseBinary(fs);
    }

    private static Mesh ParseBinary(FileStream fs)
    {
        using var br = new BinaryReader(fs);
        br.ReadBytes(80); // skip header
        int triangleCount = br.ReadInt32();

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<int>();

        for (int i = 0; i < triangleCount; i++)
        {
            br.ReadSingle(); br.ReadSingle(); br.ReadSingle(); // normal (ignored, recalculate)
            var v1 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            var v2 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            var v3 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            br.ReadUInt16(); // attribute byte count

            int idx = vertices.Count;
            vertices.AddRange(new[] { v1, v2, v3 });
            indices.AddRange(new[] { idx, idx + 1, idx + 2 });
        }

        return new Mesh(vertices, indices);
    }
}

public record Mesh(List<Vector3> Vertices, List<int> Indices)
{
    public float CalculateVolumeCm3()
    {
        float volume = 0;
        for (int i = 0; i < Indices.Count; i += 3)
        {
            var v0 = Vertices[Indices[i]];
            var v1 = Vertices[Indices[i + 1]];
            var v2 = Vertices[Indices[i + 2]];
            volume += Vector3.Dot(v0, Vector3.Cross(v1, v2)) / 6f;
        }
        return Math.Abs(volume) / 1000f; // mm³ → cm³
    }

    public float CalculateWeightGrams(float densityGcm3)
        => CalculateVolumeCm3() * densityGcm3;
}
```

**Keine NuGet nötig** — STL ist simpel genug zum selbst parsen. Alternativ: `netgen-mesh` oder `TriangleNet` für komplexere Operationen.

### 3.2 3D Rendering (Silk.NET + Avalonia NativeControlHost)

**Silk.NET** ist der cross-platform Pfad für 3D-Rendering in Avalonia (Windows + Linux).
**HelixToolkit.Avalonia.SharpDX** ist Windows-only (DirectX) — nicht nutzbar.

```csharp
// Silk.NET OpenGL via Avalonia NativeControlHost
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

public class StlRenderControl : NativeControlHost
{
    private GL _gl;
    private int _vao, _vbo, _ebo;
    private uint _shaderProgram;

    protected override IPlatformHandle CreatePlatformControlHandle(IPlatformHandle parent)
    {
        // Create native OpenGL context via Silk.NET
        var handle = base.CreatePlatformControlHandle(parent);
        _gl = GL.GetApi(CreateDefaultContext());
        SetupShaders();
        return handle;
    }

    public void LoadMesh(Mesh mesh)
    {
        // Upload vertices and indices to GPU
        _gl.GenVertexArrays(1, out _vao);
        _gl.BindVertexArray(_vao);
        // ... (same as OpenTK pattern but with Silk.NET API)
    }

    public void Render(Camera camera)
    {
        _gl.UseProgram(_shaderProgram);
        _gl.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indexCount, DrawElementsType.UnsignedInt, 0);
    }
}
```

**NativeControlHost Airspace-Limitation:** Das OpenGL-Control überlagert andere Avalonia-Controls nicht (kein Transparency). STL-Viewer in separatem Panel, nicht über anderen UI-Elementen.

### 3.3 Thumbnail Generation (Server-side, ohne GPU)

```csharp
// Software rendering for Raspberry Pi (no GPU needed)
public byte[] GenerateThumbnail(Mesh mesh, int width = 256, int height = 256)
{
    // Project 3D mesh to 2D, render with software rasterizer
    // Options:
    // 1. Silk.NET with software rendering context
    // 2. Custom software rasterizer (simple Z-buffer)
    // 3. ImageSharp to draw the projected wireframe

    using var image = new Image<Rgba32>(width, height);
    // Render mesh to image...
    using var ms = new MemoryStream();
    image.SaveAsPng(ms);
    return ms.ToArray();
}
```

**NuGet:** `SixLabors.ImageSharp` (cross-platform image manipulation, kein System.Drawing nötig)

### 3.4 G-code Parser & Visualizer

```csharp
public class GcodeParser
{
    public List<GcodeLayer> Parse(string filePath)
    {
        var layers = new List<GcodeLayer>();
        var currentLayer = new GcodeLayer { Z = 0 };
        float x = 0, y = 0, z = 0, e = 0, speed = 1500;
        bool extruding = false;

        foreach (var line in File.ReadLines(filePath))
        {
            if (line.StartsWith("G1") || line.StartsWith("G0"))
            {
                var cmd = ParseGcodeLine(line);
                var newX = cmd.GetValueOrDefault('X', x);
                var newY = cmd.GetValueOrDefault('Y', y);
                var newZ = cmd.GetValueOrDefault('Z', z);
                var newE = cmd.GetValueOrDefault('E', e);

                if (newZ != z)
                {
                    layers.Add(currentLayer);
                    currentLayer = new GcodeLayer { Z = newZ };
                    z = newZ;
                }

                bool isExtruding = newE > e && cmd.ContainsKey('E');
                currentLayer.Moves.Add(new GcodeMove(x, y, newX, newY, isExtruding, speed));

                x = newX; y = newY; e = newE;
            }
            if (line.StartsWith("G1 F") || line.StartsWith("G0 F"))
                speed = ParseFloat(line, 'F', speed);
        }
        layers.Add(currentLayer);
        return layers;
    }
}

public record GcodeLayer
{
    public float Z { get; set; }
    public List<GcodeMove> Moves { get; } = new();
}

public record GcodeMove(float FromX, float FromY, float ToX, float ToY, bool Extruding, float Speed);
```

### 3.5 STL Repair Check

```csharp
public class StlRepairChecker
{
    public StlReport Check(Mesh mesh)
    {
        var report = new StlReport();

        // 1. Non-manifold edges (edges with only 1 or 3+ faces)
        var edgeCounts = new Dictionary<(int, int), int>();
        for (int i = 0; i < mesh.Indices.Count; i += 3)
        {
            for (int j = 0; j < 3; j++)
            {
                int a = mesh.Indices[i + j];
                int b = mesh.Indices[i + (j + 1) % 3];
                var edge = a < b ? (a, b) : (b, a);
                edgeCounts[edge] = edgeCounts.GetValueOrDefault(edge, 0) + 1;
            }
        }

        report.NonManifoldEdges = edgeCounts.Count(e => e.Value != 2);

        // 2. Holes (boundary loops)
        report.HasHoles = edgeCounts.Any(e => e.Value == 1);

        // 3. Reversed normals (optional check)
        // 4. Self-intersections (expensive — optional)

        report.IsPrintable = report.NonManifoldEdges == 0 && !report.HasHoles;
        return report;
    }
}
```

### 3.6 ZIP-Archiv-Scan

```csharp
// .NET 10 built-in: System.IO.Compression
using System.IO.Compression;

public class ZipScanner
{
    public IEnumerable<ScannedFile> ScanZip(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (Is3DFile(entry.FullName))
            {
                yield return new ScannedFile
                {
                    Path = $"{zipPath}!/{entry.FullName}",
                    Size = entry.Length,
                    // For thumbnail: entry.Open() → partial read → render
                };
            }
        }
    }
}
```

### 3.7 OpenSCAD Integration

```csharp
// Call OpenSCAD CLI to export .scad → .stl
public class OpenScadClient
{
    public async Task<string> ExportToStl(string scadPath, string outputPath, Dictionary<string, string> parameters)
    {
        var paramArgs = string.Join(" ", parameters.Select(p => $"-D '{p.Key}=\"{p.Value}\"'"));
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "openscad",
                Arguments = $"-o \"{outputPath}\" {paramArgs} \"{scadPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        process.Start();
        await process.WaitForExitAsync();
        return outputPath;
    }

    // Parse .scad file for parametric variables
    public Dictionary<string, ScadParameter> ParseParameters(string scadPath)
    {
        var parameters = new Dictionary<string, ScadParameter>();
        foreach (var line in File.ReadLines(scadPath))
        {
            // Match: var = default_value;
            var match = Regex.Match(line, @"^(\w+)\s*=\s*([^;]+);");
            if (match.Success)
                parameters[match.Groups[1].Value] = ParseScadValue(match.Groups[2].Value);
        }
        return parameters;
    }
}
```

### 3.8 Filesystem Watching

```csharp
// .NET 10 built-in: System.IO.FileSystemWatcher
public class DriveScanner
{
    private readonly List<FileSystemWatcher> _watchers = new();

    public void StartScan()
    {
        // Enumerate all drives
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            ScanDrive(drive.Name);
        }
    }

    public void WatchFolder(string path)
    {
        var watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            Filter = "" // all files
        };
        watcher.Created += OnFileCreated;
        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);
    }

    private void ScanDrive(string root)
    {
        // Background scan — parallel, non-blocking
        Task.Run(() =>
        {
            foreach (var file in Enumerate3DFiles(root))
            {
                OnFileFound(file);
            }
        });
    }
}
```

---

## 4. Filament-Management & Kosten-Rechner

### 4.1 Material-Datenbank (Auto-Fill Densities)

### Filament Spool Model (Full CRUD + Size + Weight)

```csharp
public class FilamentSpool
{
    public int Id { get; set; }
    public string Brand { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string MaterialType { get; set; } = "PLA";   // PLA, PETG, ...
    public decimal Density { get; set; }                 // g/cm³
    public decimal DiameterMm { get; set; } = 1.75m;     // 1.75, 2.85, 3.00

    // Weight tracking
    public decimal NominalWeightG { get; set; } = 1000m;  // advertised (e.g. 1000g)
    public decimal ActualWeightG { get; set; }            // measured (scale)
    public decimal ConsumedWeightG { get; set; } = 0m;    // auto-deducted on print
    public decimal EmptySpoolWeightG { get; set; } = 0m;  // for weigh-to-calculate
    public decimal RemainingWeightG => (ActualWeightG > 0 ? ActualWeightG : NominalWeightG) - ConsumedWeightG;

    // Spool dimensions (for AMS/box compatibility)
    public decimal SpoolWidthMm { get; set; }              // e.g. 56mm
    public decimal SpoolOuterDiameterMm { get; set; }      // e.g. 200mm
    public decimal SpoolInnerDiameterMm { get; set; }      // e.g. 52mm
    public decimal SpoolHubDiameterMm { get; set; }        // e.g. 80mm

    // Cost
    public decimal PurchasePrice { get; set; }             // total price paid
    public decimal PricePerKg => NominalWeightG > 0 ? PurchasePrice / (NominalWeightG / 1000m) : 0m;
    public decimal PricePerGram => NominalWeightG > 0 ? PurchasePrice / NominalWeightG : 0m;

    // Visual
    public string? ColorHex { get; set; }                  // e.g. "#1a1a1a"
    public string? PhotoPath { get; set; }                // shop photo or own photo

    // NFC / QR
    public string? NfcTagUid { get; set; }                // OpenPrintTag UUID
    public string? QrCode { get; set; }                   // generated QR identifier

    // Status & lifecycle
    public SpoolStatus Status { get; set; } = SpoolStatus.Active;
    public DateTime PurchaseDate { get; set; }
    public DateTime? ArchivedAt { get; set; }              // when removed/archived

    // Relations
    public List<DryingSession> DryingSessions { get; set; } = new();
    public List<PrintJob> PrintJobs { get; set; } = new();
}

public enum SpoolStatus
{
    Active,     // in use, ready to print
    InDryer,    // currently drying
    InStorage,  // stored away
    Empty,      // consumed, waiting to be archived
    Archived    // removed/kept for history
}
```

### Filament Manager (Add / Edit / Remove)

```csharp
public class FilamentManager
{
    private readonly FlipsiForgeDbContext _db;

    public async Task<FilamentSpool> AddSpoolAsync(SpoolConfig config)
    {
        var spool = new FilamentSpool
        {
            Brand = config.Brand,
            MaterialName = config.MaterialName,
            MaterialType = config.MaterialType,
            Density = MaterialDatabase.GetValueOrDefault(config.MaterialType, 1.24m),
            DiameterMm = config.DiameterMm,
            NominalWeightG = config.NominalWeightG,
            ActualWeightG = config.ActualWeightG,
            EmptySpoolWeightG = config.EmptySpoolWeightG,
            SpoolWidthMm = config.SpoolWidthMm,
            SpoolOuterDiameterMm = config.SpoolOuterDiameterMm,
            SpoolInnerDiameterMm = config.SpoolInnerDiameterMm,
            SpoolHubDiameterMm = config.SpoolHubDiameterMm,
            PurchasePrice = config.PurchasePrice,
            ColorHex = config.ColorHex,
            PurchaseDate = DateTime.UtcNow,
            Status = SpoolStatus.Active
        };
        _db.Spools.Add(spool);
        await _db.SaveChangesAsync();
        return spool;
    }

    // Multi-pack: add multiple identical spools at once
    public async Task<List<FilamentSpool>> AddMultiPackAsync(SpoolConfig config, int count)
    {
        var spools = new List<FilamentSpool>();
        for (int i = 0; i < count; i++)
            spools.Add(await AddSpoolAsync(config));
        return spools;
    }

    public async Task EditSpoolAsync(int spoolId, SpoolConfig updates)
    {
        var spool = await _db.Spools.FindAsync(spoolId);
        if (spool == null) return;
        spool.Brand = updates.Brand;
        spool.MaterialName = updates.MaterialName;
        spool.MaterialType = updates.MaterialType;
        spool.Density = updates.Density;
        spool.DiameterMm = updates.DiameterMm;
        spool.NominalWeightG = updates.NominalWeightG;
        spool.ActualWeightG = updates.ActualWeightG;
        spool.ColorHex = updates.ColorHex;
        spool.PurchasePrice = updates.PurchasePrice;
        // ... alle Felder
        await _db.SaveChangesAsync();
    }

    public async Task RemoveSpoolAsync(int spoolId, bool keepHistory = true)
    {
        var spool = await _db.Spools.FindAsync(spoolId);
        if (spool == null) return;

        if (keepHistory)
        {
            spool.Status = SpoolStatus.Archived;
            spool.ArchivedAt = DateTime.UtcNow;
        }
        else
        {
            // Full purge — delete spool + all related data
            var drying = _db.DryingSessions.Where(d => d.SpoolId == spoolId);
            _db.DryingSessions.RemoveRange(drying);
            _db.Spools.Remove(spool);
        }
        await _db.SaveChangesAsync();
    }
}
```

```csharp
public static readonly Dictionary<string, MaterialInfo> MaterialDatabase = new()
{
    ["PLA"]    = new("PLA", 1.24m),
    ["PETG"]   = new("PETG", 1.27m),
    ["TPU"]    = new("TPU", 1.21m),
    ["TPE"]    = new("TPE", 1.21m),
    ["ABS"]    = new("ABS", 1.04m),
    ["ASA"]    = new("ASA", 1.05m),
    ["PC"]     = new("PC", 1.30m),
    ["PC/ABS"] = new("PC/ABS", 1.19m),
    ["PA6"]    = new("PA6 (Nylon)", 1.52m),
    ["PA12"]   = new("PA12 (Nylon)", 1.01m),
    ["HIPS"]   = new("HIPS", 1.03m),
    ["PVA"]    = new("PVA", 1.23m),
    ["PMMA"]   = new("PMMA (Acrylic)", 1.18m),
    ["POM"]    = new("POM (Acetal)", 1.40m),
    ["PP"]     = new("PP (Polypropylene)", 0.90m),
    ["Wood"]   = new("Wood-filled", 1.28m),
    ["PA-CF"]  = new("PA-CF (Carbon)", 1.27m),
    ["PETG-CF"] = new("PETG-CF (Carbon)", 1.27m),
    ["PLA-CF"] = new("PLA-CF (Carbon)", 1.27m),
};

public record MaterialInfo(string Name, decimal DensityGcm3);

// g/m for 1.75mm filament
public static decimal GramsPerMeter(decimal density, decimal diameter = 1.75m)
    => density * (decimal)Math.PI * (double)(diameter / 2m * diameter / 2m) * 1000m / 1000m;
```

### 4.2 NFC (OpenPrintTag) Support

**OpenPrintTag** ist Prusa's offener NFC-Standard (MIT License, Oktober 2025).
- **Tag-Typ:** ISO 15693 (NFC-V), NXP ICODE SLIX2 — NICHT NTAG!
- **Format:** NDEF + CBOR (compact binary)
- **Felder:** Material-Typ (Enum), Marke, Farbe (RGBA), Gewicht (nominal/aktuell/verbraucht), Dichte, Durchmesser, 68+ Property Tags

**USB Reader:** ACS ACR1552U (liest NFC-V / ISO 15693)

```csharp
// NuGet: PCSC + PCSC.Iso7816
// CBOR: System.Formats.Cbor (built-in .NET 5+)
using PCSC;
using PCSC.Iso7816;
using System.Formats.Cbor;

public class OpenPrintTagReader
{
    public OpenPrintTag Read()
    {
        var context = ContextFactory.Instance.EstablishContext(SCardScope.System);
        var readers = context.GetReaders();
        using var reader = context.ConnectReader(readers[0], SCardShareMode.Shared, SCardProtocol.T1);

        // Read all blocks from ICODE SLIX2 (316 bytes)
        var data = ReadAllBlocks(reader);
        // Decode NDEF → CBOR → OpenPrintTag struct
        return DecodeCbor(data);
    }

    private OpenPrintTag DecodeCbor(byte[] data)
    {
        using var cbor = new CborReader(data);
        // Read per OpenPrintTag schema
        return new OpenPrintTag
        {
            MaterialType = (MaterialType)cbor.ReadInt32(),
            BrandName = cbor.ReadTextString(),
            Color = cbor.ReadTextString(),
            NominalWeightG = cbor.ReadSingle(),
            ConsumedWeightG = cbor.ReadSingle(),
            // ... weitere Felder laut Spec
        };
    }
}
```

**⚠️ Wichtig:** Keine offizielle C# SDK von Prusa. Wir müssen einen `OpenPrintTagCodec` schreiben (CBOR ↔ C# POCO). Budget: 1-2 Tage.

### 4.3 QR-Code Generation

```csharp
// NuGet: QRCoder 1.8.0 (pure C#, zero deps)
using QRCoder;

public class QrCodeGenerator
{
    public byte[] GenerateSpoolQrCode(int spoolId)
    {
        var url = $"flipsiforge://spool/{spoolId}";
        using var qrData = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var renderer = new PngByteQRCode(qrData);
        return renderer.GetGraphic(pixelsPerModule: 20);
    }

    // SVG für druckbare Labels
    public string GenerateSpoolQrCodeSvg(int spoolId)
    {
        var url = $"flipsiforge://spool/{spoolId}";
        using var qrData = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.H);
        return new SvgQRCode(qrData).GetGraphic(20);
    }
}
```

### 4.4 Fuzzy Search

```csharp
// NuGet: Raffinert.FuzzySharp 5.0.3
using FuzzySharp;

public class FilamentSearchService
{
    public IEnumerable<FilamentSpool> Search(string query, IEnumerable<FilamentSpool> spools)
    {
        return Process.ExtractAllBy(query, spools.ToList(), s => s.Name, cutoff: 60)
            .Select(r => r.Value);
    }

    // Live search as you type
    public IEnumerable<FilamentSpool> LiveSearch(string query, IEnumerable<FilamentSpool> spools)
    {
        if (string.IsNullOrWhiteSpace(query))
            return spools;
        return Process.ExtractAllBy(query, spools.ToList(),
            s => $"{s.Brand} {s.MaterialName} {s.Color}", cutoff: 50)
            .Select(r => r.Value);
    }
}
```

### 4.5 Kosten-Rechner

```csharp
public record PrintCostInputs(
    decimal GramsUsed,
    decimal PricePerKg,
    decimal PowerWatts,
    decimal Hours,
    decimal KwhPrice,
    decimal PrinterPrice,
    decimal PrinterLifespanHours = 5000m,
    decimal WasteFactor = 1.10m,
    decimal FailureBuffer = 1.10m,
    decimal HandsOnMinutes = 0m,
    decimal HourlyRate = 0m);

public record PrintCostBreakdown(
    decimal Material, decimal Electricity, decimal MachineWear,
    decimal Labor, decimal Total, decimal PerGram);

public static class CostCalculator
{
    public static PrintCostBreakdown Calculate(PrintCostInputs i)
    {
        decimal material   = i.GramsUsed / 1000m * i.PricePerKg * i.WasteFactor;
        decimal electric   = i.PowerWatts / 1000m * i.Hours * i.KwhPrice;
        decimal machineHr  = i.PrinterPrice / i.PrinterLifespanHours;
        decimal wear       = i.Hours * machineHr;
        decimal labor      = i.HandsOnMinutes / 60m * i.HourlyRate;
        decimal subtotal   = material + electric + wear + labor;
        decimal total      = subtotal * i.FailureBuffer;
        decimal perGram    = i.GramsUsed > 0 ? total / i.GramsUsed : 0m;
        return new PrintCostBreakdown(material, electric, wear, labor, total, perGram);
    }
}
```

**Beispiel (Sir's Setup):**
- 100g PLA (€20/kg) × 1.10 Waste = €2.20
- 95W × 6h × €0.29/kWh = €0.17
- 6h × (€400 / 5000h) = €0.48
- × 1.1 Failure = **€3.14 Gesamtkosten**

### 4.6 SQLite (Lokal) + PostgreSQL (Server)

```csharp
// EF Core — funktioniert für SQLite UND PostgreSQL (nur OnConfiguring ändert sich)
public class FlipsiForgeDbContext : DbContext
{
    public DbSet<FilamentSpool> Spools => Set<FilamentSpool>();
    public DbSet<DryingSession> DryingSessions => Set<DryingSession>();
    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();
    public DbSet<PrinterProfile> Printers => Set<PrinterProfile>();

    protected override void OnConfiguring(DbContextOptionsBuilder b)
    {
        // Desktop (SQLite):
        b.UseSqlite("Data Source=flipsiforge.db");
        // Server (PostgreSQL):
        // b.UseNpgsql("Host=localhost;Database=flipsiforge;Username=flipsi;Password=...");
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<FilamentSpool>(e =>
        {
            e.HasIndex(s => s.Name);
            e.HasIndex(s => new { s.Brand, s.MaterialType });
            e.Property(s => s.PurchasePricePerKg).HasColumnType("decimal(10,2)");
        });

        // PrinterProfile — supports add/remove with optional history retention
        mb.Entity<PrinterProfile>(e =>
        {
            e.HasIndex(p => p.Name);
            e.Property(p => p.Protocol).HasConversion<string>();
        });

        // PrintJob → PrinterProfile: cascade delete or restrict (keep history)
        mb.Entity<PrintJob>(e =>
        {
            e.HasOne(j => j.Printer)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict); // Keep print history when printer removed
        });
    }
}
```

### Printer Management (Add / Remove)

```csharp
public class PrinterManager
{
    private readonly FlipsiForgeDbContext _db;

    public async Task<PrinterProfile> AddPrinterAsync(PrinterConfig config)
    {
        var profile = new PrinterProfile
        {
            Name = config.Name,
            Protocol = config.Protocol,  // Moonraker, Marlin, Bambu, PrusaLink, OctoPrint
            Host = config.Host,
            Port = config.Port,
            ApiKey = config.ApiKey,
            BuildVolumeX = config.BuildVolumeX,
            BuildVolumeY = config.BuildVolumeY,
            BuildVolumeZ = config.BuildVolumeZ,
            CreatedAt = DateTime.UtcNow
        };
        _db.Printers.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task RemovePrinterAsync(int printerId, bool keepHistory = true)
    {
        var printer = await _db.Printers.FindAsync(printerId);
        if (printer == null) return;

        // 1. Disconnect active connections (WebSocket, USB-serial, MQTT)
        if (_activeConnections.TryGetValue(printerId, out var conn))
        {
            await conn.DisconnectAsync();
            _activeConnections.Remove(printerId);
        }

        // 2. Handle print history
        if (keepHistory)
        {
            // Keep PrintJob records (OnDelete.Restrict) — printer reference becomes null
            printer.IsActive = false;
            printer.RemovedAt = DateTime.UtcNow;
        }
        else
        {
            // Full purge — delete printer + all related print history
            var jobs = _db.PrintJobs.Where(j => j.PrinterId == printerId);
            _db.PrintJobs.RemoveRange(jobs);
            _db.Printers.Remove(printer);
        }

        await _db.SaveChangesAsync();
    }
}
```

```bash
# Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
# oder im Code: db.Database.Migrate();
```

---

## 5. Model-Repository APIs

### Thingiverse (Offizielle API ✅)

- **Auth:** OAuth2 Bearer Token
- **Rate Limit:** 300 Requests / 5 Minuten
- **Base URL:** `https://api.thingiverse.com`
- **App Registration:** https://www.thingiverse.com/apps/create (Desktop App Type)

```csharp
// GET /search/{term}/ → Suche
// GET /things/{id} → Details
// GET /things/{id}/files → Downloads
// GET /things/{id}/images → Vorschaubilder
```

### Printables (❌ Keine öffentliche API)

- **Status:** Keine öffentliche API bestätigt (Prusa Forum, März 2024)
- **Workaround:** HTML Scraping (fragil) oder Offizielle Partnerschaft mit Prusa
- **Fallback:** User-assisted — FlipsiForge öffnet `printables.com/model/{id}` im Browser, User lädt herunter, FlipsiForge überwacht Download-Ordner
- **Auto-Sync:** Nur möglich wenn Prusa API freigibt. Bis dahin: manuell.

### MakerWorld (❌ Keine offizielle API)

- **Status:** Keine offizielle API von Bambu Lab
- **Workaround:** Reverse-engineered (`kloshi-io/makerworld-api-reverse` in Node/TS → nach C# portieren)
- **Ansatz:** Interne `v1/design-service` Endpoints + `__NEXT_DATA__` Fallback
- **Reason Codes:** `invalid_url`, `not_found`, `upstream_blocked` (401/403/429), `timeout`
- **Cache aggressiv** — Upstream-Änderungen können es brechen

### Unified Search Implementation

```csharp
public class UnifiedModelSearch
{
    private readonly ThingiverseClient _thingiverse;
    private readonly PrintablesScraper _printables;  // best-effort
    private readonly MakerWorldClient _makerworld;    // best-effort

    public async Task<List<ModelResult>> SearchAllAsync(string query)
    {
        var tasks = new List<Task<List<ModelResult>>>
        {
            _thingiverse.SearchAsync(query),
            TrySearchAsync(() => _printables.SearchAsync(query)),
            TrySearchAsync(() => _makerworld.SearchAsync(query)),
        };

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r)
            .OrderByDescending(m => m.Rating)
            .ToList();
    }

    private async Task<List<ModelResult>> TrySearchAsync(Func<Task<List<ModelResult>>> search)
    {
        try { return await search(); }
        catch { return new List<ModelResult>(); } // graceful degradation
    }
}
```

---

## 6. Cloud-Sync

### Nextcloud (Priorität 1) — WebDAV

```csharp
// DIY mit HttpClient (~50 Zeilen, keine NuGet nötig)
public class NextcloudSync : ICloudSyncProvider
{
    private readonly HttpClient _http;
    private readonly string _base;

    public NextcloudSync(string server, string user, string appPassword)
    {
        _base = $"{server}/remote.php/dav/files/{user}";
        _http = new HttpClient();
        var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{appPassword}"));
        _http.DefaultRequestHeaders.Add("Authorization", $"Basic {creds}");
    }

    public async Task UploadAsync(string localPath, string remoteName)
    {
        using var fs = File.OpenRead(localPath);
        await _http.PutAsync($"{_base}/FlipsiForge/{remoteName}", new StreamContent(fs));
    }

    public async Task<byte[]> DownloadAsync(string remoteName)
    {
        return await _http.GetByteArrayAsync($"{_base}/FlipsiForge/{remoteName}");
    }

    public async Task<List<RemoteFile>> ListAsync()
    {
        var propfind = """<?xml version="1.0"?><d:propfind xmlns:d="DAV:"><d:prop><d:resourcetype/><d:getcontentlength/><d:getetag/><d:getlastmodified/></d:prop></d:propfind>""";
        var req = new HttpRequestMessage(new("PROPFIND"), $"{_base}/FlipsiForge/");
        req.Content = new StringContent(propfind, Encoding.UTF8, "application/xml");
        req.Headers.Add("Depth", "1");
        var resp = await _http.SendAsync(req);
        // Parse <d:response> elements → RemoteFile list
    }
}
```

### Google Drive

**NuGet:** `Google.Apis.Drive.v3` (+ `Google.Apis.Auth`)
**OAuth:** Desktop App → Loopback HTTP Listener → `http://localhost:PORT/`

```csharp
var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
    new ClientSecrets { ClientId = "...", ClientSecret = "..." },
    new[] { DriveService.Scope.DriveFile }, "user", CancellationToken.None,
    new FileDataStore("FlipsiForge.DriveToken"));
```

### OneDrive

**NuGet:** `Microsoft.Graph` + `Microsoft.Identity.Client` (MSAL)
**Endpoint:** `https://graph.microsoft.com/v1.0/me/drive`
**Scopes:** `Files.ReadWrite.All`

### Dropbox

**NuGet:** `Dropbox.Api` (v7.0.0+ — ältere Versionen funktionieren seit Jan 2026 nicht mehr!)
**OAuth:** PKCE Flow für Desktop-Apps

### Unified Interface

```csharp
public interface ICloudSyncProvider
{
    Task<List<RemoteFile>> ListAsync();
    Task UploadAsync(string localPath, string remoteName);
    Task DownloadAsync(string remoteName, string localPath);
    Task<string> GetEtagAsync(string remoteName);  // Sync-Check
}
```

Tokens in OS-Secure-Storage: DPAPI (Windows), Keychain (macOS), SecretService (Linux).

---

## 7. Server & Auto-Discovery

### ASP.NET Core Server

```csharp
// FlipsiForge.Server Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();           // Gateway REST API
builder.Services.AddSignalR();               // WebSocket live data
builder.Services.AddDbContext<FlipsiForgeDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("FlipsiForge")));

// mDNS Auto-Discovery (Server advertised sich selbst im LAN)
builder.Services.AddHostedService<MdnsAdvertiser>();

var app = builder.Build();

app.MapControllers();
app.MapHub<PrinterHub>("/hubs/printer");    // WebSocket
app.UseAuthentication();
app.UseAuthorization();
app.Run();
```

### mDNS Auto-Discovery

```csharp
// NuGet: Makaretu.Dns.Multicast (advertise + discover, cross-platform)
using Makaretu.Dns;

public class MdnsAdvertiser : IHostedService
{
    private MulticastService _mdns;

    public Task StartAsync(CancellationToken ct)
    {
        _mdns = new MulticastService();
        var profile = new ServiceProfile("flipsiforge", "_flipsiforge._tcp", 5000);
        profile.AddProperty("version", "1.0");
        profile.AddProperty("api", "http");
        _mdns.ServiceInstanceDiscovery += (s, e) => { /* respond */ };
        _mdns.Start();
        return Task.CompletedTask;
    }
}

// Client side: discover server
public class MdnsDiscovery
{
    public async Task<List<DiscoveredServer>> DiscoverAsync(TimeSpan timeout)
    {
        var servers = new List<DiscoveredServer>();
        var mdns = new MulticastService();
        mdns.ServiceInstanceDiscovered += (s, e) =>
        {
            servers.Add(new DiscoveredServer(e.ServiceInstanceName, e.Addresses));
        };
        mdns.SendQuery("_flipsiforge._tcp.local");
        await Task.Delay(timeout);
        mdns.Stop();
        return servers;
    }
}
```

---

## 8. Plugin-System

**Hybrid:** MEF2 für Discovery + AssemblyLoadContext für Isolation + Hot-Unload

```csharp
// NuGet: System.Composition (MEF2)
// Contract (shared library)
public interface IFlipsiPlugin
{
    string Name { get; }
    string Version { get; }
}

// Plugin Implementierung (separate DLL)
[Export(typeof(IFlipsiPlugin))]
public class CustomSlicerPlugin : IFlipsiPlugin
{
    public string Name => "Custom Slicer";
    public string Version => "1.0";
}

// Host: Plugin laden
var catalog = new AggregateCatalog(
    new AssemblyCatalog(typeof(Program).Assembly),
    new DirectoryCatalog("./plugins", "*.dll"));
using var container = new CompositionContainer(catalog);
var plugins = container.GetExports<IFlipsiPlugin>();

// Mit AssemblyLoadContext (Isolation + Unload)
var ctx = new PluginLoadContext("./plugins/CustomSlicer.dll", isCollectible: true);
var asm = ctx.LoadFromAssemblyName(new("CustomSlicer"));
var pluginType = asm.GetTypes().First(t => typeof(IFlipsiPlugin).IsAssignableFrom(t));
var plugin = (IFlipsiPlugin)Activator.CreateInstance(pluginType);
// Unload: ctx.Unload();
```

**⚠️ Sicherheit:** AssemblyLoadContext isoliert Dependencies, ist aber KEINE Security-Boundary. Untrusted Plugins → separater Prozess (IPC via gRPC/Named Pipes).

---

## 9. i18n Internationalisierung

**NuGet:** `Lang.Avalonia` + `Lang.Avalonia.Json` (v12.1.0.1, Avalonia 12 kompatibel)

**13 Sprachen** — gleicher Standard wie FlipsiColor und FlipsiSort:

| # | Sprache | Kulturcode | JSON Datei |
|---|---------|------------|-----------|
| 1 | English (Fallback) | `en-US` | `en-US.json` |
| 2 | Deutsch | `de-DE` | `de-DE.json` |
| 3 | Français | `fr-FR` | `fr-FR.json` |
| 4 | Español | `es-ES` | `es-ES.json` |
| 5 | Italiano | `it-IT` | `it-IT.json` |
| 6 | Português | `pt-PT` | `pt-PT.json` |
| 7 | Nederlands | `nl-NL` | `nl-NL.json` |
| 8 | Polski | `pl-PL` | `pl-PL.json` |
| 9 | Русский | `ru-RU` | `ru-RU.json` |
| 10 | 中文 (简体) | `zh-CN` | `zh-CN.json` |
| 11 | 日本語 | `ja-JP` | `ja-JP.json` |
| 12 | 한국어 | `ko-KR` | `ko-KR.json` |
| 13 | Türkçe | `tr-TR` | `tr-TR.json` |

**Regel (wie FlipsiColor/FlipsiSort):** Eingestellte Sprache = NUR Wörter in dieser Sprache. Deutsch → keine englischen Wörter sichtbar. Alle 13 Sprachen müssen ECHTE Übersetzungen sein — keine English-Kopien als Platzhalter.

```
I18n/
├── en-US.json    # English (Fallback)
├── de-DE.json    # Deutsch
├── fr-FR.json    # Français
├── es-ES.json    # Español
├── it-IT.json    # Italiano
├── pt-PT.json    # Português
├── nl-NL.json    # Nederlands
├── pl-PL.json    # Polski
├── ru-RU.json    # Русский
├── zh-CN.json    # 中文 (vereinfacht)
├── ja-JP.json    # 日本語
├── ko-KR.json    # 한국어
└── tr-TR.json    # Türkçe
```

```json
{
  "language": "Deutsch",
  "description": "Deutsche Übersetzung",
  "cultureName": "de-DE",
  "Localization": {
    "File": { "Scan": "Scannen", "Import": "Importieren" },
    "Printer": { "Start": "Drucken", "Pause": "Pause", "Cancel": "Abbrechen" }
  }
}
```

**Live-Sprachwechsel zur Laufzeit** (kein Neustart):
```csharp
I18nManager.Instance.Culture = new CultureInfo("de-DE");
```

---

## 10. Home Assistant Integration

### HACS Custom Integration (Python)

```
flipsiforge-hacs/
├── custom_components/flipsiforge/
│   ├── __init__.py
│   ├── sensor.py          # Sensoren: filament_stock, print_status, cost_per_month
│   ├── manifest.json
│   └── config_flow.py     # Config Flow (Server URL eingeben)
├── hacs.json
└── README.md
```

**manifest.json:**
```json
{
  "domain": "flipsiforge",
  "name": "FlipsiForge",
  "documentation": "https://github.com/TechFlipsi/flipsiforge-hacs",
  "codeowners": ["@TechFlipsi"],
  "version": "1.0.0",
  "config_flow": true,
  "iot_class": "local_polling"
}
```

**Sensoren die FlipsiForge HA liefert:**
- `sensor.flipsiforge_filament_pla_remaining` — PLA Restbestand (g)
- `sensor.flipsiforge_filament_petg_remaining` — PETG Restbestand (g)
- `sensor.flipsiforge_printer_snapmaker_status` — Drucker Status
- `sensor.flipsiforge_printer_snapmaker_progress` — Druck-Fortschritt (%)
- `sensor.flipsiforge_print_cost_this_month` — Kosten diesen Monat (€)
- `sensor.flipsiforge_prints_total` — Gesamtzahl Drucke

### HA Add-on (Docker Container)

```yaml
# config.yaml
name: FlipsiForge
version: "1.0.0"
slug: flipsiforge
description: FlipsiForge 3D printer management server
arch:
  - aarch64
  - amd64
startup: services
boot: auto
webui: "http://[HOST]:[PORT:8080]"
ports:
  8080/tcp: 8080
ingress: true
ingress_port: 8080
options:
  api_key: ""
  sync_interval: 3600
schema:
  api_key: str
  sync_interval: int
map:
  - share:rw
  - media:rw
```

**.NET auf HA Alpine:** `dotnet publish -r linux-musl-arm64 --self-contained` (Alpine nutzt musl libc → `linux-musl-*` RIDs).

---

## 11. Push-Notifications

### Telegram (Priorität 1)

```csharp
// NuGet: Telegram.Bot 22.*
using Telegram.Bot;

public class TelegramNotifier
{
    private readonly TelegramBotClient _bot;

    public TelegramNotifier(string botToken, long chatId)
    {
        _bot = new TelegramBotClient(botToken);
        ChatId = chatId;
    }

    public long ChatId { get; }

    public async Task NotifyPrintFinishedAsync(string printerName, string fileName, TimeSpan duration, decimal cost)
    {
        var msg = $"🖨️ Druck fertig!\n\n" +
                  $"Drucker: {printerName}\n" +
                  $"Datei: {fileName}\n" +
                  $"Dauer: {duration:hh\\:mm\\:ss}\n" +
                  $"Kosten: €{cost:F2}";
        await _bot.SendTextMessageAsync(ChatId, msg);
    }

    public async Task NotifyErrorAsync(string printerName, string error)
    {
        var msg = $"⚠️ Fehler an {printerName}!\n\n{error}";
        await _bot.SendTextMessageAsync(ChatId, msg, Telegram.Bot.Types.Enums.ParseMode.Markdown);
    }
}
```

### Weitere Notification Channels

- **Web-Push:** `Web-push-csharp` (VAPID keys, Browser-Benachrichtigung)
- **Email:** `MailKit` (SMTP)
- **Desktop-Notification:** Avalonia native (`INotificationService`)

---

## 12. NuGet Package Übersicht

| Need | Package | Version |
|------|---------|---------|
| UI Framework | `Avalonia` + `Avalonia.Desktop` + `Avalonia.Themes.Fluent` | 12.0.5 |
| MVVM | `CommunityToolkit.Mvvm` | 8.4.2 |
| DataGrid | `Avalonia.Controls.DataGrid` | 12.0.1 |
| i18n | `Lang.Avalonia` + `Lang.Avalonia.Json` | 12.1.0.1 |
| SQLite | `Microsoft.Data.Sqlite` + `SQLitePCLRaw.bundle_e_sqlite3` | 10.0.x |
| SQLite EF Core | `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.9 |
| PostgreSQL EF Core | `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.x |
| EF Core Tooling | `Microsoft.EntityFrameworkCore.Design` | 10.0.x |
| 3D Rendering | `Silk.NET` + `Silk.NET.OpenGL` | 2.x |
| G-code Visualization | `SkiaSharp` | 2.88.x |
| Image Processing | `SixLabors.ImageSharp` | 3.x |
| QR Codes | `QRCoder` | 1.8.0 |
| QR Scanning | `ZXing.Net` | 0.16.x |
| NFC (OpenPrintTag) | `PCSC` + `PCSC.Iso7816` | 7.0.x |
| CBOR (OpenPrintTag) | `System.Formats.Cbor` | built-in |
| Fuzzy Search | `Raffinert.FuzzySharp` | 5.0.3 |
| USB-Serial (Marlin) | `System.IO.Ports` | 9.0.x |
| MQTT (Bambu) | `MQTTnet` | 4.x |
| Video/RTSP | `LibVLCSharp.Avalonia` | 3.x |
| Logging | `Serilog` + `Serilog.Sinks.File` + `Serilog.Sinks.Console` | 4.x / 6.x |
| mDNS Discovery | `Makaretu.Dns.Multicast` | 1.6.x |
| Plugin System | `System.Composition` (MEF2) | 10.0.x |
| Telegram Bot | `Telegram.Bot` | 22.x |
| Nextcloud WebDAV | DIY `HttpClient` | — |
| Google Drive | `Google.Apis.Drive.v3` | latest |
| OneDrive | `Microsoft.Graph` + `Microsoft.Identity.Client` | latest |
| Dropbox | `Dropbox.Api` | 7.0.0+ |
| ASP.NET Core | `Microsoft.AspNetCore.App` | 10.0.x |

---

## 13. Bekannte Risiken & Mitigation

| Risiko | Status | Mitigation |
|--------|--------|------------|
| **Printables API** | ❌ Keine öffentliche API | HTML Scraping (fragil) oder Prusa-Partnerschaft. Fallback: Browser öffnen + Download-Ordner überwachen |
| **MakerWorld API** | ❌ Keine offizielle API | Reverse-engineered von `kloshi-io/makerworld-api-reverse`. Port nach C#. Reason Codes für UX. Aggressives Caching |
| **OpenPrintTag C# SDK** | ❌ Nicht existent | Eigenen `OpenPrintTagCodec` schreiben (CBOR ↔ C#). ~1-2 Tage Aufwand |
| **ISO 15693 in C#** | ⚠️ Fiddly | PC/SC via `pcsc-sharp`. Raw APDUs für ICODE SLIX2. Alternative: NFC.cool Handy-App als Bridge |
| **Dropbox SDK** | ⚠️ v7+ Pflicht | Alte Versionen kaputt seit Jan 2026. `dotnet add package Dropbox.Api` ≥7.0.0 |
| **HA Alpine + .NET** | ⚠️ musl libc | `dotnet publish -r linux-musl-arm64 --self-contained` für Alpine-basierte HA Add-ons |
| **Plugin Sicherheit** | ⚠️ ALC ≠ Security | Trusted Plugins in-process (MEF/ALC). Untrusted Plugins → separater Prozess (gRPC/IPC) |
| **STL Thumbnails auf Pi** | ⚠️ Keine GPU | Software-Rendering (Silk.NET windowless context oder custom rasterizer). ImageSharp für 2D-Output |
| **Kosten-Defaults** | ⚠️ Werden stale | "Zuletzt geprüft" Datum pro Default-Wert. User kann überschreiben. `CostDefaults` Tabelle in DB |
| **Multi-User SQLite** | ❌ Single Writer | Desktop = SQLite (OK, Single-User). Server = PostgreSQL (MVCC, Multi-Writer) |

---

## Referenz-Projekte für Implementierung

| Projekt | Was wir lernen |
|---------|---------------|
| **FlipsiColor.Avalonia** | Projekt-Struktur, Avalonia MVVM, i18n JSON, Installer |
| **FlipsiSort** | File-Scanner, Datei-Indexierung, Einstellungen-Versionierung |
| **Mainsail** | Moonraker API Patterns, Dashboard UI |
| **OctoPrint** | Plugin-System Architektur, G-code Viewer, Timelapse |
| **Spoolman** | Filament-DB Schema, QR-Codes, NFC |
| **FDM Monster** | Multi-Protokoll (Moonraker + OctoPrint + PrusaLink + Bambu) |
| **Manyfold** | STL-Viewer, Tags, Modell-Verwaltung |
| **StlVault** | C# STL-Rendering (gleiche Sprache!) |