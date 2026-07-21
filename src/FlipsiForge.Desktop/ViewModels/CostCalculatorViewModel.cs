// SPDX-License-Identifier: GPL-3.0-or-later
// CostCalculatorViewModel: Interaktiver Druck-Kosten-Rechner.
// Filamentkosten (aus Spule) + Stromkosten (Stunden × Watt × €/kWh) + Verschleiß.
// Spulen kommen aus der DB (Brand + Material), Werte werden live berechnet.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Display-Row für eine Spule im Kosten-Rechner-Dropdown.</summary>
public sealed class SpoolOptionVm : ObservableObject
{
    public Spool Spool { get; }
    public int Id => Spool.Id;
    public string Display => $"{Spool.Brand} — {Spool.MaterialName} ({Spool.RemainingWeightG:F0}g)";
    public decimal RemainingWeightG => Spool.RemainingWeightG;
    public decimal CostEur => Spool.CostEur;
    public decimal TotalWeightG => Spool.TotalWeightG;

    public SpoolOptionVm(Spool s) { Spool = s; }
}

/// <summary>ViewModel für den interaktiven Druck-Kosten-Rechner.</summary>
public partial class CostCalculatorViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;

    /// <summary>Verfügbare Spulen für das Dropdown.</summary>
    public ObservableCollection<SpoolOptionVm> SpoolOptions { get; } = new();

    /// <summary>Ausgewählte Spule (null = keine).</summary>
    [ObservableProperty]
    private SpoolOptionVm? _selectedSpool;

    /// <summary>Druckdauer in Stunden (Default 3h).</summary>
    [ObservableProperty]
    private double _printHours = 3.0;

    /// <summary>Strompreis in €/kWh (Default 0.25).</summary>
    [ObservableProperty]
    private double _powerPrice = 0.25;

    /// <summary>Stromverbrauch des Druckers in Watt (Default 150W).</summary>
    [ObservableProperty]
    private double _powerConsumptionW = 150.0;

    /// <summary>Verschleiß-Kosten in €/h (Default 0.05).</summary>
    [ObservableProperty]
    private double _wearCostPerHour = 0.05;

    /// <summary>Filament-Verbrauch in Gramm für diesen Druck (User-Eingabe).</summary>
    [ObservableProperty]
    private double _filamentUsedG = 50.0;

    // === Ergebnisse (live berechnet) ===

    /// <summary>Filamentkosten in Euro (anteilig aus Spule oder pauschal).</summary>
    public double FilamentCostEur =>
        SelectedSpool != null && SelectedSpool.TotalWeightG > 0
            ? (double)SelectedSpool.CostEur * (FilamentUsedG / (double)SelectedSpool.TotalWeightG)
            : 0;

    /// <summary>Stromkosten in Euro (Stunden × Watt × €/kWh).</summary>
    public double PowerCostEur =>
        PrintHours * PowerConsumptionW / 1000.0 * PowerPrice;

    /// <summary>Verschleißkosten in Euro (Stunden × €/h).</summary>
    public double WearCostEur =>
        PrintHours * WearCostPerHour;

    /// <summary>Gesamtkosten in Euro.</summary>
    public double TotalCostEur =>
        FilamentCostEur + PowerCostEur + WearCostEur;

    // === Format-Strings für XAML ===

    public string FilamentCostDisplay => $"{FilamentCostEur:F2} €";
    public string PowerCostDisplay => $"{PowerCostEur:F2} €";
    public string WearCostDisplay => $"{WearCostEur:F2} €";
    public string TotalCostDisplay => $"{TotalCostEur:F2} €";

    public CostCalculatorViewModel() : this(ServiceLocator.CreateDb()) { }

    public CostCalculatorViewModel(FlipsiForgeDbContext db)
    {
        _db = db;
        // Fire-and-forget Load (Pitfall: kein sync-async im Konstruktor)
        _ = Task.Run(async () =>
        {
            try { await LoadSpoolsAsync(); }
            catch { /* Best-effort; leeres Dropdown ist OK */ }
        });
    }

    /// <summary>Lädt die Spulen aus der DB.</summary>
    public async Task LoadSpoolsAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                SpoolOptions.Clear();
                foreach (var s in _db.Spools.Where(s => s.Status == SpoolStatus.Active).ToList())
                    SpoolOptions.Add(new SpoolOptionVm(s));

                // Erste Spule automatisch auswählen wenn vorhanden
                if (SpoolOptions.Count > 0)
                    SelectedSpool = SpoolOptions[0];
            }
            catch
            {
                // Best-effort — leere Liste
            }
        });
    }

    /// <summary>Recalculate-Befehl — benachrichtigt alle Ergebnis-Properties.</summary>
    partial void OnPrintHoursChanged(double value) => RefreshResults();
    partial void OnPowerPriceChanged(double value) => RefreshResults();
    partial void OnPowerConsumptionWChanged(double value) => RefreshResults();
    partial void OnWearCostPerHourChanged(double value) => RefreshResults();
    partial void OnFilamentUsedGChanged(double value) => RefreshResults();
    partial void OnSelectedSpoolChanged(SpoolOptionVm? value) => RefreshResults();

    /// <summary>Benachrichtigt alle Ergebnis-Properties neu (für Live-Berechnung).</summary>
    private void RefreshResults()
    {
        OnPropertyChanged(nameof(FilamentCostEur));
        OnPropertyChanged(nameof(PowerCostEur));
        OnPropertyChanged(nameof(WearCostEur));
        OnPropertyChanged(nameof(TotalCostEur));
        OnPropertyChanged(nameof(FilamentCostDisplay));
        OnPropertyChanged(nameof(PowerCostDisplay));
        OnPropertyChanged(nameof(WearCostDisplay));
        OnPropertyChanged(nameof(TotalCostDisplay));
    }
}