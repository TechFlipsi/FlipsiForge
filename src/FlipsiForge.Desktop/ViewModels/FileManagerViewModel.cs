// SPDX-License-Identifier: GPL-3.0-or-later
// FileManagerViewModel: ScannedFiles-Liste, Suche (Stub-ISearchService),
// Format-Filter, Ansicht (List/Grid), Sortierung, Favorit & Häufigkeit.
using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Display-Row für eine gescannte Datei inkl. Usage-Infos.</summary>
public sealed class FileRowVm : ObservableObject
{
    public ScannedFile File { get; }
    public FileUsageEntry Usage { get; set; }
    public int Id => File.Id;
    public string Name => File.FileName;
    public string Path => File.Path;
    public string SizeDisplay => FormatBytes(File.FileSizeBytes);
    public string ModifiedDisplay => File.LastModified.ToString("dd.MM.yyyy HH:mm");
    public int OpenCount => Usage.OpenCount;
    public bool IsFavorite { get => Usage.IsFavorite; set { Usage.IsFavorite = value; OnPropertyChanged(); OnPropertyChanged(nameof(FavoriteGlyph)); } }
    public string FavoriteGlyph => IsFavorite ? "★" : "☆";
    public string Extension => File.Extension;
    public bool IsAiHit { get; set; }

    public FileRowVm(ScannedFile f, FileUsageEntry u)
    {
        File = f;
        Usage = u;
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(SizeDisplay));
        OnPropertyChanged(nameof(ModifiedDisplay));
        OnPropertyChanged(nameof(OpenCount));
        OnPropertyChanged(nameof(IsFavorite));
        OnPropertyChanged(nameof(FavoriteGlyph));
    }

    private static string FormatBytes(long b)
    {
        if (b < 1024) return $"{b} B";
        if (b < 1024L * 1024) return $"{b / 1024.0:F1} KB";
        if (b < 1024L * 1024 * 1024) return $"{b / (1024.0 * 1024):F1} MB";
        return $"{b / (1024.0 * 1024 * 1024):F2} GB";
    }
}

