using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class EditorLayoutPolicyTests
{
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
