using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ValheimServerGUI.App.Converters;

/// <summary>Maps a "show password" bool to a <see cref="char"/> mask: shown → none ('\0'), hidden → bullet.</summary>
public sealed class PasswordCharConverter : IValueConverter
{
    public static readonly PasswordCharConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? '\0' : '•';

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
