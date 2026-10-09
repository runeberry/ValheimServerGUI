using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ValheimServerGUI.App.Controls;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.App.Converters;

/// <summary>
/// The display text for every enum the UI shows. Enum values stay the stored/compared tokens; only the text a
/// user reads comes from <see cref="Strings"/>. Used directly by view models (status/role columns) and as the
/// item display of dropdowns over enum values. Anything that is not a mapped enum displays as itself.
/// </summary>
public sealed class EnumDisplayConverter : IValueConverter
{
    public static readonly EnumDisplayConverter Instance = new();

    public static string ToText(ServerStatus value) => value switch
    {
        ServerStatus.Stopped => Strings.ServerStatus_Stopped,
        ServerStatus.Starting => Strings.ServerStatus_Starting,
        ServerStatus.Running => Strings.ServerStatus_Running,
        ServerStatus.Stopping => Strings.ServerStatus_Stopping,
        _ => value.ToString(),
    };

    public static string ToText(PlayerStatus value) => value switch
    {
        PlayerStatus.Offline => Strings.PlayerStatus_Offline,
        PlayerStatus.Joining => Strings.PlayerStatus_Joining,
        PlayerStatus.Online => Strings.PlayerStatus_Online,
        PlayerStatus.Leaving => Strings.PlayerStatus_Leaving,
        _ => value.ToString(),
    };

    public static string ToText(PlayerRole value) => value switch
    {
        PlayerRole.Admin => Strings.Role_Admin,
        PlayerRole.Permitted => Strings.Role_Permitted,
        PlayerRole.Banned => Strings.Role_Banned,
        PlayerRole.None => Strings.Role_None,
        _ => value.ToString(),
    };

    public static string ToText(AppTheme value) => value switch
    {
        AppTheme.System => Strings.Theme_System,
        AppTheme.Light => Strings.Theme_Light,
        AppTheme.Dark => Strings.Theme_Dark,
        _ => value.ToString(),
    };

    public static string ToText(DurationUnit value) => value switch
    {
        DurationUnit.Seconds => Strings.DurationUnit_Seconds,
        DurationUnit.Minutes => Strings.DurationUnit_Minutes,
        DurationUnit.Hours => Strings.DurationUnit_Hours,
        DurationUnit.Days => Strings.DurationUnit_Days,
        _ => value.ToString(),
    };

    public static string ToText(LogView value) => value switch
    {
        LogView.Server => Strings.Logs_View_Server,
        LogView.Application => Strings.Logs_View_Application,
        _ => value.ToString(),
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ServerStatus v => ToText(v),
        PlayerStatus v => ToText(v),
        PlayerRole v => ToText(v),
        AppTheme v => ToText(v),
        DurationUnit v => ToText(v),
        LogView v => ToText(v),
        _ => value,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
