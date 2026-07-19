// SPDX-License-Identifier: GPL-3.0-or-later
// AiAssistantViewModel: Chat-Verlauf + Streaming via IAIChatEngine (Stub-Fallback).
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlipsiForge.Core.Data;
using FlipsiForge.Core.Models;
using FlipsiForge.Desktop.Services;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Display-Row für eine Chat-Nachricht (User/AI).</summary>
public sealed class ChatRowVm : ObservableObject
{
    public string Role { get; }
    public string Content { get; set; }
    public DateTime Timestamp { get; }
    public bool IsUser => Role == "user";
    public bool IsAssistant => Role == "assistant";
    public string TimeDisplay => Timestamp.ToString("HH:mm");
    public ChatRowVm(string role, string content, DateTime ts)
    {
        Role = role; Content = content; Timestamp = ts;
    }

    /// <summary>Public Wrapper für protected OnPropertyChanged — erlaubt externen ViewModels
    /// (z.B. AiAssistantViewModel) Properties dieses RowVm zu refreshen (z.B. beim Streaming).</summary>
    public void RaisePropertyChanged(string propertyName)
        => OnPropertyChanged(propertyName);
}

/// <summary>ViewModel für den KI-Drucker-Assistenten.</summary>
public partial class AiAssistantViewModel : ViewModelBase
{
    private readonly FlipsiForgeDbContext _db;
    private readonly IAIChatEngine _engine;
    private readonly DesktopSettings _settings;

    /// <summary>Chat-Verlauf (Display-Rows).</summary>
    public ObservableCollection<ChatRowVm> Messages { get; } = new();

    private string _input = "";
    public string Input { get => _input; set => SetProperty(ref _input, value); }

    private bool _isGenerating;
    public bool IsGenerating { get => _isGenerating; set => SetProperty(ref _isGenerating, value); }

    private string _modelBadge = "Modell nicht geladen";
    public string ModelBadge { get => _modelBadge; set => SetProperty(ref _modelBadge, value); }

    public bool AiEnabled => _settings.AiEnabled;
    public bool AiDisabled => !_settings.AiEnabled;
    public bool ModelNotLoaded => !_engine.IsModelLoaded;

    public AiAssistantViewModel()
        : this(ServiceLocator.CreateDb(), ServiceLocator.Require<IAIChatEngine>(), DesktopSettings.Load())
    { }

    public AiAssistantViewModel(FlipsiForgeDbContext db, IAIChatEngine engine, DesktopSettings settings)
    {
        _db = db;
        _engine = engine;
        _settings = settings;
        LoadHistory();
        UpdateModelBadge();
    }

    private void UpdateModelBadge()
    {
        ModelBadge = _engine.IsModelLoaded ? "✓ Modell geladen" : "⚠ Modell nicht geladen";
    }

    private void LoadHistory()
    {
        Messages.Clear();
        foreach (var m in _db.ChatMessages.OrderBy(m => m.Timestamp).Take(100).ToList())
            Messages.Add(new ChatRowVm(m.Role, m.Content, m.Timestamp));
        if (Messages.Count == 0)
        {
            Messages.Add(new ChatRowVm("assistant",
                "Hallo! Ich bin dein FlipsiForge-Drucker-Assistent. Frag mich zu Druck-Einstellungen, Filament, Wartung u.v.m.",
                DateTime.UtcNow));
        }
    }

    /// <summary>Sendet die aktuelle Eingabe als User-Nachricht und streamt die KI-Antwort.</summary>
    [RelayCommand]
    public async Task SendAsync()
    {
        var text = Input?.Trim() ?? "";
        if (string.IsNullOrEmpty(text) || IsGenerating) return;

        if (!AiEnabled)
        {
            // Banner statt Echo — User-Nachricht trotzdem anzeigen + Hinweis vom Assistant
            Add("user", text);
            Add("assistant", "KI ist in den Einstellungen deaktiviert. Bitte aktivieren zum Nutzen.");
            Input = "";
            return;
        }

        Input = "";
        IsGenerating = true;
        Add("user", text);

        try
        {
            var sb = new System.Text.StringBuilder();
            var row = new ChatRowVm("assistant", "", DateTime.UtcNow);
            await Dispatcher.UIThread.InvokeAsync(() => Messages.Add(row));

            await foreach (var chunk in _engine.StreamAsync(text))
            {
                sb.Append(chunk.Text);
                var snapshot = sb.ToString();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    row.Content = snapshot;
                    row.RaisePropertyChanged(nameof(row.Content));
                });
            }

            // Persist in DB
            PersistMessage("user", text);
            PersistMessage("assistant", sb.ToString());
        }
        catch (Exception ex)
        {
            Add("assistant", $"Fehler: {ex.Message}");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    /// <summary>Öffnet die Einstellungen → Sektion KI-Assistent.</summary>
    [RelayCommand]
    public void ConfigureModel()
    {
        // Settings werden vom MainViewModel geöffnet; hier nur Signal:
        // In einer späteren Iteration kann ein EventRaised werden. Für jetzt
        // wird der Klick im XAML via VisualTree nach oben propagiert.
    }

    private void Add(string role, string content)
    {
        var row = new ChatRowVm(role, content, DateTime.UtcNow);
        Messages.Add(row);
        PersistMessage(role, content);
    }

    private void PersistMessage(string role, string content)
    {
        try
        {
            _db.ChatMessages.Add(new ChatMessage { Role = role, Content = content, Timestamp = DateTime.UtcNow });
            _db.SaveChanges();
        }
        catch
        {
            // Best-effort
        }
    }
}