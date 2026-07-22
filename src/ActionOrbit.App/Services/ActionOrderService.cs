namespace ActionOrbit.App.Services;

internal readonly record struct ActionOrderMove(int OriginalIndex, int NewIndex);

internal static class ActionOrderService
{
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
