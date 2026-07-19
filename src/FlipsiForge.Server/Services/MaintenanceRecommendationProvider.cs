// FlipsiForge.Server — v0.2.0
// MaintenanceRecommendationProvider — generiert Wartungs-Empfehlungen.
// Dual-Mode wie von Sir spezifiziert: mit Internet = modellspezifisch
// (hier Stub mit generischen Hersteller-Hinweisen), ohne Internet =
// allgemeine Tipps basierend auf Druckstunden.
//
// SPDX-License-Identifier: GPL-3.0-only
// (c) 2026 TechFlipsi / Fabian Kirchweger

using FlipsiForge.Core.Models;

namespace FlipsiForge.Server.Services;

/// <summary>
/// Generiert Wartungs-Empfehlungen für einen Drucker in zwei Modi:
/// <list type="bullet">
///   <item>Mit Internet (<c>onlineMode=true</c>): modellspezifische Empfehlungen
///     basierend auf Brand/Model (Stub gibt generische Marken-Hinweise).</item>
///   <item>Ohne Internet (<c>onlineMode=false</c>): allgemeine Tipps für alle
///     Drucker, basierend auf Druckstunden und Standard-Verschleißteilen.</item>
/// </list>
/// Niemals "Internet erforderlich" als Blocker.
/// </summary>
public sealed class MaintenanceRecommendationProvider
{
    /// <summary>Generiert Empfehlungen für den gegebenen Drucker.</summary>
    /// <param name="printer">Drucker-Profil.</param>
    /// <param name="onlineMode">True = modellspezifisch (Internet), False = allgemeine Tipps.</param>
    public Task<IReadOnlyList<MaintenanceRecommendation>> GetRecommendationsAsync(Printer printer, bool onlineMode)
    {
        var list = new List<MaintenanceRecommendation>();
        var hours = (double)printer.TotalPrintHours;

        if (onlineMode)
        {
            // Modellspezifische Stub-Empfehlungen — echte Implementierung würde
            // Hersteller-Datenbank + Community-Tipps befragen.
            list.Add(new MaintenanceRecommendation
            {
                Component = "Nozzle",
                Action = "Inspect for wear",
                Reason = $"{printer.Brand} {printer.Model}: Check nozzle diameter after ~500h",
                OnlineMode = "online",
                IntervalHours = 500
            });
            list.Add(new MaintenanceRecommendation
            {
                Component = "Firmware",
                Action = "Check for updates",
                Reason = $"Brand-specific firmware notes for {printer.Brand}",
                OnlineMode = "online"
            });
        }

        // Allgemeine Empfehlungen — immer vorhanden (auch offline)
        if (hours >= 300)
            list.Add(new MaintenanceRecommendation
            {
                Component = "Nozzle",
                Action = "Replace or clean",
                Reason = $"Allgemein: Düse nach 300-500h prüfen (aktuell: {hours:F1}h)",
                OnlineMode = onlineMode ? "online+offline" : "offline",
                IntervalHours = 300
            });

        if (hours >= 1000)
            list.Add(new MaintenanceRecommendation
            {
                Component = "Belt",
                Action = "Check tension and wear",
                Reason = $"Allgemein: Riemen nach 1000h prüfen (aktuell: {hours:F1}h)",
                OnlineMode = onlineMode ? "online+offline" : "offline",
                IntervalHours = 1000
            });

        if (hours >= 1500)
            list.Add(new MaintenanceRecommendation
            {
                Component = "Bearings",
                Action = "Inspect and lubricate",
                Reason = $"Allgemein: Lager nach 1500h warten (aktuell: {hours:F1}h)",
                OnlineMode = onlineMode ? "online+offline" : "offline",
                IntervalHours = 1500
            });

        if (hours >= 100)
            list.Add(new MaintenanceRecommendation
            {
                Component = "Bed",
                Action = "Clean with isopropyl alcohol",
                Reason = "Allgemein: Druckbett regelmäßig reinigen für erste Schicht",
                OnlineMode = onlineMode ? "online+offline" : "offline",
                IntervalHours = 100
            });

        // Wenn noch keine Empfehlung vorliegt: Standard-Tipp
        if (list.Count == 0)
            list.Add(new MaintenanceRecommendation
            {
                Component = "General",
                Action = "Visual inspection",
                Reason = "Allgemein: Regelmäßige Sichtprüfung aller beweglichen Teile",
                OnlineMode = onlineMode ? "online+offline" : "offline"
            });

        return Task.FromResult<IReadOnlyList<MaintenanceRecommendation>>(list);
    }
}