// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Input;
using FlipsiForge.Desktop.ViewModels;

namespace FlipsiForge.Desktop.Views;

/// <summary>Forge-Bot Mascot-Overlay (nicht als Tab, sondern als Overlay in MainWindow).</summary>
public partial class ForgeBotView : UserControl
{
    public ForgeBotView()
    {
        InitializeComponent();
    }

    /// <summary>Bei Klick auf den Mascot das kleine Menü toggeln.</summary>
    private void Mascot_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ForgeBotViewModel vm)
            vm.BotClickedCommand.Execute(null);
        e.Handled = true;
    }
}