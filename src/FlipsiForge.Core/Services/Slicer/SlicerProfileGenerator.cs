using System.Text;
using FlipsiForge.Core.Models;
using FlipsiForge.Core.Services.AI;

namespace FlipsiForge.Core.Services.Slicer;

/// <summary>
/// Generiert Slicer-Profile (PrusaSlicer / OrcaSlicer INI-Format).
/// Nutzt IAIChatEngine für KI-gestützte Generierung — Fallback auf Template-Basis wenn KI nicht geladen.
/// </summary>
public sealed class SlicerProfileGenerator
{
    private readonly IAIChatEngine _ai;

    /// <summary>
    /// Erzeugt den Generator.
    /// </summary>
    /// <param name="ai">KI-Chat-Engine (kann im Stub-Modus sein → Template-Fallback).</param>
    public SlicerProfileGenerator(IAIChatEngine ai)
    {
        _ai = ai;
    }

    /// <summary>
    /// Generiert ein Slicer-Profil basierend auf Drucker, Spule und Druck-Ziel.
    /// </summary>
    /// <param name="printer">Drucker-Profil (Build-Volume, Hotend-Max, etc.).</param>
    /// <param name="spool">Filament-Spule (Material, Temperatur, etc.).</param>
    /// <param name="goal">Druck-Ziel (Stärke / Speed / Qualität).</param>
    /// <returns>SlicerProfile mit INI-Content.</returns>
    public async Task<SlicerProfile> GenerateAsync(Printer printer, Spool spool, PrintGoal goal)
    {
        var name = $"{spool.Brand}_{spool.MaterialType}_{goal}_{DateTime.UtcNow:yyyyMMdd-HHmm}";

        if (_ai.IsLoaded)
        {
            try
            {
                var content = await GenerateWithAiAsync(printer, spool, goal).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return new SlicerProfile
                    {
                        Name = name,
                        Content = content,
                        GeneratedAt = DateTime.UtcNow
                    };
                }
            }
            catch
            {
                // Fallback auf Template
            }
        }

        // Template-Fallback
        return new SlicerProfile
        {
            Name = name,
            Content = GenerateTemplate(printer, spool, goal),
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Baut den KI-Prompt und fragt die Chat-Engine.
    /// </summary>
    private async Task<string> GenerateWithAiAsync(Printer printer, Spool spool, PrintGoal goal)
    {
        var prompt = BuildAiPrompt(printer, spool, goal);
        var history = new List<ChatMessage>();
        var response = await _ai.CompleteChatAsync(history, prompt).ConfigureAwait(false);

        // KI liefert evtl. Markdown-Wrapper — wir extrahieren den INI-Block
        return ExtractIniBlock(response);
    }

    /// <summary>
    /// Baut den Prompt für die KI mit allen relevanten Specs.
    /// </summary>
    private static string BuildAiPrompt(Printer printer, Spool spool, PrintGoal goal)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generiere ein PrusaSlicer-kompatibles INI-Profil mit folgenden Spezifikationen:");
        sb.AppendLine();
        sb.AppendLine("## Drucker");
        sb.AppendLine($"- Brand/Model: {printer.Brand} {printer.Model}");
        sb.AppendLine($"- Bauraum: {printer.BuildVolumeX}×{printer.BuildVolumeY}×{printer.BuildVolumeZ} mm");
        sb.AppendLine($"- Düse: {printer.NozzleDiameter} mm");
        sb.AppendLine($"- Max Hotend-Temp: {printer.MaxHotendTemp}°C");
        sb.AppendLine($"- Max Bed-Temp: {printer.MaxBedTemp}°C");
        sb.AppendLine($"- Enclosure: {(printer.IsEnclosed ? "ja" : "nein")}");
        sb.AppendLine($"- Direct-Drive: {(printer.IsDirectDrive ? "ja" : "nein")}");
        sb.AppendLine();
        sb.AppendLine("## Filament");
        sb.AppendLine($"- Brand/Material: {spool.Brand} {spool.MaterialName} ({spool.MaterialType})");
        sb.AppendLine($"- Durchmesser: {spool.DiameterMm} mm");
        sb.AppendLine($"- Dichte: {spool.DensityGcm3} g/cm³");
        sb.AppendLine();
        sb.AppendLine("## Druck-Ziel");
        sb.AppendLine(goal switch
        {
            PrintGoal.MaximumStrength => "Maximale Festigkeit — hohe Infill-Dichte, langsam, starke Walls",
            PrintGoal.FastPrint => "Schneller Druck — geringere Dichte, hohe Speed, akzeptable Qualität",
            PrintGoal.VisualQuality => "Beste optische Qualität — feine Layer, langsam, anti-stringing",
            PrintGoal.Prototype => "Prototyp — minimale Dichte, schnell, lowest cost",
            _ => "Allrounder"
        });
        sb.AppendLine();
        sb.AppendLine("Antworte NUR mit dem INI-Content (PrusaSlicer-Format), kein Markdown-Wrapper.");

        return sb.ToString();
    }

