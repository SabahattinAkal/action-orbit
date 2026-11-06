namespace ActionOrbit.App.Services;

public enum EditorLayoutMode
{
    Wide,
    Compact
}

internal static class EditorLayoutPolicy
{
    internal const double CompactThreshold = 1080;

    public static EditorLayoutMode Resolve(double availableWidth, bool showProfiles = true, bool showDetails = true) =>
        double.IsFinite(availableWidth) && availableWidth >=
            (showProfiles && showDetails ? CompactThreshold : showProfiles || showDetails ? 720 : 0)
            ? EditorLayoutMode.Wide
            : EditorLayoutMode.Compact;

    public static bool ShouldScrollWorkspace(EditorLayoutMode mode) =>
        mode == EditorLayoutMode.Compact;
}
