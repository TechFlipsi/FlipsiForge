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

### 3.9 Format-Filter Badges

Zeigt oben in der Leiste die Anzahl pro Dateiformat als klickbare Badges:

```csharp
public class FormatFilterBar
{
    // Live-Zähler pro Format
    public Dictionary<string, int> FormatCounts { get; } = new()
    {
        ["STL"] = 0, ["3MF"] = 0, ["G-code"] = 0, ["OBJ"] = 0,
        ["STEP"] = 0, ["PLY"] = 0, ["AMF"] = 0, ["X3D"] = 0
    };

    public string? ActiveFilter { get; set; }  // null = alle, "STL" = nur STL

    // Wird beim Scannen live aktualisiert
    public void OnFileFound(ScannedFile file)
    {
        var format = GetFormatName(file.Extension);
        if (FormatCounts.ContainsKey(format))
            FormatCounts[format]++;
    }

    // Filter anwenden
    public IEnumerable<ScannedFile> ApplyFilter(IEnumerable<ScannedFile> files)
    {
        if (string.IsNullOrEmpty(ActiveFilter))
            return files;  // "Alle" — kein Filter
        return files.Where(f => GetFormatName(f.Extension) == ActiveFilter);
    }
}
```

**UI (Avalonia AXAML):**
```xml
<!-- Format-Filter Leiste oben -->
<StackPanel Orientation="Horizontal" Classes="format-filter-bar">
    <Button Content="Alle" Command="{Binding SetFormatFilterCommand}" CommandParameter="{x:Null}"
            Classes.active="{Binding IsAllActive}" />
    <Button Content="STL: 200" Command="{Binding SetFormatFilterCommand}" CommandParameter="STL"
            Classes.active="{Binding IsStlActive}" IsVisible="{Binding HasStl}" />
    <Button Content="3MF: 50" Command="{Binding SetFormatFilterCommand}" CommandParameter="3MF"
            Classes.active="{Binding Is3mfActive}" IsVisible="{Binding Has3mf}" />
    <Button Content="G-code: 120" Command="{Binding SetFormatFilterCommand}" CommandParameter="G-code"
            Classes.active="{Binding IsGcodeActive}" IsVisible="{Binding HasGcode}" />
    <Button Content="OBJ: 30" Command="{Binding SetFormatFilterCommand}" CommandParameter="OBJ"
            Classes.active="{Binding IsObjActive}" IsVisible="{Binding HasObj}" />
    <!-- ... weitere Formate -->
</StackPanel>
```

### 3.10 KI-Suche (immer Dateinamen + KI kombiniert, lokal eingebettet)

Suche läuft **immer beides gleichzeitig** — Dateinamen-Suche (sofort, offline) + KI-Suche (lokal eingebettet, versteht Bedeutung). Kein Ollama, kein externer Service. KI ist direkt in die App eingebettet (ONNX Runtime), wie FlipsiSort/FlipsiColor ihre KI eingebettet haben.

**KI-Treffer sind gekennzeichnet** — jedes KI-Ergebnis hat ein "🤖 KI" Badge damit User sofort sieht woher der Treffer kommt.

```csharp
// Lokale KI — ONNX Runtime, kein Ollama, kein externer Service
// Kleines quantisiertes Modell (z.B. all-MiniLM-L6-v2 quantized, ~23MB)
// Wird mit der App ausgeliefert, kein Download nötig
using Microsoft.ML.OnnxRuntime;

public class LocalEmbeddingModel
{
    private readonly InferenceSession _session;

    public LocalEmbeddingModel(string modelPath = "Assets/ai/all-MiniLM-L6-v2-q8.onnx")
    {
        // ONNX Modell wird mit der App gebündelt
        _session = new InferenceSession(modelPath);
    }

    // Text → Vektor (Embedding) für Ähnlichkeitssuche
    public float[] Embed(string text)
    {
        var tokens = Tokenize(text);
        var input = new DenseTensor<long>(tokens, new[] { 1, tokens.Length });
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", input),
            NamedOnnxValue.CreateFromTensor("attention_mask", CreateAttentionMask(tokens))
        };
        using var results = _session.Run(inputs);
        return results.First().AsTensor<float>().ToArray();
    }
}

public class FileSearchService
{
    private readonly LocalEmbeddingModel _embedder;  // immer lokal
    private readonly HttpClient? _externalAi;        // optional, nur wenn User konfiguriert
    private readonly string? _externalModel;

    // Datei-Embeddings werden beim Scannen generiert und gespeichert
    private readonly Dictionary<string, float[]> _fileEmbeddings = new();

    // === Einzige Such-Methode: immer Dateinamen + KI kombiniert ===
    public async Task<SearchResults> SearchAsync(
        string query,
        IEnumerable<ScannedFile> files)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new SearchResults { AllFiles = files.ToList() };

        var fileList = files.ToList();

        // 1. Dateinamen-Suche (sofort, offline, Fuzzy) — läuft synchron
        var filenameHits = Process.ExtractAllBy(query, fileList,
            f => $"{f.FileName} {f.Tags} {f.Notes}", cutoff: 60)
            .Select(r => (r.Value, r.Score, Source: "filename"))
            .ToList();

        // 2. KI-Suche (lokal eingebettet) — läuft parallel
        var aiTask = Task.Run(() => AiSearchLocalAsync(query, fileList));

        // Dateinamen-Treffer sofort anzeigen
        var aiHits = await aiTask;

        // 3. Ergebnisse zusammenführen — Dateinamen-Treffer zuerst, dann KI-Treffer
        var seen = new HashSet<string>();
        var combined = new List<SearchResult>();

        foreach (var (file, score, source) in filenameHits.OrderByDescending(x => x.Score))
        {
            if (seen.Add(file.Path))
                combined.Add(new SearchResult(file, score, source));
        }

        foreach (var (file, score, source) in aiHits.OrderByDescending(x => x.Score))
        {
            if (seen.Add(file.Path))
                combined.Add(new SearchResult(file, score, source));
        }

        return new SearchResults { Files = combined };
    }

    // KI-Suche — lokal via Embedding-Ähnlichkeit (kein Ollama!)
    private async Task<List<(ScannedFile Value, float Score, string Source)>> AiSearchLocalAsync(
        string query, List<ScannedFile> files)
    {
        try
        {
            // Query → Embedding (lokal, ONNX)
            var queryEmbedding = _embedder.Embed(query);

            var results = new List<(ScannedFile, float, string)>();

            foreach (var file in files)
            {
                // Datei-Embedding aus Cache (beim Scannen generiert)
                if (!_fileEmbeddings.TryGetValue(file.Path, out var fileEmbedding))
                {
                    // Falls noch nicht indexiert → on-the-fly embedden
                    fileEmbedding = _embedder.Embed($"{file.FileName} {file.Tags} {file.Notes}");
                    _fileEmbeddings[file.Path] = fileEmbedding;
                }

                // Cosine Similarity zwischen Query und Datei
                var similarity = CosineSimilarity(queryEmbedding, fileEmbedding);

                if (similarity > 0.3f)  // Threshold für "passend"
                {
                    results.Add((file, similarity, "ai"));
                }
            }

            return results;
        }
        catch
        {
            // Falls lokales Modell nicht verfügbar → nur Dateinamen-Suche
            return new List<(ScannedFile, float, string)>();
        }
    }

    // Optional: externe KI-Anbieter (nur wenn User in Einstellungen konfiguriert)
    private async Task<List<(ScannedFile, float, string)>> AiSearchExternalAsync(
        string query, List<ScannedFile> files)
    {
        if (_externalAi == null)
            return new List<(ScannedFile, float, string)>();  // nicht konfiguriert

        // LLM-basierte Suche (z.B. OpenAI, Anthropic)
        // Nur wenn User explizit einen Anbieter in Einstellungen eingetragen hat
        // ... (wie vorheriges Prompt-basiertes Pattern)
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
    }
}

public record SearchResults
{
    public List<ScannedFile>? AllFiles { get; init; }
    public List<SearchResult>? Files { get; init; }
}

public record SearchResult(ScannedFile File, float Score, string Source)
{
    public bool IsFilenameMatch => Source == "filename";
    public bool IsAiMatch => Source == "ai";
    public string BadgeText => Source switch
    {
        "filename" => "",           // kein Badge für Dateinamen-Treffer
        "ai" => "🤖 KI",           // KI-Treffer Badge
        _ => ""
    };
}
```

