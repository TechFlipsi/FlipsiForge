// SPDX-License-Identifier: GPL-3.0-or-later
// NfcQrViewModel: ViewModel fuer den NFC/QR-Tab - Verwaltung von NFC- und QR-Tags.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Dialogs;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Display-Row fuer einen NFC/QR-Tag.</summary>
public sealed class TagRowVm : ObservableObject
{
    public NfcQrTag Tag { get; }
    public int Id => Tag.Id;
    public string Code => Tag.Code;
    public string Type => Tag.Type;
    public string TypeGlyph => Tag.Type == "NFC" ? "📲 NFC" : "🔳 QR";
    public string TypeBadgeColor => Tag.Type == "NFC" ? "#3b82f6" : "#22c55e";

    private string? _assignedSpoolDisplay;
    public string AssignedSpoolDisplay => _assignedSpoolDisplay ?? "Frei";

    private string? _assignedSpoolColor;
    public string? AssignedSpoolColor => _assignedSpoolColor;

    private bool _isAssigned;
    public bool IsAssigned => _isAssigned;

    public TagRowVm(NfcQrTag tag, string? spoolDisplay, string? spoolColor, bool isAssigned)
    {
        Tag = tag;
        _assignedSpoolDisplay = spoolDisplay;
        _assignedSpoolColor = spoolColor;
        _isAssigned = isAssigned;
    }
}

/// <summary>ViewModel fuer die NFC/QR-Code-Verwaltung.</summary>
public partial class NfcQrViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;

    /// <summary>Alle Tags als Display-Rows.</summary>
    public ObservableCollection<TagRowVm> Tags { get; } = new();

    public NfcQrViewModel() : this(ServiceLocator.CreateDb()) { }

    public NfcQrViewModel(FlipsiForgeDbContext db)
    {
        _db = db;
        Load();
    }

    /// <summary>Laedt alle Tags aus der DB und bildet die Display-Rows mit Spulen-Info.</summary>
    public void Load()
    {
        try
        {
            Tags.Clear();
            var spools = _db.Spools.ToList();
            var tags = _db.NfcQrTags.ToList();
            foreach (var t in tags)
            {
                var spool = spools.FirstOrDefault(s => s.Id == t.AssignedSpoolId);
                var display = spool != null
                    ? $"{spool.Brand} {spool.MaterialName}"
                    : null;
                var color = spool?.ColorHex;
                var isAssigned = spool != null;
                Tags.Add(new TagRowVm(t, display, color, isAssigned));
            }
        }
        catch
        {
            // DB-Fehler nicht fatal - leere Liste
            Tags.Clear();
        }
    }

    /// <summary>Oeffnet den Tag-Add-Dialog.</summary>
    [RelayCommand]
    public async Task AddAsync()
    {
        var dlg = new TagAddDialog();
        var ok = await dlg.ShowDialogAsync();
        if (ok != true) return;
        Load();
    }

    /// <summary>Loescht einen Tag nach Bestaetigung.</summary>
    [RelayCommand]
    public async Task DeleteAsync(TagRowVm row)
    {
        if (row == null) return;
        var ok = await new ConfirmDialog(
            $"Tag \"{row.Code}\" wirklich loeschen?",
            "Loeschen bestaetigen").ShowDialogAsync();
        if (ok != true) return;
        try
        {
            _db.NfcQrTags.Remove(row.Tag);
            _db.SaveChanges();
        }
        catch { /* nicht fatal */ }
        Tags.Remove(row);
    }

    /// <summary>Oeffnet den Edit-Dialog fuer einen Tag (oeffnet TagAddDialog mit Pre-Fill).
    /// Da TagAddDialog aktuell nur Add macht, nutzen wir ihn als schnelle Neuzuweisung.</summary>
    [RelayCommand]
    public async Task EditAsync(TagRowVm row)
    {
        if (row == null) return;
        // Einfacher Edit: loesche alten Tag, oeffne Add-Dialog mit Pre-Fill
        var dlg = new TagAddDialog();
        // Pre-Fill Code und Typ (ueber Reflection oder direktes FindControl nach Show)
        // Da der Dialog kein Pre-Fill-Konstruktor hat, nutzen wir ein einfacheres Pattern:
        // Tag loeschen und neu anlegen
        var ok = await new ConfirmDialog(
            $"Tag \"{row.Code}\" bearbeiten? (Alten Tag loeschen und neu anlegen)",
            "Tag bearbeiten").ShowDialogAsync();
        if (ok != true) return;

        try
        {
            _db.NfcQrTags.Remove(row.Tag);
            _db.SaveChanges();
        }
        catch { }
        Tags.Remove(row);

        // Oeffne Add-Dialog fuer Neuanlage
        var addDlg = new TagAddDialog();
        await addDlg.ShowDialogAsync();
        Load();
    }

    /// <summary>Weist einen Tag einer Spule zu (oeffnet eine ComboBox-Auswahl).
    /// Vereinfachte Variante: oeffnet TagAddDialog fuer Neuzuweisung.</summary>
    [RelayCommand]
    public async Task AssignAsync(TagRowVm row)
    {
        if (row == null) return;
        // Loesche Zuweisung oder oeffne Auswahl-Dialog
        // Fuer v0.5.0: Toggle - wenn zugewiesen, loese Zuweisung; sonst TagAddDialog
        if (row.IsAssigned)
        {
            // Zuweisung loesen
            try
            {
                row.Tag.AssignedSpoolId = null;
                _db.NfcQrTags.Update(row.Tag);
                _db.SaveChanges();
            }
            catch { }
            Load();
        }
        else
        {
            // Neuzuweisung via Add-Dialog
            await AddAsync();
        }
    }
}