// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlipsiForge.Core.Data;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop;

/// <summary>Haupt-Application-Klasse. Initialisiert DB + ServiceLocator + Bot.</summary>
public partial class App : Application
{
    /// <summary>Hält eine Referenz auf das MainViewModel für spätere Zugriffe.</summary>
    public static ViewModels.MainViewModel? MainVm { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // ServiceLocator initialisieren (Default-Stubs)
        ServiceLocator.InitializeDefaults();

        // Desktop-Settings laden (JSON) — wird in SettingsViewModel & ForgeBot genutzt
        var settings = DesktopSettings.Load();

        // DB initialisieren + Filament-DB seeden + AppSettings (best-effort)
        try
        {
            using (var db = new FlipsiForgeDbContext())
            {
                db.Database.EnsureCreated();
                FilamentDbSeeder.SeedAsync(db).GetAwaiter().GetResult();

                // AppSettings-Zeile existiert garantiert (Seed in OnModelCreating).
                // Sync der Desktop-Settings in die DB, falls nicht vorhanden.
                var app = db.Settings.FirstOrDefault();
                if (app != null)
                {
                    app.AiEnabled = settings.AiEnabled;
                    app.AiModel = settings.AiModel;
                    app.ServerMode = settings.ServerMode;
                    app.WebUiEnabled = settings.WebUiEnabled;
                    app.Language = settings.Language;
                    app.WatchFolders = settings.ScanFolders;
                    db.SaveChanges();
                }
            }
        }
        catch (Exception ex)
        {
            // DB-Fehler sind nicht fatal — App soll trotzdem starten
            System.Diagnostics.Debug.WriteLine($"DB-Init fehlerhaft: {ex.Message}");
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            MainVm = mainWindow.DataContext as ViewModels.MainViewModel;

            // Forge-Bot Timer starten, wenn in Settings enabled
            if (settings.BotEnabled && MainVm?.ForgeBot is { } bot)
                bot.Start();

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}