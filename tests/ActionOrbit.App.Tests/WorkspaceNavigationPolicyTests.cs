using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class WorkspaceNavigationPolicyTests
{
    [Theory]
    [InlineData("home")]
    [InlineData("editor")]
    [InlineData("library")]
    [InlineData("settings")]
    public void IsSupported_AcceptsMainWindowWorkspaces(string workspace)
    {
        Assert.True(WorkspaceNavigationPolicy.IsSupported(workspace));
    }

    [Theory]
    [InlineData("shelf")]
    [InlineData("")]
    [InlineData(null)]
    public void IsSupported_RejectsFloatingShelfAndUnknownRoutes(string? workspace)
    {
        Assert.False(WorkspaceNavigationPolicy.IsSupported(workspace));
    }
}
