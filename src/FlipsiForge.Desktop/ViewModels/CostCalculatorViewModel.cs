// SPDX-License-Identifier: GPL-3.0-or-later
// CostCalculatorViewModel: Interaktiver Druck-Kosten-Rechner.
// Filamentkosten (aus Spule) + Stromkosten (Stunden * Watt * EUR/kWh) + Verschleiss.
// Spulen kommen aus der DB (Brand + Material), Werte werden live berechnet.
// Zusaetzlich: STL/GCODE-Datei laden → Felder sperren (Filament + Druckdauer).
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Display-Row fuer eine Spule im Kosten-Rechner-Dropdown.</summary>
public sealed class SpoolOptionVm : ObservableObject
{
    public Spool Spool { get; }
    public int Id => Spool.Id;
    public string Display => $"{Spool.Brand} - {Spool.MaterialName} ({Spool.RemainingWeightG:F0}g)";
    public decimal RemainingWeightG => Spool.RemainingWeightG;
    public decimal CostEur => Spool.CostEur;
    public decimal TotalWeightG => Spool.TotalWeightG;

    public SpoolOptionVm(Spool s) { Spool = s; }
}

/// <summary>ViewModel fuer den interaktiven Druck-Kosten-Rechner.</summary>
public partial class CostCalculatorViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;

    /// <summary>Verfuegbare Spulen fuer das Dropdown.</summary>
    public ObservableCollection<SpoolOptionVm> SpoolOptions { get; } = new();

    /// <summary>Ausgewaehlte Spule (null = keine).</summary>
    [ObservableProperty]
    private SpoolOptionVm? _selectedSpool;

    /// <summary>Druckdauer in Stunden (Default 3h).</summary>
    [ObservableProperty]
    private double _printHours = 3.0;

    /// <summary>Strompreis in EUR/kWh (Default 0.25).</summary>
    [ObservableProperty]
    private double _powerPrice = 0.25;

    /// <summary>Stromverbrauch des Druckers in Watt (Default 150W).</summary>
    [ObservableProperty]
    private double _powerConsumptionW = 150.0;

    /// <summary>Verschleiss-Kosten in EUR/h (Default 0.05).</summary>
    [ObservableProperty]
    private double _wearCostPerHour = 0.05;

    /// <summary>Filament-Verbrauch in Gramm fuer diesen Druck (User-Eingabe).</summary>
    [ObservableProperty]
    private double _filamentUsedG = 50.0;

    // === Datei-Laden Status ===

    /// <summary>True wenn eine Datei geladen wurde (Badge anzeigen).</summary>
    [ObservableProperty]
    private bool _isFileLoaded;

    /// <summary>Name der geladenen Datei (fuer Badge-Anzeige).</summary>
    [ObservableProperty]
    private string _loadedFileName = "";

    /// <summary>Sperrt das Filament-Feld (wenn aus GCODE/STL geladen).</summary>
    [ObservableProperty]
    private bool _isFilamentLocked;

    /// <summary>Sperrt das Druckdauer-Feld (wenn aus GCODE geladen).</summary>
    [ObservableProperty]
    private bool _isTimeLocked;

    /// <summary>
    /// Wird von der View gesetzt, um den File-Picker zu oeffnen.
    /// Gibt den ausgewaehlten Dateipfad zurueck oder null bei Abbruch.
    /// </summary>
    public Func<Task<string?>>? OpenFilePicker { get; set; }

    // === Ergebnisse (live berechnet) ===

    /// <summary>Filamentkosten in Euro (anteilig aus Spule oder pauschal).</summary>
    public double FilamentCostEur =>
        SelectedSpool != null && SelectedSpool.TotalWeightG > 0
            ? (double)SelectedSpool.CostEur * (FilamentUsedG / (double)SelectedSpool.TotalWeightG)
            : 0;

    /// <summary>Stromkosten in Euro (Stunden * Watt * EUR/kWh).</summary>
    public double PowerCostEur =>
        PrintHours * PowerConsumptionW / 1000.0 * PowerPrice;

    /// <summary>Verschleisskosten in Euro (Stunden * EUR/h).</summary>
    public double WearCostEur =>
        PrintHours * WearCostPerHour;

    /// <summary>Gesamtkosten in Euro.</summary>
    public double TotalCostEur =>
        FilamentCostEur + PowerCostEur + WearCostEur;

    // === Format-Strings fuer XAML ===

    public string FilamentCostDisplay => $"{FilamentCostEur:F2} EUR";
    public string PowerCostDisplay => $"{PowerCostEur:F2} EUR";
    public string WearCostDisplay => $"{WearCostEur:F2} EUR";
    public string TotalCostDisplay => $"{TotalCostEur:F2} EUR";

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

    /// <summary>Laedt die Spulen aus der DB (FUER das Filament-Dropdown).</summary>
    public async Task LoadSpoolsAsync()
    {
        try
        {
            var spools = _db.Spools.Where(s => s.Status == SpoolStatus.Active).ToList();
            SpoolOptions.Clear();
            foreach (var s in spools)
                SpoolOptions.Add(new SpoolOptionVm(s));
            if (SpoolOptions.Count > 0 && SelectedSpool == null)
                SelectedSpool = SpoolOptions[0];
        }
        catch
        {
            // Best-effort - leere Liste
        }
    }

    /// <summary>Oeffnet den File-Picker und laedt die ausgewaehlte Datei (STL/GCODE).</summary>
    [RelayCommand]
    public async Task LoadFileAsync()
    {
        if (OpenFilePicker == null) return;
        var path = await OpenFilePicker();
        if (path == null) return;
        await LoadFromPathAsync(path);
    }

    /// <summary>Entfernt die geladene Datei und hebt die Sperre auf.</summary>
    [RelayCommand]
    public void ClearFile()
    {
        IsFileLoaded = false;
        LoadedFileName = "";
        IsFilamentLocked = false;
        IsTimeLocked = false;
    }

    /// <summary>Parst die Datei und sperrt die entsprechenden Felder.</summary>
    private async Task LoadFromPathAsync(string path)
    {
        try
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            LoadedFileName = Path.GetFileName(path);

            if (ext == ".gcode" || ext == ".gco")
            {
                var (filamentG, timeH) = await ParseGcodeAsync(path);
                if (filamentG.HasValue)
                {
                    FilamentUsedG = Math.Round(filamentG.Value, 1);
                    IsFilamentLocked = true;
                }
                if (timeH.HasValue)
                {
                    PrintHours = Math.Round(timeH.Value, 2);
                    IsTimeLocked = true;
                }
            }
            else if (ext == ".stl" || ext == ".3mf")
            {
                // Einfache Schaetzung: Dateigroesse / 1000 * 1.24 (PLA density approximation)
                var fileInfo = new FileInfo(path);
                var weightG = fileInfo.Length / 1000.0 * 1.24;
                FilamentUsedG = Math.Round(weightG, 1);
                IsFilamentLocked = true;
                // Druckdauer bei STL unklar → nicht sperren
            }

            IsFileLoaded = true;
        }
        catch
        {
            // Best-effort — bei Fehler Datei als nicht geladen markieren
            IsFileLoaded = false;
        }
    }

    /// <summary>Parst GCODE-Datei und extrahiert filament_used + estimated_time.</summary>
    private async Task<(double? filamentG, double? timeH)> ParseGcodeAsync(string path)
    {
        double? filamentG = null;
        double? timeH = null;

        // GCODE kann gross sein → Zeile fuer Zeile lesen bis beide Werte gefunden
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (filamentG == null && TryParseFilamentGrams(line, out var g))
                filamentG = g;
            if (timeH == null && TryParsePrintHours(line, out var h))
                timeH = h;
            if (filamentG != null && timeH != null) break;
        }
        return (filamentG, timeH);
    }

    // Regex fuer ";Filament used:" / "; filament used" - Extrahiere Gramm
    // Beispielformate:
    //   ;Filament used: 12.34g
    //   ; filament used = 12.34g
    //   ; filament used: 12.3456g
    private static readonly Regex FilamentUsedRegex =
        new(@";\s*[Ff]ilament\s+used[:=]\s*([\d.]+)\s*g", RegexOptions.Compiled);

    // Regex fuer "; estimated printing time" / ";M73"
    // Beispielformate:
    //   ; estimated printing time (normal mode) = 2h 30m 15s
    //   ; estimated printing time = 2h 30m 15s
    //   ;M73 P50 R45          → M73 R = remaining time
    //   ;M73 P100 R0
    private static readonly Regex EstTimeRegex =
        new(@";\s*estimated\s+printing\s+time.*?=\s*([\dhms \t]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex M73TimeRegex =
        new(@";M73\s+.*?R(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static bool TryParseFilamentGrams(string line, out double grams)
    {
        grams = 0;
        var m = FilamentUsedRegex.Match(line);
        if (!m.Success) return false;
        return double.TryParse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out grams);
    }

    /// <summary>Versucht Druckzeit aus einer GCODE-Zeile zu extrahieren (Stunden).</summary>
    private static bool TryParsePrintHours(string line, out double hours)
    {
        hours = 0;

        // Format 1: ; estimated printing time ... = 2h 30m 15s
        var m = EstTimeRegex.Match(line);
        if (m.Success)
        {
            hours = ParseTimeSpanString(m.Groups[1].Value);
            return hours > 0;
        }

        // Format 2: ;M73 P50 R45 → R = remaining minutes
        var m73 = M73TimeRegex.Match(line);
        if (m73.Success && int.TryParse(m73.Groups[1].Value, out var remainingMin))
        {
            // Nur der letzte M73-Wert zaehlt → wir nehmen ihn als geschaetzte Restzeit
            // (nicht perfekt, aber ein Start)
            hours = remainingMin / 60.0;
            return true;
        }

        return false;
    }

    /// <summary>Parst einen TimeSpan-String wie "2h 30m 15s" in Stunden.</summary>
    private static double ParseTimeSpanString(string s)
    {
        double hours = 0;
        var hMatch = Regex.Match(s, @"(\d+)\s*h", RegexOptions.IgnoreCase);
        var mMatch = Regex.Match(s, @"(\d+)\s*m(?!s)", RegexOptions.IgnoreCase);
        var sMatch = Regex.Match(s, @"(\d+)\s*s", RegexOptions.IgnoreCase);
        if (hMatch.Success) hours += int.Parse(hMatch.Groups[1].Value);
        if (mMatch.Success) hours += int.Parse(mMatch.Groups[1].Value) / 60.0;
        if (sMatch.Success) hours += int.Parse(sMatch.Groups[1].Value) / 3600.0;
        return hours;
    }

    // === Live-Berechnung Refresh ===

    /// <summary>Recalculate-Befehl - benachrichtigt alle Ergebnis-Properties.</summary>
    partial void OnPrintHoursChanged(double value) => RefreshResults();
    partial void OnPowerPriceChanged(double value) => RefreshResults();
    partial void OnPowerConsumptionWChanged(double value) => RefreshResults();
    partial void OnWearCostPerHourChanged(double value) => RefreshResults();
    partial void OnFilamentUsedGChanged(double value) => RefreshResults();
    partial void OnSelectedSpoolChanged(SpoolOptionVm? value) => RefreshResults();

    /// <summary>Benachrichtigt alle Ergebnis-Properties neu (fuer Live-Berechnung).</summary>
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