**UI Verhalten:**
1. User tippt "Drache" in Suchfeld
2. **Sofort** (0ms): Dateinamen-Treffer erscheinen — `drache.stel`, `drachen_burg.stl` (kein Badge)
3. **Nach ~100ms**: KI-Treffer werden ergänzt — `dragon_v2.stl` 🤖 KI, `mythical_creature.3mf` 🤖 KI
4. KI-Treffer haben ein "🤖 KI" Badge rechts neben dem Dateinamen
5. User sieht sofort: "ah, dragon_v2.stl ist ein KI-Treffer — heißt nicht Drache aber die KI sagt es passt"

**Lokale KI — kein Ollama, kein externer Service:**
```csharp
// IMMER lokal — ONNX Runtime, mit App gebündelt
// Modell: all-MiniLM-L6-v2 quantized (~23MB) oder ähnlich
// Kein Ollama, kein Python, kein extra Prozess, kein Internet
var search = new FileSearchService(
    embedder: new LocalEmbeddingModel()  // ONNX, eingebettet
);

// Optional: User kann in Einstellungen externen Anbieter konfigurieren
// Wenn nicht konfiguriert → lokale KI wird verwendet
var searchWithExternal = new FileSearchService(
    embedder: new LocalEmbeddingModel(),
    externalAi: new HttpClient { BaseAddress = new("https://api.openai.com/v1") },
    externalModel: "gpt-4o-mini"  // nur wenn User es will
);
```

**Warum ONNX und nicht Ollama:**
- Kein extra Service der laufen muss (Ollama = separater Prozess)
- Kein Port, keine Konfiguration, kein Installationsaufwand für User
- Modell wird mit App gebündelt (.onnx Datei in Assets/)
- FlipsiSort und FlipsiColor machen es ähnlich — KI ist eingebettet, nicht extern
- ONNX Runtime ist cross-platform (Windows + Linux), NuGet verfügbar
- ~23MB Modellgröße (quantized) — kein Problem für Installer/Portable

**NuGet:** `Microsoft.ML.OnnxRuntime` (cross-platform, ARM64 + x64)

**KI-Anbieter Schnittstelle (optional, in Einstellungen):**
```csharp
public interface IAiProvider
{
    Task<float[]> EmbedAsync(string text);      // für Embedding-Suche
    Task<string> CompleteAsync(string prompt);  // für komplexe Empfehlungen
}

// Implementierungen:
public class LocalOnnxProvider : IAiProvider { ... }     // Default — immer verfügbar
public class OpenAiProvider : IAiProvider { ... }         // Optional — User konfiguriert
public class AnthropicProvider : IAiProvider { ... }     // Optional
public class OllamaProvider : IAiProvider { ... }         // Optional — falls User Ollama hat

// In Einstellungen:
// [ ] KI-Anbieter: ( ) Lokal (Standard)  ( ) OpenAI  ( ) Anthropic  ( ) Ollama  ( ) Custom
```

**Beispiel KI-Suche mit Badge:**

| Eingabe | Dateinamen-Treffer (sofort, kein Badge) | KI-Treffer (mit 🤖 KI Badge) |
|---------|---------------------------------------|------------------------------|
| "Drache" | `drache.stl`, `drachen_burg.stl` | `dragon_v2.stl` 🤖 KI, `mythical_creature.3mf` 🤖 KI, `wyvern_print.gcode` 🤖 KI |
| "Auto" | `auto.stl` | `car_body.stl` 🤖 KI, `bmw_m3.3mf` 🤖 KI, `truck_wheel.stl` 🤖 KI |
| "Düse" | `düse.stl` | `nozzle_v6.stl` 🤖 KI, `hotend_block.stl` 🤖 KI |
| "klein" | — (nichts) | Alle kleinen Modelle 🤖 KI (Embedding erkennt Geometrie-Kontext) |

**Performance:**
- Embedding-Suche ist **schneller** als LLM-Prompt — nur Vektor-Multiplikation
- Datei-Embeddings werden beim Scannen generiert und gecacht (SQLite)
- Suche dauert ~50-100ms (ONNX Inference für Query, dann Cosine Similarity)
- Kein Warten auf LLM-Antwort — Embeddings sind bereits berechnet
- Bei > 2000 Dateien: Vektor-Index (FAISS oder simple SQLite Vektor-Suche)

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

## 12. KI-Assistent — Filament & Settings Empfehlung

### Konzept

Eine kleine KI-Integration die dem User hilft, die richtigen Einstellungen für seine Datei zu finden. Hybrid-System: regelbasierte Datenbank + LLM für komplexe Empfehlungen.

### Warum kein trainiertes Modell existiert

Stand Juli 2026 gibt es **kein** speziell für 3D-Druck-Einstellungen trainiertes Modell. Aber:
- **Slicer Copilot** (github.com/pfrankov/slicer-copilot, 11★, Apache-2.0) zeigt dass LLMs das können — er nutzt GPT-4o um .3mf-Projekte zu analysieren und Settings zu optimieren. Funktioniert auch mit lokalem LLM.
- **LLM-3D Print** (arxiv.org/abs/2408.14307) — wissenschaftliche Studie: LLMs können ohne Fine-Tuning 3D-Druck-Parameter optimieren und Defekte erkennen.
- **Cura AI Plugins** — Cura hat AI-basierte Empfehlungs-Plugins die Modelle analysieren.

### Architektur: Hybrid-System

```
┌──────────────────────────────────────────────────┐
│              KI-Assistent Pipeline                │
│                                                  │
│  1. STL analysieren                               │
│     → Wandstärke, Overhangs, Größe, Detailgrad    │
│     → Mesh.Statistics (aus FileScanner)           │
│                                                  │
│  2. Regelbasierte Empfehlung (schnell, offline)   │
│     → Material → Standard-Temperatur/Speed        │
│     → Drucker → Build-Volume-Check                │
│     → Datei-Größe → Layer-Höhe-Vorschlag          │
│                                                  │
│  3. LLM-Empfehlung (optional, komplex)            │
│     → Geometrie-Analyse ("dünne Wände → PETG")    │
│     → Ziel-Modus ("Festigkeit" vs "Speed")        │
│     → Slicer-Profil-Generierung                   │
│     → Erklärung WARUM                             │
│                                                  │
│  4. Output                                        │
│     → Empfohlene Spule aus Inventar               │
│     → Komplettes Slicer-Profil (JSON)             │
│     → Warnungen (falls Settings kritisch)         │
│     → Begründung (Text)                           │
└──────────────────────────────────────────────────┘
```

### Regelbasierte Datenbank (Teil 1 — immer offline verfügbar)

