// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FlipsiForge.Desktop.ViewModels;

namespace FlipsiForge.Desktop.Views;

/// <summary>
/// Cost-Calculator-View. Verbindet den File-Picker mit dem ViewModel
/// (ViewModel kann nicht direkt auf StorageProvider zugreifen).
/// </summary>
public partial class CostCalculatorView : UserControl
{
    public CostCalculatorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (DataContext is CostCalculatorViewModel vm)
        {
            // File-Picker-Callback setzen — ViewModel ruft diese Func auf,
            // um den System-File-Dialog zu oeffnen.
            vm.OpenFilePicker = OpenFilePickerAsync;
        }
    }

    /// <summary>Oeffnet den System-File-Open-Dialog und gibt den Pfad zurueck.</summary>
    private async Task<string?> OpenFilePickerAsync()
    {
        var window = this.VisualRoot as Window;
        if (window == null) return null;

        try
        {
            var provider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (provider == null) return null;

            var fileTypes = new[]
            {
                new FilePickerFileType("3D-Druck Dateien")
                {
                    Patterns = new[] { "*.stl", "*.3mf", "*.gcode", "*.gco" }
                },
                new FilePickerFileType("Alle Dateien")
                {
                    Patterns = new[] { "*.*" }
                }
            };

            var options = new FilePickerOpenOptions
            {
                Title = "Druck-Datei laden (STL, 3MF, GCODE)",
                AllowMultiple = false,
                FileTypeFilter = fileTypes
            };

            var files = await provider.OpenFilePickerAsync(options);
            if (files.Count == 0) return null;

            // StorageItem.Path ist ein IStorageItem - wir brauchen den lokalen Pfad
            var storageFile = files[0];
            return storageFile.TryGetLocalPath();
        }
        catch
        {
            // Best-effort — Dialog konnte nicht geoeffnet werden
            return null;
        }
    }
}