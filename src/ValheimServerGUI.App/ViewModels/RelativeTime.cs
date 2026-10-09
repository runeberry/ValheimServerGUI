using System;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// Formats a timestamp relative to now ("just now", "5 minutes ago", "in 2 hours") for the "Since" columns.
/// Replaces the v2.4 <c>TimeAgo</c> helper; each view model re-formats on its own 1s tick while visible.
/// </summary>
internal static class RelativeTime
{
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
