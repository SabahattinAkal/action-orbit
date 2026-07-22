using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class ActionOrderServiceTests
{
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
