// SPDX-License-Identifier: GPL-3.0-or-later
// PrinterViewModel: CRUD-Listen-ViewModel fuer die Drucker-Uebersicht.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Dialogs;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Display-Row fuer einen Drucker inkl. Verbindungsstatus.</summary>
public sealed class PrinterRowVm : ObservableObject
{
    public Printer Printer { get; }
    public int Id => Printer.Id;
    public string Display => $"{Printer.Brand} {Printer.Model}".Trim();
    public string ProtocolDisplay => Printer.Protocol.ToString();
    public string IpUsb => Printer.IpAddress ?? Printer.UsbPort ?? "—";
    public string Nozzle => Printer.NozzleDiameter.ToString("0.##") + " mm";
    public string BuildVolume => $"{Printer.BuildVolumeX} × {Printer.BuildVolumeY} × {Printer.BuildVolumeZ} mm";

    /// <summary>true wenn der Drucker eine IP-Adresse hat (fuer "Daten abrufen" Button-Sichtbarkeit).</summary>
    public bool HasIp => !string.IsNullOrWhiteSpace(Printer.IpAddress);

    private ConnectionTestState _conn = ConnectionTestState.Unknown;
    public ConnectionTestState Connection
    {
        get => _conn;
        set => SetProperty(ref _conn, value);
    }

    public string StatusGlyph => Connection switch
    {
        ConnectionTestState.Online => "🟢 online",
        ConnectionTestState.Offline => "🔴 offline",
        ConnectionTestState.Testing => "⏳ teste…",
        _ => "⚪ unbekannt"
    };

    private bool _isDetecting;
    /// <summary>true waehrend AutoDetect laeuft — fuer Loading-Spinner im Button.</summary>
    public bool IsDetecting
    {
        get => _isDetecting;
        set => SetProperty(ref _isDetecting, value);
    }

    public PrinterRowVm(Printer p) { Printer = p; }

    public void RefreshFromPrinter()
    {
        OnPropertyChanged(nameof(Display));
        OnPropertyChanged(nameof(ProtocolDisplay));
        OnPropertyChanged(nameof(IpUsb));
        OnPropertyChanged(nameof(Nozzle));
        OnPropertyChanged(nameof(BuildVolume));
        OnPropertyChanged(nameof(StatusGlyph));
        OnPropertyChanged(nameof(HasIp));
    }

    /// <summary>Public Wrapper fuer protected OnPropertyChanged — erlaubt externen ViewModels
    /// (z.B. PrinterViewModel) einzelne Properties dieses RowVm zu refreshen.</summary>
    public void RaisePropertyChanged(string propertyName)
        => OnPropertyChanged(propertyName);
}

