// SPDX-License-Identifier: GPL-3.0-or-later
// MainViewModel: Verwaltet die Sidebar-Navigation (CurrentView).
// Settings ist ein eigener Tab. ForgeBot-Overlay entfernt.
// KI-Assistent heisst jetzt ForgeBot (mit kompletter ForgeBot-Persönlichkeit).
// Drucker + DruckWächter zusammengefasst zu "Drucker" (PrinterView hat beides).
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Desktop.Views;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Haupt-ViewModel der App — Sidebar-Navigation.</summary>
public partial class MainViewModel : ViewModelBase
{
    /// <summary>Version (für Sidebar-Anzeige).</summary>
    public string AppVersion => "v0.4.1";

    /// <summary>Name der gerade aktiven View (für SelectedItem-Highlight).</summary>
    [ObservableProperty]
    private string _currentViewName = "Datei-Manager";

    /// <summary>Der gerade angezeigte UserControl-Inhalt.</summary>
    [ObservableProperty]
    private UserControl? _currentView;

    /// <summary>Nav-Items der Sidebar.</summary>
    public IReadOnlyList<NavItem> NavItems { get; } = new List<NavItem>
    {
        new("Datei-Manager", "📁"),
        new("Drucker", "🖨️"),
        new("Filament", "🧶"),
        new("Model-Repo", "🌐"),
        new("Statistik", "📊"),
        new("Kosten-Rechner", "🔥"),
        new("ForgeBot", "🤖"),
        new("Einstellungen", "⚙️")
    };

    public MainViewModel()
    {
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

    /// <summary>Erzeugt eine View-Instanz anhand ihres Namens.</summary>
    private static UserControl? BuildView(string name) => name switch
    {
        "Datei-Manager" => new FileManagerView(),
        "Drucker" => new PrinterView(),
        "Filament" => new FilamentView(),
        "Model-Repo" => new ModelRepoView(),
        "Statistik" => new StatisticsView(),
        "Kosten-Rechner" => new CostCalculatorView(),
        "ForgeBot" => new AiAssistantView(),
        "Einstellungen" => new SettingsView(),
        _ => null
    };
}

/// <summary>Ein Sidebar-Navigations-Eintrag (Name + Emoji-Icon).</summary>
public sealed record NavItem(string Name, string Icon)
{
    public string DisplayText => $"{Icon} {Name}";
}