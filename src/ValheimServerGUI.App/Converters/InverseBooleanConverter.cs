using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ValheimServerGUI.App.Converters;

/// <summary>Negates a bool both ways. Used to pair the Existing/New world radios on one <c>UseNewWorld</c> flag.</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public static readonly InverseBooleanConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;
}
