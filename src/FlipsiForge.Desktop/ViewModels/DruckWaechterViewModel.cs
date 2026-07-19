// SPDX-License-Identifier: GPL-3.0-or-later
// DruckWaechterViewModel: Bento-Card ViewModel für den DruckWächter-Tab.
// Zeigt alle Drucker als Karten mit Schiebereglern (Shelly/Licht),
// Filament-Buttons, Temperaturen und Druck-Historie.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Display-Row für einen Extruder oder ein Bett mit Temperatur.</summary>
public sealed class TempRowVm : ObservableObject
{
    public string Label { get; init; } = "";
    public decimal Current { get; init; }
    public decimal Target { get; init; }

    /// <summary>Prozent Balken (0-100), skaliert auf max 300°C.</summary>
    public double BarPercent => Math.Clamp((double)Current / 300.0 * 100.0, 0, 100);

    /// <summary>"hot" (>150), "warm" (50-150), "cool" (<50).</summary>
    public string BarClass => Current >= 150 ? "hot" : Current >= 50 ? "warm" : "cool";

    public string DisplayTemp => $"{Current:F0}°C";
}

/// <summary>Display-Row für einen Drucker im DruckWächter.</summary>
public sealed class DruckWaechterCardVm : ObservableObject
{
    public Printer Printer { get; }
    public int PrinterId => Printer.Id;
    public string Name => $"{Printer.Brand} {Printer.Model}".Trim();
    public string Description => $"{Printer.Model} ({Printer.Protocol})";

    private bool _isShellyOn;
    public bool IsShellyOn
    {
        get => _isShellyOn;
        set => SetProperty(ref _isShellyOn, value);
    }

    private bool _isLightOn;
    public bool IsLightOn
    {
        get => _isLightOn;
        set => SetProperty(ref _isLightOn, value);
    }

    private bool _hasShelly = true;
    public bool HasShelly
    {
        get => _hasShelly;
        set => SetProperty(ref _hasShelly, value);
    }

    private bool _hasLightMacro = true;
    public bool HasLightMacro
    {
        get => _hasLightMacro;
        set => SetProperty(ref _hasLightMacro, value);
    }

    private bool _hasFilamentMacro = true;
    public bool HasFilamentMacro
    {
        get => _hasFilamentMacro;
        set => SetProperty(ref _hasFilamentMacro, value);
    }

    private string _statusText = "Offline";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>"printing", "idle", "paused", "error", "offline".</summary>
    private string _statusClass = "offline";
    public string StatusClass
    {
        get => _statusClass;
        set => SetProperty(ref _statusClass, value);
    }

    private decimal? _powerW;
    public decimal? PowerW
    {
        get => _powerW;
        set => SetProperty(ref _powerW, value);
    }

    private bool _hasPowerMeter;
    public bool HasPowerMeter
    {
        get => _hasPowerMeter;
        set => SetProperty(ref _hasPowerMeter, value);
    }

    public ObservableCollection<TempRowVm> ExtruderTemps { get; } = new();
    public ObservableCollection<TempRowVm> BedTemps { get; } = new();

    private int _extruderCount = 1;
    public int ExtruderCount
    {
        get => _extruderCount;
        set => SetProperty(ref _extruderCount, value);
    }

    /// <summary>True wenn >1 Extruder → Filament-Popup nötig.</summary>
    public bool IsMultiExtruder => ExtruderCount > 1;

    private string _lastPrintInfo = "—";
    public string LastPrintInfo
    {
        get => _lastPrintInfo;
        set => SetProperty(ref _lastPrintInfo, value);
    }

    public DruckWaechterCardVm(Printer printer)
    {
        Printer = printer;
    }
}

