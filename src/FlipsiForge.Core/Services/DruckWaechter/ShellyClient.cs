// SPDX-License-Identifier: GPL-3.0-or-later
// DruckWächter — Shelly HTTP RPC Client (Gen2 / Gen3 / Gen4).
// Spricht die lokale Shelly HTTP-API direkt an (keine Cloud, kein HA nötig).
// Siehe: https://shelly-api-docs.shelly.cloud/gen2/ComponentsAndEndpoints
using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace FlipsiForge.Core.Services.DruckWaechter;

/// <summary>
/// Schalt-Status eines Shelly-Switch-Kanals (Gen2/3/4).
/// </summary>
public readonly struct ShellySwitchStatus
{
    /// <summary>Schaltzustand des Kanals (true = Relais an).</summary>
    public bool On { get; init; }

    /// <summary>
    /// Aktuelle Leistungsaufnahme in Watt (nur bei Power-Meter Modellen, z.B. Shelly Plus 1PM).
    /// Null wenn das Gerät kein PM hat.
    /// </summary>
    public decimal? PowerW { get; init; }

    /// <summary>
    /// Kumulierter Energieverbrauch in kWh (nur bei Power-Meter Modellen).
    /// Null wenn das Gerät kein PM hat.
    /// </summary>
    public decimal? EnergyKWh { get; init; }

    /// <summary>True wenn der Shelly ein Power-Meter hat (apower/aenergy vorhanden).</summary>
    public bool HasPowerMeter => PowerW.HasValue || EnergyKWh.HasValue;
}

/// <summary>
/// HTTP RPC Client für Shelly Gen2/Gen3/Gen4 Geräte.
/// kommuniziert direkt mit der lokalen HTTP-API der Shelly-Geräte —
/// es wird KEINE Cloud, KEINE MQTT-Verbindung und KEIN Home Assistant benötigt.
/// </summary>
/// <remarks>
/// Endpunkte:
///   GET  http://{ip}/rpc/Switch.GetStatus?id={id}   → {"output":bool,"apower":dec,"aenergy":{"total":dec}}
///   GET  http://{ip}/rpc/Switch.Set?id={id}&amp;on={bool} → {"was_on":bool}
/// (Switch.Set ist bei Shelly Gen2+ ein GET mit Query-Params, obwohl semantisch ein Command.)
/// </remarks>
public sealed class ShellyClient
{
    private readonly HttpClient _http;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Erzeugt einen Shelly-Client. Der HttpClient wird NIE disposed —
    /// der Caller ist verantwortlich für den Lifecycle (typischerweise via IHttpClientFactory).
    /// </summary>
    /// <param name="http">HttpClient (via IHttpClientFactory injiziert).</param>
    public ShellyClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    /// <summary>
    /// Schaltet einen Shelly-Switch-Kanal ein oder aus.
    /// </summary>
    /// <param name="ip">IP-Adresse des Shelly-Geräts (z.B. "192.168.178.60").</param>
    /// <param name="switchId">Kanal-ID (Shelly Plus 1PM = 0).</param>
    /// <param name="on">true = einschalten, false = ausschalten.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>true wenn der HTTP-Aufruf erfolgreich war (Status 2xx); false bei Fehler/Offline.</returns>
    public async Task<bool> SwitchSetAsync(string ip, int switchId, bool on, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;
        try
        {
            var url = $"http://{ip}/rpc/Switch.Set?id={switchId.ToString(CultureInfo.InvariantCulture)}&on={(on ? "true" : "false")}";
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(DefaultTimeout);
            using var resp = await _http.GetAsync(url, cts.Token).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Fragt den Status eines Shelly-Switch-Kanals ab.
    /// </summary>
    /// <param name="ip">IP-Adresse des Shelly-Geräts.</param>
    /// <param name="switchId">Kanal-ID.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>
    /// ShellySwitchStatus mit Output/Power/Energy. Bei Fehler oder Offline
    /// wird ein Status mit On=false und null-Power zurückgegeben.
    /// </returns>
    public async Task<ShellySwitchStatus> SwitchGetStatusAsync(string ip, int switchId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ip)) return default;
        try
        {
            var url = $"http://{ip}/rpc/Switch.GetStatus?id={switchId.ToString(CultureInfo.InvariantCulture)}";
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(DefaultTimeout);
            using var resp = await _http.GetAsync(url, cts.Token).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return default;
            using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            var json = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: ct).ConfigureAwait(false);
            if (json.ValueKind != JsonValueKind.Object) return default;

            // "output" ist ein bool — bei Gen2/3/4 immer vorhanden.
            bool isOn = false;
            if (json.TryGetProperty("output", out var outEl) && outEl.ValueKind == JsonValueKind.False)
                isOn = false;
            else if (outEl.ValueKind == JsonValueKind.True)
                isOn = true;
            else if (outEl.ValueKind == JsonValueKind.String && bool.TryParse(outEl.GetString(), out var b))
                isOn = b;

            // apower/aenergy NUR bei PM-Modellen. Normale Plus 1 (ohne PM) liefert die Felder nicht.
            decimal? powerW = GetDecimal(json, "apower");
            decimal? energy = null;
            if (json.TryGetProperty("aenergy", out var aenergy) && aenergy.ValueKind == JsonValueKind.Object)
                energy = GetDecimal(aenergy, "total");

            return new ShellySwitchStatus
            {
                On = isOn,
                PowerW = powerW,
                EnergyKWh = energy
            };
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Erkennt ob das Shelly-Gerät ein Power-Meter hat (PM).
    /// Auto-Detect: prüft ob das "aenergy"-Feld in der Switch.GetStatus-Antwort existiert.
    /// </summary>
    /// <param name="ip">IP-Adresse des Shelly-Geräts.</param>
    /// <param name="switchId">Kanal-ID (default 0).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>true wenn PM-Felder (apower oder aenergy.total) vorhanden sind; false sonst.</returns>
    public async Task<bool> DetectPowerMeterAsync(string ip, int switchId = 0, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;
        var status = await SwitchGetStatusAsync(ip, switchId, ct).ConfigureAwait(false);
        return status.HasPowerMeter;
    }

    /// <summary>
    /// Defensive Extraktion eines decimal-Werts aus einem JsonElement-Pfad
    /// (gleicher Stil wie JsonHelper im Printing-Namespace, aber hier gekapselt
    /// weil der DruckWaechter eigenständig nutzbar bleiben soll).
    /// </summary>
    private static decimal? GetDecimal(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var child)) return null;
        return child.ValueKind == JsonValueKind.Number ? child.GetDecimal() : null;
    }
}