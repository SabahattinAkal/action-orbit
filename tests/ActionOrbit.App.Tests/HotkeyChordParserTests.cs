using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.Tests;

public sealed class HotkeyChordParserTests
{
    [Theory]
    [InlineData("Ctrl+Shift+T", "Ctrl|Shift|T")]
    [InlineData(" Win + . ", "Win|.")]
    [InlineData("Ctrl++", "Ctrl|+")]
    [InlineData("+", "+")]
    [InlineData("Ctrl+Plus", "Ctrl|Plus")]
    public void TryParseTokens_PreservesSupportedKeys(string input, string expected)
    {
        var succeeded = HotkeyChordParser.TryParseTokens(input, out var tokens);

        Assert.True(succeeded);
        Assert.Equal(expected, string.Join('|', tokens));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl++A")]
    [InlineData("Ctrl+++")]
    public void TryParseTokens_RejectsIncompleteOrAmbiguousChords(string input)
    {
        Assert.False(HotkeyChordParser.TryParseTokens(input, out var tokens));
        Assert.Empty(tokens);
    }
}
