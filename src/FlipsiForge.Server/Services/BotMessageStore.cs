// FlipsiForge.Server — v0.2.0
// BotMessageStore — In-Memory-Speicher für Forge-Bot Nachrichten.
// Die echte Implementierung würde Nachrichten in SQLite persistieren,
// diese Stub-Version reicht für v0.2.0 als Singleton-Speicher.
//
// SPDX-License-Identifier: GPL-3.0-only
// (c) 2026 TechFlipsi / Fabian Kirchweger

using System.Collections.Concurrent;

namespace FlipsiForge.Server.Services;

/// <summary>
/// In-Memory-Speicher für Forge-Bot Nachrichten. Singleton-registriert.
/// </summary>
public sealed class BotMessageStore
{
    private readonly ConcurrentDictionary<int, BotMessage> _messages = new();
    private int _nextId = 1;

    /// <summary>Fügt eine Nachricht hinzu und gibt sie zurück.</summary>
    public BotMessage Add(string text)
    {
        var id = Interlocked.Increment(ref _nextId);
        var msg = new BotMessage { Id = id, Text = text, Timestamp = DateTime.UtcNow };
        _messages[id] = msg;
        return msg;
    }

    /// <summary>Liefert alle nicht-dismissed Nachrichten, neueste zuerst.</summary>
    public IReadOnlyList<BotMessage> All()
        => _messages.Values.OrderBy(m => m.Timestamp).ToList();

    /// <summary>Liefert nur nicht-dismissed Nachrichten.</summary>
    public IReadOnlyList<BotMessage> Active()
        => _messages.Values.Where(m => !m.Dismissed).OrderBy(m => m.Timestamp).ToList();

    /// <summary>Dismissed die letzte aktive Nachricht. Gibt true zurück wenn eine gefunden wurde.</summary>
    public bool DismissLast()
    {
        var last = _messages.Values.Where(m => !m.Dismissed).MaxBy(m => m.Timestamp);
        if (last is null) return false;
        last.Dismissed = true;
        return true;
    }
}

/// <summary>
/// Erzeugt Forge-Bot Tipps/Sprüche (Eilik-Stil, Dark Void + Ember Theme).
/// Stub-Version gibt eine kleine Auswahl von Tipps zurück.
/// </summary>
public static class ForgeBotLines
{
    private static readonly string[] Lines =
    {
        "Heute ist ein guter Tag zum Drucken! 🔥",
        "Vergiss nicht, dein Druckbett zu reinigen.",
        "PETG trocknen? 65°C für 4 Stunden.",
        "Stringing? Retraction erhöhen und Temp senken.",
        "Brim bei ABS hilft gegen Warping.",
        "Mein Lieblingsfilament? ASA für draußen.",
        "Druckbett zu kalt? Layer-Haftung leidet.",
        "Input Shaping kalibrieren = bessere Oberfläche.",
        "TPU: Retraction AUS, Direct Drive ON.",
        "Düse nach 500h wechseln — Verschleiß ist unsichtbar."
    };

    /// <summary>Liefert einen zufälligen Tipp.</summary>
    public static string Random() => Lines[System.Random.Shared.Next(Lines.Length)];
}