namespace ActionOrbit.App.Services;

public enum EditorLayoutMode
{
    Wide,
    Compact
}

internal static class EditorLayoutPolicy
{
    internal const double CompactThreshold = 1080;

    public static EditorLayoutMode Resolve(double availableWidth) =>
        double.IsFinite(availableWidth) && availableWidth >= CompactThreshold
            ? EditorLayoutMode.Wide
            : EditorLayoutMode.Compact;

    public static bool ShouldScrollWorkspace(EditorLayoutMode mode) =>
        mode == EditorLayoutMode.Compact;
}
