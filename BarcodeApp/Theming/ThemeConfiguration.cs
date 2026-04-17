using Avalonia.Styling;

namespace BarcodeApp.Theming;

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public static class ThemeConfiguration
{
    public static AppThemeMode DefaultMode => AppThemeMode.System;

    public static AppThemeMode Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return AppThemeMode.System;

        return value.Trim().ToLowerInvariant() switch
        {
            "system" => AppThemeMode.System,
            "light" => AppThemeMode.Light,
            "dark" => AppThemeMode.Dark,
            _ => AppThemeMode.System
        };
    }

    public static AppThemeMode ResolveFromEnvironment(string? envValue)
    {
        return Parse(envValue);
    }

    public static ThemeVariant ToThemeVariant(AppThemeMode mode)
    {
        return mode switch
        {
            AppThemeMode.Light => ThemeVariant.Light,
            AppThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}