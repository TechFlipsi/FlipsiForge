using Avalonia.Controls;

namespace FlipsiForge.Desktop.Views;

/// <summary>
/// Kombinierte View: Drucker-Karten (PrinterView) + DruckWächter-Controls
/// (DruckWaechterView) in einem ScrollViewer. Wird vom MainViewModel für
/// den zusammengefassten Tab "Drucker &amp; Wächter" erzeugt.
/// </summary>
public partial class CombinedPrinterView : UserControl
{
    public CombinedPrinterView()
    {
        InitializeComponent();
    }
}