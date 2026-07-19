// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Desktop.Dialogs;

/// <summary>Modaler Drucker-Add/Edit-Dialog.</summary>
public partial class PrinterDialog : Window
{
    /// <summary>true = gespeichert, false/null = abgebrochen.</summary>
    public bool? DialogResult { get; private set; }

    private readonly Printer _printer;

    public PrinterDialog() : this(new Printer(), true) { }

    public PrinterDialog(Printer printer, bool isNew)
    {
        InitializeComponent();
        _printer = printer;
        Title = isNew ? "Drucker hinzufügen" : $"Drucker bearbeiten — {printer.Brand} {printer.Model}";
        LoadFromPrinter();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LoadFromPrinter()
    {
        var brand = this.FindControl<TextBox>("BrandBox"); if (brand != null) brand.Text = _printer.Brand;
        var model = this.FindControl<TextBox>("ModelBox"); if (model != null) model.Text = _printer.Model;
        var proto = this.FindControl<ComboBox>("ProtocolCombo");
        if (proto != null)
        {
            var idx = (int)_printer.Protocol;
            if (idx >= 0 && idx < proto.ItemCount) proto.SelectedIndex = idx;
        }
        var ip = this.FindControl<TextBox>("IpBox"); if (ip != null) ip.Text = _printer.IpAddress ?? "";
        var usb = this.FindControl<TextBox>("UsbBox"); if (usb != null) usb.Text = _printer.UsbPort ?? "";
        var noz = this.FindControl<NumericUpDown>("NozzleBox"); if (noz != null) noz.Value = (decimal)_printer.NozzleDiameter;
        var bvx = this.FindControl<NumericUpDown>("BvX"); if (bvx != null) bvx.Value = _printer.BuildVolumeX;
        var bvy = this.FindControl<NumericUpDown>("BvY"); if (bvy != null) bvy.Value = _printer.BuildVolumeY;
        var bvz = this.FindControl<NumericUpDown>("BvZ"); if (bvz != null) bvz.Value = _printer.BuildVolumeZ;
        var enc = this.FindControl<CheckBox>("EnclosedCheck"); if (enc != null) enc.IsChecked = _printer.IsEnclosed;
        var dd = this.FindControl<CheckBox>("DirectDriveCheck"); if (dd != null) dd.IsChecked = _printer.IsDirectDrive;
        var mh = this.FindControl<NumericUpDown>("MaxHotendBox"); if (mh != null) mh.Value = _printer.MaxHotendTemp;
        var mb = this.FindControl<NumericUpDown>("MaxBedBox"); if (mb != null) mb.Value = _printer.MaxBedTemp;
        var notes = this.FindControl<TextBox>("NotesBox"); if (notes != null) notes.Text = _printer.Notes ?? "";
    }

    private void SaveToPrinter()
    {
        var brand = this.FindControl<TextBox>("BrandBox"); if (brand != null) _printer.Brand = brand.Text ?? "";
        var model = this.FindControl<TextBox>("ModelBox"); if (model != null) _printer.Model = model.Text ?? "";
        var proto = this.FindControl<ComboBox>("ProtocolCombo");
        if (proto != null && proto.SelectedIndex >= 0)
            _printer.Protocol = (PrinterProtocol)proto.SelectedIndex;
        var ip = this.FindControl<TextBox>("IpBox"); if (ip != null) _printer.IpAddress = string.IsNullOrWhiteSpace(ip.Text) ? null : ip.Text;
        var usb = this.FindControl<TextBox>("UsbBox"); if (usb != null) _printer.UsbPort = string.IsNullOrWhiteSpace(usb.Text) ? null : usb.Text;
        var noz = this.FindControl<NumericUpDown>("NozzleBox"); if (noz != null) _printer.NozzleDiameter = (decimal)(noz.Value ?? 0.4m);
        var bvx = this.FindControl<NumericUpDown>("BvX"); if (bvx != null) _printer.BuildVolumeX = (int)(bvx.Value ?? 220);
        var bvy = this.FindControl<NumericUpDown>("BvY"); if (bvy != null) _printer.BuildVolumeY = (int)(bvy.Value ?? 220);
        var bvz = this.FindControl<NumericUpDown>("BvZ"); if (bvz != null) _printer.BuildVolumeZ = (int)(bvz.Value ?? 250);
        var enc = this.FindControl<CheckBox>("EnclosedCheck"); if (enc != null) _printer.IsEnclosed = enc.IsChecked == true;
        var dd = this.FindControl<CheckBox>("DirectDriveCheck"); if (dd != null) _printer.IsDirectDrive = dd.IsChecked == true;
        var mh = this.FindControl<NumericUpDown>("MaxHotendBox"); if (mh != null) _printer.MaxHotendTemp = (int)(mh.Value ?? 300);
        var mb = this.FindControl<NumericUpDown>("MaxBedBox"); if (mb != null) _printer.MaxBedTemp = (int)(mb.Value ?? 120);
        var notes = this.FindControl<TextBox>("NotesBox"); if (notes != null) _printer.Notes = string.IsNullOrWhiteSpace(notes.Text) ? null : notes.Text;
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        SaveToPrinter();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>Öffnet den Dialog modal über dem Hauptfenster und wartet auf Close.</summary>
    public async Task<bool?> ShowDialogAsync()
    {
        var main = (Avalonia.Application.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (main == null)
        {
            DialogResult = false;
            return false;
        }
        await ShowDialog(main);
        return DialogResult;
    }
}