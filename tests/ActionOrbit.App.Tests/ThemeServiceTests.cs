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
}
