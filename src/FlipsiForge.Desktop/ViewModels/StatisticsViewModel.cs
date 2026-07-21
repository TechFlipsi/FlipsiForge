// SPDX-License-Identifier: GPL-3.0-or-later
// StatisticsViewModel: Liest PrintHistory-Aggregate aus der DB und stellt
// sie als Bento-Card-Dashboard-Properties bereit. Fire-and-forget Load im
// Konstruktor (try/catch, damit der Tab nie crasht).
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>ViewModel für das Statistik-Dashboard (Bento-Cards).</summary>
public partial class StatisticsViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;

    /// <summary>Anzahl Drucke gesamt.</summary>
    [ObservableProperty]
    private int _totalPrints;

    /// <summary>Erfolgsrate in Prozent (0-100, eine Kommastelle).</summary>
    [ObservableProperty]
    private double _successRate;

    /// <summary>Filament-Verbrauch gesamt in Gramm.</summary>
    [ObservableProperty]
    private double _filamentUsedG;

    /// <summary>Gesamtkosten in Euro.</summary>
    [ObservableProperty]
    private double _totalCostEur;

    /// <summary>Anzahl erfolgreicher Drucke.</summary>
    [ObservableProperty]
    private int _successfulPrints;

    /// <summary>Anzahl fehlgeschlagener Drucke.</summary>
    [ObservableProperty]
    private int _failedPrints;

    /// <summary>Formatierte Gesamt-Drucke-Anzeige.</summary>
    public string TotalPrintsDisplay => TotalPrints.ToString();

    /// <summary>Formatierte Erfolgsrate mit %-Zeichen.</summary>
    public string SuccessRateDisplay => $"{SuccessRate:F1}%";

    /// <summary>Formatierter Filament-Verbrauch (in kg wenn &gt; 1000g).</summary>
    public string FilamentDisplay =>
        FilamentUsedG >= 1000 ? $"{FilamentUsedG / 1000:F2} kg" : $"{FilamentUsedG:F0} g";

    /// <summary>Formatierte Gesamtkosten mit €-Zeichen.</summary>
    public string CostDisplay => $"{TotalCostEur:F2} €";

    public StatisticsViewModel() : this(ServiceLocator.CreateDb()) { }

    public StatisticsViewModel(FlipsiForgeDbContext db)
    {
        _db = db;
        // Fire-and-forget mit try/catch — Tab darf nie crashen (Pitfall: kein
        // sync-async im Konstruktor, Empty-State im XAML).
        _ = Task.Run(async () =>
        {
            try { await LoadAsync(); }
            catch { /* Best-effort; leere Stats sind OK */ }
        });
    }

    /// <summary>Lädt die Aggregate asynchron aus der DB und benachrichtigt die UI.</summary>
    public async Task LoadAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var history = _db.PrintHistory.ToList();
                TotalPrints = history.Count;
                SuccessfulPrints = history.Count(h => h.Success);
                FailedPrints = history.Count(h => !h.Success);
                SuccessRate = TotalPrints > 0
                    ? Math.Round((double)SuccessfulPrints / TotalPrints * 100.0, 1)
                    : 0;
                FilamentUsedG = history
                    .Where(h => h.FilamentUsedG.HasValue)
                    .Sum(h => (double)h.FilamentUsedG!.Value);
                TotalCostEur = history
                    .Where(h => h.CostEur.HasValue)
                    .Sum(h => (double)h.CostEur!.Value);

                // Display-Properties refreshen (abhängige Properties)
                OnPropertyChanged(nameof(TotalPrintsDisplay));
                OnPropertyChanged(nameof(SuccessRateDisplay));
                OnPropertyChanged(nameof(FilamentDisplay));
                OnPropertyChanged(nameof(CostDisplay));
            }
            catch
            {
                // Best-effort — leere Dashboard ist OK
            }
        });
    }
}