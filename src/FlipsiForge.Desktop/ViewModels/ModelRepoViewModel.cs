// SPDX-License-Identifier: GPL-3.0-or-later
// ModelRepoViewModel: Model-Browser mit Dummy-Daten (die echte API ist noch
// nicht implementiert). Zeigt sofort beim Öffnen TRENDING-Modelle — ohne
// dass der User suchen muss. Suchzeile + Kategorie-Badge-Filter vorhanden.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Kategorie für Model-Filter-Badges.</summary>
public enum ModelCategory
{
    Alle,
    Beliebt,
    Neu,
    Funktional,
    Deko
}

/// <summary>Display-Row für ein 3D-Modell (Thingiverse/Printables-Style).</summary>
public sealed class ModelCardVm : ObservableObject
{
    public string Name { get; }
    public string Designer { get; }
    public string Source { get; }      // Quelle-Badge: "Thingiverse", "Printables", "MakerWorld"
    public int Likes { get; }
    public int Downloads { get; }
    public ModelCategory Category { get; }
    public string Emoji { get; }       // Thumbnail-Platzhalter
    public DateTime PublishedAt { get; }

    public string LikesDisplay => Likes >= 1000 ? $"{Likes / 1000.0:F1}k" : Likes.ToString();
    public string DownloadsDisplay => Downloads >= 1000 ? $"{Downloads / 1000.0:F1}k" : Downloads.ToString();

    public ModelCardVm(string name, string designer, string source, int likes, int downloads,
                       ModelCategory cat, string emoji, DateTime publishedAt)
    {
        Name = name; Designer = designer; Source = source;
        Likes = likes; Downloads = downloads; Category = cat;
        Emoji = emoji; PublishedAt = publishedAt;
    }
}

/// <summary>Display-Row für ein Kategorie-Filter-Badge.</summary>
public sealed class ModelCategoryBadgeVm : ObservableObject
{
    public string Label { get; }
    public ModelCategory Category { get; }
    public bool IsActive { get; set; }

    public ModelCategoryBadgeVm(string label, ModelCategory cat, bool active = false)
    {
        Label = label; Category = cat; IsActive = active;
    }
}

/// <summary>ViewModel für den Model-Repository-Browser.</summary>
public partial class ModelRepoViewModel : ViewModelBase
{
    /// <summary>Alle Modelle (ungefiltert, inkl. Trending-Dummy-Daten).</summary>
    public ObservableCollection<ModelCardVm> AllModels { get; } = new();

    /// <summary>Aktuell angezeigte Modelle (nach Filter + Suche).</summary>
    public ObservableCollection<ModelCardVm> FilteredModels { get; } = new();

    /// <summary>Kategorie-Filter-Badges.</summary>
    public ObservableCollection<ModelCategoryBadgeVm> CategoryBadges { get; } = new();

    /// <summary>Aktiv ausgewählte Kategorie.</summary>
    [ObservableProperty]
    private ModelCategory _selectedCategory = ModelCategory.Alle;

    /// <summary>Suchtext (leer = alle Modelle).</summary>
    [ObservableProperty]
    private string _searchText = "";

    public ModelRepoViewModel()
    {
        LoadDummyData();
    }

