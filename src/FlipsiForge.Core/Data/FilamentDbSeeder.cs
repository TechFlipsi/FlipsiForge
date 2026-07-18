using FlipsiForge.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FlipsiForge.Core.Data;

/// <summary>Seedet die Filament-Marken-Datenbank mit 79 Einträgen.</summary>
public static class FilamentDbSeeder
{
    public static async Task SeedAsync(FlipsiForgeDbContext db)
    {
        if (await db.FilamentBrandSpecs.AnyAsync())
            return;

        var seeds = GetSeedData();
        db.FilamentBrandSpecs.AddRange(seeds);
        await db.SaveChangesAsync();
    }

    private static List<FilamentBrandSpec> GetSeedData()
    {
        var list = new List<FilamentBrandSpec>();

        // eSUN
        list.Add(New("eSUN", "PLA+", MaterialType.PLA, 200, 230, 215, 40, 60, 50, 100, 40, 150, 60, 0.8m,
            maxService: 55, impactResistant: true, density: 1.24m, tensile: 50,
            suitable: "Dekoration, Prototypen, Spielzeug, Innenbereich",
            notSuitable: "Außenbereich, Auto, heiße Umgebungen, Geschirr",
            notes: "Impact-resistenter als Standard PLA"));

        list.Add(New("eSUN", "PETG", MaterialType.PETG, 220, 250, 235, 60, 90, 80, 50, 30, 100, 50, 1.5m,
            maxService: 70, foodSafe: true, weatherResistant: true, heatResistant: true,
            needsDrying: true, dryingTemp: 65, dryingHours: 4, stringsEasily: true,
            impactResistant: true, density: 1.27m, tensile: 55,
            suitable: "Funktionsbauteile, Außenbereich (ohne direkte Sonne), Geschirr",
            notSuitable: "Auto-Innenraum (>70°C), direkte UV-Last",
            notes: "Stringing bei zu heiß"));

        list.Add(New("eSUN", "ABS+", MaterialType.ABS, 230, 250, 240, 90, 110, 100, 30, 30, 80, 50, 1.0m,
            maxService: 95, heatResistant: true, needsEnclosure: true, warpsEasily: true,
            impactResistant: true, density: 1.04m, tensile: 45,
            suitable: "Auto-Innenraum, Funktionsbauteile, Gehäuse",
            notSuitable: "Außenbereich (UV), Kontakt mit Lebensmitteln",
            notes: "Weniger Warping als Standard ABS"));

        list.Add(New("eSUN", "TPU 95A", MaterialType.TPU, 210, 230, 220, 30, 60, 45, 50, 15, 40, 25, 0m,
            maxService: 80, flexible: true, weatherResistant: true, needsDirectDrive: true,
            needsDrying: true, dryingTemp: 50, dryingHours: 4, impactResistant: true,
            density: 1.21m, tensile: 30,
            suitable: "Dichtungen, Griffe, Schutzhüllen, flexible Teile",
            notSuitable: "Steife Bauteile, feine Details, High-Speed",
            notes: "Retraction AUS! Direct Drive empfohlen"));

        list.Add(New("eSUN", "ASA", MaterialType.ASA, 240, 260, 250, 90, 110, 100, 30, 30, 80, 50, 1.0m,
            maxService: 90, uvResistant: true, weatherResistant: true, heatResistant: true,
            needsEnclosure: true, warpsEasily: true, impactResistant: true,
            density: 1.05m, tensile: 48,
            suitable: "Außenbereich, Auto-Exterieur, Garten, Wetterfest",
            notSuitable: "Kontakt mit Lebensmitteln, Anfänger (schwierig)",
            notes: "UV-resistent, wetterfest"));

        // Prusament
        list.Add(New("Prusament", "PLA", MaterialType.PLA, 190, 220, 210, 50, 60, 55, 100, 40, 150, 80, 0.8m,
            maxService: 55, biodegradable: true, recyclable: true, density: 1.24m, tensile: 60,
            suitable: "Dekoration, Prototypen, Figuren, Innenbereich",
            notSuitable: "Außenbereich, Auto, heiße Umgebungen",
            notes: "±0.02mm Toleranz, OpenPrintTag NFC"));

        list.Add(New("Prusament", "PETG", MaterialType.PETG, 230, 245, 240, 70, 90, 80, 50, 30, 100, 50, 1.5m,
            maxService: 70, foodSafe: true, weatherResistant: true, heatResistant: true,
            needsDrying: true, dryingTemp: 65, dryingHours: 4, stringsEasily: true,
            impactResistant: true, density: 1.27m, tensile: 55,
            suitable: "Funktionsbauteile, Außen, Geschirr, Lagerung",
            notSuitable: "Auto-Innenraum (>70°C)",
            notes: "240°C für beste Layer-Haftung"));

        list.Add(New("Prusament", "ASA", MaterialType.ASA, 240, 260, 250, 90, 110, 100, 30, 30, 80, 50, 1.0m,
            maxService: 90, uvResistant: true, weatherResistant: true, heatResistant: true,
            needsEnclosure: true, warpsEasily: true, impactResistant: true,
            density: 1.05m, tensile: 48,
            suitable: "Außenbereich, Auto, Garten, UV-exponiert",
            notSuitable: "Lebensmittel, Anfänger",
            notes: "UV-resistent, Enclosure empfohlen"));

        list.Add(New("Prusament", "ABS", MaterialType.ABS, 230, 250, 240, 90, 110, 100, 30, 30, 80, 50, 1.0m,
            maxService: 95, heatResistant: true, needsEnclosure: true, warpsEasily: true,
            impactResistant: true, density: 1.04m, tensile: 45,
            suitable: "Auto-Innenraum, Funktionsbauteile",
            notSuitable: "Außenbereich (UV), Lebensmittel",
            notes: "Warping möglich, Enclosure nötig"));

        // Polymaker
        list.Add(New("Polymaker", "PolyLite PLA", MaterialType.PLA, 190, 220, 210, 40, 60, 50, 100, 40, 150, 60, 0.8m,
            maxService: 55, biodegradable: true, density: 1.24m, tensile: 50,
            suitable: "Standard PLA, gut Preis-Leistung",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Standard PLA"));

        list.Add(New("Polymaker", "PolyLite PETG", MaterialType.PETG, 220, 250, 240, 60, 90, 80, 50, 30, 100, 50, 1.5m,
            maxService: 70, foodSafe: true, weatherResistant: true, heatResistant: true,
            needsDrying: true, dryingTemp: 65, dryingHours: 4, stringsEasily: true,
            density: 1.27m, tensile: 55,
            suitable: "Gute Layer-Haftung",
            notSuitable: "Auto-Innenraum (>70°C)",
            notes: "Etwas spröder als eSUN PETG"));

        list.Add(New("Polymaker", "CoPA (Nylon)", MaterialType.PA6, 250, 280, 270, 80, 100, 90, 40, 20, 60, 40, 1.0m,
            maxService: 110, heatResistant: true, needsEnclosure: true,
            needsDrying: true, dryingTemp: 80, dryingHours: 8, warpsEasily: true,
            impactResistant: true, density: 1.52m, tensile: 70,
            suitable: "Funktionsbauteile, Zahnräder, Verschleißteile, Auto",
            notSuitable: "Außen (UV), feuchtigkeitsempfindlich, Anfänger",
            notes: "MUSS getrocknet werden! Zieht Feuchtigkeit"));

        list.Add(New("Polymaker", "PolyFlex TPU90", MaterialType.TPU, 220, 240, 230, 30, 60, 45, 50, 15, 40, 25, 0m,
            maxService: 80, flexible: true, weatherResistant: true, needsDirectDrive: true,
            needsDrying: true, dryingTemp: 50, dryingHours: 4, impactResistant: true,
            density: 1.21m, tensile: 28,
            suitable: "Dichtungen, Griffe, flexible Verbindungen",
            notSuitable: "Steife Bauteile, feine Details",
            notes: "Flexibel, Retraction AUS"));

        list.Add(New("Polymaker", "PC-Max", MaterialType.PC, 260, 300, 280, 100, 120, 110, 30, 20, 60, 40, 1.0m,
            maxService: 115, heatResistant: true, needsEnclosure: true,
            needsDrying: true, dryingTemp: 100, dryingHours: 4, warpsEasily: true,
            impactResistant: true, density: 1.30m, tensile: 65,
            suitable: "Hitzeschild, Industrielle Teile, hohe Belastung",
            notSuitable: "Außen (UV), Anfänger, ohne Enclosure",
            notes: "Enclosure Pflicht, hitzebeständig"));

        // Bambu Lab
        list.Add(New("Bambu Lab", "PLA Matte", MaterialType.PLA, 190, 220, 210, 35, 55, 45, 100, 50, 200, 120, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "Dekoration, Figuren, matte Oberflächen, High-Speed",
            notSuitable: "Außen, Auto, Heiß",
            notes: "High-Speed optimiert, RFID-Tags"));

        list.Add(New("Bambu Lab", "PLA Basic", MaterialType.PLA, 190, 220, 210, 35, 55, 45, 100, 50, 200, 120, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "Standard für Bambu Drucker",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Standard für Bambu Drucker"));

        list.Add(New("Bambu Lab", "PETG HF", MaterialType.PETG, 220, 250, 240, 60, 90, 80, 50, 50, 200, 100, 1.5m,
            maxService: 70, foodSafe: true, weatherResistant: true, heatResistant: true,
            needsDrying: true, dryingTemp: 65, dryingHours: 4, stringsEasily: true,
            impactResistant: true, density: 1.27m, tensile: 55,
            suitable: "Funktionsbauteile, High-Speed, Geschirr",
            notSuitable: "Auto-Innenraum (>70°C)",
            notes: "High-Speed PETG"));

        list.Add(New("Bambu Lab", "ABS", MaterialType.ABS, 230, 260, 245, 90, 110, 100, 30, 30, 80, 50, 1.0m,
            maxService: 95, heatResistant: true, needsEnclosure: true, warpsEasily: true,
            impactResistant: true, density: 1.04m, tensile: 45,
            suitable: "Auto-Innenraum, Gehäuse, Funktionsbauteile",
            notSuitable: "Außen (UV), Lebensmittel",
            notes: "Für Bambu X1C/P1S (geschlossen)"));

        list.Add(New("Bambu Lab", "TPU 95A", MaterialType.TPU, 210, 240, 225, 30, 60, 45, 50, 15, 40, 25, 0m,
            maxService: 80, flexible: true, weatherResistant: true, needsDirectDrive: true,
            needsDrying: true, dryingTemp: 50, dryingHours: 4, impactResistant: true,
            density: 1.21m, tensile: 30,
            suitable: "Dichtungen, Griffe, Schutzhüllen",
            notSuitable: "Steife Bauteile, High-Speed",
            notes: "Retraction AUS, langsam"));

        // Sunlu
        list.Add(New("Sunlu", "PLA", MaterialType.PLA, 190, 220, 210, 40, 60, 50, 100, 40, 150, 60, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "Budget Dekoration, Prototypen, Innenbereich",
            notSuitable: "Außen, Auto, Heiß, Qualitäts-kritisch",
            notes: "Budget PLA, gute Qualität"));

        list.Add(New("Sunlu", "PETG", MaterialType.PETG, 220, 250, 235, 60, 90, 80, 50, 30, 100, 50, 1.5m,
            maxService: 70, foodSafe: true, weatherResistant: true, heatResistant: true,
            needsDrying: true, dryingTemp: 65, dryingHours: 4, stringsEasily: true,
            density: 1.27m, tensile: 55,
            suitable: "Budget PETG",
            notSuitable: "Auto-Innenraum (>70°C)",
            notes: "Budget PETG"));

        list.Add(New("Sunlu", "ABS", MaterialType.ABS, 230, 250, 240, 90, 110, 100, 30, 30, 80, 50, 1.0m,
            maxService: 95, heatResistant: true, needsEnclosure: true, warpsEasily: true,
            density: 1.04m, tensile: 45,
            suitable: "Budget ABS",
            notSuitable: "Außen (UV), Lebensmittel",
            notes: "Budget ABS"));

        // Overture
        list.Add(New("Overture", "PLA", MaterialType.PLA, 190, 220, 210, 40, 60, 50, 100, 40, 150, 60, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "±0.02mm Toleranz, gut dokumentiert",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Konsistente Qualität"));

        list.Add(New("Overture", "PETG", MaterialType.PETG, 220, 245, 235, 60, 90, 80, 50, 30, 100, 50, 1.5m,
            maxService: 70, foodSafe: true, weatherResistant: true, heatResistant: true,
            needsDrying: true, dryingTemp: 65, dryingHours: 4, stringsEasily: true,
            density: 1.27m, tensile: 55,
            suitable: "Konsistente Qualität",
            notSuitable: "Auto-Innenraum (>70°C)",
            notes: "Gut dokumentiert"));

        // Hatchbox
        list.Add(New("Hatchbox", "PLA", MaterialType.PLA, 190, 220, 210, 40, 60, 50, 100, 40, 150, 60, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "Beliebte US-Marke, zuverlässig",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Beliebte US-Marke"));

        // Elegoo
        list.Add(New("Elegoo", "PLA", MaterialType.PLA, 190, 220, 210, 40, 60, 50, 100, 40, 150, 60, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "Gute Budget-Qualität",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Gute Budget-Qualität"));

        list.Add(New("Elegoo", "PETG", MaterialType.PETG, 220, 250, 240, 60, 90, 80, 50, 30, 100, 50, 1.5m,
            maxService: 70, foodSafe: true, weatherResistant: true, heatResistant: true,
            needsDrying: true, dryingTemp: 65, dryingHours: 4, stringsEasily: true,
            density: 1.27m, tensile: 55,
            suitable: "Beliebt mit Neptune Druckern",
            notSuitable: "Auto-Innenraum (>70°C)",
            notes: "Beliebt mit Neptune Druckern"));

        // Creality
        list.Add(New("Creality", "PLA", MaterialType.PLA, 190, 220, 210, 40, 60, 50, 100, 40, 150, 60, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "Oft mit Drucker gebündelt",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Oft mit Drucker gebündelt"));

        // Inland
        list.Add(New("Inland", "PLA", MaterialType.PLA, 190, 220, 210, 40, 60, 50, 100, 40, 150, 60, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "Micro Center Hausmarke, günstig",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Micro Center Hausmarke"));

        // Fillamentum
        list.Add(New("Fillamentum", "PLA", MaterialType.PLA, 190, 220, 210, 50, 60, 55, 100, 40, 150, 60, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "Premium, schöne Farben",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Premium, schöne Farben"));

        list.Add(New("Fillamentum", "ASA", MaterialType.ASA, 240, 260, 250, 90, 110, 100, 30, 30, 80, 50, 1.0m,
            maxService: 90, uvResistant: true, weatherResistant: true, heatResistant: true,
            needsEnclosure: true, warpsEasily: true, impactResistant: true,
            density: 1.05m, tensile: 48,
            suitable: "UV-resistent, Premium",
            notSuitable: "Lebensmittel, Anfänger",
            notes: "Premium ASA"));

        // ColorFabb
        list.Add(New("ColorFabb", "PLA/PHA", MaterialType.PLA, 190, 220, 210, 50, 60, 55, 100, 40, 120, 50, 0.8m,
            maxService: 55, impactResistant: true, density: 1.24m, tensile: 55,
            suitable: "PLA+PHA Mix, flexibler als reines PLA",
            notSuitable: "Außen, Auto, Heiß",
            notes: "PLA+PHA Mix"));

        // 3DXTech
        list.Add(New("3DXTech", "Carbon Fiber PLA", MaterialType.PLA, 200, 230, 215, 40, 60, 50, 100, 30, 80, 40, 0.8m,
            maxService: 55, abrasive: true, impactResistant: true, density: 1.27m, tensile: 70,
            suitable: "Funktionsteile, Verschleißteile, steife Bauteile",
            notSuitable: "Außen, Flexible Teile — HARTE DÜSE NÖTIG!",
            notes: "CF verstärkt, harte Düse nötig!"));

        list.Add(New("3DXTech", "Carbon Fiber Nylon", MaterialType.PA6, 250, 280, 270, 80, 100, 90, 40, 20, 60, 40, 1.0m,
            maxService: 110, heatResistant: true, abrasive: true, needsEnclosure: true,
            needsDrying: true, dryingTemp: 80, dryingHours: 8, warpsEasily: true,
            impactResistant: true, density: 1.52m, tensile: 70,
            suitable: "CF Nylon, extrem stark",
            notSuitable: "Außen (UV), Anfänger — HARTE DÜSE NÖTIG!",
            notes: "CF Nylon, extrem stark"));

        // Siraya Tech
        list.Add(New("Siraya Tech", "Build", MaterialType.PETG, 230, 250, 240, 70, 90, 80, 50, 30, 80, 50, 1.5m,
            maxService: 75, weatherResistant: true, heatResistant: true,
            needsDrying: true, dryingTemp: 65, dryingHours: 4, stringsEasily: true,
            impactResistant: true, density: 1.27m, tensile: 60,
            suitable: "Engineering-Teile, sehr feste Layer-Haftung",
            notSuitable: "Auto-Innenraum (>75°C)",
            notes: "Sehr feste Layer-Haftung"));

        // Duramic
        list.Add(New("Duramic", "PLA+", MaterialType.PLA, 200, 230, 215, 40, 60, 50, 100, 40, 150, 60, 0.8m,
            maxService: 55, impactResistant: true, density: 1.24m, tensile: 50,
            suitable: "Impact-resistent",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Impact-resistent"));

        // Eryone
        list.Add(New("Eryone", "PLA", MaterialType.PLA, 190, 220, 210, 40, 60, 50, 100, 40, 150, 60, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "Budget, gut für Einsteiger",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Budget, gut für Einsteiger"));

        // MatterHackers
        list.Add(New("MatterHackers", "Pro PLA", MaterialType.PLA, 190, 220, 210, 40, 60, 50, 100, 40, 120, 50, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "±0.02mm, Premium-Qualität",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Premium-Qualität"));

        // Atomic Filament
        list.Add(New("Atomic Filament", "PLA", MaterialType.PLA, 190, 220, 210, 40, 60, 50, 100, 40, 150, 60, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "Made in USA, sehr konsistent",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Made in USA, sehr konsistent"));

        // Fiberlogy
        list.Add(New("Fiberlogy", "ASA", MaterialType.ASA, 240, 260, 250, 90, 110, 100, 30, 30, 80, 50, 1.0m,
            maxService: 90, uvResistant: true, weatherResistant: true, heatResistant: true,
            needsEnclosure: true, warpsEasily: true, impactResistant: true,
            density: 1.05m, tensile: 48,
            suitable: "Außenbereich, Auto, UV-exponiert",
            notSuitable: "Lebensmittel, Anfänger",
            notes: "Premium, gute Konsistenz"));

        // CookieCAD
        list.Add(New("CookieCAD", "PLA (Color/Silk)", MaterialType.PLA, 190, 220, 210, 40, 60, 50, 100, 40, 120, 50, 0.8m,
            maxService: 55, density: 1.24m, tensile: 50,
            suitable: "Spezialfarben, Gradient, Silk",
            notSuitable: "Außen, Auto, Heiß",
            notes: "Spezialfarben, Gradient, Silk"));

        return list;
    }

    private static FilamentBrandSpec New(
        string brand, string product, MaterialType material,
        int hotMin, int hotMax, int hotOpt, int bedMin, int bedMax, int bedOpt,
        int fan, int speedMin, int speedMax, int speedOpt, decimal retraction,
        int maxService = 0, bool uvResistant = false, bool weatherResistant = false,
        bool foodSafe = false, bool flexible = false, bool abrasive = false,
        bool heatResistant = false, bool needsEnclosure = false, bool needsDirectDrive = false,
        bool needsDrying = false, int dryingTemp = 0, int dryingHours = 0,
        bool biodegradable = false, bool recyclable = false,
        bool warpsEasily = false, bool stringsEasily = false, bool impactResistant = false,
        decimal density = 0, decimal tensile = 0,
        string suitable = "", string notSuitable = "", string notes = "")
    {
        return new FilamentBrandSpec
        {
            Brand = brand,
            ProductName = product,
            MaterialType = material,
            HotendMin = hotMin, HotendMax = hotMax, HotendOptimal = hotOpt,
            BedMin = bedMin, BedMax = bedMax, BedOptimal = bedOpt,
            FanPercent = fan,
            SpeedMin = speedMin, SpeedMax = speedMax, SpeedOptimal = speedOpt,
            RetractionMm = retraction,
            LayerHeightMin = 0.12m, LayerHeightMax = 0.24m, LayerHeightOptimal = 0.16m,
            IsUVResistant = uvResistant,
            IsWeatherResistant = weatherResistant,
            IsFoodSafe = foodSafe,
            IsFlexible = flexible,
            IsAbrasive = abrasive,
            IsHeatResistant = heatResistant,
            MaxServiceTempC = maxService,
            NeedsEnclosure = needsEnclosure,
            NeedsDirectDrive = needsDirectDrive,
            NeedsDryingBeforePrint = needsDrying,
            DryingTempC = dryingTemp,
            DryingDurationH = dryingHours,
            IsBiodegradable = biodegradable,
            IsRecyclable = recyclable,
            WarpsEasily = warpsEasily,
            StringsEasily = stringsEasily,
            IsImpactResistant = impactResistant,
            TensileStrengthMpa = tensile,
            DensityGcm3 = density,
            SuitableFor = suitable,
            NotSuitableFor = notSuitable,
            Notes = notes
        };
    }
}