    /// <summary>
    /// Extrahiert den INI-Block aus der KI-Antwort (entfernt Markdown-Wrapper).
    /// </summary>
    private static string ExtractIniBlock(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return "";
        var text = response.Trim();
        // Strip ```ini ... ``` Wrapper
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0) text = text[(firstNewline + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
        }
        return text.Trim();
    }

    /// <summary>
    /// Template-basierte Generierung ohne KI. Liefert ein grundlegendes PrusaSlicer-INI.
    /// </summary>
    private static string GenerateTemplate(Printer printer, Spool spool, PrintGoal goal)
    {
        var (layerHeight, speed, infill, fan, hotend, bed) = GetMaterialDefaults(spool.MaterialType, goal);

        var sb = new StringBuilder();
        sb.AppendLine("; FlipsiForge generiertes Slicer-Profil (Template-Fallback)");
        sb.AppendLine($"; Drucker: {printer.Brand} {printer.Model}");
        sb.AppendLine($"; Filament: {spool.Brand} {spool.MaterialName} ({spool.MaterialType})");
        sb.AppendLine($"; Ziel: {goal}");
        sb.AppendLine($"; Generiert: {DateTime.UtcNow:O}");
        sb.AppendLine();
        sb.AppendLine("[printer]");
        sb.AppendLine($"nozzle_diameter = {printer.NozzleDiameter:0.##}");
        sb.AppendLine($"bed_shape = 0x0,{printer.BuildVolumeX}x0,{printer.BuildVolumeX}x{printer.BuildVolumeY},0x{printer.BuildVolumeY}");
        sb.AppendLine($"max_print_height = {printer.BuildVolumeZ}");
        sb.AppendLine($"min_extrusion_temp = 170");
        sb.AppendLine($"max_extrusion_temp = {printer.MaxHotendTemp}");
        sb.AppendLine();
        sb.AppendLine("[filament]");
        sb.AppendLine($"filament_diameter = {spool.DiameterMm:0.##}");
        sb.AppendLine($"filament_density = {spool.DensityGcm3:0.###}");
        sb.AppendLine($"temperature = {hotend}");
        sb.AppendLine($"bed_temperature = {bed}");
        sb.AppendLine($"fan_speed = {fan}");
        sb.AppendLine($"cooling = 1");
        sb.AppendLine();
        sb.AppendLine("[print]");
        sb.AppendLine($"layer_height = {layerHeight:0.##}");
        sb.AppendLine($"perimeters = 3");
        sb.AppendLine($"infill_density = {infill}");
        sb.AppendLine($"print_speed = {speed}");
        sb.AppendLine($"travel_speed = 200");
        sb.AppendLine($"retract_length = 1.0");
        sb.AppendLine($"brim_width = 5");
        sb.AppendLine();

        if (spool.MaterialType is MaterialType.ABS or MaterialType.ASA)
        {
            sb.AppendLine("; ABS/ASA-spezifisch");
            sb.AppendLine($"enclose = 1");
            sb.AppendLine($"bed_temperature = {Math.Max(bed, 100)}");
            sb.AppendLine($"fan_speed = 0");
        }
        else if (spool.MaterialType == MaterialType.TPU)
        {
            sb.AppendLine("; TPU-spezifisch");
            sb.AppendLine($"print_speed = 30");
            sb.AppendLine($"retract_length = 0");
            sb.AppendLine($"retract_speed = 20");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Liefert Standard-Werte je Material und Druck-Ziel.
    /// </summary>
    private static (decimal layerHeight, int speed, int infill, int fan, int hotend, int bed)
        GetMaterialDefaults(MaterialType mat, PrintGoal goal)
    {
        // Material-Basis-Werte (layer, speed, infill, fan, hotend, bed)
        return mat switch
        {
            MaterialType.PLA => (0.20m, 60, 20, 100, 210, 60),
            MaterialType.PETG => (0.20m, 50, 20, 50, 235, 80),
            MaterialType.ABS => (0.20m, 50, 25, 0, 245, 100),
            MaterialType.ASA => (0.20m, 50, 25, 20, 250, 100),
            MaterialType.TPU => (0.20m, 30, 20, 50, 220, 50),
            MaterialType.PC => (0.20m, 40, 30, 30, 270, 110),
            MaterialType.PA6 => (0.20m, 50, 30, 50, 260, 90),
            _ => (0.20m, 50, 20, 80, 220, 70)
        } switch
        {
            var b when goal == PrintGoal.MaximumStrength => (b.Item1 * 0.8m, b.Item2 - 10, 60, b.Item4, b.Item5, b.Item6),
            var b when goal == PrintGoal.FastPrint => (b.Item1 * 1.2m, b.Item2 + 30, 10, b.Item4, b.Item5, b.Item6),
            var b when goal == PrintGoal.VisualQuality => (b.Item1 * 0.6m, b.Item2 - 20, 20, b.Item4, b.Item5, b.Item6),
            var b when goal == PrintGoal.Prototype => (b.Item1 * 1.5m, b.Item2 + 40, 5, b.Item4, b.Item5, b.Item6),
            var b => b
        };
    }
}