using System.Text.Json;

namespace FlipsiForge.MockMoonraker;

/// <summary>
/// Simuliert einen 3D-Drucker mit State-Machine: idle → heating → printing → complete.
/// Thread-safe, alle Properties mit Lock geschützt.
/// Unterstützt Szenarien: demo, printing, paused, error, offline.
/// </summary>
public sealed class PrinterSimulator
{
    private readonly object _lock = new();
    private Timer? _progressTimer;
    private int _progressPercent;
    private int _currentLayer;
    private int _totalLayers = 150;
    private int _elapsedSec;
    private decimal _hotendTemp = 25m;
    private decimal _targetHotendTemp = 210m;
    private decimal _bedTemp = 25m;
    private decimal _targetBedTemp = 60m;
    private string _state = "ready";
    private string _filename = "demo_print.gcode";
    private readonly string _scenario;

    public PrinterSimulator(string scenario)
    {
        _scenario = scenario;
        ApplyScenario(scenario);
    }

    private void ApplyScenario(string scenario)
    {
        lock (_lock)
        {
            switch (scenario.ToLowerInvariant())
            {
                case "printing":
                    _state = "printing";
                    _progressPercent = 35;
                    _currentLayer = 52;
                    _hotendTemp = 210m;
                    _bedTemp = 60m;
                    _filename = "benchy.gcode";
                    _elapsedSec = 1840;
                    StartProgressTimer();
                    break;
                case "paused":
                    _state = "paused";
                    _progressPercent = 50;
                    _currentLayer = 75;
                    _hotendTemp = 180m;
                    _bedTemp = 55m;
                    _filename = "benchy.gcode";
                    _elapsedSec = 2600;
                    break;
                case "error":
                    _state = "error";
                    _progressPercent = 20;
                    _currentLayer = 30;
                    _hotendTemp = 0m;
                    _bedTemp = 0m;
                    _filename = "failed_print.gcode";
                    _elapsedSec = 800;
                    break;
                case "offline":
                    _state = "shutdown";
                    break;
                case "demo":
                default:
                    _state = "ready";
                    _progressPercent = 0;
                    _currentLayer = 0;
                    _hotendTemp = 25m;
                    _bedTemp = 25m;
                    _filename = "demo_print.gcode";
                    _elapsedSec = 0;
                    break;
            }
        }
    }

