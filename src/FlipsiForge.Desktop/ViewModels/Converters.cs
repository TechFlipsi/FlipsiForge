// SPDX-License-Identifier: GPL-3.0-or-later
// Kleine Converter-Sammlung für die Desktop-Views.
using System.Globalization;
using Avalonia.Data.Converters;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>Konvertiert 0 → true, alle anderen Zahlen → false (für Empty-State-Anzeige).</summary>
public sealed class ZeroToBoolConverter : IValueConverter
{
    public static readonly ZeroToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i) return i == 0;
        if (value is long l) return l == 0;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}