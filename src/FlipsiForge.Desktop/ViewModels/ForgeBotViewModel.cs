// SPDX-License-Identifier: GPL-3.0-or-later
// ForgeBotViewModel: Mascot-Overlay mit Timer-basierter Nachrichten-Steuerung.
// Verhaltensregeln: max 1 Nachricht pro 15 Min, keine während Druck läuft,
// DnD-Zeitfenster, vordefinierte deutsche Tipps/Jokes/Lifehacks.
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Forge-Bot Mascot-Overlay-ViewModel.</summary>
public partial class ForgeBotViewModel : ViewModelBase
{
    private readonly DesktopSettings _settings;
    private readonly IPrinterService _printerService;
    private System.Threading.Timer? _tickTimer;
    private DateTime? _lastMessageAt;
    private DateTime? _mutedUntil;

    /// <summary>Aktuelle Sprechblasen-Nachricht (null = nichts sichtbar).</summary>
    [ObservableProperty]
    private string? _currentMessage;

    /// <summary>True, wenn die Sprechblase sichtbar ist.</summary>
    [ObservableProperty]
    private bool _isMessageVisible;

    /// <summary>True, wenn das kleine Menü sichtbar ist.</summary>
    [ObservableProperty]
    private bool _isMenuOpen;

    /// <summary>True, wenn der Bot sichtbar ist (Settings-Einstellung).</summary>
    public bool IsBotVisible => _settings.BotEnabled;

    /// <summary>Tipps auf Deutsch.</summary>
    private static readonly string[] Tips =
    {
        "Tipp: PETG vor dem Druck 4h bei 65°C trocknen — verhindert Stringing.",
        "Tipp: Die Druckbett-Adhesion steigt, wenn du das Bett mit Isopropanol reinigst.",
        "Tipp: Vor dem Drucken von ABS solltest du den Druckraum schließen.",
        "Tipp: TPU braucht Direct Drive — und Retraction AUS!",
        "Tipp: Düsen-Verschleiß? Bei CF-Filament harte Düse (Ruby/Stahl) verwenden.",
        "Tipp: Pressure Advance kalibrieren = sauberere Nähte und bessere Toleranzen.",
        "Tipp: Spulen luftdicht lagern, mit Trockenmittel-Beutelchen.",
        "Tipp: Flow-Rate kalibrieren = bessere Passgenauigkeit mechanischer Teile.",
        "Tipp: ASA statt ABS für Außen-Drucke — UV-resistent und wetterfest.",
        "Tipp: Schichthöhe 0.16 mm ist ein guter Allround-Wert für PLA."
    };

    /// <summary>Lustige Sprüche.</summary>
    private static readonly string[] Jokes =
    {
        "Was machen 3D-Drucker im Urlaub? Sie legen Schicht für Schicht nach. 🏖️",
        "Mein Lieblingsfilament? PETG — es ist so flexibel wie meine Ausreden. 😄",
        "Warum war der Drucker so entspannt? Weil er im Flow war. 🧘",
        "Ich bin nicht faul, ich mache nur Homing-Sequence. 🤖",
        "Ohne Kaffee kein G-Code. ☕",
        "Was sagt der eine Drucker zum anderen? „Schicht schon zuende?“"
    };

    /// <summary>Lifehacks.</summary>
    private static readonly string[] Lifehacks =
    {
        "Lifehack: Brim bei runden Teilen verhindert Warping effektiv.",
        "Lifehack: Tree-Support spart Material und ist leichter zu entfernen.",
        "Lifehack: Heizbett 60°C statt 50°C für PLA = viel bessere Adhesion.",
        "Lifehack: Erste Schicht langsam (20 mm/s) = sauberere Drucke.",
        "Lifehack: Spule wiegen, bevor du einen langen Druck startest — nie wieder Mid-Print-Filament-Mangel.",
        "Lifehack: GCODE-Vorschau slicen vor dem Druck, dann keine bösen Überraschungen."
    };

    public ForgeBotViewModel() : this(DesktopSettings.Load(), ServiceLocator.Require<IPrinterService>()) { }

    public ForgeBotViewModel(DesktopSettings settings, IPrinterService printerService)
    {
        _settings = settings;
        _printerService = printerService;
        if (_settings.BotEnabled)
            Start();
    }

    /// <summary>Startet den Timer für die nächste Nachricht.</summary>
    public void Start()
    {
        _tickTimer?.Dispose();
        var intervalSec = GetIntervalSeconds();
        _tickTimer = new System.Threading.Timer(OnTick, null, intervalSec * 1000, intervalSec * 1000);
    }