    /// <summary>Lädt Dummy-Modelle (die echte API ist noch nicht implementiert).</summary>
    private void LoadDummyData()
    {
        var now = DateTime.UtcNow;
        var models = new List<ModelCardVm>
        {
            new("Benchy", "joris", "Printables", 8542, 154000, ModelCategory.Beliebt, "🚤", now.AddDays(-180)),
            new("Calibration Cube", "aching", "Thingiverse", 5210, 98000, ModelCategory.Funktional, "🧊", now.AddDays(-200)),
            new("Phone Stand", "pauljacobson", "Thingiverse", 3180, 56000, ModelCategory.Funktional, "📱", now.AddDays(-30)),
            new("Articulated Dragon", "zhujb", "MakerWorld", 18200, 320000, ModelCategory.Beliebt, "🐉", now.AddDays(-90)),
            new("Vase Spirograph", "roman", "Printables", 2440, 21000, ModelCategory.Deko, "🏺", now.AddDays(-10)),
            new("Desk Cable Organizer", "dutchmogul", "Thingiverse", 1890, 15000, ModelCategory.Funktional, "🔌", now.AddDays(-15)),
            new("Halloween Pumpkin", "makeal", "Thingiverse", 980, 8200, ModelCategory.Deko, "🎃", now.AddDays(-5)),
            new("Keychain Tag Generator", "daniel", "Printables", 1500, 32000, ModelCategory.Neu, "🏷️", now.AddDays(-2)),
            new("Planter Succulent", "sphynx", "MakerWorld", 3210, 18000, ModelCategory.Deko, "🌱", now.AddDays(-3)),
            new("Tool Holder Wall", "sgbryan", "Thingiverse", 2750, 12500, ModelCategory.Funktional, "🔧", now.AddDays(-8)),
            new("Flexi Rex", "DrLex", "Thingiverse", 12000, 210000, ModelCategory.Beliebt, "🦖", now.AddDays(-400)),
            new("Lithophane Photo Frame", "jason", "Printables", 4200, 45000, ModelCategory.Funktional, "🖼️", now.AddDays(-60)),
            new("Christmas Tree LED", "marco", "MakerWorld", 680, 4200, ModelCategory.Deko, "🎄", now.AddDays(-1)),
            new("SD Card Holder", "wstein", "Thingiverse", 1340, 9800, ModelCategory.Funktional, "💾", now.AddDays(-12)),
            new("Mini Castle Set", "elizabeth", "Printables", 2200, 16000, ModelCategory.Deko, "🏰", now.AddDays(-20))
        };

        AllModels.Clear();
        foreach (var m in models)
            AllModels.Add(m);

        // Kategorie-Badges initialisieren
        CategoryBadges.Clear();
        CategoryBadges.Add(new ModelCategoryBadgeVm("Alle", ModelCategory.Alle, true));
        CategoryBadges.Add(new ModelCategoryBadgeVm("Beliebt", ModelCategory.Beliebt));
        CategoryBadges.Add(new ModelCategoryBadgeVm("Neu", ModelCategory.Neu));
        CategoryBadges.Add(new ModelCategoryBadgeVm("Funktional", ModelCategory.Funktional));
        CategoryBadges.Add(new ModelCategoryBadgeVm("Deko", ModelCategory.Deko));

        ApplyFilter();
    }

    /// <summary>Setzt die aktive Kategorie (von Badge-Klick) und filtert neu.</summary>
    [RelayCommand]
    public void ApplyCategory(ModelCategory category)
    {
        SelectedCategory = category;
        // Badges aktualisieren (IsActive togglen)
        foreach (var b in CategoryBadges)
            b.IsActive = b.Category == category;
        ApplyFilter();
    }

    /// <summary>Wendet den Such- + Kategorie-Filter an und aktualisiert FilteredModels.</summary>
    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedCategoryChanged(ModelCategory value) => ApplyFilter();

    /// <summary>Filtert AllModels nach Kategorie + Suchtext → FilteredModels.</summary>
    public void ApplyFilter()
    {
        FilteredModels.Clear();

        // "Neu" = in den letzten 7 Tagen veröffentlicht
        var cutoff = DateTime.UtcNow.AddDays(-7);

        var filtered = AllModels.AsEnumerable();

        if (SelectedCategory != ModelCategory.Alle)
        {
            filtered = SelectedCategory switch
            {
                ModelCategory.Neu => filtered.Where(m => m.PublishedAt >= cutoff),
                _ => filtered.Where(m => m.Category == SelectedCategory)
            };
        }

        // Suche (case-insensitive Contains in Name oder Designer)
        var q = (SearchText ?? "").Trim();
        if (!string.IsNullOrEmpty(q))
        {
            filtered = filtered.Where(m =>
                m.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                m.Designer.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        // "Beliebt" zuerst sortieren wenn "Alle" — sonst nach Likes
        var sorted = SelectedCategory == ModelCategory.Alle
            ? filtered.OrderByDescending(m => m.Likes).ToList()
            : filtered.OrderByDescending(m => m.Likes).ToList();

        foreach (var m in sorted)
            FilteredModels.Add(m);
    }
}