/// <summary>ViewModel für den DruckWächter-Tab.</summary>
public partial class DruckWaechterViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;

    /// <summary>Alle Drucker als Bento-Cards.</summary>
    public ObservableCollection<DruckWaechterCardVm> Cards { get; } = new();

    /// <summary>True wenn ein Filament-Popup offen ist.</summary>
    [ObservableProperty]
    private bool _isFilamentPopupOpen;

    /// <summary>Aktueller Drucker der im Popup ausgewählt ist.</summary>
    [ObservableProperty]
    private DruckWaechterCardVm? _popupCard;

    /// <summary>True = Laden, False = Entladen.</summary>
    [ObservableProperty]
    private bool _popupIsLoad = true;

    /// <summary>Popup-Titel.</summary>
    public string PopupTitle => PopupCard is null
        ? ""
        : $"{(PopupIsLoad ? "📥 Filament Laden" : "📤 Filament Entladen")} — {PopupCard.Name}";

    /// <summary>Ausgewählter Extruder-Index im Popup.</summary>
    [ObservableProperty]
    private int _popupSelectedExtruder;

    public DruckWaechterViewModel() : this(ServiceLocator.CreateDb()) { }

    public DruckWaechterViewModel(FlipsiForgeDbContext db)
    {
        _db = db;
        Load();
    }

    /// <summary>Lädt alle aktiven Drucker aus der DB als Karten.</summary>
    public void Load()
    {
        Cards.Clear();
        foreach (var p in _db.Printers.Where(x => x.IsActive).ToList())
        {
            var card = new DruckWaechterCardVm(p)
            {
                StatusText = "Bereit",
                StatusClass = "idle",
                LastPrintInfo = "—"
            };
            // Default Temperaturen
            card.ExtruderTemps.Add(new TempRowVm { Label = "Extruder", Current = 24, Target = 0 });
            card.BedTemps.Add(new TempRowVm { Label = "Bett", Current = 22, Target = 0 });
            Cards.Add(card);
        }
    }

    /// <summary>Öffnet das Filament-Laden Popup.</summary>
    [RelayCommand]
    public void ShowLoadFilament(DruckWaechterCardVm? card)
    {
        if (card is null || !card.HasFilamentMacro) return;
        PopupCard = card;
        PopupIsLoad = true;
        PopupSelectedExtruder = 0;
        IsFilamentPopupOpen = true;
    }

    /// <summary>Öffnet das Filament-Entladen Popup.</summary>
    [RelayCommand]
    public void ShowUnloadFilament(DruckWaechterCardVm? card)
    {
        if (card is null || !card.HasFilamentMacro) return;
        PopupCard = card;
        PopupIsLoad = false;
        PopupSelectedExtruder = 0;
        IsFilamentPopupOpen = true;
    }

    /// <summary>Schließt das Filament-Popup ohne Aktion.</summary>
    [RelayCommand]
    public void CloseFilamentPopup()
    {
        IsFilamentPopupOpen = false;
        PopupCard = null;
    }

    /// <summary>Bestätigt das Filament-Popup und führt die Aktion aus.</summary>
    [RelayCommand]
    public async Task ConfirmFilamentAsync()
    {
        // TODO: Core.DruckWaechterService.LoadFilamentAsync(printerId, extruderIndex)
        // Vorübergehend: nur Popup schließen
        await Task.Delay(100);
        CloseFilamentPopup();
    }

    /// <summary>Schaltet den Shelly für einen Drucker.</summary>
    [RelayCommand]
    public async Task ToggleShellyAsync(DruckWaechterCardVm? card)
    {
        if (card is null || !card.HasShelly) return;
        // TODO: Core.DruckWaechterService.SetShellyAsync(printerId, !card.IsShellyOn)
        card.IsShellyOn = !card.IsShellyOn;
        await Task.Delay(50);
    }

    /// <summary>Schaltet das Licht für einen Drucker.</summary>
    [RelayCommand]
    public async Task ToggleLightAsync(DruckWaechterCardVm? card)
    {
        if (card is null || !card.HasLightMacro) return;
        // TODO: Core.DruckWaechterService.SetLightAsync(printerId, !card.IsLightOn)
        card.IsLightOn = !card.IsLightOn;
        await Task.Delay(50);
    }
}