/// <summary>ViewModel fuer die Drucker-Uebersicht (CRUD + Verbindungstest + Auto-Detect).</summary>
public partial class PrinterViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;
    private readonly IPrinterService _printerService;

    /// <summary>Alle Drucker als Display-Rows.</summary>
    public ObservableCollection<PrinterRowVm> Printers { get; } = new();

    /// <summary>Protokoll-Auswahl fuer Dialoge.</summary>
    public IReadOnlyList<string> ProtocolOptions { get; } =
        new[] { "KlipperMoonraker", "Marlin", "BambuLab", "PrusaLink", "OctoPrint" };

    /// <summary>Toast-/Status-Nachricht fuer User-Feedback (Erfolg/Fehler bei Auto-Detect).</summary>
    [ObservableProperty]
    private string? _toastMessage;

    /// <summary>true solange Toast sichtbar ist (fuer Auto-Hide via Timer).</summary>
    [ObservableProperty]
    private bool _isToastVisible;

    /// <summary>true = Erfolgs-Toast (gruen/orange), false = Fehler-Toast (rot).</summary>
    [ObservableProperty]
    private bool _isSuccessToast;

    /// <summary>true = Erfolgs-Toast soll sichtbar sein (IsToastVisible &amp;&amp; IsSuccessToast).</summary>
    [ObservableProperty]
    private bool _isSuccessToastVisible;

    /// <summary>true = Fehler-Toast soll sichtbar sein (IsToastVisible &amp;&amp; !IsSuccessToast).</summary>
    [ObservableProperty]
    private bool _isErrorToastVisible;

    public PrinterViewModel()
        : this(ServiceLocator.CreateDb(), ServiceLocator.Require<IPrinterService>())
    { }

    public PrinterViewModel(FlipsiForgeDbContext db, IPrinterService printerService)
    {
        _db = db;
        _printerService = printerService;
        try { Load(); }
        catch { /* DB-Fehler nicht fatal — UI zeigt leer */ }
    }

    /// <summary>Lädt alle Drucker aus der DB.</summary>
    public void Load()
    {
        Printers.Clear();
        foreach (var p in _db.Printers.ToList())
            Printers.Add(new PrinterRowVm(p));
    }

    /// <summary>Oeffnet den Add-Dialog.</summary>
    [RelayCommand]
    public async Task AddAsync()
    {
        var p = new Printer { Brand = "", Model = "", IsActive = true };
        var result = await OpenDialogAsync(p, isNew: true);
        if (result != true) return;

        _db.Printers.Add(p);
        _db.SaveChanges();
        Printers.Add(new PrinterRowVm(p));
    }

    /// <summary>Oeffnet den Edit-Dialog fuer eine Row.</summary>
    [RelayCommand]
    public async Task EditAsync(PrinterRowVm row)
    {
        if (row == null) return;
        var result = await OpenDialogAsync(row.Printer, isNew: false);
        if (result != true) return;
        _db.SaveChanges();
        row.RefreshFromPrinter();
    }

    /// <summary>Loescht einen Drucker (ohne Historie — wird von Core gehalten).</summary>
    [RelayCommand]
    public async Task DeleteAsync(PrinterRowVm row)
    {
        if (row == null) return;
        // Bestätigungsdialog: Historie bleibt erhalten
        var ok = await ConfirmAsync(
            $"Drucker „{row.Display}“ löschen?\nDie Druck-Historie bleibt erhalten.",
            "Löschen bestätigen");
        if (ok != true) return;

        _db.Printers.Remove(row.Printer);
        _db.SaveChanges();
        Printers.Remove(row);
    }

    /// <summary>Testet die Verbindung zu einem Drucker.</summary>
    [RelayCommand]
    public async Task TestConnectionAsync(PrinterRowVm row)
    {
        if (row == null) return;
        row.Connection = ConnectionTestState.Testing;
        try
        {
            var state = await _printerService.TestConnectionAsync(row.Printer);
            row.Connection = state;
        }
        catch
        {
            row.Connection = ConnectionTestState.Offline;
        }
        row.RaisePropertyChanged(nameof(row.StatusGlyph));
    }

    /// <summary>
    /// Ruft Drucker-Daten per Auto-Detect ab (Bauvolumen, Duese, Temperaturen, Firmware)
    /// und schreibt die Ergebnisse in die DB. Zeigt Toast bei Erfolg/Fehler.
    /// </summary>
    [RelayCommand]
    public async Task AutoDetectAsync(PrinterRowVm row)
    {
        if (row == null) return;
        var ip = row.Printer.IpAddress;
        if (string.IsNullOrWhiteSpace(ip))
        {
            ShowToast("Drucker hat keine IP-Adresse — kann nicht abrufen.", "error");
            return;
        }

        row.IsDetecting = true;
        try
        {
            var result = await _printerService.AutoDetectAsync(ip, row.Printer.Protocol);
            if (result.HasData)
            {
                // Gefundene Werte in den Drucker uebernehmen (nur Werte > 0 / nicht null)
                if (result.BuildVolumeX > 0) row.Printer.BuildVolumeX = result.BuildVolumeX;
                if (result.BuildVolumeY > 0) row.Printer.BuildVolumeY = result.BuildVolumeY;
                if (result.BuildVolumeZ > 0) row.Printer.BuildVolumeZ = result.BuildVolumeZ;
                if (result.MaxHotendTemp > 0) row.Printer.MaxHotendTemp = result.MaxHotendTemp;
                if (result.MaxBedTemp > 0) row.Printer.MaxBedTemp = result.MaxBedTemp;
                if (result.NozzleDiameter > 0) row.Printer.NozzleDiameter = result.NozzleDiameter;
                if (!string.IsNullOrEmpty(result.FirmwareVersion)) row.Printer.FirmwareVersion = result.FirmwareVersion;
                row.Printer.IsEnclosed = result.IsEnclosed;
                row.Printer.IsDirectDrive = result.IsDirectDrive;

                _db.SaveChanges();
                row.RefreshFromPrinter();
                ShowToast("Drucker-Daten aktualisiert", "success");
            }
            else
            {
                ShowToast("Drucker nicht erreichbar", "error");
            }
        }
        catch
        {
            ShowToast("Drucker nicht erreichbar", "error");
        }
        finally
        {
            row.IsDetecting = false;
        }
    }

    /// <summary>Zeigt einen Toast fuer 4 Sekunden an.</summary>
    private void ShowToast(string message, string kind)
    {
        ToastMessage = message;
        IsSuccessToast = kind == "success";
        UpdateToastVisibility(show: true);
        // Auto-Hide nach 4 Sekunden
        _ = Task.Run(async () =>
        {
            await Task.Delay(4000);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => UpdateToastVisibility(show: false));
        });
    }

    /// <summary>Aktualisiert die Toast-Sichtbarkeit (erfolgs- vs fehler-toast).</summary>
    private void UpdateToastVisibility(bool show)
    {
        IsToastVisible = show;
        IsSuccessToastVisible = show && IsSuccessToast;
        IsErrorToastVisible = show && !IsSuccessToast;
    }

    // === Hilfs-Methoden: Dialoge ===

    private static async Task<bool?> OpenDialogAsync(Printer printer, bool isNew)
    {
        var dialog = new PrinterDialog(printer, isNew);
        await dialog.ShowDialogAsync();
        return dialog.DialogResult;
    }

    private static async Task<bool?> ConfirmAsync(string message, string title)
    {
        var dlg = new ConfirmDialog(message, title);
        await dlg.ShowDialogAsync();
        return dlg.DialogResult;
    }
}