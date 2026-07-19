using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Core.Services.Printing;

/// <summary>
/// Basisklasse für HTTP-basierte Drucker-Verbindungen (Moonraker, PrusaLink, OctoPrint).
/// Stellt gemeinsame HTTP-Helper bereit und behandelt Verbindungs-Fehler robust.
/// Abgeleitete Klassen implementieren <see cref="ParseStatus"/>, <see cref="ParseTemps"/>,
/// <see cref="ParseJob"/> für das jeweilige Protokoll-JSON-Format.
/// </summary>
public abstract class HttpPrinterConnectionBase : IPrinterConnection
{
    /// <summary>HTTP-Client (kann von IHttpClientFactory injiziert werden).</summary>
    protected readonly HttpClient Http;

    /// <summary>Basis-URL ohne trailing slash (z.B. http://192.168.1.50:80).</summary>
    protected readonly string BaseUrl;

    /// <summary>Optional API-Key für Authentifizierung (OctoPrint, PrusaLink).</summary>
    protected readonly string? ApiKey;

    /// <summary>True wenn ConnectAsync erfolgreich war.</summary>
    protected bool IsConnectedField;

    /// <summary>
    /// Erzeugt eine HTTP-basierte Drucker-Verbindung.
    /// </summary>
    /// <param name="http">HttpClient (via IHttpClientFactory).</param>
    /// <param name="baseUrl">Basis-URL des Druckers, z.B. http://192.168.1.50.</param>
    /// <param name="apiKey">Optional API-Key.</param>
    protected HttpPrinterConnectionBase(HttpClient http, string baseUrl, string? apiKey = null)
    {
        Http = http;
        BaseUrl = baseUrl.TrimEnd('/');
        ApiKey = apiKey;
    }

    /// <summary>Default-TimeOut für HTTP-Calls (10 Sekunden).</summary>
    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public abstract Task<PrinterStatus> GetStatusAsync();

    /// <inheritdoc />
    public abstract Task<PrinterTemps> GetTemperaturesAsync();

    /// <inheritdoc />
    public abstract Task<PrinterJobInfo?> GetCurrentJobAsync();

    /// <inheritdoc />
    public abstract Task<bool> SendGcodeAsync(string filePath, bool requireConfirmation);

    /// <inheritdoc />
    public virtual Task<bool> PauseAsync() => Task.FromResult(false);

    /// <inheritdoc />
    public virtual Task<bool> ResumeAsync() => Task.FromResult(false);

    /// <inheritdoc />
    public virtual Task<bool> CancelAsync() => Task.FromResult(false);

    /// <inheritdoc />
    public virtual async Task<bool> ConnectAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(DefaultTimeout);
            // Pingen via /api/connection o.ä. — Basisklasse macht nur einen Head-Request
            using var resp = await Http.GetAsync(BaseUrl, cts.Token).ConfigureAwait(false);
            IsConnectedField = resp.IsSuccessStatusCode;
            return IsConnectedField;
        }
        catch
        {
            IsConnectedField = false;
            return false;
        }
    }

    /// <inheritdoc />
    public virtual Task DisconnectAsync()
    {
        IsConnectedField = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Führt einen HTTP-GET durch und parst JSON. Bei Fehler → default.
    /// </summary>
    /// <typeparam name="T">Ziel-Typ.</typeparam>
    /// <param name="relativePath">Pfad relativ zur BaseUrl.</param>
    /// <returns>Geparstes JSON oder default(T) bei Fehler.</returns>
    protected async Task<T?> GetJsonAsync<T>(string relativePath)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/{relativePath.TrimStart('/')}");
            ApplyAuth(req);
            using var cts = new CancellationTokenSource(DefaultTimeout);
            using var resp = await Http.SendAsync(req, cts.Token).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return default;
            using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<T>(stream).ConfigureAwait(false);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// POST-Aufruf ohne Body. Liefert true bei Erfolg.
    /// </summary>
    protected async Task<bool> PostAsync(string relativePath)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{relativePath.TrimStart('/')}");
            ApplyAuth(req);
            using var cts = new CancellationTokenSource(DefaultTimeout);
            using var resp = await Http.SendAsync(req, cts.Token).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Setzt den API-Key Header falls vorhanden. Override in Subklassen für protokollspezifische Header.</summary>
    protected virtual void ApplyAuth(HttpRequestMessage req)
    {
        if (!string.IsNullOrEmpty(ApiKey))
            req.Headers.TryAddWithoutValidation("X-Api-Key", ApiKey);
    }
}

/// <summary>Helper für JSON-Parser — defensive Extraktion von JsonElement-Werten.</summary>
internal static class JsonHelper
{
    public static decimal? GetDecimal(JsonElement el, params string[] path)
    {
        var cur = el;
        foreach (var key in path)
        {
            if (!cur.TryGetProperty(key, out cur)) return null;
        }
        return cur.ValueKind == JsonValueKind.Number ? cur.GetDecimal() : null;
    }

    public static int? GetInt(JsonElement el, params string[] path)
    {
        var d = GetDecimal(el, path);
        return d.HasValue ? (int)d.Value : null;
    }

    public static string? GetString(JsonElement el, params string[] path)
    {
        var cur = el;
        foreach (var key in path)
        {
            if (!cur.TryGetProperty(key, out cur)) return null;
        }
        return cur.ValueKind == JsonValueKind.String ? cur.GetString() : null;
    }
}