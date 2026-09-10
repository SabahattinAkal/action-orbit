namespace ActionOrbit.App.Services;

internal readonly record struct ActionOrderMove(int OriginalIndex, int NewIndex);

internal static class ActionOrderService
{
    public static bool TryMoveRelative<T>(
        IList<T> items,
        T source,
        T target,
        bool placeAfterTarget,
        out ActionOrderMove move)
    {
        ArgumentNullException.ThrowIfNull(items);

        var originalIndex = items.IndexOf(source);
        var targetIndex = items.IndexOf(target);
        move = default;

        if (originalIndex < 0 || targetIndex < 0 || originalIndex == targetIndex)
        {
            return false;
        }

        var insertionIndex = targetIndex + (placeAfterTarget ? 1 : 0);
        if (originalIndex < insertionIndex)
        {
            insertionIndex--;
        }

        var newIndex = Math.Clamp(insertionIndex, 0, items.Count - 1);
        if (newIndex == originalIndex)
        {
            return false;
        }

        items.RemoveAt(originalIndex);
        items.Insert(newIndex, source);
        move = new ActionOrderMove(originalIndex, newIndex);
        return true;
    }

    public static bool TryMoveToTarget<T>(
        IList<T> items,
        T source,
        T target,
        out ActionOrderMove move)
    {
        ArgumentNullException.ThrowIfNull(items);

        var originalIndex = items.IndexOf(source);
        var targetIndex = items.IndexOf(target);
        move = default;

        if (originalIndex < 0 || targetIndex < 0 || originalIndex == targetIndex)
        {
            return false;
        }

        items.RemoveAt(originalIndex);
        var newIndex = Math.Clamp(targetIndex, 0, items.Count);
        items.Insert(newIndex, source);
        move = new ActionOrderMove(originalIndex, newIndex);
        return true;
    }
}