/// <summary>ViewModel für den Datei-Manager.</summary>
public partial class FileManagerViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;
    private readonly ISearchService _search;

    /// <summary>Alle geladenen Dateien (Display-Rows).</summary>
    public ObservableCollection<FileRowVm> Files { get; } = new();

    /// <summary>Filter-Badges als (Name, Count)-Tupel.</summary>
    public ObservableCollection<FilterBadgeVm> FilterBadges { get; } = new();

    /// <summary>Sortier-Optionen.</summary>
    public IReadOnlyList<string> SortOptions { get; } =
        new[] { "Name ↑", "Name ↓", "Datum ↓", "Datum ↑", "Größe ↓", "Größe ↑" };

    private string _selectedSort = "Name ↑";
    public string SelectedSort { get => _selectedSort; set => SetProperty(ref _selectedSort, value); }

    private bool _isGrid;
    public bool IsGrid { get => _isGrid; set => SetProperty(ref _isGrid, value); }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedFilter = "Alle";
    public string SelectedFilter { get => _selectedFilter; set => SetProperty(ref _selectedFilter, value); }

    private bool _autoScan;
    public bool AutoScan { get => _autoScan; set => SetProperty(ref _autoScan, value); }

    public FileManagerViewModel() : this(ServiceLocator.CreateDb(), ServiceLocator.Require<ISearchService>()) { }

    public FileManagerViewModel(FlipsiForgeDbContext db, ISearchService search)
    {
        _db = db;
        _search = search;
        Load();
    }

    /// <summary>Lädt alle Dateien aus der DB und initialisiert Filter-Badges.</summary>
    public void Load()
    {
        Files.Clear();
        var usage = FileUsageStore.LoadAll();
        foreach (var f in _db.ScannedFiles.ToList())
        {
            var u = FileUsageStore.GetOrNew(usage, f.Id);
            Files.Add(new FileRowVm(f, u));
        }
        FileUsageStore.SaveAll(usage);
        RebuildFilterBadges();
    }

    private void RebuildFilterBadges()
    {
        FilterBadges.Clear();
        FilterBadges.Add(new FilterBadgeVm("Alle", Files.Count, "Alle"));
        FilterBadges.Add(new FilterBadgeVm("STL", Files.Count(f => MatchesExt(f, ".stl")), "STL"));
        FilterBadges.Add(new FilterBadgeVm("3MF", Files.Count(f => MatchesExt(f, ".3mf")), "3MF"));
        FilterBadges.Add(new FilterBadgeVm("GCODE", Files.Count(f => MatchesExt(f, ".gcode")), "GCODE"));
        FilterBadges.Add(new FilterBadgeVm("OBJ", Files.Count(f => MatchesExt(f, ".obj")), "OBJ"));
    }

    private static bool MatchesExt(FileRowVm f, string ext)
        => f.File.Extension?.Equals(ext, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Setzt einen Filter aktiv (von Badge-Klick).</summary>
    [RelayCommand]
    public void ApplyFilter(string filter)
    {
        SelectedFilter = filter;
    }

    /// <summary>Führt die Suche aus (Filename-Stub + optional KI-Suche via ISearchService).</summary>
    [RelayCommand]
    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // Reset: alle Dateien anzeigen, kein AI-Hit
            foreach (var f in Files) f.IsAiHit = false;
            return;
        }

        try
        {
            var results = await _search.SearchAsync(SearchText);
            var hits = results.ToDictionary(r => r.FileId);
            foreach (var f in Files)
                f.IsAiHit = hits.TryGetValue(f.Id, out var r) && r.IsAiHit;
        }
        catch
        {
            // Fallback: kein Filter — Benutzer kann weiterarbeiten
        }
    }

    /// <summary>Öffnet den Datei-Öffnen-Dialog und incrementiert Usage.</summary>
    [RelayCommand]
    public void Open(FileRowVm row)
    {
        if (row == null) return;
        FileUsageStore.IncrementOpen(row.Id);
        row.Usage.OpenCount++;
        row.Refresh();
    }

    /// <summary>Schaltet den Favorit-Status um.</summary>
    [RelayCommand]
    public void ToggleFavorite(FileRowVm row)
    {
        if (row == null) return;
        FileUsageStore.ToggleFavorite(row.Id);
        row.IsFavorite = !row.IsFavorite;
        row.Refresh();
    }

    /// <summary>Öffnet einen Ordner-Dialog und fügt ihn zu WatchFolders hinzu.</summary>
    [RelayCommand]
    public async Task BrowseFolderAsync()
    {
        // Stub: nur Signal an UI; Core.Services kümmert sich später um echten Scan.
        await Task.CompletedTask;
    }

    /// <summary>Sortiert die Datei-Liste nach der gewählten Sortier-Option.</summary>
    [RelayCommand]
    public void Sort()
    {
        var sorted = SelectedSort switch
        {
            "Name ↑" => Files.OrderBy(f => f.Name).ToList(),
            "Name ↓" => Files.OrderByDescending(f => f.Name).ToList(),
            "Datum ↓" => Files.OrderByDescending(f => f.File.LastModified).ToList(),
            "Datum ↑" => Files.OrderBy(f => f.File.LastModified).ToList(),
            "Größe ↓" => Files.OrderByDescending(f => f.File.FileSizeBytes).ToList(),
            "Größe ↑" => Files.OrderBy(f => f.File.FileSizeBytes).ToList(),
            _ => Files.ToList()
        };
        Files.Clear();
        foreach (var s in sorted) Files.Add(s);
    }
}

/// <summary>Display-Row für ein Filter-Badge.</summary>
public sealed class FilterBadgeVm : ObservableObject
{
    public string Label { get; }
    public int Count { get; }
    public string Key { get; }
    public string Display => $"{Label} ({Count})";
    public FilterBadgeVm(string label, int count, string key) { Label = label; Count = count; Key = key; }
}