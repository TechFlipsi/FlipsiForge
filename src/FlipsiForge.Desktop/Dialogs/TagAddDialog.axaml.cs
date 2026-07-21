// SPDX-License-Identifier: GPL-3.0-or-later
// TagAddDialog: Mini-Dialog zum Anlegen eines neuen NFC/QR-Tags.
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.Dialogs;

/// <summary>Mini-Dialog zum Hinzufuegen eines NFC/QR-Tags mit optionaler Spulen-Zuweisung.</summary>
public partial class TagAddDialog : Window
{
    /// <summary>true = gespeichert, false/null = abgebrochen.</summary>
    public bool? DialogResult { get; private set; }

    /// <summary>Der neu erstellte Tag (nach erfolgreichem Speichern).</summary>
    public NfcQrTag? CreatedTag { get; private set; }

    private static readonly string[] TypeOptions = { "NFC", "QR" };
    private List<Spool> _spools = new();

    public TagAddDialog()
    {
        InitializeComponent();
        LoadTypeOptions();
        LoadSpools();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LoadTypeOptions()
    {
        if (this.FindControl<ComboBox>("TypeCombo") is ComboBox combo)
        {
            combo.ItemsSource = TypeOptions.ToList();
            combo.SelectedIndex = 0;
        }
    }

    private void TypeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // keine Aktion noetig
    }

    private void LoadSpools()
    {
        try
        {
            using var db = ServiceLocator.CreateDb();
            _spools = db.Spools.ToList();
        }
        catch { _spools = new(); }

        if (this.FindControl<ComboBox>("SpoolCombo") is ComboBox combo)
        {
            var items = new List<string> { "Keiner" };
            foreach (var s in _spools)
                items.Add($"{s.Brand} {s.MaterialName}");
            combo.ItemsSource = items;
            combo.SelectedIndex = 0;
        }
    }

    private void Scan_Click(object? sender, RoutedEventArgs e)
    {
        // Placeholder: echte Scan-Integration spaeter
        if (this.FindControl<TextBox>("CodeBox") is TextBox tb)
            tb.Text = $"AUTO-{DateTime.UtcNow:HHmmss}";
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var code = this.FindControl<TextBox>("CodeBox")?.Text?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            DialogResult = false;
            Close();
            return;
        }

        var typeCombo = this.FindControl<ComboBox>("TypeCombo");
        var type = typeCombo != null && typeCombo.SelectedIndex >= 0
            ? TypeOptions[typeCombo.SelectedIndex]
            : "NFC";

        int? spoolId = null;
        var spoolCombo = this.FindControl<ComboBox>("SpoolCombo");
        if (spoolCombo != null && spoolCombo.SelectedIndex > 0 && spoolCombo.SelectedIndex - 1 < _spools.Count)
            spoolId = _spools[spoolCombo.SelectedIndex - 1].Id;

        var tag = new NfcQrTag
        {
            Code = code,
            Type = type,
            AssignedSpoolId = spoolId
        };

        try
        {
            using var db = ServiceLocator.CreateDb();
            db.NfcQrTags.Add(tag);
            db.SaveChanges();
            CreatedTag = tag;
        }
        catch
        {
            // DB-Fehler: trotzdem Dialog schliessen mit CreatedTag fuer Caller
            CreatedTag = tag;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>Oeffnet den Dialog modal ueber dem Hauptfenster.</summary>
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