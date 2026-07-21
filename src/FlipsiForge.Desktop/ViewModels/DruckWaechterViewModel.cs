// SPDX-License-Identifier: GPL-3.0-or-later
// DruckWaechterViewModel: Bento-Card ViewModel für den DruckWächter-Tab.
// Zeigt alle Drucker als Karten mit Schiebereglern (Shelly/Licht),
// Filament-Buttons, Temperaturen und Druck-Historie.
// Verdrahtet mit Core.Services.DruckWaechter.DruckWaechterService.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Core.Services.DruckWaechter;
using FlipsiForge.Core.Services.Printing;
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

    /// <summary>Aktualisiert die Karte aus einem DruckWaechterStatus-Snapshot.</summary>
    public void UpdateFromStatus(DruckWaechterStatus status)
    {
        IsShellyOn = status.IsShellyOn;
        PowerW = status.PowerW;
        HasPowerMeter = status.PowerW.HasValue;
        HasLightMacro = status.HasLightMacro;
        HasFilamentMacro = status.HasFilamentMacro;
        ExtruderCount = status.ExtruderCount;

        (StatusText, StatusClass) = status.State switch
        {
            DruckWaechterPrinterState.Printing => ("Druck läuft", "printing"),
            DruckWaechterPrinterState.Idle => ("Bereit", "idle"),
            DruckWaechterPrinterState.Paused => ("Pausiert", "paused"),
            DruckWaechterPrinterState.Error => ("Fehler", "error"),
            _ => ("Offline", "offline")
        };

        // Temperaturen aktualisieren
        ExtruderTemps.Clear();
        for (int i = 0; i < status.ExtruderTemps.Count; i++)
        {
            var (name, temp) = status.ExtruderTemps[i];
            var label = status.ExtruderTemps.Count > 1
                ? $"Kopf {i + 1}"
                : "Extruder";
            ExtruderTemps.Add(new TempRowVm { Label = label, Current = temp });
        }

        BedTemps.Clear();
        for (int i = 0; i < status.BedTemps.Count; i++)
        {
            var (name, temp) = status.BedTemps[i];
            var label = status.BedTemps.Count > 1
                ? $"Bett {i + 1}"
                : "Bett";
            BedTemps.Add(new TempRowVm { Label = label, Current = temp });
        }
    }
}