```csharp
public static readonly Dictionary<string, PrintSettings> MaterialDefaults = new()
{
    ["PLA"] = new()
    {
        HotendMin = 190, HotendMax = 220, HotendOptimal = 210,
        BedMin = 40, BedMax = 70, BedOptimal = 60,
        MaxSpeed = 150, OptimalSpeed = 60,
        FanPercent = 100,
        RetractionMm = 0.8m,
        Notes = "Einfach zu drucken, gut für Anfänger. Keine warping issues."
    },
    ["PETG"] = new()
    {
        HotendMin = 220, HotendMax = 250, HotendOptimal = 240,
        BedMin = 60, BedMax = 90, BedOptimal = 80,
        MaxSpeed = 100, OptimalSpeed = 50,
        FanPercent = 50,
        RetractionMm = 1.5m,
        Notes = "Stärker als PLA, flexibler. Achtung: Stringing bei zu heiß."
    },
    ["TPU"] = new()
    {
        HotendMin = 210, HotendMax = 240, HotendOptimal = 225,
        BedMin = 30, BedMax = 60, BedOptimal = 50,
        MaxSpeed = 40, OptimalSpeed = 25,
        FanPercent = 50,
        RetractionMm = 0,
        Notes = "Flexibel. Sehr langsam drucken. Retraction aus (0mm)."
    },
    ["ABS"] = new()
    {
        HotendMin = 230, HotendMax = 260, HotendOptimal = 245,
        BedMin = 90, BedMax = 110, BedOptimal = 100,
        MaxSpeed = 80, OptimalSpeed = 50,
        FanPercent = 30,
        RetractionMm = 1.0m,
        Notes = "Warping! Geschlossener Drucker empfohlen. Bett heiß."
    },
    ["ASA"] = new()
    {
        HotendMin = 235, HotendMax = 260, HotendOptimal = 250,
        BedMin = 90, BedMax = 110, BedOptimal = 100,
        MaxSpeed = 80, OptimalSpeed = 50,
        FanPercent = 30,
        RetractionMm = 1.0m,
        Notes = "UV-resistent. Wie ABS aber wetterfest. Warping möglich."
    },
    ["PC"] = new()
    {
        HotendMin = 260, HotendMax = 300, HotendOptimal = 280,
        BedMin = 100, BedMax = 120, BedOptimal = 110,
        MaxSpeed = 60, OptimalSpeed = 40,
        FanPercent = 30,
        RetractionMm = 1.0m,
        Notes = "Sehr hitzebeständig. Braucht heißen Drucker. Enclosure Pflicht."
    },
    ["PA6"] = new()
    {
        HotendMin = 250, HotendMax = 290, HotendOptimal = 270,
        BedMin = 80, BedMax = 100, BedOptimal = 90,
        MaxSpeed = 60, OptimalSpeed = 40,
        FanPercent = 40,
        RetractionMm = 1.0m,
        Notes = "Nylon. Muss vor dem Druck getrocknet werden! Zieht Feuchtigkeit."
    },
};

public record PrintSettings
{
    public int HotendMin { get; init; }
    public int HotendMax { get; init; }
    public int HotendOptimal { get; init; }
    public int BedMin { get; init; }
    public int BedMax { get; init; }
    public int BedOptimal { get; init; }
    public int MaxSpeed { get; init; }       // mm/s
    public int OptimalSpeed { get; init; }   // mm/s
    public int FanPercent { get; init; }
    public decimal RetractionMm { get; init; }
    public string Notes { get; init; } = "";
}
```

### LLM-Empfehlung (Teil 2 — optional, für komplexe Analyse)

```csharp
public class AIPrintAdvisor
{
    private readonly HttpClient _llm;  // Ollama local API or cloud
    private readonly string _model;    // "gemma4:12b" (local) or "gpt-4o-mini" (cloud)

    public async Task<PrintRecommendation> RecommendAsync(
        MeshAnalysis mesh,       // Wandstärke, Overhangs, Größe, Detailgrad
        FilamentSpool filament,   // Ausgewähltes Filament (oder null wenn keins gewählt)
        PrinterProfile printer,  // Ausgewählter Drucker
        PrintGoal goal,           // Strength, Speed, Quality, Prototype
        string? useCase,          // Optional: "Auto-Innenraum", "Außen", "Dekoration", etc.
        List<FilamentSpool> inventory)  // Komplettes Filament-Inventar
    {
        var inventoryText = FormatInventory(inventory);
        var useCaseText = string.IsNullOrWhiteSpace(useCase) ? "nicht angegeben" : useCase;

        var prompt = $"""
        Du bist ein 3D-Druck-Experte. Empfiehl Druck-Einstellungen.

        Modell-Analyse:
        - Minimale Wandstärke: {mesh.MinWallThickness}mm
        - Maximale Overhang: {mesh.MaxOverhang}°
        - Build-Volume: {mesh.BoundingBox}
        - Detailgrad: {mesh.DetailLevel} (high/medium/low)
        - Triangle Count: {mesh.TriangleCount}

        Verwendungszweck: {useCaseText}
        (z.B. "Auto-Innenraum" → hitzebeständig/UV-stabil, "Außen" → wetterfest,
        "Dekoration" → optische Qualität, "Funktionsbauteil" → fest/verschleißfest,
        "flexible Dichtung" → flexibel, "Prototyp" → schnell/billig)

        Filament (vom User gewählt): {filament?.MaterialType ?? "keins"} ({filament?.Brand} {filament?.MaterialName})

        Verfügbares Filament-Inventar:
        {inventoryText}

        Drucker: {printer.Name}
        Build-Volume: {printer.BuildVolumeX}×{printer.BuildVolumeY}×{printer.BuildVolumeZ}mm
        Düse: {printer.NozzleDiameter}mm
        Geschlossen: {printer.IsEnclosed} (ja/nein — wichtig für ABS/ASA!)

        Ziel: {goal}

        Aufgabe:
        1. Ist das gewählte Filament geeignet für diesen Verwendungszweck?
        2. Wenn nicht: Welches aus dem Inventar wäre besser? (mit Begründung)
        3. Wenn kein passendes im Inventar: Welches sollte gekauft werden?
        4. Empfiehl:
           - Hotend-Temperatur (°C)
           - Bett-Temperatur (°C)
           - Layer-Höhe (mm)
           - Druckgeschwindigkeit (mm/s)
           - Retraction (mm)
           - Cooling-Fan (%)
           - Infill-Dichte (%) und Pattern
        5. Begründung für jede Empfehlung
        6. Warnungen (z.B. "ABS braucht geschlossenen Drucker — deiner ist offen")

        Antworte als JSON.
        """;

        var response = await _llm.PostAsJsonAsync("/api/chat", new
        {
            model = _model,
            messages = new[] { new { role = "user", content = prompt } },
            format = "json",
            stream = false
        });

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        return ParseRecommendation(result.Message.Content);
    }

    private string FormatInventory(List<FilamentSpool> inventory)
    {
        if (inventory == null || !inventory.Any())
            return "  (Inventar leer — kein Filament vorhanden)";

        return string.Join("\n", inventory
            .Where(s => s.Status == SpoolStatus.Active)
            .Select(s => $"  - {s.Brand} {s.MaterialName} ({s.MaterialType}, {s.ColorHex}, {s.RemainingWeightG:F0}g übrig, {s.DiameterMm}mm)"));
    }
}

public enum PrintGoal { MaximumStrength, FastPrint, VisualQuality, Prototype }
```

### Lokale Ausführung (Ollama)