    /// <summary>Stoppt den Timer.</summary>
    public void Stop()
    {
        _tickTimer?.Dispose();
        _tickTimer = null;
        IsMessageVisible = false;
        CurrentMessage = null;
    }

    /// <summary>Liefert das Timer-Intervall in Sekunden, abhängig von Frequenz + Randomisierung.</summary>
    private int GetIntervalSeconds()
    {
        var r = new Random();
        return _settings.BotFrequency switch
        {
            BotFrequency.Rare => r.Next(15 * 60, 20 * 60),
            BotFrequency.Often => r.Next(2 * 60, 3 * 60),
            _ => 6 * 60
        };
    }

    /// <summary>Timer-Callback: prüft ob Nachricht angezeigt werden darf.</summary>
    private async void OnTick(object? state)
    {
        try
        {
            if (!_settings.BotEnabled) return;
            if (IsInDndWindow()) return;
            if (_mutedUntil.HasValue && _mutedUntil > DateTime.UtcNow) return;
            // Max 1 Nachricht pro 15 Min
            if (_lastMessageAt.HasValue && DateTime.UtcNow - _lastMessageAt < TimeSpan.FromMinutes(15))
                return;
            // Keine Nachrichten während Druck läuft
            if (await _printerService.IsAnyPrinterPrintingAsync()) return;

            var msg = PickRandomMessage();
            await Dispatcher.UIThread.InvokeAsync(() => ShowMessage(msg));
        }
        catch
        {
            // Best-effort
        }
    }

    /// <summary>Zeigt eine Sprechblasen-Nachricht für 5-10 Sekunden.</summary>
    public void ShowMessage(string text)
    {
        CurrentMessage = text;
        IsMessageVisible = true;
        _lastMessageAt = DateTime.UtcNow;

        // In DB/JSON loggen
        try
        {
            BotMessageEntryStore.Log(new Services.BotMessage
            {
                Text = text,
                ShownAt = DateTime.UtcNow,
                Category = Categorize(text)
            });
        }
        catch { }

        // Auto-hide nach 5-10 Sekunden
        var dur = new Random().Next(5000, 10000);
        _ = Task.Delay(dur).ContinueWith(t =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsMessageVisible = false;
                CurrentMessage = null;
            });
        });
    }

    private static string Categorize(string text)
    {
        if (text.StartsWith("Tipp")) return "tip";
        if (text.StartsWith("Lifehack")) return "lifehack";
        return "joke";
    }

    private static string PickRandomMessage()
    {
        var r = new Random();
        var cat = r.Next(3);
        return cat switch
        {
            0 => Tips[r.Next(Tips.Length)],
            1 => Jokes[r.Next(Jokes.Length)],
            _ => Lifehacks[r.Next(Lifehacks.Length)]
        };
    }

    /// <summary>True, wenn die aktuelle Zeit im DnD-Zeitfenster liegt.</summary>
    private bool IsInDndWindow()
    {
        if (string.IsNullOrEmpty(_settings.BotDndStart) || string.IsNullOrEmpty(_settings.BotDndEnd))
            return false;
        try
        {
            var now = DateTime.Now.TimeOfDay;
            var start = TimeSpan.Parse(_settings.BotDndStart);
            var end = TimeSpan.Parse(_settings.BotDndEnd);
            if (start < end)
                return now >= start && now <= end;
            // Über Mitternacht: z.B. 22:00–07:00
            return now >= start || now <= end;
        }
        catch { return false; }
    }

    /// <summary>Klick auf den Bot: öffnet das kleine Menü.</summary>
    [RelayCommand]
    public void BotClicked()
    {
        IsMenuOpen = !IsMenuOpen;
    }

    /// <summary>Stummschaltung für 1 Stunde.</summary>
    [RelayCommand]
    public void MuteOneHour()
    {
        _mutedUntil = DateTime.UtcNow.AddHours(1);
        IsMenuOpen = false;
        IsMessageVisible = false;
        CurrentMessage = null;
    }

    /// <summary>Schließt das Menü ohne Aktion.</summary>
    [RelayCommand]
    public void CloseMenu()
    {
        IsMenuOpen = false;
    }

    /// <summary>Löst sofort eine zufällige Nachricht aus (für Demo-/Test-Zwecke).</summary>
    [RelayCommand]
    public void TriggerMessage()
    {
        ShowMessage(PickRandomMessage());
    }
}