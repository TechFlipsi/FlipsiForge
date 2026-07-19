// SPDX-License-Identifier: GPL-3.0-or-later
// PrinterViewModel: CRUD-Listen-ViewModel für die Drucker-Übersicht.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Dialogs;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Display-Row für einen Drucker inkl. Verbindungsstatus.</summary>
public sealed class PrinterRowVm : ObservableObject
{
    public Printer Printer { get; }
    public int Id => Printer.Id;
    public string Display => $"{Printer.Brand} {Printer.Model}".Trim();
    public string ProtocolDisplay => Printer.Protocol.ToString();
    public string IpUsb => Printer.IpAddress ?? Printer.UsbPort ?? "—";
    public string Nozzle => Printer.NozzleDiameter.ToString("0.##") + " mm";
    public string BuildVolume => $"{Printer.BuildVolumeX} × {Printer.BuildVolumeY} × {Printer.BuildVolumeZ} mm";

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

    public PrinterRowVm(Printer p) { Printer = p; }

    public void RefreshFromPrinter()
    {
        OnPropertyChanged(nameof(Display));
        OnPropertyChanged(nameof(ProtocolDisplay));
        OnPropertyChanged(nameof(IpUsb));
        OnPropertyChanged(nameof(Nozzle));
        OnPropertyChanged(nameof(BuildVolume));
        OnPropertyChanged(nameof(StatusGlyph));
    }

    /// <summary>Public Wrapper für protected OnPropertyChanged — erlaubt externen ViewModels
    /// (z.B. PrinterViewModel) einzelne Properties dieses RowVm zu refreshen.</summary>
    public void RaisePropertyChanged(string propertyName)
        => OnPropertyChanged(propertyName);
}

/// <summary>ViewModel für die Drucker-Übersicht (CRUD + Verbindungstest).</summary>
public partial class PrinterViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;
    private readonly IPrinterService _printerService;

    /// <summary>Alle Drucker als Display-Rows.</summary>
    public ObservableCollection<PrinterRowVm> Printers { get; } = new();

    /// <summary>Protokoll-Auswahl für Dialoge.</summary>
    public IReadOnlyList<string> ProtocolOptions { get; } =
        new[] { "KlipperMoonraker", "Marlin", "BambuLab", "PrusaLink", "OctoPrint" };

    public PrinterViewModel()
        : this(ServiceLocator.CreateDb(), ServiceLocator.Require<IPrinterService>())
    { }

    public PrinterViewModel(FlipsiForgeDbContext db, IPrinterService printerService)
    {
        _db = db;
        _printerService = printerService;
        Load();
    }

    /// <summary>Lädt alle Drucker aus der DB.</summary>
    public void Load()
    {
        Printers.Clear();
        foreach (var p in _db.Printers.ToList())
            Printers.Add(new PrinterRowVm(p));
    }

    /// <summary>Öffnet den Add-Dialog.</summary>
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

    /// <summary>Öffnet den Edit-Dialog für eine Row.</summary>
    [RelayCommand]
    public async Task EditAsync(PrinterRowVm row)
    {
        if (row == null) return;
        var result = await OpenDialogAsync(row.Printer, isNew: false);
        if (result != true) return;
        _db.SaveChanges();
        row.RefreshFromPrinter();
    }

    /// <summary>Löscht einen Drucker (ohne Historie — wird von Core gehalten).</summary>
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