```csharp
// Läuft komplett offline wenn Ollama installiert ist
// Modelle: gemma4:12b (sehr gut auf Deutsch), qwen3.5:14b, oder kleiner
var advisor = new AIPrintAdvisor(
    new HttpClient { BaseAddress = new("http://localhost:11434") },
    model: "gemma4:12b"
);

// Oder Cloud (optional, nur wenn User es will)
var cloudAdvisor = new AIPrintAdvisor(
    new HttpClient { BaseAddress = new("https://api.openai.com/v1") },
    model: "gpt-4o-mini"
);
```

### Mitgelieferte Offline-Datenbanken (für KI ohne Internet)

Damit die KI auch ohne Internet Tipps geben kann, werden zwei Datenbanken mitgeliefert:

#### Filament-Marken-Datenbank

Empfohlene Druck-Einstellungen pro Hersteller und Material — direkt von den Hersteller-Spec-Sheets. **User kann eigene Marken/Produkte hinzufügen** — die Datenbank ist erweiterbar.

```csharp
// Datenbank ist eine Liste, nicht statisch — User kann Einträge hinzufügen/bearbeiten/löschen
// Gespeichert in SQLite als eigene Tabelle (FilamentBrandSpecs)
// Beim ersten Start wird die Datenbank mit vordefinierten Werten befüllt (Seed)

public class FilamentBrandSpec
{
    public int Id { get; set; }
    public string Brand { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string MaterialType { get; set; } = "";    // PLA, PETG, TPU, ABS, ASA, PC, PA6, etc.

    // Temperaturen
    public int HotendMin { get; set; }
    public int HotendMax { get; set; }
    public int HotendOptimal { get; set; }
    public int BedMin { get; set; }
    public int BedMax { get; set; }
    public int BedOptimal { get; set; }

    // Speed
    public int SpeedMin { get; set; }       // mm/s — langsamster empfohlener Speed
    public int SpeedMax { get; set; }       // mm/s — schnellster empfohlener Speed
    public int SpeedOptimal { get; set; }   // mm/s — optimaler Speed
    public int OuterWallSpeed { get; set; } // mm/s — Außenwand (langsamer für Qualität)
    public int InfillSpeed { get; set; }    // mm/s — Infill (schneller möglich)

    // Druck-Einstellungen
    public int FanPercent { get; set; }
    public decimal RetractionMm { get; set; }
    public decimal? PressureAdvance { get; set; }
    public decimal LayerHeightMin { get; set; }    // mm — minimal empfohlene Layer-Höhe
    public decimal LayerHeightMax { get; set; }    // mm — maximal empfohlene Layer-Höhe
    public decimal LayerHeightOptimal { get; set; } // mm — optimale Layer-Höhe

    // Filament-Eigenschaften (für KI-Empfehlungen offline)
    public bool IsUVResistant { get; set; }        // UV-beständig (Außenbereich)
    public bool IsWeatherResistant { get; set; }    // Wetterfest (Regen, Sonne)
    public bool IsFoodSafe { get; set; }            // Lebensmittelecht
    public bool IsFlexible { get; set; }            // Flexibel (Dichtungen, etc.)
    public bool IsAbrasive { get; set; }            // Schleifend (CF, GF, Holz — harte Düse nötig!)
    public bool IsHeatResistant { get; set; }       // Hitzebeständig (Auto-Innenraum, etc.)
    public int MaxServiceTempC { get; set; }        // °C — max Dauer-Temperaturbelastung des fertigen Teils
    public bool NeedsEnclosure { get; set; }         // Geschlossener Drucker nötig (ABS, ASA, PC)
    public bool NeedsDirectDrive { get; set; }       // Direct Drive Extruder nötig (TPU)
    public bool NeedsDryingBeforePrint { get; set; } // Muss vor Druck getrocknet werden (Nylon, PETG)
    public int DryingTempC { get; set; }             // °C — empfohlene Trocknungstemperatur
    public int DryingDurationH { get; set; }         // Stunden — empfohlene Trocknungsdauer
    public bool IsBiodegradable { get; set; }        // Biologisch abbaubar (PLA)
    public bool IsRecyclable { get; set; }           // Recyclbar
    public bool WarpsEasily { get; set; }            // Warping-Anfällig (ABS, ASA, PC)
    public bool StringsEasily { get; set; }          // Stringing-Anfällig (PETG bei zu heiß)
    public bool IsImpactResistant { get; set; }      // Schlagfest (PLA+, ABS, PETG)
    public decimal TensileStrengthMpa { get; set; }  // Zugfestigkeit in MPa (falls bekannt)
    public decimal DensityGcm3 { get; set; }          // Dichte g/cm³
    public string SuitableFor { get; set; } = "";     // Wofür geeignet (z.B. "Dekoration, Prototypen, Spielzeug")
    public string NotSuitableFor { get; set; } = "";  // Wofür ungeeignet (z.B. "Außenbereich, Auto, heiße Umgebungen")
    public string Notes { get; set; } = "";           // Marken-spezifische Notizen
    public bool IsUserAdded { get; set; } = false;
}
```

**Vordefinierte Marken (Seed-Daten — alle bekannten Hersteller):**

Jeder Eintrag enthält jetzt **komplette Filament-Eigenschaften** für offline KI-Empfehlungen:

