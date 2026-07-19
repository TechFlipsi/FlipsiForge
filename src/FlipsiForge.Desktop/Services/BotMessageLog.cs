// SPDX-License-Identifier: GPL-3.0-or-later
// Desktop-seitige Persistenz für Forge-Bot-Meldungen. Core.Models hat aktuell
// (Stand Build) kein DbSet<BotMessage> — deswegen persistiert der Desktop die
// letzten Bot-Nachrichten als JSON im LocalApplicationData-Ordner. Wenn Core
// später ein DbSet<BotMessage> ergänzt, kann das hier auf DB umgestellt werden.
using System.Text.Json;

namespace FlipsiForge.Desktop.Services;

/// <summary>Ein Forge-Bot-Meldungs-Logeintrag.</summary>
public sealed record BotMessage
{
    public int Id { get; init; }
    public string Text { get; init; } = "";
    public DateTime ShownAt { get; init; } = DateTime.UtcNow;
    public string Category { get; init; } = "tip"; // tip, joke, lifehack, status
}

/// <summary>JSON-Persistenz für <see cref="BotMessage"/>.</summary>
public sealed class BotMessageEntryStore
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Liefert den Pfad der Log-Datei.</summary>
    public static string GetFilePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlipsiForge");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "bot_messages.json");
    }

    /// <summary>Loggt eine neue Nachricht und speichert die letzten 200.</summary>
    public static void Log(BotMessage msg)
    {
        try
        {
            var all = LoadAll();
            msg = msg with { Id = all.Count == 0 ? 1 : all.Max(x => x.Id) + 1 };
            all.Add(msg);
            if (all.Count > 200)
                all = all.Skip(all.Count - 200).ToList();
            File.WriteAllText(GetFilePath(), JsonSerializer.Serialize(all, JsonOpts));
        }
        catch
        {
            // Best-effort
        }
    }

    /// <summary>Lädt alle geloggten Nachrichten.</summary>
    public static List<BotMessage> LoadAll()
    {
        try
        {
            var path = GetFilePath();
            if (!File.Exists(path)) return new();
            return JsonSerializer.Deserialize<List<BotMessage>>(File.ReadAllText(path), JsonOpts) ?? new();
        }
        catch
        {
            return new();
        }
    }
}