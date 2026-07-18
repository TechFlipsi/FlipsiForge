using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlipsiForge.Core.Data;

namespace FlipsiForge.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Datenbank initialisieren + Filament-DB seeden
            using (var db = new FlipsiForgeDbContext())
            {
                db.Database.EnsureCreated();
                FilamentDbSeeder.SeedAsync(db).GetAwaiter().GetResult();
            }

            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}