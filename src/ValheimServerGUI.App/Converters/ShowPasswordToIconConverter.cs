using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using ValheimServerGUI.App.Controls;

namespace ValheimServerGUI.App.Converters;

/// <summary>Maps the "show password" bool to the visibility-toggle glyph: shown → open eye
/// (<c>Visible_16x</c>), hidden → struck-through eye (<c>Visible_Hidden_16x</c>).</summary>
public sealed class ShowPasswordToIconConverter : IValueConverter
{
    public static readonly ShowPasswordToIconConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AppIcons.Get(value is true ? "Visible_16x" : "Visible_Hidden_16x");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
