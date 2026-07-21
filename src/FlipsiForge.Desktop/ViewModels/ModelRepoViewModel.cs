// SPDX-License-Identifier: GPL-3.0-or-later
// ModelRepoViewModel: Echte 3Drop.com-Integration.
// Laedt Trending-Modelle beim Oeffnen und sucht via 3Drop-API.
// Bento-Cards mit Thumbnail, Name, Designer, Likes/Downloads, Quelle-Badge.
// Klick auf Karte oeffnet die Original-URL im System-Browser.
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Sortier-Option fuer die 3Drop API.</summary>
public enum ModelSortMode
{
    TrendingDaily,
    Trending,
    Latest,
    Popular
}

/// <summary>Display-Row fuer ein 3D-Modell aus der 3Drop API.</summary>
public sealed class ModelCardVm : ObservableObject
{
    public string Name { get; }
    public string Designer { get; }
    public string Source { get; }      // Quelle-Badge: "Printables", "Thingiverse", "MakerWorld", ...
    public int Likes { get; }
    public int Downloads { get; }
    public string ThumbnailUrl { get; }
    public string DetailUrl { get; }   // Original-URL auf der Quell-Plattform
    public int Id { get; }              // 3Drop-Modell-ID

    // Emoji-Platzhalter falls kein Thumbnail geladen werden kann
    public string Emoji => Source switch
    {
        "Printables" => "🖨️",
        "Thingiverse" => "🧩",
        "MakerWorld" => "🏭",
        "Cults" => "🎨",
        "Thangs" => "🔍",
        "MyMiniFactory" => "🏭",
        "GrabCAD" => "📐",
        _ => "📦"
    };

    public string LikesDisplay => Likes >= 1000 ? $"{Likes / 1000.0:F1}k" : Likes.ToString();
    public string DownloadsDisplay => Downloads >= 1000 ? $"{Downloads / 1000.0:F1}k" : Downloads.ToString();

    public ModelCardVm(string name, string designer, string source, int likes, int downloads,
                       string thumbnailUrl, string detailUrl, int id)
    {
        Name = name; Designer = designer; Source = source;
        Likes = likes; Downloads = downloads;
        ThumbnailUrl = thumbnailUrl; DetailUrl = detailUrl; Id = id;
    }
}

/// <summary>Display-Row fuer ein Kategorie-Filter-Badge.</summary>
public sealed class ModelCategoryBadgeVm : ObservableObject
{
    public string Label { get; }
    public ModelSortMode SortMode { get; }
    public bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public ModelCategoryBadgeVm(string label, ModelSortMode mode, bool active = false)
    {
        Label = label; SortMode = mode; _isActive = active;
    }
}

