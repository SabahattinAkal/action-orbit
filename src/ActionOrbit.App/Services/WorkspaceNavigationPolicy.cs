using System.Diagnostics.CodeAnalysis;

namespace ActionOrbit.App.Services;

internal static class WorkspaceNavigationPolicy
{
    public static bool IsSupported([NotNullWhen(true)] string? workspace) =>
        workspace is "home" or "editor" or "library" or "settings";
}
