using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class ActiveWindowServiceTests
{
    [Fact]
    public void ResolveIgnoredProcess_CanReturnEmptyInsteadOfPreviousApplication()
    {
        var result = ActiveWindowService.ResolveIgnoredProcess(
            "ActionOrbit.App.exe",
            "actionorbit.app.exe",
            "explorer.exe",
            fallbackToLastExternal: false);

        Assert.Equal("", result);
    }

    [Fact]
    public void ResolveIgnoredProcess_PreservesPreviousApplicationForPreviewFlows()
    {
        var result = ActiveWindowService.ResolveIgnoredProcess(
            "ActionOrbit.App.exe",
            "ActionOrbit.App.exe",
            "explorer.exe",
            fallbackToLastExternal: true);

        Assert.Equal("explorer.exe", result);
    }

    [Fact]
    public void ResolveIgnoredProcess_ReturnsObservedProcessWhenItIsNotIgnored()
    {
        var result = ActiveWindowService.ResolveIgnoredProcess(
            "chrome.exe",
            "ActionOrbit.App.exe",
            "explorer.exe",
            fallbackToLastExternal: false);

        Assert.Equal("chrome.exe", result);
    }
}
