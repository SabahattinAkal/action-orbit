using Microsoft.Win32;
using System.Windows.Media;

namespace ActionOrbit.App.Services;

public static class ThemeService
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool IsLightMode(string? configuredMode)
    {
        if (string.Equals(configuredMode, "light", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(configuredMode, "dark", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue("AppsUseLightTheme") is not int value || value != 0;
        }
        catch
        {
            return true;
        }
    }

    public static void ApplyApplicationTheme(string? configuredMode, string? accent)
    {
        var resources = System.Windows.Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        var isLight = IsLightMode(configuredMode);
        var palette = isLight
            ? new Dictionary<string, string>
            {
                ["WindowBackgroundBrush"] = "#F4F5F7",
                ["PrimaryTextBrush"] = "#171A20",
                ["MutedTextBrush"] = "#6B7280",
                ["SurfaceBrush"] = "#FFFFFF",
                ["SoftSurfaceBrush"] = "#F8F9FB",
                ["BorderBrush"] = "#E3E6EC",
                ["InputBorderBrush"] = "#DCE1EA",
                ["HoverSurfaceBrush"] = "#F2F4F7",
                ["PressedSurfaceBrush"] = "#E9EDF3",
                ["SelectedSurfaceBrush"] = "#EEF1F6",
                ["SelectedBorderBrush"] = "#D8DEE8",
                ["NavigationBrush"] = "#E9ECF1",
                ["IconSurfaceBrush"] = "#F1F3F6",
                ["DangerTextBrush"] = "#9F1239",
                ["DangerSurfaceBrush"] = "#FFF5F7",
                ["DangerBorderBrush"] = "#F8CED8"
            }
            : new Dictionary<string, string>
            {
                ["WindowBackgroundBrush"] = "#0F1115",
                ["PrimaryTextBrush"] = "#F4F6FA",
                ["MutedTextBrush"] = "#A5ADBA",
                ["SurfaceBrush"] = "#181B21",
                ["SoftSurfaceBrush"] = "#20242C",
                ["BorderBrush"] = "#303641",
                ["InputBorderBrush"] = "#3A414E",
                ["HoverSurfaceBrush"] = "#292E38",
                ["PressedSurfaceBrush"] = "#343B47",
                ["SelectedSurfaceBrush"] = "#2B303A",
                ["SelectedBorderBrush"] = "#4A5362",
                ["NavigationBrush"] = "#20242B",
                ["IconSurfaceBrush"] = "#262B34",
                ["DangerTextBrush"] = "#FDA4AF",
                ["DangerSurfaceBrush"] = "#3A2028",
                ["DangerBorderBrush"] = "#713744"
            };

        foreach (var (key, color) in palette)
        {
            resources[key] = CreateFrozenBrush(color);
        }

        resources["PrimaryActionBrush"] = CreateFrozenBrush(NormalizeAccent(accent));
        resources["OnPrimaryActionBrush"] = CreateFrozenBrush("#FFFFFF");
    }

    private static System.Windows.Media.Brush CreateFrozenBrush(string color)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
        brush.Freeze();
        return brush;
    }

    private static string NormalizeAccent(string? accent)
    {
        var value = accent?.Trim() ?? "";
        return value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit)
            ? value
            : "#A51E39";
    }
}
