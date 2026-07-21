// SPDX-License-Identifier: GPL-3.0-or-later
// FilamentViewModel: CRUD-Listen-ViewModel fuer Filament-Spulen + Gruppen-Ansicht.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Dialogs;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Display-Row fuer eine Filament-Spule.</summary>
public sealed class SpoolRowVm : ObservableObject
{
    public Spool Spool { get; }
    public int Id => Spool.Id;
    public string Brand => Spool.Brand;
    public string Material => Spool.MaterialName;
    public string MaterialType => Spool.MaterialType.ToString();
    public string ColorHex => Spool.ColorHex;
    public string? ColorName => Spool.ColorName;
    public string Weight => $"{Spool.RemainingWeightG:F0}/{Spool.TotalWeightG:F0} g";
    public string Diameter => $"{Spool.DiameterMm:F2} mm";
    public string Density => $"{Spool.DensityGcm3:F2} g/cm³";
    public string Cost => $"{Spool.CostEur:F2} €";

    /// <summary>Restgewicht als Prozent (0-100) fuer Fortschrittsbalken.</summary>
    public double RemainingPercent => Spool.TotalWeightG > 0
        ? Math.Clamp((double)(Spool.RemainingWeightG / Spool.TotalWeightG) * 100.0, 0, 100)
        : 0;

    private SpoolStatus _status;
    public SpoolStatus Status
    {
        get => _status;
        set { SetProperty(ref _status, value); OnPropertyChanged(nameof(StatusGlyph)); }
    }

    public string StatusGlyph => Status switch
    {
        SpoolStatus.Active => "🟢 Aktiv",
        SpoolStatus.Empty => "⚫ Leer",
        SpoolStatus.Drying => "🟡 Trocknet",
        SpoolStatus.Archived => "⚪ Archiviert",
        _ => "—"
    };

    /// <summary>Selektiert-Status fuer manuelle Gruppierung.</summary>
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public SpoolRowVm(Spool s)
    {
        Spool = s;
        _status = s.Status;
    }

    public void RefreshFromSpool()
    {
        OnPropertyChanged(nameof(Brand));
        OnPropertyChanged(nameof(Material));
        OnPropertyChanged(nameof(MaterialType));
        OnPropertyChanged(nameof(ColorHex));
        OnPropertyChanged(nameof(ColorName));
        OnPropertyChanged(nameof(Weight));
        OnPropertyChanged(nameof(Diameter));
        OnPropertyChanged(nameof(Density));
        OnPropertyChanged(nameof(Cost));
        OnPropertyChanged(nameof(RemainingPercent));
        Status = Spool.Status;
    }
}

/// <summary>
/// Display-Row fuer eine Spulen-Gruppe (Auto-Gruppierung bei exakt gleichen Werten).
/// </summary>
public sealed class SpoolGroupVm : ObservableObject
{
    public string Brand { get; }
    public string Material { get; }
    public string MaterialType { get; }
    public string ColorHex { get; }
    public string? ColorName { get; }

    /// <summary>Spulen in dieser Gruppe.</summary>
    public ObservableCollection<SpoolRowVm> Spools { get; } = new();

    public int SpoolCount => Spools.Count;

    /// <summary>Gesamt-Restgewicht aller Spulen in der Gruppe (Gramm).</summary>
    public decimal TotalRemainingG => Spools.Sum(s => s.Spool.RemainingWeightG);

    /// <summary>Formatierte Anzeige des Gesamtgewichts (kg wenn &gt; 1000g).</summary>
    public string TotalWeightDisplay => TotalRemainingG >= 1000
        ? $"{TotalRemainingG / 1000:F1} kg gesamt ({SpoolCount} Spulen)"
        : $"{TotalRemainingG:F0} g gesamt ({SpoolCount} Spulen)";

    /// <summary>Header-Anzeige fuer die Gruppe: "Marke Material Farbname".</summary>
    public string Header => $"{Brand} {MaterialType}" +
        (string.IsNullOrEmpty(ColorName) ? "" : $" {ColorName}");

    public SpoolGroupVm(string brand, string material, string materialType,
        string colorHex, string? colorName)
    {
        Brand = brand;
        Material = material;
        MaterialType = materialType;
        ColorHex = colorHex;
        ColorName = colorName;
    }
}

