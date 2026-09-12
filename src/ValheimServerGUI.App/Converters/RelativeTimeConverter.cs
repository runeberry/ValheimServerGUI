using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ValheimServerGUI.App.Converters;

/// <summary>
/// Formats a <see cref="DateTimeOffset"/>/<see cref="DateTime"/> as a relative time ("just now",
/// "5 minutes ago", "in 2 hours"). Replaces the v2.4 static <c>TimeAgo</c> helper (§15 #2) with a binding
/// converter, so a "Since" column re-reads the same source instead of being kept in step by hand.
/// </summary>
public sealed class RelativeTimeConverter : IValueConverter
{
    public static readonly RelativeTimeConverter Instance = new();

    /// <summary>Now-provider seam so relative output is deterministic under test.</summary>
    internal Func<DateTimeOffset> NowProvider { get; set; } = () => DateTimeOffset.Now;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var when = value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt.ToUniversalTime(), TimeSpan.Zero),
            _ => (DateTimeOffset?)null,
        };

        if (when is null) return string.Empty;

        return Format(when.Value, NowProvider());
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    internal static string Format(DateTimeOffset when, DateTimeOffset now)
    {
        var delta = now - when;
        var future = delta < TimeSpan.Zero;
        var span = future ? -delta : delta;

        var text = span.TotalSeconds switch
        {
            < 10 => "just now",
            < 60 => Plural((int)span.TotalSeconds, "second"),
            < 3600 => Plural((int)span.TotalMinutes, "minute"),
            < 86400 => Plural((int)span.TotalHours, "hour"),
            _ => Plural((int)span.TotalDays, "day"),
        };

        if (text == "just now") return text;
        return future ? $"in {text}" : $"{text} ago";
    }

    private static string Plural(int count, string unit)
        => count == 1 ? $"1 {unit}" : $"{count} {unit}s";
}