| # | Marke | Produkt | Material | Hotend °C | Bed °C | Fan % | Speed mm/s | Retraction | UV | Wetterfest | Lebensmittelecht | Flexibel | Schleifend | Hitzebeständig | Max Service °C | Enclosure | Direct Drive | Trocknen | Trocknung °C/h | Warping | Stringing | Schlagfest | Zugfestigkeit MPa | Dichte | Wofür geeignet | Wofür ungeeignet |
|---|-------|---------|----------|-----------|--------|-------|------------|------------|-----|-----------|------------------|----------|------------|----------------|---------------|-----------|-------------|----------|----------------|---------|-----------|------------|-------------------|--------|----------------|------------------|
| 1 | eSUN | PLA+ | PLA | 200-230 (215) | 40-60 (50) | 100 | 40-150 (60) | 0.8 | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 55 | ❌ | ❌ | ❌ | — | ❌ | ❌ | ✅ | 50 | 1.24 | Dekoration, Prototypen, Spielzeug, Innenbereich | Außenbereich, Auto, heiße Umgebungen, Geschirr |
| 2 | eSUN | PETG | PETG | 220-250 (235) | 60-90 (80) | 50 | 30-100 (50) | 1.5 | ❌ | ✅ | ✅ | ❌ | ❌ | ✅ | 70 | ❌ | ❌ | ✅ | 65/4h | ❌ | ✅ | ✅ | 55 | 1.27 | Funktionsbauteile, Außenbereich (ohne direkte Sonne), Geschirr, Lagerung | Auto-Innenraum (>70°C), direkte UV-Last |
| 3 | eSUN | ABS+ | ABS | 230-250 (240) | 90-110 (100) | 30 | 30-80 (50) | 1.0 | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | 95 | ✅ | ❌ | ❌ | — | ✅ | ❌ | ✅ | 45 | 1.04 | Auto-Innenraum, Funktionsbauteile, Gehäuse | Außenbereich (UV), Kontakt mit Lebensmitteln |
| 4 | eSUN | TPU 95A | TPU | 210-230 (220) | 30-60 (45) | 50 | 15-40 (25) | 0 | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ | 80 | ❌ | ✅ | ✅ | 50/4h | ❌ | ❌ | ✅ | 30 | 1.21 | Dichtungen, Griffe, Schutzhüllen, flexible Teile | Steife Bauteile, feine Details, High-Speed |
| 5 | eSUN | ASA | ASA | 240-260 (250) | 90-110 (100) | 30 | 30-80 (50) | 1.0 | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | 90 | ✅ | ❌ | ❌ | — | ✅ | ❌ | ✅ | 48 | 1.05 | Außenbereich, Auto-Exterieur, Garten, Wetterfest | Kontakt mit Lebensmitteln, Anfänger (schwierig) |
| 6 | Prusament | PLA | PLA | 190-220 (210) | 50-60 (55) | 100 | 40-150 (80) | 0.8 | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 55 | ❌ | ❌ | ❌ | — | ❌ | ❌ | ❌ | 60 | 1.24 | Dekoration, Prototypen, Figuren, Innenbereich | Außenbereich, Auto, heiße Umgebungen |
| 7 | Prusament | PETG | PETG | 230-245 (240) | 70-90 (80) | 50 | 30-100 (50) | 1.5 | ❌ | ✅ | ✅ | ❌ | ❌ | ✅ | 70 | ❌ | ❌ | ✅ | 65/4h | ❌ | ✅ | ✅ | 55 | 1.27 | Funktionsbauteile, Außen, Geschirr, Lagerung | Auto-Innenraum (>70°C) |
| 8 | Prusament | ASA | ASA | 240-260 (250) | 90-110 (100) | 30 | 30-80 (50) | 1.0 | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | 90 | ✅ | ❌ | ❌ | — | ✅ | ❌ | ✅ | 48 | 1.05 | Außenbereich, Auto, Garten, UV-exponiert | Lebensmittel, Anfänger |
| 9 | Polymaker | CoPA (Nylon) | PA6 | 250-280 (270) | 80-100 (90) | 40 | 20-60 (40) | 1.0 | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | 110 | ✅ | ❌ | ✅ | 80/8h | ✅ | ❌ | ✅ | 70 | 1.52 | Funktionsbauteile, Zahnräder, Verschleißteile, Auto | Außen (UV), feuchtigkeitsempfindlich, Anfänger |
| 10 | Polymaker | PC-Max | PC | 260-300 (280) | 100-120 (110) | 30 | 20-60 (40) | 1.0 | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | 115 | ✅ | ❌ | ✅ | 100/4h | ✅ | ❌ | ✅ | 65 | 1.30 | Hitzeschild, Industrielle Teile, hohe Belastung | Außen (UV), Anfänger, ohne Enclosure |
| 11 | Bambu Lab | PLA Matte | PLA | 190-220 (210) | 35-55 (45) | 100 | 50-200 (120) | 0.8 | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 55 | ❌ | ❌ | ❌ | — | ❌ | ❌ | ❌ | 50 | 1.24 | Dekoration, Figuren, matte Oberflächen, High-Speed | Außen, Auto, Heiß |
| 12 | Bambu Lab | PETG HF | PETG | 220-250 (240) | 60-90 (80) | 50 | 50-200 (100) | 1.5 | ❌ | ✅ | ✅ | ❌ | ❌ | ✅ | 70 | ❌ | ❌ | ✅ | 65/4h | ❌ | ✅ | ✅ | 55 | 1.27 | Funktionsbauteile, High-Speed, Geschirr | Auto-Innenraum (>70°C) |
| 13 | Bambu Lab | ABS | ABS | 230-260 (245) | 90-110 (100) | 30 | 30-80 (50) | 1.0 | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | 95 | ✅ | ❌ | ❌ | — | ✅ | ❌ | ✅ | 45 | 1.04 | Auto-Innenraum, Gehäuse, Funktionsbauteile | Außen (UV), Lebensmittel |
| 14 | Bambu Lab | TPU 95A | TPU | 210-240 (225) | 30-60 (45) | 50 | 15-40 (25) | 0 | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ | 80 | ❌ | ✅ | ✅ | 50/4h | ❌ | ❌ | ✅ | 30 | 1.21 | Dichtungen, Griffe, Schutzhüllen | Steife Bauteile, High-Speed |
| 15 | 3DXTech | Carbon Fiber PLA | PLA | 200-230 (215) | 40-60 (50) | 100 | 30-80 (40) | 0.8 | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | 55 | ❌ | ❌ | ❌ | — | ❌ | ❌ | ✅ | 70 | 1.27 | Funktionsteile, Verschleißteile, steife Bauteile | Außen, Flexible Teile — HARTE DÜSE NÖTIG! |
| 16 | Polymaker | PolyFlex TPU90 | TPU | 220-240 (230) | 30-60 (45) | 50 | 15-40 (25) | 0 | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ | 80 | ❌ | ✅ | ✅ | 50/4h | ❌ | ❌ | ✅ | 28 | 1.21 | Dichtungen, Griffe, flexible Verbindungen | Steife Bauteile, feine Details |
| 17 | ColorFabb | PLA/PHA | PLA | 190-220 (210) | 50-60 (55) | 100 | 40-120 (50) | 0.8 | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 55 | ❌ | ❌ | ❌ | — | ❌ | ❌ | ✅ | 55 | 1.24 | Dekoration, Figuren, impact-resistent | Außen, Auto, Heiß |
| 18 | Siraya Tech | Build | PETG | 230-250 (240) | 70-90 (80) | 50 | 30-80 (50) | 1.5 | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ | 75 | ❌ | ❌ | ✅ | 65/4h | ❌ | ✅ | ✅ | 60 | 1.27 | Engineering-Teile, sehr feste Layer-Haftung | Auto-Innenraum (>75°C) |
| 19 | Fiberlogy | ASA | ASA | 240-260 (250) | 90-110 (100) | 30 | 30-80 (50) | 1.0 | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | 90 | ✅ | ❌ | ❌ | — | ✅ | ❌ | ✅ | 48 | 1.05 | Außenbereich, Auto, UV-exponiert | Lebensmittel, Anfänger |
| 20 | Sunlu | PLA | PLA | 190-220 (210) | 40-60 (50) | 100 | 40-150 (60) | 0.8 | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 55 | ❌ | ❌ | ❌ | — | ❌ | ❌ | ❌ | 50 | 1.24 | Budget Dekoration, Prototypen, Innenbereich | Außen, Auto, Heiß, Qualitäts-kritisch |

*(Einträge 21-79 haben dieselbe Struktur — alle Marken aus der vorherigen Tabelle mit ergänzten Eigenschaften)*

**Vollständige Eigenschaften pro Eintrag (offline verfügbar):**

| Kategorie | Felder | Beispiel |
|-----------|--------|---------|
| **Temperaturen** | Hotend (Min/Max/Optimal), Bed (Min/Max/Optimal) | PLA: 190-220 (210) / 40-60 (50) |
| **Speed** | Min/Max/Optimal, OuterWall, Infill | PLA: 40-150 (60), OuterWall 30, Infill 80 |
| **Druck-Settings** | Fan %, Retraction, Pressure Advance, Layer Height (Min/Max/Optimal) | PLA: 100%, 0.8mm, PA 0.04, 0.12-0.24 (0.16) |
| **UV/Wetter** | IsUVResistant, IsWeatherResistant | ASA: ✅/✅, PLA: ❌/❌ |
| **Sicherheit** | IsFoodSafe, IsBiodegradable, IsRecyclable | PETG: ✅/❌/✅, PLA: ❌/✅/✅ |
| **Mechanisch** | IsFlexible, IsAbrasive, IsImpactResistant, TensileStrength | TPU: ✅/❌/✅/30MPa |
| **Hitze** | IsHeatResistant, MaxServiceTempC | PLA: ❌/55°C, ABS: ✅/95°C, PC: ✅/115°C |
| **Druck-Anforderungen** | NeedsEnclosure, NeedsDirectDrive, NeedsDryingBeforePrint | ABS: ✅/❌/❌, TPU: ❌/✅/✅ |
| **Trocknung** | DryingTempC, DryingDurationH | Nylon: 80°C/8h, PETG: 65°C/4h |
| **Probleme** | WarpsEasily, StringsEasily | ABS: ✅/❌, PETG: ❌/✅ |
| **Verwendung** | SuitableFor, NotSuitableFor | PLA: "Dekoration, Prototypen" / "Außen, Auto, Heiß" |
| **Physikalisch** | DensityGcm3, TensileStrengthMpa | PLA: 1.24/50MPa |

