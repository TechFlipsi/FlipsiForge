// SPDX-License-Identifier: GPL-3.0-or-later
// SettingsViewModel: bidirektionales ViewModel für die SettingsView.
// Lädt <see cref="DesktopSettings"/> aus JSON und speichert zurück.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Desktop.Services;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>ViewModel für die SettingsView mit allen 10 Sektionen.</summary>
public partial class SettingsViewModel : ViewModelBase
{
    private DesktopSettings _settings;

    // === Dropdown-Options-Listen ===
    public IReadOnlyList<string> LanguageOptions { get; } = new[] { "Deutsch", "English", "Español", "Français" };
    public IReadOnlyList<string> StartTabOptions { get; } = new[] { "Datei-Manager", "Drucker", "Filament", "Model-Repo", "Statistik", "Kosten-Rechner", "DruckWächter", "KI-Assistent", "Forge-Bot" };
    public IReadOnlyList<string> CurrencyOptions { get; } = new[] { "EUR", "USD", "CHF", "GBP" };
    public IReadOnlyList<string> DateFormatOptions { get; } = new[] { "dd.MM.yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "d. MMMM yyyy" };
    public IReadOnlyList<string> LogLevelOptions { get; } = new[] { "Trace", "Debug", "Info", "Warning", "Error" };

    public IReadOnlyList<string> AiModelOptions { get; } = new[] { "Auto", "E4B", "E2B", "E2B QAT", "Aus" };
    public IReadOnlyList<string> BotFrequencyOptions { get; } = new[] { "Selten (15-20 min)", "Normal (6 min)", "Oft (2-3 min)" };
    public IReadOnlyList<string> BotPositionOptions { get; } = new[] { "Unten rechts", "Unten links", "Oben rechts", "Oben links" };
    public IReadOnlyList<string> BotLanguageOptions { get; } = new[] { "Deutsch", "English" };
    public IReadOnlyList<string> ServerModeOptions { get; } = new[] { "Full", "Lite" };
    public IReadOnlyList<string> NotificationKindOptions { get; } = new[] { "In-App", "System", "E-Mail", "Webhook" };

    // === Scan-Ordner ObservableCollection ===
    public ObservableCollection<string> ScanFolders { get; } = new();

    // === Bindable Properties ===
    // 1. Allgemein
    public string Language
    {
        get => _idx(LanguageOptions, _settings.Language, 0);
        set => SetSetting(s => s.Language = LanguageFromDisplay(value));
    }
    public string StartTab
    {
        get => _idx(StartTabOptions, _settings.StartTab, 0);
        set => SetSetting(s => s.StartTab = value);
    }
    public string Currency
    {
        get => _idx(CurrencyOptions, _settings.Currency, 0);
        set => SetSetting(s => s.Currency = value);
    }
    public string DateFormat
    {
        get => _idx(DateFormatOptions, _settings.DateFormat, 0);
        set => SetSetting(s => s.DateFormat = value);
    }
    public bool MinimizeToTray
    {
        get => _settings.MinimizeToTray;
        set => SetSetting(s => s.MinimizeToTray = value);
    }

    // 2. Datei-Manager
    public bool ScanStl { get => _settings.ScanStl; set => SetSetting(s => s.ScanStl = value); }
    public bool Scan3mf { get => _settings.Scan3mf; set => SetSetting(s => s.Scan3mf = value); }
    public bool ScanGcode { get => _settings.ScanGcode; set => SetSetting(s => s.ScanGcode = value); }
    public bool ScanObj { get => _settings.ScanObj; set => SetSetting(s => s.ScanObj = value); }
    public bool AutoScan { get => _settings.AutoScan; set => SetSetting(s => s.AutoScan = value); }
    public int ScanIntervalMinutes { get => _settings.ScanIntervalMinutes; set => SetSetting(s => s.ScanIntervalMinutes = value); }
    public string DefaultView
    {
        get => _settings.DefaultView.ToString();
        set => SetSettingEnum<FileViewMode>((s, e) => s.DefaultView = e, value);
    }
    public string DefaultSort
    {
        get => _settings.DefaultSort.ToString();
        set => SetSettingEnum<FileSortMode>((s, e) => s.DefaultSort = e, value);
    }
    public bool ShowThumbnails { get => _settings.ShowThumbnails; set => SetSetting(s => s.ShowThumbnails = value); }
    public bool DetectDuplicates { get => _settings.DetectDuplicates; set => SetSetting(s => s.DetectDuplicates = value); }
    public string NewScanFolder { get; set; } = "";

    // 3. Drucker
    public bool PrinterAutoConnect { get => _settings.PrinterAutoConnect; set => SetSetting(s => s.PrinterAutoConnect = value); }
    public bool RequirePrintConfirmation { get => _settings.RequirePrintConfirmation; set => SetSetting(s => s.RequirePrintConfirmation = value); }
    public string TemperatureUnit
    {
        get => _settings.TemperatureUnit.ToString();
        set => SetSettingEnum<TempUnit>((s, e) => s.TemperatureUnit = e, value);
    }
    public int WebcamRefreshSeconds { get => _settings.WebcamRefreshSeconds; set => SetSetting(s => s.WebcamRefreshSeconds = value); }

    // 4. Filament
    public bool FilamentCostTracking { get => _settings.FilamentCostTracking; set => SetSetting(s => s.FilamentCostTracking = value); }
    public int DryingLogRetentionDays { get => _settings.DryingLogRetentionDays; set => SetSetting(s => s.DryingLogRetentionDays = value); }
    public bool EnableQrNfc { get => _settings.EnableQrNfc; set => SetSetting(s => s.EnableQrNfc = value); }

    // 5. KI-Assistent
    public bool AiEnabled { get => _settings.AiEnabled; set => SetSetting(s => s.AiEnabled = value); }
    public string AiModel
    {
        get => AiModelToDisplay(_settings.AiModel);
        set => SetSetting(s => s.AiModel = AiModelFromDisplay(value));
    }
    public bool AiSearchEnabled { get => _settings.AiSearchEnabled; set => SetSetting(s => s.AiSearchEnabled = value); }
    public bool AiMaintenanceSuggestions { get => _settings.AiMaintenanceSuggestions; set => SetSetting(s => s.AiMaintenanceSuggestions = value); }
    public bool AiStreaming { get => _settings.AiStreaming; set => SetSetting(s => s.AiStreaming = value); }
    public string? OpenAiApiKey { get => _settings.OpenAiApiKey; set => SetSetting(s => s.OpenAiApiKey = value); }
    public string? AnthropicApiKey { get => _settings.AnthropicApiKey; set => SetSetting(s => s.AnthropicApiKey = value); }
    public string? OllamaUrl { get => _settings.OllamaUrl; set => SetSetting(s => s.OllamaUrl = value); }
    public string? ModelDownloadPath { get => _settings.ModelDownloadPath; set => SetSetting(s => s.ModelDownloadPath = value); }

    // 6. Forge-Bot
    public bool BotEnabled { get => _settings.BotEnabled; set => SetSetting(s => s.BotEnabled = value); }
    public string BotFrequency
    {
        get => BotFreqToDisplay(_settings.BotFrequency);
        set => SetSetting(s => s.BotFrequency = BotFreqFromDisplay(value));
    }
    public string BotDndStart { get => _settings.BotDndStart; set => SetSetting(s => s.BotDndStart = value); }
    public string BotDndEnd { get => _settings.BotDndEnd; set => SetSetting(s => s.BotDndEnd = value); }
    public string BotPosition
    {
        get => BotPosToDisplay(_settings.BotPosition);
        set => SetSetting(s => s.BotPosition = BotPosFromDisplay(value));
    }
    public string BotLanguage
    {
        get => _idx(BotLanguageOptions, _settings.BotLanguage, 0);
        set => SetSetting(s => s.BotLanguage = value == "English" ? "en" : "de");
    }

    // 7. Server & Netzwerk
    public string ServerMode
    {
        get => _settings.ServerMode.ToString();
        set => SetSettingEnum<ServerMode>((s, e) => s.ServerMode = e, value);
    }
    public int ServerPort { get => _settings.ServerPort; set => SetSetting(s => s.ServerPort = value); }
    public bool WebUiEnabled { get => _settings.WebUiEnabled; set => SetSetting(s => s.WebUiEnabled = value); }
    public string ApiKey { get => _settings.ApiKey; set => SetSetting(s => s.ApiKey = value); }
    public bool UseHttps { get => _settings.UseHttps; set => SetSetting(s => s.UseHttps = value); }

    // 8. Benachrichtigungen
    public bool NotifyPushEnabled { get => _settings.NotifyPushEnabled; set => SetSetting(s => s.NotifyPushEnabled = value); }
    public bool NotifyPrintFinished { get => _settings.NotifyPrintFinished; set => SetSetting(s => s.NotifyPrintFinished = value); }
    public bool NotifyPrintFailed { get => _settings.NotifyPrintFailed; set => SetSetting(s => s.NotifyPrintFailed = value); }
    public int FilamentWarningPercent { get => _settings.FilamentWarningPercent; set => SetSetting(s => s.FilamentWarningPercent = value); }
    public bool NotifyMaintenanceReminder { get => _settings.NotifyMaintenanceReminder; set => SetSetting(s => s.NotifyMaintenanceReminder = value); }
    public string NotificationKind
    {
        get => NotKindToDisplay(_settings.NotificationKind);
        set => SetSetting(s => s.NotificationKind = NotKindFromDisplay(value));
    }

    // 9. Erweitert
    public int BackupIntervalDays { get => _settings.BackupIntervalDays; set => SetSetting(s => s.BackupIntervalDays = value); }
    public string BackupPath { get => _settings.BackupPath; set => SetSetting(s => s.BackupPath = value); }
    public string LogLevel
    {
        get => _idx(LogLevelOptions, _settings.LogLevel, 2);
        set => SetSetting(s => s.LogLevel = value);
    }
    public bool DebugMode { get => _settings.DebugMode; set => SetSetting(s => s.DebugMode = value); }

    // 10. Über
    public string AppVersion => "v0.4.0";
    public string License => "GPL-3.0-or-later";
    public string Developer => "TechFlipsi (Fabian Kirchweger)";
    public string Website => "https://techflipsi.at";
    public string GitHub => "https://github.com/TechFlipsi/FlipsiForge";

    // 10. DruckWächter
    public decimal DwStrompreis { get => _settings.DwStrompreis; set => SetSetting(s => s.DwStrompreis = value); }
    public decimal DwFilamentPreis { get => _settings.DwFilamentPreis; set => SetSetting(s => s.DwFilamentPreis = value); }
    public int DwAutoAusTimerMinuten { get => _settings.DwAutoAusTimerMinuten; set => SetSetting(s => s.DwAutoAusTimerMinuten = value); }
    public int DwAbkuehlSchwelleC { get => _settings.DwAbkuehlSchwelleC; set => SetSetting(s => s.DwAbkuehlSchwelleC = value); }
    public bool DwNachtModusAktiv { get => _settings.DwNachtModusAktiv; set => SetSetting(s => s.DwNachtModusAktiv = value); }
    public string DwNachtModusVon { get => _settings.DwNachtModusVon; set => SetSetting(s => s.DwNachtModusVon = value); }
    public string DwNachtModusBis { get => _settings.DwNachtModusBis; set => SetSetting(s => s.DwNachtModusBis = value); }
    public bool DwTelegramAktiv { get => _settings.DwTelegramAktiv; set => SetSetting(s => s.DwTelegramAktiv = value); }
    public string? DwTelegramBotToken { get => _settings.DwTelegramBotToken; set => SetSetting(s => s.DwTelegramBotToken = value); }
    public long? DwTelegramChatId { get => _settings.DwTelegramChatId; set => SetSetting(s => s.DwTelegramChatId = value); }

    // === Status ===
    [ObservableProperty]
    private string _saveStatus = "";

    public SettingsViewModel()
    {
        _settings = DesktopSettings.Load();
        SyncScanFolders();
    }

    /// <summary>Lädt die Settings neu (für Cancel-Verhalten).</summary>
    public void Reload()
    {
        _settings = DesktopSettings.Load();
        SyncScanFolders();
        OnPropertiesChanged();
    }

    /// <summary>Schreibt alle Felder zurück in die JSON + DB.</summary>
    [RelayCommand]
    public void Save()
    {
        _settings.Save();
        SaveStatus = "✓ Gespeichert";
        OnPropertiesChanged(nameof(SaveStatus));
    }

    /// <summary>Setzt auf Werkseinstellungen zurück (mit Bestätigung via UI).</summary>
    [RelayCommand]
    public void ResetToDefaults()
    {
        _settings = DesktopSettings.CreateDefaults();
        _settings.Save();
        SyncScanFolders();
        OnPropertiesChanged();
        SaveStatus = "✓ Auf Werkseinstellungen zurückgesetzt";
    }

    /// <summary>Generiert einen neuen zufälligen API-Key.</summary>
    [RelayCommand]
    public void RegenerateApiKey()
    {
        ApiKey = DesktopSettings.GenerateApiKey();
        _settings.ApiKey = ApiKey;
        _settings.Save();
        OnPropertiesChanged(nameof(ApiKey));
        SaveStatus = "✓ Neuer API-Key generiert";
        OnPropertiesChanged(nameof(SaveStatus));
    }

    /// <summary>Fügt einen Scan-Ordner hinzu.</summary>
    [RelayCommand]
    public void AddScanFolder()
    {
        var path = NewScanFolder?.Trim();
        if (string.IsNullOrEmpty(path)) return;
        if (!_settings.ScanFolders.Contains(path))
        {
            _settings.ScanFolders.Add(path);
            ScanFolders.Add(path);
            _settings.Save();
        }
        NewScanFolder = "";
        OnPropertiesChanged(nameof(NewScanFolder));
    }

    /// <summary>Entfernt einen Scan-Ordner aus der Liste.</summary>
    [RelayCommand]
    private void RemoveScanFolder(string path)
    {
        _settings.ScanFolders.Remove(path);
        ScanFolders.Remove(path);
        _settings.Save();
    }

    /// <summary>Leert den Cache (Stub-Implementierung: löscht ggf. Thumbnails).</summary>
    [RelayCommand]
    public void ClearCache()
    {
        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlipsiForge", "cache");
        if (Directory.Exists(cacheDir))
        {
            try { Directory.Delete(cacheDir, true); } catch { /* best-effort */ }
        }
        SaveStatus = "✓ Cache geleert";
        OnPropertiesChanged(nameof(SaveStatus));
    }

    /// <summary>Exportiert die Settings als JSON-Datei (wahlweise in BackupPath).</summary>
    [RelayCommand]
    public void ExportSettings()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FlipsiForge", $"settings_export_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            _settings.Save();
            File.Copy(DesktopSettings.GetFilePath(), path, overwrite: true);
            SaveStatus = $"✓ Exportiert nach: {path}";
        }
        catch (Exception ex)
        {
            SaveStatus = $"✗ Export fehlgeschlagen: {ex.Message}";
        }
        OnPropertiesChanged(nameof(SaveStatus));
    }

    /// <summary>Oeffnet den Logs-Ordner im System-Explorer (Erweitert-Sektion).</summary>
    [RelayCommand]
    public void OpenLogs()
    {
        try
        {
            var logDir = Services.LoggerService.GetLogDirectory();
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            // Plattform-spezifischer Aufruf des System-Explorers
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
            {
                System.Diagnostics.Process.Start("explorer.exe", logDir);
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Linux))
            {
                // xdg-open oeffnet den Standard-File-Manager
                System.Diagnostics.Process.Start("xdg-open", logDir);
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.OSX))
            {
                System.Diagnostics.Process.Start("open", logDir);
            }

            SaveStatus = $"✓ Logs-Ordner geoeffnet: {logDir}";
        }
        catch (Exception ex)
        {
            SaveStatus = $"✗ Konnte Logs-Ordner nicht oeffnen: {ex.Message}";
        }
        OnPropertiesChanged(nameof(SaveStatus));
    }

    // === Hilfs-Methoden ===

    private void SyncScanFolders()
    {
        ScanFolders.Clear();
        foreach (var f in _settings.ScanFolders)
            ScanFolders.Add(f);
    }

    /// <summary>
    /// Wendet eine Setter-Aktion auf <see cref="_settings"/> an und benachrichtigt
    /// die UI über die Änderung der aufrufenden Property (via CallerMemberName).
    /// </summary>
    private void SetSetting(System.Action<DesktopSettings> setter,
        [System.Runtime.CompilerServices.CallerMemberName] string? prop = null)
    {
        setter(_settings);
        if (prop != null) OnPropertyChanged(prop);
    }

    /// <summary>
    /// Setzt eine Enum-Property anhand ihres Display-Strings. Der Setter-Lambda
    /// bekommt den geparsten Enum-Wert und wendet ihn auf <see cref="_settings"/> an.
    /// </summary>
    private void SetSettingEnum<TEnum>(System.Action<DesktopSettings, TEnum> setter, string displayValue,
        [System.Runtime.CompilerServices.CallerMemberName] string? prop = null)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(displayValue, true, out var e))
        {
            setter(_settings, e);
        }
        if (prop != null) OnPropertyChanged(prop);
    }

    private void OnPropertiesChanged(params string[] props)
    {
        foreach (var p in props) OnPropertyChanged(p);
        // Falls leer → alle refreshen
        if (props.Length == 0)
        {
            foreach (var p in GetType().GetProperties())
                if (p.CanRead && p.GetIndexParameters().Length == 0)
                    OnPropertyChanged(p.Name);
        }
    }

    // === Index-Helfer (String-Anzeige in ComboBox) ===
    // IReadOnlyList<string> hat kein IndexOf — daher Schleife.
    private static string _idx(IReadOnlyList<string> opts, string value, int defaultIdx)
    {
        for (var i = 0; i < opts.Count; i++)
            if (string.Equals(opts[i], value, StringComparison.Ordinal))
                return opts[i];
        return opts[defaultIdx];
    }

    private static string LanguageFromDisplay(string display)
        => display switch { "Deutsch" => "de", "English" => "en", "Español" => "es", "Français" => "fr", _ => "de" };

    private static string AiModelToDisplay(AiModelChoice c)
        => c switch { AiModelChoice.Auto => "Auto", AiModelChoice.E4B => "E4B", AiModelChoice.E2B => "E2B",
            AiModelChoice.E2BQat => "E2B QAT", AiModelChoice.Off => "Aus", _ => "Auto" };
    private static AiModelChoice AiModelFromDisplay(string? s)
        => s switch { "Auto" => AiModelChoice.Auto, "E4B" => AiModelChoice.E4B, "E2B" => AiModelChoice.E2B,
            "E2B QAT" => AiModelChoice.E2BQat, "Aus" => AiModelChoice.Off, _ => AiModelChoice.Auto };

    // WICHTIG: Die Enum-Typen (BotFrequency, BotPosition, NotificationKind) müssen hier
    // vollqualifiziert werden, da die Klasse gleichnamige Properties besitzt und der
    // Compiler sonst die Instance-Property statt des Enum-Typs auflöst (CS0120).
    private static string BotFreqToDisplay(FlipsiForge.Core.Models.BotFrequency f)
        => f switch { FlipsiForge.Core.Models.BotFrequency.Rare => "Selten (15-20 min)",
            FlipsiForge.Core.Models.BotFrequency.Normal => "Normal (6 min)",
            FlipsiForge.Core.Models.BotFrequency.Often => "Oft (2-3 min)", _ => "Normal (6 min)" };
    private static FlipsiForge.Core.Models.BotFrequency BotFreqFromDisplay(string? s)
        => s switch { "Selten (15-20 min)" => FlipsiForge.Core.Models.BotFrequency.Rare,
            "Oft (2-3 min)" => FlipsiForge.Core.Models.BotFrequency.Often,
            _ => FlipsiForge.Core.Models.BotFrequency.Normal };

    private static string BotPosToDisplay(FlipsiForge.Core.Models.BotPosition p)
        => p switch { FlipsiForge.Core.Models.BotPosition.BottomRight => "Unten rechts",
            FlipsiForge.Core.Models.BotPosition.BottomLeft => "Unten links",
            FlipsiForge.Core.Models.BotPosition.TopRight => "Oben rechts",
            FlipsiForge.Core.Models.BotPosition.TopLeft => "Oben links", _ => "Unten rechts" };
    private static FlipsiForge.Core.Models.BotPosition BotPosFromDisplay(string? s)
        => s switch { "Unten links" => FlipsiForge.Core.Models.BotPosition.BottomLeft,
            "Oben rechts" => FlipsiForge.Core.Models.BotPosition.TopRight,
            "Oben links" => FlipsiForge.Core.Models.BotPosition.TopLeft,
            _ => FlipsiForge.Core.Models.BotPosition.BottomRight };

    // Core.NotificationKind hat die Werte InApp, System, Email, Webhook (nicht None/Tray/Desktop/Both).
    private static string NotKindToDisplay(FlipsiForge.Core.Models.NotificationKind k)
        => k switch { FlipsiForge.Core.Models.NotificationKind.InApp => "In-App",
            FlipsiForge.Core.Models.NotificationKind.System => "System",
            FlipsiForge.Core.Models.NotificationKind.Email => "E-Mail",
            FlipsiForge.Core.Models.NotificationKind.Webhook => "Webhook", _ => "System" };
    private static FlipsiForge.Core.Models.NotificationKind NotKindFromDisplay(string? s)
        => s switch { "In-App" => FlipsiForge.Core.Models.NotificationKind.InApp,
            "E-Mail" => FlipsiForge.Core.Models.NotificationKind.Email,
            "Webhook" => FlipsiForge.Core.Models.NotificationKind.Webhook,
            _ => FlipsiForge.Core.Models.NotificationKind.System };
}