namespace ActionOrbit.App.Services;

internal sealed class ActiveProfileResolutionCache
{
    private long _revision;
    private long _resolvedRevision = -1;
    private string _resolvedProcessName = "";

    public bool RequiresResolution(string processName) =>
        _revision != _resolvedRevision
        || !string.Equals(
            _resolvedProcessName,
            processName,
            StringComparison.OrdinalIgnoreCase);

    public void RecordResolution(string processName)
    {
        _resolvedProcessName = processName;
        _resolvedRevision = _revision;
    }

    public void Invalidate() => _revision++;
}