/// <summary>ViewModel für den DruckWächter-Tab.</summary>
public partial class DruckWaechterViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;
    private DruckWaechterService? _service;
    private readonly HttpClient _httpClient = new();

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

    /// <summary>True wenn keine Drucker vorhanden (für Empty-State UI).</summary>
    public bool HasNoPrinters => Cards.Count == 0;

    public DruckWaechterViewModel() : this(ServiceLocator.CreateDb()) { }

    public DruckWaechterViewModel(FlipsiForgeDbContext db)
    {
        _db = db;
        Load();
        // Refresh asynchron — fehler tolerant, crashed nicht wenn kein Service
        // oder keine Drucker vorhanden.
        _ = Task.Run(async () =>
        {
            try
            {
                await RefreshAllAsync();
            }
            catch
            {
                // Best-effort — UI bleibt sichtbar mit "Offline" Status
            }
        });
    }

    /// <summary>Lädt alle aktiven Drucker aus der DB als Karten.</summary>
    public void Load()
    {
        Cards.Clear();
        var printers = _db.Printers.Where(x => x.IsActive).ToList();
        if (printers.Count == 0)
        {
            // Keine Drucker — leerer Zustand, keine Cards. UI zeigt Header.
            return;
        }
        foreach (var p in printers)
        {
            var card = new DruckWaechterCardVm(p)
            {
                StatusText = "Lade…",
                StatusClass = "idle",
                LastPrintInfo = "—"
            };
            card.ExtruderTemps.Add(new TempRowVm { Label = "Extruder", Current = 0 });
            card.BedTemps.Add(new TempRowVm { Label = "Bett", Current = 0 });
            Cards.Add(card);
        }
    }

    /// <summary>
    /// Initialisiert den DruckWaechterService aus den Desktop-Settings.
    /// Wird beim ersten Refresh aufgerufen.
    /// </summary>
    private DruckWaechterService GetOrCreateService()
    {
        if (_service is not null) return _service;

        var settings = DesktopSettings.Load();
        var config = BuildConfigFromSettings(settings);
        var printers = _db.Printers.Where(x => x.IsActive).ToList();

        _service = new DruckWaechterService(
            _httpClient,
            config,
            printerId => CreateMoonrakerConnection(printerId, printers)
        );

        return _service;
    }

    /// <summary>Erzeugt eine DruckWaechterConfig aus den Desktop-Settings.</summary>
    private DruckWaechterConfig BuildConfigFromSettings(DesktopSettings settings)
    {
        var printers = _db.Printers.Where(x => x.IsActive).ToList();
        var config = new DruckWaechterConfig
        {
            Global = new DruckWaechterGlobalConfig
            {
                Strompreis = settings.DwStrompreis,
                FilamentPreis = settings.DwFilamentPreis,
                AutoAusTimerMinuten = settings.DwAutoAusTimerMinuten,
                AbkuehlSchwelleC = settings.DwAbkuehlSchwelleC,
                NachtModusAktiv = settings.DwNachtModusAktiv,
                NachtModusVon = ParseTimeOnly(settings.DwNachtModusVon, 0, 0),
                NachtModusBis = ParseTimeOnly(settings.DwNachtModusBis, 6, 0),
                TelegramAktiv = settings.DwTelegramAktiv,
                TelegramBotToken = settings.DwTelegramBotToken,
                TelegramChatId = settings.DwTelegramChatId
            },
            Printers = printers.Select(p => new DruckWaechterPrinterConfig
            {
                PrinterId = p.Id,
                ShellyIp = p.ShellyIp, // Aus dem Printer-Model (neu hinzugefügt)
                ShellySwitchId = p.ShellySwitchId,
                ShutdownVerfuegbar = p.Protocol == PrinterProtocol.KlipperMoonraker,
                ShutdownDelaySek = 60,
                LichtMacroAn = "FLASHLIGHT_ON",
                LichtMacroAus = "FLASHLIGHT_OFF",
                FilamentMacroLaden = "LOAD_FILAMENT",
                FilamentMacroEntladen = "UNLOAD_FILAMENT"
            }).ToList()
        };
        return config;
    }

    /// <summary>Erzeugt eine MoonrakerConnection für den gegebenen Drucker.</summary>
    private MoonrakerConnection CreateMoonrakerConnection(int printerId, List<Printer> printers)
    {
        var printer = printers.FirstOrDefault(p => p.Id == printerId);
        var baseUrl = printer?.IpAddress ?? "http://localhost:7125";
        if (!baseUrl.StartsWith("http"))
            baseUrl = $"http://{baseUrl}";
        return new MoonrakerConnection(_httpClient, baseUrl);
    }

    /// <summary>Parst einen "HH:mm" String zu TimeOnly.</summary>
    private static TimeOnly ParseTimeOnly(string? s, int defaultH, int defaultM)
    {
        if (TimeOnly.TryParse(s, out var t)) return t;
        return new TimeOnly(defaultH, defaultM);
    }

    /// <summary>Aktualisiert alle Drucker-Karten asynchron.</summary>
    private async Task RefreshAllAsync()
    {
        if (Cards.Count == 0) return;
        DruckWaechterService svc;
        try
        {
            svc = GetOrCreateService();
        }
        catch
        {
            // Service konnte nicht erstellt werden — alle Cards auf Offline
            foreach (var card in Cards)
            {
                card.StatusText = "Offline";
                card.StatusClass = "offline";
            }
            return;
        }
        foreach (var card in Cards)
        {
            try
            {
                var status = await svc.GetPrinterStatusAsync(card.PrinterId);
                card.UpdateFromStatus(status);
            }
            catch
            {
                card.StatusText = "Offline";
                card.StatusClass = "offline";
            }
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
        if (PopupCard is null) return;
        var svc = GetOrCreateService();
        try
        {
            if (PopupIsLoad)
                await svc.LoadFilamentAsync(PopupCard.PrinterId, PopupSelectedExtruder);
            else
                await svc.UnloadFilamentAsync(PopupCard.PrinterId, PopupSelectedExtruder);
        }
        catch
        {
            // Best-effort — UI schließen trotzdem
        }
        CloseFilamentPopup();
    }

    /// <summary>Schaltet den Shelly für einen Drucker.</summary>
    [RelayCommand]
    public async Task ToggleShellyAsync(DruckWaechterCardVm? card)
    {
        if (card is null || !card.HasShelly) return;
        var svc = GetOrCreateService();
        var newState = !card.IsShellyOn;
        try
        {
            var ok = await svc.SetShellyAsync(card.PrinterId, newState);
            if (ok) card.IsShellyOn = newState;
        }
        catch
        {
            // Best-effort
        }
    }

    /// <summary>Schaltet das Licht für einen Drucker.</summary>
    [RelayCommand]
    public async Task ToggleLightAsync(DruckWaechterCardVm? card)
    {
        if (card is null || !card.HasLightMacro) return;
        var svc = GetOrCreateService();
        var newState = !card.IsLightOn;
        try
        {
            var ok = await svc.SetLightAsync(card.PrinterId, newState);
            if (ok) card.IsLightOn = newState;
        }
        catch
        {
            // Best-effort
        }
    }
}