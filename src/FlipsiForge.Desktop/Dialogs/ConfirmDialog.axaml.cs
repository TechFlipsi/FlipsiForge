// SPDX-License-Identifier: GPL-3.0-or-later
// ConfirmDialog: einfaches modales Bestätigungsfenster.
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FlipsiForge.Desktop.Dialogs;

/// <summary>Modales Bestätigungs-Window (OK/Cancel).</summary>
public partial class ConfirmDialog : Window
{
    /// <summary>true = OK, false = Abbrechen, null = geschlossen.</summary>
    public bool? DialogResult { get; private set; }

    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string message, string title) : this()
    {
        Title = title;
        var msg = this.FindControl<TextBlock>("MessageText");
        if (msg != null) msg.Text = message;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>Öffnet den Dialog modal über dem Hauptfenster und wartet auf Close.</summary>
    public async Task<bool?> ShowDialogAsync()
    {
        var main = (Avalonia.Application.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (main == null)
        {
            DialogResult = false;
            return false;
        }
        await ShowDialog(main);
        return DialogResult;
    }
}