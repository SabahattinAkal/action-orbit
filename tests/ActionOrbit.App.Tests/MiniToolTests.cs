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
            ["timer", "caffeine", "system_glance", "calculator", "color_picker"],
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
        Assert.Equal(5, folder.Children.Count);
        Assert.All(folder.Children, action => Assert.Equal("mini_tool", action.Type));
        Assert.Equal(MiniToolCatalog.Tools.Select(tool => tool.Id), folder.Children.Select(action => action.Target));
    }

    private sealed class RecordingLauncher : IMiniToolLauncher
    {
        public string? LastToolId { get; private set; }

        public void Show(string toolId) => LastToolId = toolId;
    }
}
