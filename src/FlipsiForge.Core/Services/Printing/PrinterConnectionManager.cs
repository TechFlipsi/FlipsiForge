using System.Collections.Concurrent;
using FlipsiForge.Core.Models;
using Microsoft.Extensions.Logging;

namespace FlipsiForge.Core.Services.Printing;

/// <summary>
/// Factory und Cache für Drucker-Verbindungen.
/// Erzeugt anhand <see cref="Printer.Protocol"/> die richtige IPrinterConnection-Implementierung
/// und hält aktive Verbindungen in einem Cache (keyed by Printer.Id).
/// </summary>
public sealed class PrinterConnectionManager : IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, IPrinterConnection> _connections = new();
    private readonly Func<HttpClient> _httpClientFactory;
    private readonly ILogger<PrinterConnectionManager>? _logger;

    /// <summary>
    /// Erzeugt den Manager.
    /// </summary>
    /// <param name="httpClientFactory">
    /// Factory die einen frischen HttpClient zurückgibt (z.B. via IHttpClientFactory.CreateClient).
    /// Falls null wird ein singleton HttpClient mit default-TimeOut erzeugt.
    /// </param>
    /// <param name="logger">Optional Logger für Diagnose.</param>
    public PrinterConnectionManager(Func<HttpClient>? httpClientFactory = null, ILogger<PrinterConnectionManager>? logger = null)
    {
        _httpClientFactory = httpClientFactory ?? (() => new HttpClient { Timeout = TimeSpan.FromSeconds(10) });
        _logger = logger;
    }

    /// <summary>
    /// Liefert die Connection für einen Drucker. Erzeugt sie beim ersten Aufruf.
    /// </summary>
    /// <param name="printer">Drucker-Profil aus DB.</param>
    /// <returns>Connection-Instanz (immer non-null — bei unbekanntem Protocol wird NotSupportedException geworfen).</returns>
    /// <exception cref="NotSupportedException">Wenn das Protocol nicht unterstützt wird.</exception>
    public IPrinterConnection GetConnection(Printer printer)
    {
        if (_connections.TryGetValue(printer.Id, out var existing))
            return existing;

        var conn = CreateConnection(printer);
        _connections[printer.Id] = conn;
        return conn;
    }

    /// <summary>
    /// Erzeugt eine neue Connection ohne Cache. Caller ist fürs disposen verantwortlich.
    /// </summary>
    public IPrinterConnection CreateConnection(Printer printer) => printer.Protocol switch
    {
        PrinterProtocol.KlipperMoonraker => new MoonrakerConnection(
            _httpClientFactory(), BuildBaseUrl(printer), apiKey: null),
        PrinterProtocol.Marlin => new MarlinConnection(
            printer.UsbPort ?? "/dev/ttyUSB0"),
        PrinterProtocol.BambuLab => new BambuConnection(
            printer.IpAddress ?? "192.168.1.50",
            serial: printer.Model, // TODO: eigenes Feld für Serial in v0.3
            accessCode: "00000000"), // Stub — muss aus Settings kommen
        PrinterProtocol.PrusaLink => new PrusaLinkConnection(
            _httpClientFactory(), BuildBaseUrl(printer),
            apiKey: "0"), // Stub — aus Settings
        PrinterProtocol.OctoPrint => new OctoPrintConnection(
            _httpClientFactory(), BuildBaseUrl(printer),
            apiKey: ""), // Stub — aus Settings
        _ => throw new NotSupportedException($"Protocol {printer.Protocol} nicht unterstützt")
    };

    /// <summary>Baut die Basis-URL aus Printer.IpAddress.</summary>
    private static string BuildBaseUrl(Printer printer)
    {
        var ip = printer.IpAddress;
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";
        if (!ip.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !ip.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            ip = $"http://{ip}";
        return ip;
    }

    /// <summary>
    /// Trennt alle aktiven Verbindungen und leert den Cache.
    /// </summary>
    public async Task DisconnectAllAsync()
    {
        foreach (var kv in _connections)
        {
            try
            {
                await kv.Value.DisconnectAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Fehler beim Disconnect von Drucker {Id}", kv.Key);
            }
        }
        _connections.Clear();
    }

    /// <summary>Synchrone Variante von DisconnectAllAsync für einfache Aufrufe.</summary>
    public void DisconnectAll()
    {
        try
        {
            DisconnectAllAsync().GetAwaiter().GetResult();
        }
        catch { /* ignore — wird pro-Connection geloggt */ }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectAllAsync().ConfigureAwait(false);
    }
}