/// <summary>ViewModel fuer die Filament-Spulen-Uebersicht.</summary>
public partial class FilamentViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;

    /// <summary>Alle Spulen als Display-Rows.</summary>
    public ObservableCollection<SpoolRowVm> Spools { get; } = new();

    /// <summary>Auto-Gruppen fuer die Gruppen-Ansicht.</summary>
    public ObservableCollection<SpoolGroupVm> Groups { get; } = new();

    /// <summary>Material-Typ-Optionen.</summary>
    public IReadOnlyList<string> MaterialTypeOptions { get; } =
        Enum.GetNames(typeof(MaterialType)).ToList();

    /// <summary>True = Gruppen-Ansicht aktiv, False = einzelne Spulen.</summary>
    [ObservableProperty]
    private bool _isGroupView;

    public FilamentViewModel() : this(ServiceLocator.CreateDb()) { }

    public FilamentViewModel(FlipsiForgeDbContext db)
    {
        _db = db;
        try { Load(); } catch { /* DB-Fehler nicht fatal */ }
    }

    /// <summary>Laedt alle Spulen aus der DB und erstellt Auto-Gruppen.</summary>
    public void Load()
    {
        Spools.Clear();
        foreach (var s in _db.Spools.ToList())
            Spools.Add(new SpoolRowVm(s));
        RebuildGroups();
    }

    /// <summary>
    /// Erstellt Auto-Gruppen aus den Spulen: nur wenn EXAKT gleiche Werte
    /// (Brand, MaterialName, ColorHex, MaterialType).
    /// </summary>
    public void RebuildGroups()
    {
        Groups.Clear();
        var grouped = Spools.GroupBy(s => new
        {
            s.Spool.Brand,
            s.Spool.MaterialName,
            s.Spool.ColorHex,
            s.Spool.MaterialType
        }).OrderByDescending(g => g.Sum(x => x.Spool.RemainingWeightG));

        foreach (var g in grouped)
        {
            var first = g.First();
            var group = new SpoolGroupVm(
                brand: g.Key.Brand,
                material: g.Key.MaterialName,
                materialType: g.Key.MaterialType.ToString(),
                colorHex: g.Key.ColorHex,
                colorName: first.Spool.ColorName);
            foreach (var spool in g)
                group.Spools.Add(spool);
            Groups.Add(group);
        }
    }

    /// <summary>Schaltet die Gruppen-Ansicht um.</summary>
    [RelayCommand]
    public void ToggleGroupView()
    {
        IsGroupView = !IsGroupView;
        if (IsGroupView) RebuildGroups();
    }

    /// <summary>Oeffnet den Spool-Add-Dialog.</summary>
    [RelayCommand]
    public async Task AddAsync()
    {
        var s = new Spool { Status = SpoolStatus.Active };
        var ok = await OpenDialogAsync(s, isNew: true);
        if (ok != true) return;
        _db.Spools.Add(s);
        _db.SaveChanges();
        Spools.Add(new SpoolRowVm(s));
        RebuildGroups();
    }

    /// <summary>Oeffnet den Edit-Dialog.</summary>
    [RelayCommand]
    public async Task EditAsync(SpoolRowVm row)
    {
        if (row == null) return;
        var ok = await OpenDialogAsync(row.Spool, isNew: false);
        if (ok != true) return;
        _db.SaveChanges();
        row.RefreshFromSpool();
        RebuildGroups();
    }

    /// <summary>Loescht eine Spule nach Bestaetigung.</summary>
    [RelayCommand]
    public async Task DeleteAsync(SpoolRowVm row)
    {
        if (row == null) return;
        var ok = await new ConfirmDialog(
            $"Spule \"{row.Brand} {row.Material}\" wirklich loeschen?",
            "Loeschen bestaetigen").ShowDialogAsync();
        if (ok != true) return;
        _db.Spools.Remove(row.Spool);
        _db.SaveChanges();
        Spools.Remove(row);
        RebuildGroups();
    }

    /// <summary>Schaltet den Status einer Spule weiter (Active -> Empty -> Drying -> Archived).</summary>
    [RelayCommand]
    public void CycleStatus(SpoolRowVm row)
    {
        if (row == null) return;
        row.Spool.Status = row.Spool.Status switch
        {
            SpoolStatus.Active => SpoolStatus.Empty,
            SpoolStatus.Empty => SpoolStatus.Drying,
            SpoolStatus.Drying => SpoolStatus.Archived,
            _ => SpoolStatus.Active
        };
        _db.SaveChanges();
        row.RefreshFromSpool();
        RebuildGroups();
    }

    private static async Task<bool?> OpenDialogAsync(Spool spool, bool isNew)
    {
        var dlg = new SpoolDialog(spool, isNew);
        await dlg.ShowDialogAsync();
        return dlg.DialogResult;
    }
}