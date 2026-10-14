using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ValheimServerGUI.App.Converters;

/// <summary>
/// Maps <c>true</c> → 1.0 and <c>false</c> → 0.0. Used to hide a control while preserving its layout slot:
/// Avalonia has no WPF-style <c>Visibility.Hidden</c> (<c>IsVisible=false</c> collapses and gives up its
/// space), so a control that must keep its space when absent binds Opacity here (plus IsHitTestVisible).
/// </summary>
public sealed class BooleanToOpacityConverter : IValueConverter
{
    public static readonly BooleanToOpacityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 1.0 : 0.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
