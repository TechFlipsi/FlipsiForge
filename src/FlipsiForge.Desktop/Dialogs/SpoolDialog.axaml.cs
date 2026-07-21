// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.Dialogs;

/// <summary>Modaler Spool-Add/Edit-Dialog mit Marken-ComboBox, MaterialType-ComboBox,
/// Hex-Code-Eingabe, NFC/QR-Tag-Zuweisung und Auto-Fill aus FilamentBrandSpec.</summary>
public partial class SpoolDialog : Window
{
    /// <summary>true = gespeichert, false/null = abgebrochen.</summary>
    public bool? DialogResult { get; private set; }

    private readonly Spool _spool;
    private bool _suppressHexUpdate = false;
    private bool _suppressSliderUpdate = false;

    // Vorgegebene Marken-Liste
    private static readonly string[] BrandOptions =
    {
        "Bambu Lab", "Prusament", "Polymaker", "Sunlu", "eSun",
        "Creality", "Anycubic", "Filamentum", "Verbatim", "Other"
    };

    // MaterialType-Optionen als Strings (erlaubt "TPU-Flex", "PC-ABS" die nicht im Enum sind)
    private static readonly string[] MaterialTypeOptions =
    {
        "PLA", "PETG", "ABS", "ASA", "TPU", "TPU-Flex", "Nylon",
        "PC", "PC-ABS", "PVA", "HIPS", "Other"
    };

    // Tags-Liste fuer ComboBox
    private List<NfcQrTag> _tags = new();

    public SpoolDialog() : this(new Spool(), true) { }

