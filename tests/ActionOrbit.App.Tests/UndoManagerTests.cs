using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class UndoManagerTests
{
    [Fact]
    public void Undo_ExecutesLatestOperationOnlyOnce()
    {
        var manager = new UndoManager();
        var value = 0;
        manager.Register("değişiklik", () => value++);

        var description = manager.Undo();
        var secondUndo = manager.Undo();

        Assert.Equal("değişiklik", description);
        Assert.Null(secondUndo);
        Assert.Equal(1, value);
        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void Register_ReplacesPreviousOperation()
    {
        var manager = new UndoManager();
        var value = 0;
        manager.Register("ilk", () => value += 1);
        manager.Register("son", () => value += 10);

        Assert.Equal("son", manager.Undo());
        Assert.Equal(10, value);
    }

    [Fact]
    public void Clear_DiscardsOperationWithoutExecutingIt()
    {
        var manager = new UndoManager();
        var executed = false;
        manager.Register("işlem", () => executed = true);

        manager.Clear();

        Assert.False(executed);
        Assert.False(manager.CanUndo);
        Assert.Null(manager.Undo());
    }
}
