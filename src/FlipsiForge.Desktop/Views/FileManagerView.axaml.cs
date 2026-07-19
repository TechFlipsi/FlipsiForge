// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FlipsiForge.Desktop.ViewModels;

namespace FlipsiForge.Desktop.Views;

/// <summary>Datei-Manager-View mit Filter-Badges, Suche, List/Grid-Modus.</summary>
public partial class FileManagerView : UserControl
{
    public FileManagerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>Suche auslösen bei Enter in der Such-Box.</summary>
    private void SearchBox_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return && e.Key != Key.Enter) return;
        if (DataContext is FileManagerViewModel vm)
            vm.SearchCommand.Execute(null);
    }

    private void SortCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is FileManagerViewModel vm)
            vm.SortCommand.Execute(null);
    }

    private void ListBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FileManagerViewModel vm)
            vm.IsGrid = false;
    }

    private void GridBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FileManagerViewModel vm)
            vm.IsGrid = true;
    }
}