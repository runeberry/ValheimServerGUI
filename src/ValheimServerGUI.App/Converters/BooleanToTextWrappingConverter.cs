using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;

namespace ValheimServerGUI.App.Converters;

/// <summary>Maps a "multiline" bool to <see cref="TextWrapping"/> (true → Wrap, false → NoWrap).</summary>
public sealed class BooleanToTextWrappingConverter : IValueConverter
{
    public static readonly BooleanToTextWrappingConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TextWrapping.Wrap : TextWrapping.NoWrap;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Maps a "multiline" bool to text alignment: multiline → Top, single-line → Center.</summary>
public sealed class MultilineToVerticalAlignmentConverter : IValueConverter
{
    public static readonly MultilineToVerticalAlignmentConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? VerticalAlignment.Top : VerticalAlignment.Center;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
