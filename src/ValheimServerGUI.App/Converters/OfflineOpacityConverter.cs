using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ValheimServerGUI.App.Converters;

/// <summary>Greys out offline player rows: true → dimmed, false → full opacity.</summary>
public sealed class OfflineOpacityConverter : IValueConverter
{
    public static readonly OfflineOpacityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 0.5 : 1.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
