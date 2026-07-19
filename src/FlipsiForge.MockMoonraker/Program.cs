using System.Net;
using System.Text.Json;

namespace FlipsiForge.MockMoonraker;

/// <summary>
/// Mock Moonraker API Server — simuliert einen Klipper-Drucker für FlipsiForge.
/// Keine externe Runtime nötig, reines .NET, keine Abhängigkeiten.
/// Unterstützt: HTTP GET Endpoints, WebSocket-Events, Druck-Simulation.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        var port = 7125;
        var scenario = "demo";

        // Parse args: --port 7125 --scenario demo
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--port" && i + 1 < args.Length)
                port = int.Parse(args[i + 1]);
            if (args[i] == "--scenario" && i + 1 < args.Length)
                scenario = args[i + 1];
        }

        var simulator = new PrinterSimulator(scenario);
        var server = new MockMoonrakerServer(port, simulator);

        Console.WriteLine($"┌─────────────────────────────────────────────┐");
        Console.WriteLine($"│  FlipsiForge Mock Moonraker v0.1.0          │");
        Console.WriteLine($"│  Port: {port,-34} │");
        Console.WriteLine($"│  Szenario: {scenario,-32} │");
        Console.WriteLine($"│  URL: http://localhost:{port,-18} │");
        Console.WriteLine($"└─────────────────────────────────────────────┘");
        Console.WriteLine();
        Console.WriteLine("Drücken Sie Ctrl+C zum Beenden.");
        Console.WriteLine();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await server.RunAsync(cts.Token);
    }
}