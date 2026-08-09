using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.Services.MiniTools;

namespace ActionOrbit.App.Tests;

public sealed class MiniToolTests
{
    [Fact]
    public void Catalog_ContainsOnlySupportedBuiltInTools()
    {
        Assert.Equal(
            [
                "timer",
                "caffeine",
                "system_glance",
                "calculator",
                "color_picker",
                "stopwatch",
                "quick_note",
                "unit_converter",
                "text_tools",
                "password_generator"
            ],
            MiniToolCatalog.Tools.Select(tool => tool.Id));
        Assert.True(MiniToolCatalog.TryGet(" TIMER ", out var timer));
        Assert.Equal("Zamanlayıcı", timer.Title);
        Assert.False(MiniToolCatalog.TryGet("command_prompt", out _));
    }

    [Fact]
    public async Task Handler_OpensAllowedTool()
    {
        var launcher = new RecordingLauncher();
        var handler = new MiniToolActionHandler(launcher);
        var action = new OrbitAction { Type = "mini_tool", Target = "calculator" };

        var result = await handler.ExecuteAsync(action);

        Assert.True(result.Succeeded);
        Assert.Equal("calculator", launcher.LastToolId);
    }

    [Fact]
    public async Task Handler_RejectsUnknownToolWithoutCallingLauncher()
    {
        var launcher = new RecordingLauncher();
        var handler = new MiniToolActionHandler(launcher);
        var action = new OrbitAction { Type = "mini_tool", Target = "powershell" };

        var result = await handler.ExecuteAsync(action);

        Assert.False(result.Succeeded);
        Assert.Null(launcher.LastToolId);
    }

    [Theory]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("-8 / 2 + 10", 6)]
    [InlineData("10 % 4", 2)]
    [InlineData("1,5 + 2.5", 4)]
    [InlineData("3 × (8 − 2) ÷ 2", 9)]
    public void Calculator_EvaluatesSafeExpressions(string expression, double expected)
    {
        var succeeded = CalculatorEngine.TryEvaluate(expression, out var result, out var issue);

        Assert.True(succeeded, issue);
        Assert.Equal(expected, result, precision: 8);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2 +")]
    [InlineData("(2 + 3")]
    [InlineData("10 / 0")]
    [InlineData("System.IO.File.Delete(1)")]
    public void Calculator_RejectsInvalidOrUnsafeExpressions(string expression)
    {
        Assert.False(CalculatorEngine.TryEvaluate(expression, out _, out var issue));
        Assert.NotEmpty(issue);
    }

    [Fact]
    public void DefaultConfig_ExposesMiniToolsAsFolder()
    {
        var config = DefaultConfigFactory.Create();
        var folder = Assert.Single(
            config.Profiles.Single(profile => profile.Id == config.DefaultProfileId).Actions,
            action => action.Id == "mini_tools");

        Assert.Equal("folder", folder.Type);
        Assert.Equal(10, folder.Children.Count);
        Assert.All(folder.Children, action => Assert.Equal("mini_tool", action.Type));
        Assert.Equal(MiniToolCatalog.Tools.Select(tool => tool.Id), folder.Children.Select(action => action.Target));
    }

    [Theory]
    [InlineData("length", "kilometer", "meter", 1, 1000)]
    [InlineData("temperature", "celsius", "fahrenheit", 20, 68)]
    [InlineData("data", "gigabyte", "megabyte", 1, 1024)]
    public void UnitConverter_ConvertsSupportedUnits(
        string category,
        string source,
        string target,
        double value,
        double expected)
    {
        var succeeded = UnitConversionEngine.TryConvert(category, source, target, value, out var result);

        Assert.True(succeeded);
        Assert.Equal(expected, result, precision: 8);
    }

    [Fact]
    public void UnitConverter_RejectsUnknownOrNonFiniteValues()
    {
        Assert.False(UnitConversionEngine.TryConvert("unknown", "a", "b", 1, out _));
        Assert.False(UnitConversionEngine.TryConvert("length", "meter", "foot", double.NaN, out _));
    }

    [Theory]
    [InlineData("1,5", 1.5)]
    [InlineData("1.5", 1.5)]
    [InlineData("-2,75", -2.75)]
    public void UnitConverter_ParsesTurkishAndInvariantDecimalSeparators(string input, double expected)
    {
        Assert.True(UnitConversionEngine.TryParseValue(input, out var result));
        Assert.Equal(expected, result, precision: 8);
    }

    [Fact]
    public void TextTools_HandleTurkishCasingCountsAndWhitespace()
    {
        Assert.Equal("İYİ İŞ", TextTransformService.ToUpper("iyi iş"));
        Assert.Equal("IŞIK İÇİN", TextTransformService.ToUpper("ışık için"));
        Assert.Equal("İyi İş", TextTransformService.ToTitleCase("İYİ İŞ"));
        Assert.Equal(3, TextTransformService.CountWords("Merhaba, güzel dünya!"));
        Assert.Equal(
            $"Merhaba dünya{Environment.NewLine}{Environment.NewLine}İkinci satır",
            TextTransformService.NormalizeWhitespace("  Merhaba   dünya  \n\n\n İkinci\t satır "));
    }

    [Fact]
    public void PasswordGenerator_GuaranteesEverySelectedCharacterGroup()
    {
        var password = PasswordGenerator.Generate(
            32,
            includeLowercase: true,
            includeUppercase: true,
            includeDigits: true,
            includeSymbols: true);

        Assert.Equal(32, password.Length);
        Assert.Contains(password, char.IsLower);
        Assert.Contains(password, char.IsUpper);
        Assert.Contains(password, char.IsDigit);
        Assert.Contains(password, character => "!@#$%&*+-_=?:".Contains(character));
        Assert.DoesNotContain(password, character => "O0Il1".Contains(character));
    }

    [Fact]
    public void PasswordGenerator_RejectsEmptyCharacterSelection()
    {
        Assert.Throws<ArgumentException>(() =>
            PasswordGenerator.Generate(20, false, false, false, false));
    }

    [Fact]
    public void QuickNoteStore_RoundTripsLocallyAndEnforcesSizeLimit()
    {
        using var temp = new TemporaryDirectory();
        var store = new QuickNoteStore(temp.Path);

        store.Save("Bugünkü kısa not");

        Assert.Equal("Bugünkü kısa not", store.Load());
        Assert.StartsWith(temp.Path, store.NotePath, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<InvalidOperationException>(() =>
            store.Save(new string('x', QuickNoteStore.MaxNoteBytes + 1)));
        Assert.Empty(Directory.GetFiles(temp.Path, "*.tmp"));
    }

    private sealed class RecordingLauncher : IMiniToolLauncher
    {
        public string? LastToolId { get; private set; }

        public void Show(string toolId) => LastToolId = toolId;
    }
}
