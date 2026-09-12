using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ValheimServerGUI.App.Converters;

/// <summary>True when the bound string is non-empty. Used to collapse the help glyph / empty labels.</summary>
public sealed class StringNotEmptyConverter : IValueConverter
{
    public static readonly StringNotEmptyConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
