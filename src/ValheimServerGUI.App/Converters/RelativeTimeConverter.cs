using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ValheimServerGUI.Localization;

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

        if (span.TotalSeconds < 10) return Strings.RelativeTime_JustNow;

        var text = span.TotalSeconds switch
        {
            < 60 => Plural((int)span.TotalSeconds, Strings.RelativeTime_Seconds_One, Strings.RelativeTime_Seconds_Other),
            < 3600 => Plural((int)span.TotalMinutes, Strings.RelativeTime_Minutes_One, Strings.RelativeTime_Minutes_Other),
            < 86400 => Plural((int)span.TotalHours, Strings.RelativeTime_Hours_One, Strings.RelativeTime_Hours_Other),
            _ => Plural((int)span.TotalDays, Strings.RelativeTime_Days_One, Strings.RelativeTime_Days_Other),
        };

        return string.Format(future ? Strings.RelativeTime_Future : Strings.RelativeTime_Past, text);
    }

    private static string Plural(int count, string one, string other)
        => count == 1 ? one : string.Format(other, count);
}
