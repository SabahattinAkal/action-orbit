using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class ActionOrderServiceTests
{
    [Theory]
    [InlineData("A", "C", false, "B,A,C,D", 1)]
    [InlineData("A", "C", true, "B,C,A,D", 2)]
    [InlineData("D", "B", false, "A,D,B,C", 1)]
    [InlineData("D", "B", true, "A,B,D,C", 2)]
    public void TryMoveRelative_PlacesItemOnRequestedSideOfTarget(
        string source,
        string target,
        bool placeAfterTarget,
        string expectedOrder,
        int expectedIndex)
    {
        var items = new List<string> { "A", "B", "C", "D" };

        var moved = ActionOrderService.TryMoveRelative(
            items,
            source,
            target,
            placeAfterTarget,
            out var result);

        Assert.True(moved);
        Assert.Equal(expectedOrder.Split(','), items);
        Assert.Equal(new ActionOrderMove(Array.IndexOf(new[] { "A", "B", "C", "D" }, source), expectedIndex), result);
    }

    [Theory]
    [InlineData("A", "B", false)]
    [InlineData("B", "A", true)]
    public void TryMoveRelative_ReturnsFalseWhenRequestedPositionIsUnchanged(
        string source,
        string target,
        bool placeAfterTarget)
    {
        var items = new List<string> { "A", "B", "C" };

        Assert.False(ActionOrderService.TryMoveRelative(items, source, target, placeAfterTarget, out _));
        Assert.Equal(["A", "B", "C"], items);
    }

    [Fact]
    public void TryMoveToTarget_MovesForwardToTargetsOriginalPosition()
    {
        var items = new List<string> { "A", "B", "C", "D" };

        var moved = ActionOrderService.TryMoveToTarget(items, "A", "C", out var result);

        Assert.True(moved);
        Assert.Equal(["B", "C", "A", "D"], items);
        Assert.Equal(new ActionOrderMove(0, 2), result);
    }

    [Fact]
    public void TryMoveToTarget_MovesBackwardToTargetsOriginalPosition()
    {
        var items = new List<string> { "A", "B", "C", "D" };

        var moved = ActionOrderService.TryMoveToTarget(items, "D", "B", out var result);

        Assert.True(moved);
        Assert.Equal(["A", "D", "B", "C"], items);
        Assert.Equal(new ActionOrderMove(3, 1), result);
    }

    [Theory]
    [InlineData("A", "A")]
    [InlineData("A", "missing")]
    [InlineData("missing", "A")]
    public void TryMoveToTarget_LeavesCollectionUntouchedForInvalidMove(string source, string target)
    {
        var items = new List<string> { "A", "B", "C" };

        Assert.False(ActionOrderService.TryMoveToTarget(items, source, target, out _));
        Assert.Equal(["A", "B", "C"], items);
    }
}
