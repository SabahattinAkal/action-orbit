using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.Tests;

public sealed class HotkeyParserTests
{
    [Theory]
    [InlineData("shift+ctrl+f13", "Ctrl+Shift+f13", "f13")]
    [InlineData("F14", "F14", "F14")]
    [InlineData("Ctrl+Alt+Shift+R", "Ctrl+Alt+Shift+R", "R")]
    [InlineData("Win+.", "Win+.", ".")]
    [InlineData("Ctrl++", "Ctrl++", "+")]
    public void TryParseDisplay_NormalizesSupportedHotkeys(
        string input,
        string expectedDisplay,
        string expectedKey)
    {
        var succeeded = HotkeyParser.TryParseDisplay(input, out var hotkey, out var error);

        Assert.True(succeeded, error);
        Assert.Equal(expectedDisplay, hotkey.Display);
        Assert.Equal(expectedKey, hotkey.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+Alt")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl++A")]
    [InlineData("Ctrl+A+B")]
    [InlineData("Ctrl+NotARealKey")]
    public void TryParseDisplay_RejectsInvalidHotkeys(string input)
    {
        var succeeded = HotkeyParser.TryParseDisplay(input, out _, out var error);

        Assert.False(succeeded);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
