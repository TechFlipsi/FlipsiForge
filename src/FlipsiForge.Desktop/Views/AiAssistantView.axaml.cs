// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using FlipsiForge.Desktop.ViewModels;

namespace FlipsiForge.Desktop.Views;

/// <summary>KI-Drucker-Assistent-Chat-View.</summary>
public partial class AiAssistantView : UserControl
{
    public AiAssistantView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>Enter sendet die Nachricht.</summary>
    private void Input_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return && e.Key != Key.Enter) return;
        if (DataContext is AiAssistantViewModel vm && !vm.IsGenerating)
            vm.SendCommand.Execute(null);
    }
}