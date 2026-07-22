namespace ActionOrbit.App.Services;

public enum EditorLayoutMode
{
    Wide,
    Compact
}

internal static class EditorLayoutPolicy
{
    internal const double CompactThreshold = 1080;
    internal const double WideContentMinHeight = 900;

    public static EditorLayoutMode Resolve(double availableWidth) =>
        double.IsFinite(availableWidth) && availableWidth >= CompactThreshold
            ? EditorLayoutMode.Wide
            : EditorLayoutMode.Compact;
}
