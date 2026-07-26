using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class ThemeServiceTests
{
    [Theory]
    [InlineData("light", true)]
    [InlineData("LIGHT", true)]
    [InlineData("dark", false)]
    [InlineData("DARK", false)]
    public void ExplicitMode_DoesNotDependOnWindowsSetting(string mode, bool expected)
    {
        Assert.Equal(expected, ThemeService.IsLightMode(mode));
    }

    [Theory]
    [InlineData("#FFFFFF", "#111318")]
    [InlineData("#FACC15", "#111318")]
    [InlineData("#000000", "#FFFFFF")]
    [InlineData("#A51E39", "#FFFFFF")]
    [InlineData("invalid", "#FFFFFF")]
    public void AccentForeground_SelectsReadableContrast(string accent, string expected)
    {
        Assert.Equal(expected, ThemeService.GetContrastingForeground(accent));
    }
}
