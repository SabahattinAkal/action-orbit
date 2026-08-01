using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Tests;

public sealed class OverlayRingSetTests
{
    [Fact]
    public void SwitchRing_UsesAdditionalRingAndWrapsWithMouseDirection()
    {
        using var temp = new TemporaryDirectory();
        var log = new LogService(temp.Path);
        var profile = new ProfileConfig
        {
            Id = "default",
            Name = "Default",
            MainRingName = "Daily",
            Actions = [Action("main", "Main")],
            RingSets =
            [
                new RingSetConfig { Id = "design", Name = "Design", Actions = [Action("figma", "Figma")] },
                new RingSetConfig { Id = "media", Name = "Media", Actions = [Action("music", "Music")] }
            ]
        };
        var execution = new ActionExecutionService(log, []);
        var viewModel = new OverlayViewModel(profile, profile, new ThemeConfig(), execution, log, IntPtr.Zero);

        Assert.Equal("Daily", viewModel.CurrentRingName);
        Assert.Equal("Main", viewModel.ActionItems[0].Action.Title);
        Assert.True(viewModel.SwitchRing(1));
        Assert.Equal("Design", viewModel.CurrentRingName);
        Assert.Equal("Figma", viewModel.ActionItems[0].Action.Title);
        Assert.True(viewModel.SwitchRing(-1));
        Assert.Equal("Daily", viewModel.CurrentRingName);
    }

    private static OrbitAction Action(string id, string title) => new()
    {
        Id = id,
        Title = title,
        Type = "open_url",
        Target = "https://example.com"
    };
}