/// <summary>ViewModel fuer den Model-Repository-Browser (3Drop-Integration).</summary>
public partial class ModelRepoViewModel : ViewModelBase
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    /// <summary>Aktuell angezeigte Modelle (aus 3Drop API).</summary>
    public ObservableCollection<ModelCardVm> FilteredModels { get; } = new();

    /// <summary>Kategorie-Filter-Badges (Trending Daily / Trending / Latest / Alle).</summary>
    public ObservableCollection<ModelCategoryBadgeVm> CategoryBadges { get; } = new();

    /// <summary>Suchtext (leer = Trending-Modelle).</summary>
    [ObservableProperty]
    private string _searchText = "";

    /// <summary>True waehrend des API-Abrufs (Loading-Spinner).</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Fehler-Status-Text (leer = OK).</summary>
    [ObservableProperty]
    private string _statusText = "";

    /// <summary>Aktuell gewaehlte Sortierung.</summary>
    [ObservableProperty]
    private ModelSortMode _selectedSort = ModelSortMode.TrendingDaily;

    /// <summary>
    /// Wird von der View gesetzt, um eine URL im System-Browser zu oeffnen.
    /// </summary>
    public Action<string>? OpenUrlInBrowser { get; set; }

    public ModelRepoViewModel()
    {
        // Kategorie-Badges initialisieren
        CategoryBadges.Clear();
        CategoryBadges.Add(new ModelCategoryBadgeVm("Trending Daily", ModelSortMode.TrendingDaily, true));
        CategoryBadges.Add(new ModelCategoryBadgeVm("Trending", ModelSortMode.Trending));
        CategoryBadges.Add(new ModelCategoryBadgeVm("Latest", ModelSortMode.Latest));
        CategoryBadges.Add(new ModelCategoryBadgeVm("Popular", ModelSortMode.Popular));

        // Trending Daily beim Oeffnen laden (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try { await LoadTrendingAsync(); }
            catch { /* Best-effort */ }
        });
    }

    /// <summary>Setzt die aktive Sortierung (von Badge-Klick) und laedt neu.</summary>
    [RelayCommand]
    public async Task ApplyCategoryAsync(ModelSortMode mode)
    {
        SelectedSort = mode;
        foreach (var b in CategoryBadges)
            b.IsActive = b.SortMode == mode;

        if (string.IsNullOrWhiteSpace(SearchText))
            await LoadTrendingAsync();
        else
            await SearchAsync(SearchText);
    }

    /// <summary>Loest eine Suche aus (von Search-Button oder Enter).</summary>
    [RelayCommand]
    public async Task SearchAsync(string query)
    {
        var q = (query ?? "").Trim();
        if (string.IsNullOrEmpty(q))
        {
            await LoadTrendingAsync();
            return;
        }

        IsLoading = true;
        StatusText = "Suche laeuft...";
        try
        {
            var url = $"https://three-drop.com/api/models?q={Uri.EscapeDataString(q)}&sort={SortToApi(SelectedSort)}";
            var results = await FetchFrom3DropAsync(url);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                FilteredModels.Clear();
                foreach (var m in results)
                    FilteredModels.Add(m);
                StatusText = results.Count == 0 ? "Keine Modelle gefunden" : $"✓ {results.Count} Modelle gefunden";
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = $"✗ Suche fehlgeschlagen: {ex.Message}";
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Laedt Trending-Modelle (beim Oeffnen oder leerer Suche).</summary>
    public async Task LoadTrendingAsync()
    {
        IsLoading = true;
        StatusText = "Lade Trending-Modelle...";
        try
        {
            var url = $"https://three-drop.com/api/models?sort={SortToApi(SelectedSort)}&w=b";
            var results = await FetchFrom3DropAsync(url);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                FilteredModels.Clear();
                foreach (var m in results)
                    FilteredModels.Add(m);
                StatusText = results.Count == 0 ? "Keine Modelle gefunden" : $"✓ {results.Count} Trending-Modelle";
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = $"✗ Abruf fehlgeschlagen: {ex.Message}";
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Oeffnet die Detail-URL eines Modells im System-Browser.</summary>
    [RelayCommand]
    public void OpenInBrowser(ModelCardVm? model)
    {
        if (model == null) return;
        try
        {
            OpenUrlInBrowser?.Invoke(model.DetailUrl);
        }
        catch { /* Best-effort */ }
    }

    /// <summary>Ruft die 3Drop API auf und parst die JSON-Antwort in ModelCardVm-Liste.</summary>
    private static async Task<List<ModelCardVm>> FetchFrom3DropAsync(string url)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.UserAgent.ParseAdd("FlipsiForge/0.5.0 (https://techflipsi.at)");
        using var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        var apiResp = JsonSerializer.Deserialize<ThreeDropApiResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var list = new List<ModelCardVm>();
        if (apiResp?.Results == null) return list;

        foreach (var r in apiResp.Results)
        {
            if (r == null) continue;
            var source = CapitalizeFirst(r.WebsiteType ?? "Unknown");
            var designer = r.Author?.Name ?? "Unbekannt";
            var thumb = !string.IsNullOrEmpty(r.ThumbnailUrl) ? r.ThumbnailUrl
                        : !string.IsNullOrEmpty(r.ImageUrl) ? r.ImageUrl : "";
            list.Add(new ModelCardVm(
                name: r.Name ?? "(ohne Name)",
                designer: designer,
                source: source,
                likes: r.LikeCount,
                downloads: r.DownloadCount,
                thumbnailUrl: thumb,
                detailUrl: r.Url ?? "",
                id: r.Id));
        }
        return list;
    }

    /// <summary>Konvertiert ModelSortMode in den 3Drop API-Sort-String.</summary>
    private static string SortToApi(ModelSortMode mode) => mode switch
    {
        ModelSortMode.TrendingDaily => "trendingDaily",
        ModelSortMode.Trending => "trending",
        ModelSortMode.Latest => "latest",
        ModelSortMode.Popular => "popular",
        _ => "trendingDaily"
    };

    /// <summary>Ersten Buchstaben gross, Rest klein (fuer Source-Badge).</summary>
    private static string CapitalizeFirst(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    }

    // === 3Drop API JSON DTOs ===

    private sealed class ThreeDropApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("results")]
        public List<ThreeDropModel>? Results { get; set; }
        [JsonPropertyName("totalResults")]
        public int TotalResults { get; set; }
    }

    private sealed class ThreeDropModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("websiteType")]
        public string? WebsiteType { get; set; }
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
        [JsonPropertyName("thumbnailUrl")]
        public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("likeCount")]
        public int LikeCount { get; set; }
        [JsonPropertyName("downloadCount")]
        public int DownloadCount { get; set; }
        [JsonPropertyName("rate")]
        public double Rate { get; set; }
        [JsonPropertyName("author")]
        public ThreeDropAuthor? Author { get; set; }
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
    }

    private sealed class ThreeDropAuthor
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
        [JsonPropertyName("websiteType")]
        public string? WebsiteType { get; set; }
    }
}