**KI-Nutzen offline:** Die KI kann sofort Antworten geben wie:
- "Ist PLA für Auto-Innenraum?" → ❌ `IsHeatResistant = false`, `MaxServiceTempC = 55` → "Nein, Auto wird >55°C"
- "Ist PETG lebensmittelecht?" → ✅ `IsFoodSafe = true` → "Ja, PETG ist lebensmittelecht"
- "Braucht TPU Direct Drive?" → ✅ `NeedsDirectDrive = true` → "Ja, TPU braucht Direct Drive"
- "Muss Nylon getrocknet werden?" → ✅ `NeedsDryingBeforePrint = true`, `DryingTempC = 80`, `DryingDurationH = 8` → "Ja, 80°C für 8 Stunden"
- "Ist ABS für Außen?" → ❌ `IsUVResistant = false` → "Nein, ABS wird durch UV spröde. ASA nehmen"
- "Welches Filament für Außen?" → Filter: `IsUVResistant = true && IsWeatherResistant = true` → ASA, Prusament ASA, Fiberlogy ASA
- "Max Speed für Bambu PLA Matte?" → `SpeedMax = 200` → "200mm/s (High-Speed optimiert)"
- "Braucht CF-PLA harte Düse?" → ✅ `IsAbrasive = true` → "Ja! Hartmetall oder Rubin-Düse nötig"

All diese Antworten funktionieren **komplett offline** — kein Internet nötig. Wenn Internet verfügbar ist, kann die KI zusätzlich aktuelle Tests/Reviews abrufen.

### Drucker-Wartungs-Empfehlungen (online + offline)

Wartungs-Empfehlungen laufen in zwei Modi:

**Mit Internet:** KI sucht online nach modellspezifischen Wartungs-Tipps für den genauen Drucker (bekannte Probleme, Ersatzteile, Firmware-Updates, Community-Tipps).

**Ohne Internet:** KI gibt allgemeine Wartungs-Empfehlungen die für alle Drucker gelten — basierend auf Druckstunden und Standard-Verschleißteilen.

```csharp
public class PrinterMaintenanceAdvisor
{
    private readonly HttpClient _llm;

    public async Task<MaintenanceRecommendation> GetMaintenanceAsync(
        PrinterProfile printer,
        PrintHistory history,
        bool hasInternet)
    {
        if (hasInternet)
        {
            // Mit Internet: modellspezifische Suche
            return await GetOnlineMaintenanceAsync(printer, history);
        }
        else
        {
            // Ohne Internet: allgemeine Empfehlungen für alle Drucker
            return GetOfflineMaintenance(printer, history);
        }
    }

    // === ONLINE: Modellspezifisch ===
    private async Task<MaintenanceRecommendation> GetOnlineMaintenanceAsync(
        PrinterProfile printer, PrintHistory history)
    {
        var prompt = $"""
        Du bist ein 3D-Drucker-Wartungsexperte. Suche nach Wartungs-Empfehlungen
        für diesen spezifischen Drucker.

        Drucker: {printer.Brand} {printer.Model}
        Firmware: {printer.FirmwareVersion}
        Druckstunden gesamt: {history.TotalPrintHours}
        Materialien verwendet: {history.MaterialsUsed}
        Letzte Wartung: {printer.LastMaintenanceDate}

        Suche online nach:
        1. Bekannte Probleme dieses Drucker-Modells
        2. Empfohlene Ersatzteile/Upgrades für dieses Modell
        3. Firmware-Updates verfügbar?
        4. Community-Tipps für Optimierung dieses Modells
        5. Modellspezifische Wartungs-Intervalle

        Antworte als JSON.
        """;

        var response = await _llm.PostAsJsonAsync("/api/chat", new
        {
            model = _model,
            messages = new[] { new { role = "user", content = prompt } },
            format = "json", stream = false
        });
        return ParseMaintenance(response);
    }

    // === OFFLINE: Allgemeine Empfehlungen für alle Drucker ===
    private MaintenanceRecommendation GetOfflineMaintenance(
        PrinterProfile printer, PrintHistory history)
    {
        var tasks = new List<MaintenanceTask>();
        var hours = history.TotalPrintHours;

        // Düse — alle 300-500h (abhängig von Material)
        if (hours > 300)
        {
            bool abrasiveUsed = history.MaterialsUsed.Any(m =>
                m.Contains("CF") || m.Contains("Carbon") || m.Contains("Wood") ||
                m.Contains("Glass") || m.Contains("Metal"));

            tasks.Add(new MaintenanceTask
            {
                Component = "Düse",
                Action = "Tauschen",
                Reason = abrasiveUsed
                    ? $"Nach {hours}h mit schleifendem Filament (CF/Holz) — Verschleiß deutlich schneller"
                    : $"Nach {hours}h Druckzeit — Düse verschlissen",
                Priority = hours > 500 ? "Hoch" : "Mittel"
            });
        }

        // Bowden-Tube — alle 500h (falls Bowden-Setup)
        if (hours > 500 && !printer.IsDirectDrive)
        {
            tasks.Add(new MaintenanceTask
            {
                Component = "Bowden-Tube",
                Action = "Tauschen oder kürzen",
                Reason = $"Nach {hours}h — Innenseite raut auf, Reibung steigt",
                Priority = "Mittel"
            });
        }

        // Riemen — alle 1000h prüfen
        if (hours > 1000)
        {
            tasks.Add(new MaintenanceTask
            {
                Component = "Zahnriemen (X/Y/Z)",
                Action = "Spannung prüfen, ggf. nachspannen",
                Reason = $"Nach {hours}h — Riemen dehnen sich mit der Zeit",
                Priority = "Mittel"
            });
        }

        // Lager — alle 1500h
        if (hours > 1500)
        {
            tasks.Add(new MaintenanceTask
            {
                Component = "Linearschienen/Lager",
                Action = "Reinigen und ölen",
                Reason = $"Nach {hours}h — Schmiermittel verbraucht, Reibung steigt",
                Priority = "Mittel"
            });
        }

        // Heizbett — Reinigung alle 50 Drucke
        if (history.TotalPrints > 50)
        {
            tasks.Add(new MaintenanceTask
            {
                Component = "Heizbett-Oberfläche",
                Action = "Reinigen (Isopropyl 70%+)",
                Reason = $"Nach {history.TotalPrints} Drucken — Haftung lässt nach",
                Priority = "Niedrig"
            });
        }

        // Extruder-Zahnrad — alle 800h reinigen
        if (hours > 800)
        {
            tasks.Add(new MaintenanceTask
            {
                Component = "Extruder-Zahnrad (Drive Gear)",
                Action = "Reinigen (Filament-Reste entfernen)",
                Reason = $"Nach {hours}h — Zahnrad verstopft mit geschmolzenem Filament",
                Priority = "Mittel"
            });
        }

        // PTFE-Tube im Hotend — alle 500h (falls nicht Direct Drive)
        if (hours > 500 && !printer.IsDirectDrive)
        {
            tasks.Add(new MaintenanceTask
            {
                Component = "PTFE-Tube (Hotend)",
                Action = "Prüfen, ggf. kürzen oder tauschen",
                Reason = $"Nach {hours}h — PTFE schmilzt bei hohen Temperaturen",
                Priority = "Mittel"
            });
        }

        // Lüfter — alle 1000h reinigen
        if (hours > 1000)
        {
            tasks.Add(new MaintenanceTask
            {
                Component = "Hotend-Lüfter & Bauteil-Lüfter",
                Action = "Reinigen (Staub entfernen)",
                Reason = $"Nach {hours}h — Staub blockiert Luftstrom → Überhitzung",
                Priority = "Niedrig"
            });
        }

        // Firmware-Check
        tasks.Add(new MaintenanceTask
        {
            Component = "Firmware",
            Action = "Auf Update prüfen (Internet nötig)",
            Reason = "Regelmäßig Firmware aktualisieren für Bugfixes/Features",
            Priority = "Niedrig"
        });

        return new MaintenanceRecommendation
        {
            OnlineRequired = false,
            IsGeneralAdvice = true,
            Message = "Allgemeine Wartungs-Empfehlungen (ohne Internet). " +
                     "Verbinde dich mit dem Internet für modellspezifische Tipps " +
                     $"für deinen {printer.Brand} {printer.Model}.",
            Tasks = tasks,
            NextMaintenanceDate = DateTime.UtcNow.AddDays(30)
        };
    }
}

public record MaintenanceRecommendation
{
    public bool OnlineRequired { get; init; }
    public bool IsGeneralAdvice { get; init; }  // true = offline allgemeine Tipps
    public string Message { get; init; } = "";
    public List<MaintenanceTask> Tasks { get; init; } = new();
    public List<KnownIssue> KnownIssues { get; init; } = new();
    public List<SparePart> RecommendedParts { get; init; } = new();
    public string? FirmwareUpdateAvailable { get; init; }
    public DateTime? NextMaintenanceDate { get; init; }
}
```

