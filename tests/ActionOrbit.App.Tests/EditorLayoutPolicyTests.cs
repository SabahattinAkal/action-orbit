using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class EditorLayoutPolicyTests
{
    [Theory]
    [InlineData(900, false, true, EditorLayoutMode.Wide)]
    [InlineData(900, true, false, EditorLayoutMode.Wide)]
    [InlineData(600, false, false, EditorLayoutMode.Wide)]
    [InlineData(600, true, false, EditorLayoutMode.Compact)]
    public void Resolve_ReclaimsSpaceWhenSidePanelsAreHidden(double width, bool profiles, bool details, EditorLayoutMode expected)
        => Assert.Equal(expected, EditorLayoutPolicy.Resolve(width, profiles, details));

    [Fact]
    public void WorkspaceScroll_IsReservedForCompactStackedLayout()
    {
        Assert.False(EditorLayoutPolicy.ShouldScrollWorkspace(EditorLayoutMode.Wide));
        Assert.True(EditorLayoutPolicy.ShouldScrollWorkspace(EditorLayoutMode.Compact));
    }

    [Theory]
    [InlineData(0, EditorLayoutMode.Compact)]
    [InlineData(920, EditorLayoutMode.Compact)]
    [InlineData(1079.9, EditorLayoutMode.Compact)]
    [InlineData(1080, EditorLayoutMode.Wide)]
    [InlineData(1600, EditorLayoutMode.Wide)]
    public void Resolve_SelectsLayoutForAvailableDipWidth(double width, EditorLayoutMode expected)
    {
        Assert.Equal(expected, EditorLayoutPolicy.Resolve(width));
    }
}
