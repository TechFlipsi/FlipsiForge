// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Desktop.Dialogs;

/// <summary>Modaler Spool-Add/Edit-Dialog mit RGB-ColorPicker.</summary>
public partial class SpoolDialog : Window
{
    /// <summary>true = gespeichert, false/null = abgebrochen.</summary>
    public bool? DialogResult { get; private set; }

    private readonly Spool _spool;
    private bool _suppressHexUpdate = false;

    public SpoolDialog() : this(new Spool(), true) { }

    public SpoolDialog(Spool spool, bool isNew)
    {
        InitializeComponent();
        _spool = spool;
        Title = isNew ? "Spule hinzufügen" : $"Spule bearbeiten — {spool.Brand} {spool.MaterialName}";
        LoadMaterialTypes();
        LoadFromSpool();
        WireColorPicker();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LoadMaterialTypes()
    {
        var combo = this.FindControl<ComboBox>("MaterialTypeCombo");
        if (combo == null) return;
        combo.ItemsSource = Enum.GetNames(typeof(MaterialType));
    }

    private void LoadFromSpool()
    {
        Set("BrandBox", _spool.Brand);
        Set("MaterialBox", _spool.MaterialName);
        var mt = this.FindControl<ComboBox>("MaterialTypeCombo");
        if (mt != null) mt.SelectedIndex = (int)_spool.MaterialType;
        Set("ColorNameBox", _spool.ColorName ?? "");

        // Color-Hex in RGB zerlegen
        var (r, g, b) = HexToRgb(_spool.ColorHex);
        SetSlider("RSlider", r);
        SetSlider("GSlider", g);
        SetSlider("BSlider", b);
        UpdateColorDisplay();

        SetNud("DiameterBox", _spool.DiameterMm);
        SetNud("TotalBox", _spool.TotalWeightG);
        SetNud("RemainingBox", _spool.RemainingWeightG);
        SetNud("DensityBox", _spool.DensityGcm3);
        SetNud("CostBox", _spool.CostEur);
        var dp = this.FindControl<DatePicker>("PurchaseDatePicker");
        if (dp != null) dp.SelectedDate = _spool.PurchaseDate;
        Set("QrBox", _spool.QrCode ?? "");
        Set("NfcBox", _spool.NfcTag ?? "");
        Set("NotesBox", _spool.Notes ?? "");
    }

    private void SaveToSpool()
    {
        _spool.Brand = Get("BrandBox") ?? "";
        _spool.MaterialName = Get("MaterialBox") ?? "";
        var mt = this.FindControl<ComboBox>("MaterialTypeCombo");
        if (mt != null && mt.SelectedIndex >= 0)
            _spool.MaterialType = (MaterialType)mt.SelectedIndex;
        _spool.ColorHex = GetCurrentHex();
        _spool.ColorName = string.IsNullOrEmpty(Get("ColorNameBox")) ? null : Get("ColorNameBox");
        _spool.DiameterMm = GetNud("DiameterBox", 1.75m);
        _spool.TotalWeightG = GetNud("TotalBox", 1000);
        _spool.RemainingWeightG = GetNud("RemainingBox", 1000);
        _spool.DensityGcm3 = GetNud("DensityBox", 1.24m);
        _spool.CostEur = GetNud("CostBox", 0);
        var dp = this.FindControl<DatePicker>("PurchaseDatePicker");
        if (dp != null && dp.SelectedDate.HasValue)
            _spool.PurchaseDate = dp.SelectedDate.Value.DateTime;
        _spool.QrCode = string.IsNullOrEmpty(Get("QrBox")) ? null : Get("QrBox");
        _spool.NfcTag = string.IsNullOrEmpty(Get("NfcBox")) ? null : Get("NfcBox");
        _spool.Notes = string.IsNullOrEmpty(Get("NotesBox")) ? null : Get("NotesBox");
    }

    // === ColorPicker ===

    private void WireColorPicker()
    {
        Hook("RSlider");
        Hook("GSlider");
        Hook("BSlider");
    }

    private void Hook(string name)
    {
        if (this.FindControl<Slider>(name) is Slider s)
            s.PropertyChanged += (snd, e) =>
            {
                if (e.Property == Slider.ValueProperty && !_suppressHexUpdate)
                    UpdateColorDisplay();
            };
    }

    private void UpdateColorDisplay()
    {
        var r = (int)(GetSlider("RSlider"));
        var g = (int)(GetSlider("GSlider"));
        var b = (int)(GetSlider("BSlider"));
        var hex = $"#{r:X2}{g:X2}{b:X2}";

        if (this.FindControl<Border>("ColorPreview") is Border bp)
            bp.Background = new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
        if (this.FindControl<TextBlock>("RText") is TextBlock rt) rt.Text = r.ToString();
        if (this.FindControl<TextBlock>("GText") is TextBlock gt) gt.Text = g.ToString();
        if (this.FindControl<TextBlock>("BText") is TextBlock bt) bt.Text = b.ToString();
        if (this.FindControl<TextBlock>("HexText") is TextBlock ht) ht.Text = hex;
    }

    private string GetCurrentHex()
    {
        var r = (int)GetSlider("RSlider");
        var g = (int)GetSlider("GSlider");
        var b = (int)GetSlider("BSlider");
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static (int r, int g, int b) HexToRgb(string hex)
    {
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || hex.Length < 7)
            return (0, 0, 0);
        try
        {
            var r = Convert.ToInt32(hex.Substring(1, 2), 16);
            var g = Convert.ToInt32(hex.Substring(3, 2), 16);
            var b = Convert.ToInt32(hex.Substring(5, 2), 16);
            return (r, g, b);
        }
        catch { return (0, 0, 0); }
    }

    // === Helpers ===
    private void Set(string name, string value)
    {
        if (this.FindControl<TextBox>(name) is TextBox tb) tb.Text = value;
    }
    private string? Get(string name)
        => this.FindControl<TextBox>(name) is TextBox tb ? tb.Text : null;
    private void SetNud(string name, decimal value)
    {
        if (this.FindControl<NumericUpDown>(name) is NumericUpDown n) n.Value = value;
    }
    private decimal GetNud(string name, decimal fallback)
        => this.FindControl<NumericUpDown>(name) is NumericUpDown n && n.Value.HasValue ? n.Value.Value : fallback;
    private void SetSlider(string name, int value)
    {
        if (this.FindControl<Slider>(name) is Slider s)
        {
            _suppressHexUpdate = true;
            s.Value = value;
            _suppressHexUpdate = false;
        }
    }
    private double GetSlider(string name)
        => this.FindControl<Slider>(name) is Slider s ? s.Value : 0;

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        SaveToSpool();
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