**Allgemeine Wartungs-Intervalle (offline, für alle Drucker):**

| Komponente | Intervall | Aktion | Kriterien |
|------------|-----------|--------|-----------|
| Düse | 300-500h | Tauschen | CF/Holz-Filament → schneller (300h), Standard → 500h |
| Bowden-Tube | 500h | Tauschen/kürzen | Nur bei Bowden-Setup (nicht Direct Drive) |
| PTFE-Tube (Hotend) | 500h | Prüfen/tauschen | Nur bei Nicht-Direct-Drive |
| Extruder-Zahnrad | 800h | Reinigen | Filament-Reste entfernen |
| Riemen (X/Y/Z) | 1000h | Spannung prüfen | Dehnen sich mit der Zeit |
| Lüfter | 1000h | Reinigen | Staub blockiert Luftstrom |
| Lager/Linearschienen | 1500h | Reinigen + ölen | Schmiermittel verbraucht |
| Heizbett | 50 Drucke | Reinigen | Isopropyl 70%+ |
| Firmware | Regelmäßig | Update prüfen | Internet nötig für Download |

**User-Erweiterung:** User kann eigene Marken/Produkte hinzufügen wenn seine Marke nicht in der Datenbank steht. Eigene Einträge werden als `IsUserAdded = true` markiert und können bearbeitet/gelöscht werden. Vordefinierte Einträge können ebenfalls bearbeitet werden (z.B. wenn ein Hersteller seine Empfehlung ändert).

```csharp
public class FilamentBrandManager
{
    private readonly FlipsiForgeDbContext _db;

    // Seed-Datenbank beim ersten Start
    public async Task SeedDatabaseAsync()
    {
        if (await _db.FilamentBrandSpecs.AnyAsync())
            return; // bereits befüllt

        var seeds = new List<FilamentBrandSpec>
        {
            new() { Brand = "eSUN", ProductName = "PLA+", MaterialType = "PLA", HotendMin = 200, HotendMax = 230, HotendOptimal = 215, BedMin = 40, BedMax = 60, BedOptimal = 50, FanPercent = 100, SpeedMin = 40, SpeedMax = 150, SpeedOptimal = 60, RetractionMm = 0.8m, Notes = "Impact-resistenter als Standard PLA" },
            new() { Brand = "eSUN", ProductName = "PETG", MaterialType = "PETG", HotendMin = 220, HotendMax = 250, HotendOptimal = 235, BedMin = 60, BedMax = 90, BedOptimal = 80, FanPercent = 50, SpeedMin = 30, SpeedMax = 100, SpeedOptimal = 50, RetractionMm = 1.5m, Notes = "Stringing bei zu heiß" },
            // ... alle 79 Einträge
        };

        _db.FilamentBrandSpecs.AddRange(seeds);
        await _db.SaveChangesAsync();
    }

    // User fügt eigene Marke hinzu
    public async Task<FilamentBrandSpec> AddCustomBrandAsync(FilamentBrandSpec spec)
    {
        spec.IsUserAdded = true;
        _db.FilamentBrandSpecs.Add(spec);
        await _db.SaveChangesAsync();
        return spec;
    }

    // User bearbeitet existierenden Eintrag
    public async Task UpdateBrandAsync(int id, FilamentBrandSpec updates)
    {
        var spec = await _db.FilamentBrandSpecs.FindAsync(id);
        if (spec == null) return;
        spec.HotendMin = updates.HotendMin;
        spec.HotendMax = updates.HotendMax;
        spec.HotendOptimal = updates.HotendOptimal;
        spec.BedMin = updates.BedMin;
        spec.BedMax = updates.BedMax;
        spec.BedOptimal = updates.BedOptimal;
        spec.FanPercent = updates.FanPercent;
        spec.SpeedOptimal = updates.SpeedOptimal;
        spec.RetractionMm = updates.RetractionMm;
        spec.Notes = updates.Notes;
        await _db.SaveChangesAsync();
    }

    // User löscht eigenen Eintrag (vordefinierte können gelöscht oder zurückgesetzt werden)
    public async Task DeleteBrandAsync(int id)
    {
        var spec = await _db.FilamentBrandSpecs.FindAsync(id);
        if (spec == null) return;
        _db.FilamentBrandSpecs.Remove(spec);
        await _db.SaveChangesAsync();
    }

    // Marke suchen (für KI-Prompt und Auto-Fill beim Spule-Anlegen)
    public async Task<FilamentBrandSpec?> FindBrandAsync(string brand, string materialType)
    {
        return await _db.FilamentBrandSpecs
            .FirstOrDefaultAsync(s => s.Brand == brand && s.MaterialType == materialType);
    }
}
```

**Auto-Fill beim Spule-Anlegen:** Wenn User eine neue Spule anlegt und Marke + Material eingibt, sucht FlipsiForge automatisch in der Marken-Datenbank und füllt die empfohlenen Temperaturen/Speed aus. User kann diese dann überschreiben.

#### Slicer-Einstellungs-Datenbank (OrcaSlicer / PrusaSlicer)

Optimierungs-Tipps und empfohlene Profileinstellungen für verschiedene Szenarien:

