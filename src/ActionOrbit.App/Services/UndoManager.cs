namespace ActionOrbit.App.Services;

public sealed class UndoManager
{
    private Action? _undoAction;

    public event EventHandler? StateChanged;

    public bool CanUndo => _undoAction is not null;
    public string Description { get; private set; } = "";
    public string ButtonText => CanUndo ? $"Geri Al · {Description}" : "Geri Al";

    public void Register(string description, Action undoAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(undoAction);

        Description = description;
        _undoAction = undoAction;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public string? Undo()
    {
        var undoAction = _undoAction;
        if (undoAction is null)
        {
            return null;
        }

        var description = Description;
        _undoAction = null;
        Description = "";
        StateChanged?.Invoke(this, EventArgs.Empty);
        undoAction();
        return description;
    }

    public void Clear()
    {
        if (!CanUndo && Description.Length == 0)
        {
            return;
        }

        _undoAction = null;
        Description = "";
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
