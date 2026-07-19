// SPDX-License-Identifier: GPL-3.0-or-later
// FilamentViewModel: CRUD-Listen-ViewModel für Filament-Spulen.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Dialogs;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Display-Row für eine Filament-Spule.</summary>
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
    public string Density => $"{Spool.DensityGcm3:F2} g/cm³";
    public string Cost => $"{Spool.CostEur:F2} €";

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
        OnPropertyChanged(nameof(Density));
        OnPropertyChanged(nameof(Cost));
        Status = Spool.Status;
    }
}

/// <summary>ViewModel für die Filament-Spulen-Übersicht.</summary>
public partial class FilamentViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;

    /// <summary>Alle Spulen als Display-Rows.</summary>
    public ObservableCollection<SpoolRowVm> Spools { get; } = new();

    /// <summary>Material-Typ-Optionen.</summary>
    public IReadOnlyList<string> MaterialTypeOptions { get; } =
        Enum.GetNames(typeof(MaterialType)).ToList();

    public FilamentViewModel() : this(ServiceLocator.CreateDb()) { }

    public FilamentViewModel(FlipsiForgeDbContext db)
    {
        _db = db;
        Load();
    }

    /// <summary>Lädt alle Spulen aus der DB.</summary>
    public void Load()
    {
        Spools.Clear();
        foreach (var s in _db.Spools.ToList())
            Spools.Add(new SpoolRowVm(s));
    }

    /// <summary>Öffnet den Spool-Add-Dialog.</summary>
    [RelayCommand]
    public async Task AddAsync()
    {
        var s = new Spool { Status = SpoolStatus.Active };
        var ok = await OpenDialogAsync(s, isNew: true);
        if (ok != true) return;
        _db.Spools.Add(s);
        _db.SaveChanges();
        Spools.Add(new SpoolRowVm(s));
    }

    /// <summary>Öffnet den Edit-Dialog.</summary>
    [RelayCommand]
    public async Task EditAsync(SpoolRowVm row)
    {
        if (row == null) return;
        var ok = await OpenDialogAsync(row.Spool, isNew: false);
        if (ok != true) return;
        _db.SaveChanges();
        row.RefreshFromSpool();
    }

    /// <summary>Löscht eine Spule nach Bestätigung.</summary>
    [RelayCommand]
    public async Task DeleteAsync(SpoolRowVm row)
    {
        if (row == null) return;
        var ok = await new ConfirmDialog(
            $"Spule „{row.Brand} {row.Material}“ wirklich löschen?",
            "Löschen bestätigen").ShowDialogAsync();
        if (ok != true) return;
        _db.Spools.Remove(row.Spool);
        _db.SaveChanges();
        Spools.Remove(row);
    }

    /// <summary>Schaltet den Status einer Spule weiter (Active → Empty → Drying → Archived).</summary>
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
    }

    private static async Task<bool?> OpenDialogAsync(Spool spool, bool isNew)
    {
        var dlg = new SpoolDialog(spool, isNew);
        await dlg.ShowDialogAsync();
        return dlg.DialogResult;
    }
}