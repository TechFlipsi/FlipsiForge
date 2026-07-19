// SPDX-License-Identifier: GPL-3.0-or-later
// MainViewModel: Verwaltet die Sidebar-Navigation (CurrentView) und das
// Öffnen des Settings-Overlays. Singleton für die App-Lebensdauer.
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Desktop.Services;
using FlipsiForge.Desktop.Views;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Haupt-ViewModel der App — Sidebar-Navigation + Settings.</summary>
public partial class MainViewModel : ViewModelBase
{
    /// <summary>Version (für Sidebar-Anzeige).</summary>
    public string AppVersion => "v0.4.0";

    /// <summary>Name der gerade aktiven View (für SelectedItem-Highlight).</summary>
    [ObservableProperty]
    private string _currentViewName = "Datei-Manager";

    /// <summary>Der gerade angezeigte UserControl-Inhalt.</summary>
    [ObservableProperty]
    private UserControl? _currentView;

    /// <summary>True, wenn das Settings-Overlay sichtbar ist.</summary>
    [ObservableProperty]
    private bool _isSettingsOpen;

    /// <summary>Settings-ViewModel (Singleton-Instanz).</summary>
    public SettingsViewModel Settings { get; } = new();

    /// <summary>Forge-Bot-ViewModel (für Overlay).</summary>
    public ForgeBotViewModel ForgeBot { get; } = new();

    /// <summary>Nav-Items der Sidebar.</summary>
    public IReadOnlyList<NavItem> NavItems { get; } = new List<NavItem>
    {
        new("Datei-Manager", "📁"),
        new("Drucker", "🖨️"),
        new("Filament", "🧶"),
        new("Model-Repo", "🌐"),
        new("Statistik", "📊"),
        new("Kosten-Rechner", "🔥"),
        new("DruckWächter", "🛡️"),
        new("KI-Assistent", "🤖"),
        new("Forge-Bot", "🔥")
    };

    public MainViewModel()
    {
        // Erste View initial setzen
        _currentView = BuildView("Datei-Manager");
    }

    /// <summary>Wechselt die angezeigte View anhand ihres Namens.</summary>
    [RelayCommand]
    public void SwitchView(string viewName)
    {
        var v = BuildView(viewName);
        if (v is null) return;
        CurrentView = v;
        CurrentViewName = viewName;
    }

    /// <summary>Öffnet das Settings-Overlay.</summary>
    [RelayCommand]
    public void OpenSettings()
    {
        Settings.Reload();
        IsSettingsOpen = true;
    }

    /// <summary>Schließt das Settings-Overlay (ohne Speichern).</summary>
    [RelayCommand]
    public void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    /// <summary>Erzeugt eine View-Instanz anhand ihres Namens.</summary>
    private static UserControl? BuildView(string name) => name switch
    {
        "Datei-Manager" => new FileManagerView(),
        "Drucker" => new PrinterView(),
        "Filament" => new FilamentView(),
        "Model-Repo" => new ModelRepoView(),
        "Statistik" => new StatisticsView(),
        "Kosten-Rechner" => new CostCalculatorView(),
        "DruckWächter" => new DruckWaechterView(),
        "KI-Assistent" => new AiAssistantView(),
        "Forge-Bot" => new ForgeBotView(),
        _ => null
    };
}

/// <summary>Ein Sidebar-Navigations-Eintrag (Name + Emoji-Icon).</summary>
public sealed record NavItem(string Name, string Icon)
{
    public string DisplayText => $"{Icon} {Name}";
}