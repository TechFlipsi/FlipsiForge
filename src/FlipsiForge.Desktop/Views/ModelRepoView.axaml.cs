// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FlipsiForge.Desktop.ViewModels;

namespace FlipsiForge.Desktop.Views;

/// <summary>
/// Model-Repository-View. Verbindet OpenUrlInBrowser-Callback
/// (System-Browser) mit dem ViewModel.
/// </summary>
public partial class ModelRepoView : UserControl
{
    public ModelRepoView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ModelRepoViewModel vm)
        {
            // System-Browser oeffnen fuer Klicks auf Modell-Karten
            vm.OpenUrlInBrowser = OpenUrlInSystemBrowser;
        }
    }

    /// <summary>Klick auf Modell-Karte → URL im System-Browser oeffnen.</summary>
    private void Card_Click(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ModelCardVm model)
        {
            if (DataContext is ModelRepoViewModel vm)
            {
                vm.OpenInBrowserCommand.Execute(model);
            }
        }
    }

    /// <summary>Enter in der Such-Box → Suche ausloesen.</summary>
    private void SearchBox_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return && e.Key != Key.Enter) return;
        if (sender is TextBox tb && DataContext is ModelRepoViewModel vm)
        {
            vm.SearchCommand.Execute(tb.Text);
        }
    }

    /// <summary>Oeffnet eine URL im Standard-System-Browser.</summary>
    private void OpenUrlInSystemBrowser(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try
        {
            var top = TopLevel.GetTopLevel(this);
            var launcher = top?.Launcher;
            if (launcher != null)
            {
                _ = launcher.LaunchUriAsync(new Uri(url));
            }
        }
        catch
        {
            // Best-effort - Browser konnte nicht geoeffnet werden
        }
    }
}