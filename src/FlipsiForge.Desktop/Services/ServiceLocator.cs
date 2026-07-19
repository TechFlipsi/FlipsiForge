// SPDX-License-Identifier: GPL-3.0-or-later
// Sehr einfacher Service-Locator für den Desktop — initialisiert in App.
// Erlaubt späteren Austausch der Stubs gegen echte Core.Services-Adapter,
// ohne dass die ViewModels angefasst werden müssen.
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;

namespace FlipsiForge.Desktop.Services;

/// <summary>Zentrale Registry für Desktop-Services (Stubs oder Core-Adapter).</summary>
public static class ServiceLocator
{
    /// <summary>Verzeichnis aller registrierten Services (interface → Instanz).</summary>
    private static readonly Dictionary<Type, object> _services = new();

    /// <summary>Registriert eine Service-Instanz unter ihrem Interface-Typ.</summary>
    public static void Register<TInterface>(TInterface instance) where TInterface : class
        => _services[typeof(TInterface)] = instance ?? throw new ArgumentNullException(nameof(instance));

    /// <summary>Liefert einen registrierten Service oder null.</summary>
    public static TInterface? Get<TInterface>() where TInterface : class
        => _services.TryGetValue(typeof(TInterface), out var s) ? (TInterface)s : null;

    /// <summary>Liefert einen Service oder wirft, wenn nicht registriert.</summary>
    public static TInterface Require<TInterface>() where TInterface : class
        => Get<TInterface>() ?? throw new InvalidOperationException(
            $"Service {typeof(TInterface).Name} nicht registriert. ServiceLocator.Initialize() aufrufen.");

    /// <summary>Initialisiert alle Default-Stubs. Kann von App.OnFrameworkInitializationCompleted überschrieben werden.</summary>
    public static void InitializeDefaults()
    {
        if (Get<ISearchService>() is null)
            Register<ISearchService>(new DesktopStubSearchService(() => new FlipsiForgeDbContext()));
        if (Get<IAIChatEngine>() is null)
            Register<IAIChatEngine>(new StubAIChatEngine());
        if (Get<IPrinterService>() is null)
            Register<IPrinterService>(new StubPrinterService());
    }

    /// <summary>Liefert eine neue DbContext-Instanz (Factory-Pattern).</summary>
    public static FlipsiForgeDbContext CreateDb() => new();
}