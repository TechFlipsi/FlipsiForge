// SPDX-License-Identifier: GPL-3.0-or-later
// FileManagerViewModel: ScannedFiles-Liste, Suche (Stub-ISearchService),
// Format-Filter, Ansicht (List/Grid), Sortierung, Favorit & Häufigkeit.
using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Display-Row fuer eine gescannte Datei inkl. Usage-Infos + Thumbnail.</summary>
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

    private Bitmap? _thumbnail;
    /// <summary>Thumbnail-Bild (STL/3MF/OBJ gerendert, andere = null).</summary>
    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        set { _thumbnail = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasThumbnail)); }
    }
    public bool HasThumbnail => _thumbnail != null;

    /// <summary>Emoji-Icon basierend auf Dateierweiterung (Fallback wenn kein Thumbnail).</summary>
    public string FileIcon => File.Extension?.ToLowerInvariant() switch
    {
        ".stl" => "📐",
        ".3mf" => "📦",
        ".gcode" => "⚙️",
        ".gco" => "⚙️",
        ".obj" => "📐",
        ".ply" => "📐",
        ".step" => "📐",
        ".stp" => "📐",
        ".amf" => "📦",
        _ => "📄"
    };

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

    /// <summary>Alle geladenen Dateien (Display-Rows, ungefiltert).</summary>
    public ObservableCollection<FileRowVm> Files { get; } = new();

    /// <summary> Gefilterte Dateien (Liste + Format-Filter + Suche). Die UI bindet daran.</summary>
    public ObservableCollection<FileRowVm> FilteredFiles { get; } = new();

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
        set { SetProperty(ref _searchText, value); ApplyFilterAndSearch(); }
    }

    private string _selectedFilter = "Alle";
    public string SelectedFilter
    {
        get => _selectedFilter;
        set { SetProperty(ref _selectedFilter, value); ApplyFilterAndSearch(); }
    }

    private bool _autoScan = true; // Auto-Scan ist immer AN — kein Toggle
    public bool AutoScan { get => _autoScan; set => SetProperty(ref _autoScan, value); }

    /// <summary>True während ein Scan läuft (für UI Spinner).</summary>
    [ObservableProperty]
    private bool _isScanning;

    public FileManagerViewModel() : this(ServiceLocator.CreateDb(), ServiceLocator.Require<ISearchService>()) { }

    public FileManagerViewModel(FlipsiForgeDbContext db, ISearchService search)
    {
        _db = db;
        _search = search;
        Load();
        // Auto-Scan beim Start — kompletten PC nach 3D-Druck-Dateien durchsuchen
        _ = Task.Run(async () =>
        {
            try
            {
                await ScanAllDrivesAsync();
            }
            catch
            {
                // Best-effort — UI bleibt sichtbar
            }
        });
    }

    /// <summary>Lädt alle Dateien aus der DB und generiert Thumbnails.</summary>
    public void Load()
    {
        Files.Clear();
        var usage = FileUsageStore.LoadAll();
        foreach (var f in _db.ScannedFiles.ToList())
        {
            var u = FileUsageStore.GetOrNew(usage, f.Id);
            var row = new FileRowVm(f, u);
            // Thumbnail asynchron generieren
            _ = LoadThumbnailAsync(row);
            Files.Add(row);
        }
        FileUsageStore.SaveAll(usage);
        RebuildFilterBadges();
        ApplyFilterAndSearch(); // FilteredFiles initial befuellen
    }

    /// <summary>Generiert Thumbnail fuer eine Datei asynchron.</summary>
    private async Task LoadThumbnailAsync(FileRowVm row)
    {
        // Nur STL/3MF/OBJ rendern
        var ext = row.File.Extension?.ToLowerInvariant();
        if (ext != ".stl" && ext != ".3mf" && ext != ".obj") return;

        await Task.Run(() =>
        {
            try
            {
                var thumb = StlThumbnailService.GetOrGenerate(
                    row.File.Path,
                    row.File.LastModified.Ticks);
                if (thumb != null)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => row.Thumbnail = thumb);
                }
            }
            catch { /* Best-effort */ }
        });
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
        // ApplyFilterAndSearch() wird durch SelectedFilter Setter aufgerufen
    }

    /// <summary>Wendet Format-Filter UND Textsuche kombiniert an.</summary>
    private void ApplyFilterAndSearch()
    {
        FilteredFiles.Clear();
        var searchLower = (SearchText ?? "").Trim().ToLowerInvariant();
        var hasSearch = !string.IsNullOrWhiteSpace(searchLower);

        foreach (var f in Files)
        {
            // 1. Format-Filter pruefen
            if (_selectedFilter != "Alle")
            {
                var filterExt = _selectedFilter.ToLowerInvariant() switch
                {
                    "stl" => ".stl",
                    "3mf" => ".3mf",
                    "gcode" => ".gcode",
                    "obj" => ".obj",
                    _ => null
                };
                if (filterExt != null && !f.File.Extension?.Equals(filterExt, StringComparison.OrdinalIgnoreCase) == true)
                    continue;
            }

            // 2. Textsuche pruefen (Fuzzy auf Dateiname)
            if (hasSearch)
            {
                var nameLower = (f.Name ?? "").ToLowerInvariant();
                var pathLower = (f.Path ?? "").ToLowerInvariant();
                // Simple Contains-Matching — schnell und zuverlaessig
                if (!nameLower.Contains(searchLower) && !pathLower.Contains(searchLower))
                {
                    // KI-Treffer pruefen (falls KI-Suche Ergebnisse geliefert hat)
                    if (!f.IsAiHit) continue;
                }
            }

            FilteredFiles.Add(f);
        }
    }

    /// <summary>Fuehrt die KI-Suche aus (Bedeutungssuche via ISearchService) + aktualisiert Filter.</summary>
    [RelayCommand]
    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            foreach (var f in Files) f.IsAiHit = false;
            ApplyFilterAndSearch();
            return;
        }

        try
        {
            // KI-Suche im Hintergrund (Stub liefert leer — echte KI kommt mit Gemma 4)
            var results = await _search.SearchAsync(SearchText);
            var hits = results.ToDictionary(r => r.FileId);
            foreach (var f in Files)
                f.IsAiHit = hits.TryGetValue(f.Id, out var r) && r.IsAiHit;
        }
        catch
        {
            // Fallback: nur Dateinamen-Suche
        }

        // Filter aktualisieren (inkl. KI-Treffer)
        ApplyFilterAndSearch();
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

    /// <summary>Durchsucht alle Laufwerke nach 3D-Druck-Dateien (STL, 3MF, GCODE, OBJ).</summary>
    public async Task ScanAllDrivesAsync()
    {
        IsScanning = true;
        try
        {
            await Task.Run(() =>
            {
                // Alle Laufwerke ermitteln
                var drives = System.IO.DriveInfo.GetDrives()
                    .Where(d => d.IsReady && (d.DriveType == System.IO.DriveType.Fixed))
                    .Select(d => d.RootDirectory.FullName)
                    .ToList();

                // Bekannte Dateierweiterungen
                var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".stl", ".3mf", ".gcode", ".obj" };

                var found = new System.Collections.Concurrent.ConcurrentBag<ScannedFile>();

                // Parallele Suche auf allen Laufwerken
                System.Threading.Tasks.Parallel.ForEach(drives, drive =>
                {
                    try
                    {
                        ScanDirectory(drive, extensions, found);
                    }
                    catch
                    {
                        // Best-effort — manche Ordner sind nicht zugänglich
                    }
                });

                // Gefundene Dateien in DB speichern (nur neue)
                var existingPaths = _db.ScannedFiles.Select(f => f.Path).ToHashSet();
                foreach (var file in found)
                {
                    if (!existingPaths.Contains(file.Path))
                    {
                        _db.ScannedFiles.Add(file);
                    }
                }
                _db.SaveChanges();
            });
        }
        finally
        {
            IsScanning = false;
        }
    }

    /// <summary>Rekursive Verzeichnis-Suche nach 3D-Druck-Dateien.</summary>
    private static void ScanDirectory(string dir, HashSet<string> extensions,
        System.Collections.Concurrent.ConcurrentBag<ScannedFile> found)
    {
        // System- und versteckte Ordner überspringen
        var skipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "windows", "program files", "program files (x86)", "$recycle.bin",
          "system volume information", "programdata", "appdata", ".git", "node_modules" };

        try
        {
            // Dateien im aktuellen Verzeichnis prüfen
            foreach (var f in System.IO.Directory.EnumerateFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly))
            {
                var ext = System.IO.Path.GetExtension(f);
                if (extensions.Contains(ext))
                {
                    var info = new System.IO.FileInfo(f);
                    found.Add(new ScannedFile
                    {
                        FileName = System.IO.Path.GetFileName(f),
                        Path = f,
                        Extension = ext,
                        FileSizeBytes = info.Length,
                        LastModified = info.LastWriteTimeUtc
                    });
                }
            }
        }
        catch { }

        // Unterverzeichnisse durchsuchen
        try
        {
            foreach (var sub in System.IO.Directory.EnumerateDirectories(dir, "*", System.IO.SearchOption.TopDirectoryOnly))
            {
                var name = System.IO.Path.GetFileName(sub);
                if (skipDirs.Contains(name)) continue;
                ScanDirectory(sub, extensions, found);
            }
        }
        catch { }
    }

    /// <summary>Manuellen Scan auslösen (gleiche wie Auto-Scan).</summary>
    [RelayCommand]
    public async Task RescanAsync()
    {
        await ScanAllDrivesAsync();
        Load();
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