    private void StartProgressTimer()
    {
        _progressTimer?.Dispose();
        _progressTimer = new Timer(_ =>
        {
            lock (_lock)
            {
                if (_state != "printing") return;
                _elapsedSec += 5;
                if (_progressPercent < 100)
                {
                    _progressPercent = Math.Min(100, _progressPercent + 1);
                    _currentLayer = Math.Min(_totalLayers, (int)(_totalLayers * _progressPercent / 100m));
                }
                if (_progressPercent >= 100)
                {
                    _state = "ready";
                    _hotendTemp = 25m;
                    _bedTemp = 25m;
                }
            }
        }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    // === API Response Generators ===

    public string GetPrinterInfo()
    {
        lock (_lock)
        {
            var state = _state;
            var stateMsg = state switch
            {
                "ready" => "Printer is ready",
                "printing" => "Printing in progress",
                "paused" => "Print paused",
                "error" => "Thermistor error: heater_bed fault",
                "shutdown" => "Printer is offline",
                _ => "Unknown"
            };

            return JsonSerializer.Serialize(new
            {
                result = new
                {
                    state,
                    state_message = stateMsg,
                    hostname = "mockprinter",
                    software_version = "v0.12.0-mock",
                    klipper_path = "/home/pi/klipper",
                    python = "/opt/klipper-env/bin/python"
                }
            }, new JsonSerializerOptions { WriteIndented = false });
        }
    }

    public string GetObjectsQuery(string query)
    {
        lock (_lock)
        {
            var status = new Dictionary<string, object>();

            if (query.Contains("webhooks"))
            {
                status["webhooks"] = new
                {
                    state = _state == "shutdown" ? "shutdown" : "ready",
                    state_message = _state == "shutdown" ? "Printer is offline" : "Printer is ready"
                };
            }

            if (query.Contains("extruder"))
            {
                status["extruder"] = new
                {
                    temperature = _hotendTemp,
                    target = _state == "printing" || _state == "paused" ? _targetHotendTemp : 0m,
                    power = _state == "printing" ? 0.8m : 0m,
                };
            }

            if (query.Contains("heater_bed"))
            {
                status["heater_bed"] = new
                {
                    temperature = _bedTemp,
                    target = _state == "printing" || _state == "paused" ? _targetBedTemp : 0m,
                    power = _state == "printing" ? 0.6m : 0m,
                };
            }

            if (query.Contains("print_stats"))
            {
                status["print_stats"] = new
                {
                    filename = _filename,
                    total_duration = _elapsedSec,
                    print_duration = (int)(_elapsedSec * 0.93),
                    filament_used = _progressPercent * 58.7m,
                    state = _state == "ready" && _progressPercent > 0 ? "complete" : _state,
                    message = "",
                    info = new { total_layer = _totalLayers, current_layer = _currentLayer }
                };
            }

            if (query.Contains("virtual_sdcard"))
            {
                status["virtual_sdcard"] = new
                {
                    progress = _progressPercent / 100m,
                    is_active = _state == "printing",
                    file_position = (long)(_progressPercent * 4587293 / 100m)
                };
            }

            if (query.Contains("display_status"))
            {
                status["display_status"] = new
                {
                    progress = _progressPercent / 100m,
                    message = (string?)null
                };
            }

            return JsonSerializer.Serialize(new
            {
                result = new
                {
                    eventtime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0,
                    status
                }
            });
        }
    }

    public string GetServerFilesList()
    {
        var files = new[]
        {
            new { filename = "demo_print.gcode", modified = DateTimeOffset.UtcNow.AddDays(-1).ToString("o"), size = 4587293 },
            new { filename = "benchy.gcode", modified = DateTimeOffset.UtcNow.AddDays(-3).ToString("o"), size = 1284512 },
            new { filename = "calibration_cube.gcode", modified = DateTimeOffset.UtcNow.AddDays(-7).ToString("o"), size = 892341 },
            new { filename = "flexi_dragon.gcode", modified = DateTimeOffset.UtcNow.AddDays(-14).ToString("o"), size = 12834567 },
        };

        return JsonSerializer.Serialize(new { result = new { files = files.Select(f => new { f.filename, modified = f.modified, size = f.size, dirname = "gcodes" }) } });
    }

    // === Commands ===

    public bool StartPrint(string filename)
    {
        lock (_lock)
        {
            if (_state == "shutdown") return false;
            _filename = filename;
            _state = "printing";
            _progressPercent = 0;
            _currentLayer = 0;
            _elapsedSec = 0;
            _hotendTemp = _targetHotendTemp;
            _bedTemp = _targetBedTemp;
            StartProgressTimer();
            return true;
        }
    }

    public bool PausePrint()
    {
        lock (_lock) { if (_state != "printing") return false; _state = "paused"; _progressTimer?.Dispose(); return true; }
    }

    public bool ResumePrint()
    {
        lock (_lock) { if (_state != "paused") return false; _state = "printing"; StartProgressTimer(); return true; }
    }

    public bool CancelPrint()
    {
        lock (_lock)
        {
            if (_state != "printing" && _state != "paused") return false;
            _state = "ready";
            _progressPercent = 0;
            _currentLayer = 0;
            _progressTimer?.Dispose();
            return true;
        }
    }

    public bool SendGcode(string gcode)
    {
        // Simuliert G-Code-Befehl
        lock (_lock)
        {
            if (gcode.StartsWith("M104") || gcode.StartsWith("M109"))
                _targetHotendTemp = ParseTemp(gcode);
            if (gcode.StartsWith("M140") || gcode.StartsWith("M190"))
                _targetBedTemp = ParseTemp(gcode);
            return true;
        }
    }

    private static decimal ParseTemp(string gcode)
    {
        var s = gcode.Split('S');
        if (s.Length > 1 && decimal.TryParse(s[1], out var t)) return t;
        return 0m;
    }

    public string GetStatus() { lock (_lock) return _state; }
}