    public SpoolDialog(Spool spool, bool isNew)
    {
        InitializeComponent();
        _spool = spool;
        Title = isNew ? "Spule hinzufuegen" : $"Spule bearbeiten - {spool.Brand} {spool.MaterialName}";
        LoadBrandOptions();
        LoadMaterialTypeOptions();
        LoadTags();
        LoadFromSpool();
        WireColorPicker();
        UpdateAutoFill();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // === MARKE ===
    private void LoadBrandOptions()
    {
        if (this.FindControl<ComboBox>("BrandCombo") is ComboBox combo)
        {
            combo.ItemsSource = BrandOptions.ToList();
            combo.SelectedIndex = 0;
        }
    }

    private void BrandCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.FindControl<ComboBox>("BrandCombo") is not ComboBox combo) return;
        if (combo.SelectedIndex < 0) return;
        var selected = BrandOptions[combo.SelectedIndex];
        var otherBox = this.FindControl<TextBox>("BrandOtherBox");
        if (otherBox != null)
            otherBox.IsVisible = selected == "Other";
        UpdateAutoFill();
    }

    // === MATERIAL-TYP ===
    private void LoadMaterialTypeOptions()
    {
        if (this.FindControl<ComboBox>("MaterialTypeCombo") is ComboBox combo)
        {
            combo.ItemsSource = MaterialTypeOptions.ToList();
            combo.SelectedIndex = 0;
        }
    }

    private void MaterialTypeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.FindControl<ComboBox>("MaterialTypeCombo") is not ComboBox combo) return;
        if (combo.SelectedIndex < 0) return;
        var selected = MaterialTypeOptions[combo.SelectedIndex];
        var otherBox = this.FindControl<TextBox>("MaterialTypeOtherBox");
        if (otherBox != null)
            otherBox.IsVisible = selected == "Other";
        UpdateAutoFill();
    }

    // === TAGS ===
    private void LoadTags()
    {
        try
        {
            using var db = ServiceLocator.CreateDb();
            _tags = db.NfcQrTags.ToList();
        }
        catch { _tags = new(); }

        if (this.FindControl<ComboBox>("TagCombo") is ComboBox combo)
        {
            var items = new List<string> { "Keiner" };
            foreach (var t in _tags)
                items.Add($"[{t.Type}] {t.Code}");
            combo.ItemsSource = items;
            combo.SelectedIndex = 0;
        }
    }

    private async void AddTag_Click(object? sender, RoutedEventArgs e)
    {
        var dlg = new TagAddDialog();
        var ok = await dlg.ShowDialogAsync();
        if (ok != true) return;
        LoadTags();
        // Selektiere den letzten (neu hinzugefuegten) Tag
        if (this.FindControl<ComboBox>("TagCombo") is ComboBox combo && combo.ItemCount > 1)
            combo.SelectedIndex = combo.ItemCount - 1;
    }

    // === LOAD / SAVE ===

    private void LoadFromSpool()
    {
        // Marke: entweder aus BrandOptions oder "Other" + Custom-Text
        var brandCombo = this.FindControl<ComboBox>("BrandCombo");
        var brandOther = this.FindControl<TextBox>("BrandOtherBox");
        if (brandCombo != null && brandOther != null)
        {
            var idx = Array.IndexOf(BrandOptions, _spool.Brand);
            if (idx >= 0)
            {
                brandCombo.SelectedIndex = idx;
                brandOther.IsVisible = false;
            }
            else
            {
                brandCombo.SelectedIndex = BrandOptions.Length - 1; // "Other"
                brandOther.IsVisible = true;
                brandOther.Text = _spool.Brand;
            }
        }

        Set("MaterialBox", _spool.MaterialName);

        // MaterialType: Enum-Wert oder Custom-String in MaterialName
        var mtCombo = this.FindControl<ComboBox>("MaterialTypeCombo");
        var mtOther = this.FindControl<TextBox>("MaterialTypeOtherBox");
        if (mtCombo != null && mtOther != null)
        {
            var mtString = _spool.MaterialType.ToString();
            var mtIdx = Array.IndexOf(MaterialTypeOptions, mtString);
            if (mtIdx < 0 && !string.IsNullOrEmpty(_spool.MaterialName))
            {
                // Versuche MaterialName zu matchen
                mtIdx = Array.IndexOf(MaterialTypeOptions, _spool.MaterialName);
            }
            if (mtIdx >= 0)
            {
                mtCombo.SelectedIndex = mtIdx;
                mtOther.IsVisible = false;
            }
            else
            {
                mtCombo.SelectedIndex = MaterialTypeOptions.Length - 1; // "Other"
                mtOther.IsVisible = true;
                mtOther.Text = _spool.MaterialType == MaterialType.Other
                    ? _spool.MaterialName
                    : mtString;
            }
        }

        Set("ColorNameBox", _spool.ColorName ?? "");

        // Color-Hex in RGB zerlegen + HexInput synchronisieren
        var (r, g, b) = HexToRgb(_spool.ColorHex);
        SetSlider("RSlider", r);
        SetSlider("GSlider", g);
        SetSlider("BSlider", b);
        UpdateColorDisplay();
        if (this.FindControl<TextBox>("HexInputBox") is TextBox hexBox)
            hexBox.Text = _spool.ColorHex ?? "#000000";

        SetNud("DiameterBox", _spool.DiameterMm);
        SetNud("TotalBox", _spool.TotalWeightG);
        SetNud("RemainingBox", _spool.RemainingWeightG);
        SetNud("DensityBox", _spool.DensityGcm3);
        SetNud("CostBox", _spool.CostEur);
        var dp = this.FindControl<DatePicker>("PurchaseDatePicker");
        if (dp != null) dp.SelectedDate = _spool.PurchaseDate;
        SetNud("HotendTempBox", _spool.RecommendedHotendTemp);
        SetNud("BedTempBox", _spool.RecommendedBedTemp);
        Set("QrBox", _spool.QrCode ?? "");
        Set("NfcBox", _spool.NfcTag ?? "");
        Set("NotesBox", _spool.Notes ?? "");
    }

    private void SaveToSpool()
    {
        // Marke: aus ComboBox oder Other-TextBox
        var brandCombo = this.FindControl<ComboBox>("BrandCombo");
        var brandOther = this.FindControl<TextBox>("BrandOtherBox");
        if (brandCombo != null && brandCombo.SelectedIndex >= 0)
        {
            var selected = BrandOptions[brandCombo.SelectedIndex];
            _spool.Brand = selected == "Other"
                ? (brandOther?.Text ?? "")
                : selected;
        }

        _spool.MaterialName = Get("MaterialBox") ?? "";

        // MaterialType: aus ComboBox oder Other-TextBox -> versuche Enum-Parse
        var mtCombo = this.FindControl<ComboBox>("MaterialTypeCombo");
        var mtOther = this.FindControl<TextBox>("MaterialTypeOtherBox");
        if (mtCombo != null && mtCombo.SelectedIndex >= 0)
        {
            var selected = MaterialTypeOptions[mtCombo.SelectedIndex];
            if (selected == "Other")
            {
                _spool.MaterialType = MaterialType.Other;
                var custom = mtOther?.Text ?? "";
                if (!string.IsNullOrEmpty(custom) && string.IsNullOrEmpty(_spool.MaterialName))
                    _spool.MaterialName = custom;
            }
            else if (Enum.TryParse<MaterialType>(selected, out var mtEnum))
            {
                _spool.MaterialType = mtEnum;
            }
            else
            {
                _spool.MaterialType = MaterialType.Other;
            }
        }

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
        _spool.RecommendedHotendTemp = (int)GetNud("HotendTempBox", 0);
        _spool.RecommendedBedTemp = (int)GetNud("BedTempBox", 0);
        _spool.QrCode = string.IsNullOrEmpty(Get("QrBox")) ? null : Get("QrBox");
        _spool.NfcTag = string.IsNullOrEmpty(Get("NfcBox")) ? null : Get("NfcBox");
        _spool.Notes = string.IsNullOrEmpty(Get("NotesBox")) ? null : Get("NotesBox");

        // Tag-Zuweisung: wenn ein Tag ausgewaehlt wurde, weise es dieser Spule zu
        var tagCombo = this.FindControl<ComboBox>("TagCombo");
        if (tagCombo != null && tagCombo.SelectedIndex > 0 && tagCombo.SelectedIndex - 1 < _tags.Count)
        {
            var tag = _tags[tagCombo.SelectedIndex - 1];
            tag.AssignedSpoolId = _spool.Id;
            try
            {
                using var db = ServiceLocator.CreateDb();
                db.NfcQrTags.Update(tag);
                db.SaveChanges();
            }
            catch { /* nicht fatal */ }
        }
    }

    // === AUTO-FILL aus FilamentBrandSpec ===

    private void UpdateAutoFill()
    {
        var brand = GetSelectedBrand();
        var mtString = GetSelectedMaterialType();
        if (brand == null || mtString == null || brand == "Other" || mtString == "Other")
        {
            HideAutoBadges();
            return;
        }

        try
        {
            using var db = ServiceLocator.CreateDb();
            // Versuche Enum-Match, sonst ignoriere
            if (!Enum.TryParse<MaterialType>(mtString, out var mtEnum))
            {
                HideAutoBadges();
                return;
            }
            var spec = db.FilamentBrandSpecs.FirstOrDefault(s =>
                s.Brand == brand && s.MaterialType == mtEnum);
            if (spec == null)
            {
                HideAutoBadges();
                return;
            }

            // Dichte ausfuellen
            if (spec.DensityGcm3 > 0)
            {
                SetNud("DensityBox", spec.DensityGcm3);
                if (this.FindControl<Border>("DensityAutoBadge") is Border b) b.IsVisible = true;
            }
            // Drucktemp ausfuellen
            if (spec.HotendOptimal > 0)
            {
                SetNud("HotendTempBox", spec.HotendOptimal);
                if (this.FindControl<Border>("HotendAutoBadge") is Border b) b.IsVisible = true;
            }
            // Bett-Temp ausfuellen
            if (spec.BedOptimal > 0)
            {
                SetNud("BedTempBox", spec.BedOptimal);
                if (this.FindControl<Border>("BedAutoBadge") is Border b) b.IsVisible = true;
            }
            // Auto-Badge anzeigen
            if (this.FindControl<StackPanel>("AutoFillBadgePanel") is StackPanel p) p.IsVisible = true;
        }
        catch
        {
            HideAutoBadges();
        }
    }

    private void HideAutoBadges()
    {
        if (this.FindControl<Border>("DensityAutoBadge") is Border d) d.IsVisible = false;
        if (this.FindControl<Border>("HotendAutoBadge") is Border h) h.IsVisible = false;
        if (this.FindControl<Border>("BedAutoBadge") is Border b) b.IsVisible = false;
        if (this.FindControl<StackPanel>("AutoFillBadgePanel") is StackPanel p) p.IsVisible = false;
    }

    private string? GetSelectedBrand()
    {
        if (this.FindControl<ComboBox>("BrandCombo") is not ComboBox combo || combo.SelectedIndex < 0)
            return null;
        var selected = BrandOptions[combo.SelectedIndex];
        if (selected == "Other")
            return this.FindControl<TextBox>("BrandOtherBox")?.Text;
        return selected;
    }

    private string? GetSelectedMaterialType()
    {
        if (this.FindControl<ComboBox>("MaterialTypeCombo") is not ComboBox combo || combo.SelectedIndex < 0)
            return null;
        var selected = MaterialTypeOptions[combo.SelectedIndex];
        if (selected == "Other")
            return this.FindControl<TextBox>("MaterialTypeOtherBox")?.Text;
        return selected;
    }

    // === COLOR PICKER ===

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
                {
                    UpdateColorDisplay();
                    // HexInput synchronisieren (Schieber -> Textfeld)
                    if (!_suppressSliderUpdate)
                    {
                        if (this.FindControl<TextBox>("HexInputBox") is TextBox hexBox)
                        {
                            _suppressSliderUpdate = true;
                            hexBox.Text = GetCurrentHex();
                            _suppressSliderUpdate = false;
                        }
                    }
                }
            };
    }

    /// <summary>Hex-Code-Eingabe aktualisiert die RGB-Schieber und Vorschau.</summary>
    private void HexInput_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressSliderUpdate) return;
        var hex = this.FindControl<TextBox>("HexInputBox")?.Text;
        if (string.IsNullOrEmpty(hex)) return;

        var (r, g, b) = HexToRgb(hex);
        _suppressSliderUpdate = true;
        SetSlider("RSlider", r);
        SetSlider("GSlider", g);
        SetSlider("BSlider", b);
        _suppressSliderUpdate = false;
        UpdateColorDisplay();
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

    // === HELPERS ===
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

    /// <summary>Oeffnet den Dialog modal ueber dem Hauptfenster und wartet auf Close.</summary>
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