```csharp
public static readonly Dictionary<string, SlicerOptimization> SlicerTips = new()
{
    ["FineDetail"] = new()
    {
        LayerHeight = 0.12m,
        WallLineCount = 3,
        InfillDensity = 20,
        InfillPattern = "cubic",
        Speed = 30,
        OuterWallSpeed = 20,
        Notes = "Für Modelle mit feinen Details. Langsam für saubere Oberfläche"
    },
    ["Strength"] = new()
    {
        LayerHeight = 0.16m,
        WallLineCount = 4,
        InfillDensity = 60,
        InfillPattern = "gyroid",
        Speed = 50,
        OuterWallSpeed = 35,
        Notes = "Gyroid Infill ist stärker als Grid bei weniger Material"
    },
    ["Speed"] = new()
    {
        LayerHeight = 0.24m,
        WallLineCount = 2,
        InfillDensity = 10,
        InfillPattern = "lines",
        Speed = 100,
        OuterWallSpeed = 70,
        Notes = "Schnellster Druck. Gut für Prototypen"
    },
    ["OverhangSupport"] = new()
    {
        LayerHeight = 0.16m,
        OverhangThreshold = 45,
        SupportType = "tree",
        SupportDensity = 15,
        Notes = "Tree-Support nutzt weniger Material als normal Support"
    },
    ["TPU_Flex"] = new()
    {
        LayerHeight = 0.20m,
        Speed = 25,
        Retraction = 0,
        Fan = 50,
        PressureAdvance = 0.05m,
        Notes = "Retraction AUS! Pressure Advance niedrig. Direct Drive nötig"
    },
    ["ABS_ASA"] = new()
    {
        LayerHeight = 0.20m,
        Speed = 50,
        Fan = 30,
        ChamberTemp = 40,
        Brim = true,
        Notes = "Brim für bessere Haftung. Enclosure nötig! Fan niedrig"
    },
};

public record SlicerOptimization
{
    public decimal LayerHeight { get; init; }
    public int WallLineCount { get; init; }
    public int InfillDensity { get; init; }
    public string InfillPattern { get; init; } = "";
    public int Speed { get; init; }
    public int OuterWallSpeed { get; init; }
    public int OverhangThreshold { get; init; }
    public string SupportType { get; init; } = "";
    public int SupportDensity { get; init; }
    public decimal Retraction { get; init; }
    public int Fan { get; init; }
    public int ChamberTemp { get; init; }
    public bool Brim { get; init; }
    public decimal PressureAdvance { get; init; }
    public string Notes { get; init; } = "";
}
```

#### Drucker-Info (vom User-Profil, nicht fixierte Liste)

Die KI nutzt die Drucker-Informationen die der User selbst in FlipsiForge angelegt hat (Build-Volume, Düse, Enclosed-Status). Keine fixierte Drucker-Liste — es gibt Tausende Modelle. Der User gibt beim Anlegen an ob sein Drucker geschlossen ist oder nicht (z.B. Snapmaker U1: "Enclosure separat kaufbar" → User kann `IsEnclosed = true` setzen wenn er die Haube hat).

### KI Daten-Zugriff — Was in den Prompt injiziert wird

| Datenquelle | Inhalt | Wann verfügbar |
|-------------|--------|----------------|
| STL-Mesh-Analyse | Wandstärke, Overhangs, Bounding-Box, Detailgrad, Triangle Count | Nach Datei-Scan |
| Filament-Inventar | Alle aktiven Spulen mit Marke, Material, Farbe, Restgewicht, Durchmesser | Immer (lokale DB) |
| Filament-Marken-Datenbank | Hersteller-Empfehlungen: eSUN, Prusament, Polymaker, Bambu, Sunlu, Overture (Temp/Speed/Fan/Retraction pro Produkt) | Immer (eingebaut, offline) |
| Slicer-Einstellungs-Datenbank | OrcaSlicer/PrusaSlicer Optimierungs-Tipps: FineDetail, Strength, Speed, OverhangSupport, TPU, ABS/ASA | Immer (eingebaut, offline) |
| Material-Standard-DB | Standard-Temperaturen/Speed/Fan für 7 Materialtypen (PLA, PETG, TPU, ABS, ASA, PC, PA6) | Immer (eingebaut) |
| User's Drucker-Profil | Build-Volume, Düse, Enclosed (User gibt an), Max-Temp | Immer (vom User angelegt) |
| Druck-Historie | Erfolgsrate pro Material/Drucker, vergangene Einstellungen | Nach ersten Drucken |
| Verwendungszweck | User-Text-Eingabe ("Auto-Innenraum", "Außen", etc.) | Optional, wenn eingegeben |
| Ziel-Modus | Strength/Speed/Quality/Prototype | Optional, wenn gewählt |

**Beispiel-Prompt-Injection:**
```
Verfügbares Filament-Inventar:
  - Prusament PLA Galaxy Black (PLA, #1a1a1a, 850g übrig, 1.75mm)
  - eSUN PETG Schwarz (PETG, #0a0a0a, 750g übrig, 1.75mm)
  - Prusament ABS Orange (ABS, #ff6600, 1000g übrig, 1.75mm)

User's Drucker: Snapmaker U1
Build-Volume: 235×235×275mm
Düse: 0.4mm
Geschlossen: nein
Max Hotend: 300°C
Max Bed: 110°C

Verwendungszweck: Auto-Innenraum
Ziel: Maximale Festigkeit
```

**KI Antwort (Beispiel):**
```json
{
  "filamentOk": false,
  "reason": "PLA ist für Auto-Innenraum ungeeignet — wird bei 50-60°C weich",
  "recommendedFilament": "ABS Orange (Prusament)",
  "alternatives": ["PETG Schwarz (eSUN) — hitzebeständiger als PLA, flexibler als ABS"],
  "buyRecommendation": null,
  "settings": {
    "hotend": 245,
    "bed": 100,
    "layerHeight": 0.16,
    "speed": 50,
    "retraction": 1.0,
    "fan": 30,
    "infill": 60,
    "infillPattern": "gyroid"
  },
  "warnings": [
    "⚠️ ABS braucht geschlossenen Drucker — Snapmaker U1 ist offen! Warping möglich.",
    "⚠️ ABS erzeugt giftige Dämpfe — gut belüften!"
  ],
  "explanation": "ABS ist ideal für Auto-Innenraum (hitzebeständig bis ~100°C). " +
    "PETG wäre die Alternative da flexibler. PLA fällt aus. " +
    " Bett 100°C für Haftung, Fan 30% gegen Warping, " +
    "Layer 0.16mm für Festigkeit, Infill 60% gyroid für maximale Stabilität."
}
```

### Ziel-Modus → Anpassung

| Ziel | Layer | Speed | Infill | Fan | Notes |
|------|-------|-------|--------|-----|-------|
| Maximale Festigkeit | 0.16mm | 40mm/s | 50-80% | 50-80% | Mehr Perimeter, höhere Temp |
| Schneller Druck | 0.24mm | 100mm/s | 10-15% | 100% | Wenig Perimeter, niedrige Temp |
| Optische Qualität | 0.12mm | 30mm/s | 15-20% | 100% | Feine Layer, langsam |
| Prototyp | 0.20mm | 80mm/s | 10% | 100% | Schnell, gut genug zum Testen |

### Referenz-Projekte

| Projekt | Stars | Ansatz |
|---------|-------|--------|
| [Slicer Copilot](https://github.com/pfrankov/slicer-copilot) | 11 | LLM (GPT-4o) analysiert .3mf → optimiert Settings. Goal-oriented. Multi-Language. Apache-2.0 |
| [LLM-3D Print](https://arxiv.org/abs/2408.14307) | — | Wissenschaft: LLMs ohne Fine-Tuning als 3D-Druck-Controller |
| Cura AI Plugins | — | ML-basierte Profil-Empfehlungen in Cura |

---

## 13. NuGet Package Übersicht

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

## 14. Bekannte Risiken & Mitigation

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

## 15. Referenz-Projekte für Implementierung

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