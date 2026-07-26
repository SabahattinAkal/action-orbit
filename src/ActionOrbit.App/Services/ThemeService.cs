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
                ["InfoTextBrush"] = "#3730A3",
                ["InfoSurfaceBrush"] = "#EEF2FF",
                ["InfoBorderBrush"] = "#D8E0FF",
                ["WarningTextBrush"] = "#9A3412",
                ["WarningSurfaceBrush"] = "#FFF7ED",
                ["WarningBorderBrush"] = "#FED7AA",
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
                ["InfoTextBrush"] = "#C7D2FE",
                ["InfoSurfaceBrush"] = "#242B44",
                ["InfoBorderBrush"] = "#46588F",
                ["WarningTextBrush"] = "#FDBA74",
                ["WarningSurfaceBrush"] = "#3A2B1C",
                ["WarningBorderBrush"] = "#7C4A24",
                ["DangerTextBrush"] = "#FDA4AF",
                ["DangerSurfaceBrush"] = "#3A2028",
                ["DangerBorderBrush"] = "#713744"
            };

        foreach (var (key, color) in palette)
        {
            resources[key] = CreateFrozenBrush(color);
        }

        var normalizedAccent = NormalizeAccent(accent);
        resources["PrimaryActionBrush"] = CreateFrozenBrush(normalizedAccent);
        resources["OnPrimaryActionBrush"] = CreateFrozenBrush(GetContrastingForeground(normalizedAccent));
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

    public static string GetContrastingForeground(string? background)
    {
        var normalized = NormalizeAccent(background);
        var red = Convert.ToByte(normalized[1..3], 16) / 255d;
        var green = Convert.ToByte(normalized[3..5], 16) / 255d;
        var blue = Convert.ToByte(normalized[5..7], 16) / 255d;
        var luminance =
            0.2126 * ToLinear(red)
            + 0.7152 * ToLinear(green)
            + 0.0722 * ToLinear(blue);
        var whiteContrast = 1.05 / (luminance + 0.05);
        var darkContrast = (luminance + 0.05) / 0.05;
        return darkContrast >= whiteContrast ? "#111318" : "#FFFFFF";
    }

    private static double ToLinear(double component) =>
        component <= 0.04045
            ? component / 12.92
            : Math.Pow((component + 0.055) / 1.055, 2.4);
}
