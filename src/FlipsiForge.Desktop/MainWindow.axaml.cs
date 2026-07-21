// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Interactivity;
using FlipsiForge.Desktop.ViewModels;

namespace FlipsiForge.Desktop;

/// <summary>Hauptfenster mit Sidebar + ContentControl + Settings-Overlay.</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Behandelt die Auswahl eines Sidebar-Eintrags und wechselt die View.</summary>
    private void NavList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // WICHTIG: Dieser Event feuert während InitializeComponent() (XamlIlPopulate),
        // BEVOR DataContext gesetzt wird. Daher müssen wir auf null prüfen, um
        // NullReferenceException beim App-Start zu vermeiden.
        if (DataContext is not MainViewModel vm) return;
        if (NavList.SelectedItem is not NavItem item) return;
        // Verhindere Rekursion: nur switchen, wenn Name differiert
        if (vm.CurrentViewName != item.Name)
            vm.SwitchViewCommand.Execute(item.Name);
    }
}