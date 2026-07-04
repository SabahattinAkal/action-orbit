namespace ActionOrbit.App.Services;

public sealed record IconOption(string Key, IReadOnlyList<string> Paths, string? ImagePath = null)
{
    public string Label => Key.Replace("-", " ");
    public bool HasPaths => Paths.Count > 0;
    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);
}

public static class IconCatalog
{
    private static readonly Dictionary<string, string[]> Icons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["apps"] =
        [
            "M4 5a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v4a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1l0 -4",
            "M4 15a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v4a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1l0 -4",
            "M14 15a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v4a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1l0 -4",
            "M14 7l6 0",
            "M17 4l0 6",
        ],
        ["browser"] =
        [
            "M4 8h16",
            "M4 6a2 2 0 0 1 2 -2h12a2 2 0 0 1 2 2v12a2 2 0 0 1 -2 2h-12a2 2 0 0 1 -2 -2l0 -12",
            "M8 4v4",
        ],
        ["world-www"] =
        [
            "M19.5 7a9 9 0 0 0 -7.5 -4a8.991 8.991 0 0 0 -7.484 4",
            "M11.5 3a16.989 16.989 0 0 0 -1.826 4",
            "M12.5 3a16.989 16.989 0 0 1 1.828 4",
            "M19.5 17a9 9 0 0 1 -7.5 4a8.991 8.991 0 0 1 -7.484 -4",
            "M11.5 21a16.989 16.989 0 0 1 -1.826 -4",
            "M12.5 21a16.989 16.989 0 0 0 1.828 -4",
            "M2 10l1 4l1.5 -4l1.5 4l1 -4",
            "M17 10l1 4l1.5 -4l1.5 4l1 -4",
            "M9.5 10l1 4l1.5 -4l1.5 4l1 -4",
        ],
        ["player-play"] =
        [
            "M7 4v16l13 -8l-13 -8",
        ],
        ["player-pause"] =
        [
            "M6 6a1 1 0 0 1 1 -1h2a1 1 0 0 1 1 1v12a1 1 0 0 1 -1 1h-2a1 1 0 0 1 -1 -1l0 -12",
            "M14 6a1 1 0 0 1 1 -1h2a1 1 0 0 1 1 1v12a1 1 0 0 1 -1 1h-2a1 1 0 0 1 -1 -1l0 -12",
        ],
        ["player-stop"] =
        [
            "M5 7a2 2 0 0 1 2 -2h10a2 2 0 0 1 2 2v10a2 2 0 0 1 -2 2h-10a2 2 0 0 1 -2 -2l0 -10",
        ],
        ["player-skip-forward"] =
        [
            "M4 5v14l12 -7l-12 -7",
            "M20 5l0 14",
        ],
        ["player-skip-back"] =
        [
            "M20 5v14l-12 -7l12 -7",
            "M4 5l0 14",
        ],
        ["plus"] =
        [
            "M12 5l0 14",
            "M5 12l14 0",
        ],
        ["x"] =
        [
            "M18 6l-12 12",
            "M6 6l12 12",
        ],
        ["rotate-clockwise"] =
        [
            "M4.05 11a8 8 0 1 1 .5 4m-.5 5v-5h5",
        ],
        ["sparkles"] =
        [
            "M16 18a2 2 0 0 1 2 2a2 2 0 0 1 2 -2a2 2 0 0 1 -2 -2a2 2 0 0 1 -2 2m0 -12a2 2 0 0 1 2 2a2 2 0 0 1 2 -2a2 2 0 0 1 -2 -2a2 2 0 0 1 -2 2m-7 12a6 6 0 0 1 6 -6a6 6 0 0 1 -6 -6a6 6 0 0 1 -6 6a6 6 0 0 1 6 6",
        ],
        ["copy"] =
        [
            "M7 9.667a2.667 2.667 0 0 1 2.667 -2.667h8.666a2.667 2.667 0 0 1 2.667 2.667v8.666a2.667 2.667 0 0 1 -2.667 2.667h-8.666a2.667 2.667 0 0 1 -2.667 -2.667l0 -8.666",
            "M4.012 16.737a2.005 2.005 0 0 1 -1.012 -1.737v-10c0 -1.1 .9 -2 2 -2h10c.75 0 1.158 .385 1.5 1",
        ],
        ["clipboard"] =
        [
            "M9 5h-2a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2 -2v-12a2 2 0 0 0 -2 -2h-2",
            "M9 5a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2a2 2 0 0 1 -2 2h-2a2 2 0 0 1 -2 -2",
        ],
        ["clipboard-copy"] =
        [
            "M9 5h-2a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h3m9 -9v-5a2 2 0 0 0 -2 -2h-2",
            "M13 17v-1a1 1 0 0 1 1 -1h1m3 0h1a1 1 0 0 1 1 1v1m0 3v1a1 1 0 0 1 -1 1h-1m-3 0h-1a1 1 0 0 1 -1 -1v-1",
            "M9 5a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2a2 2 0 0 1 -2 2h-2a2 2 0 0 1 -2 -2",
        ],
        ["clipboard-check"] =
        [
            "M9 5h-2a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2 -2v-12a2 2 0 0 0 -2 -2h-2",
            "M9 5a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2a2 2 0 0 1 -2 2h-2a2 2 0 0 1 -2 -2",
            "M9 14l2 2l4 -4",
        ],
        ["clipboard-x"] =
        [
            "M9 5h-2a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2 -2v-12a2 2 0 0 0 -2 -2h-2",
            "M9 5a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2a2 2 0 0 1 -2 2h-2a2 2 0 0 1 -2 -2",
            "M10 12l4 4m0 -4l-4 4",
        ],
        ["clipboard-plus"] =
        [
            "M9 5h-2a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2 -2v-12a2 2 0 0 0 -2 -2h-2",
            "M9 5a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2a2 2 0 0 1 -2 2h-2a2 2 0 0 1 -2 -2",
            "M10 14h4",
            "M12 12v4",
        ],
        ["cut"] =
        [
            "M4 17a3 3 0 1 0 6 0a3 3 0 1 0 -6 0",
            "M14 17a3 3 0 1 0 6 0a3 3 0 1 0 -6 0",
            "M9.15 14.85l8.85 -10.85",
            "M6 4l8.85 10.85",
        ],
        ["scissors"] =
        [
            "M3 7a3 3 0 1 0 6 0a3 3 0 1 0 -6 0",
            "M3 17a3 3 0 1 0 6 0a3 3 0 1 0 -6 0",
            "M8.6 8.6l10.4 10.4",
            "M8.6 15.4l10.4 -10.4",
        ],
        ["select-all"] =
        [
            "M8 9a1 1 0 0 1 1 -1h6a1 1 0 0 1 1 1v6a1 1 0 0 1 -1 1h-6a1 1 0 0 1 -1 -1l0 -6",
            "M12 20v.01",
            "M16 20v.01",
            "M8 20v.01",
            "M4 20v.01",
            "M4 16v.01",
            "M4 12v.01",
            "M4 8v.01",
            "M4 4v.01",
            "M8 4v.01",
            "M12 4v.01",
            "M16 4v.01",
            "M20 4v.01",
            "M20 8v.01",
            "M20 12v.01",
            "M20 16v.01",
            "M20 20v.01",
        ],
        ["arrow-back-up"] =
        [
            "M9 14l-4 -4l4 -4",
            "M5 10h11a4 4 0 1 1 0 8h-1",
        ],
        ["arrow-forward-up"] =
        [
            "M15 14l4 -4l-4 -4",
            "M19 10h-11a4 4 0 1 0 0 8h1",
        ],
        ["device-floppy"] =
        [
            "M6 4h10l4 4v10a2 2 0 0 1 -2 2h-12a2 2 0 0 1 -2 -2v-12a2 2 0 0 1 2 -2",
            "M10 14a2 2 0 1 0 4 0a2 2 0 1 0 -4 0",
            "M14 4l0 4l-6 0l0 -4",
        ],
        ["printer"] =
        [
            "M17 17h2a2 2 0 0 0 2 -2v-4a2 2 0 0 0 -2 -2h-14a2 2 0 0 0 -2 2v4a2 2 0 0 0 2 2h2",
            "M17 9v-4a2 2 0 0 0 -2 -2h-6a2 2 0 0 0 -2 2v4",
            "M7 15a2 2 0 0 1 2 -2h6a2 2 0 0 1 2 2v4a2 2 0 0 1 -2 2h-6a2 2 0 0 1 -2 -2l0 -4",
        ],
        ["search"] =
        [
            "M3 10a7 7 0 1 0 14 0a7 7 0 1 0 -14 0",
            "M21 21l-6 -6",
        ],
        ["zoom-in"] =
        [
            "M3 10a7 7 0 1 0 14 0a7 7 0 1 0 -14 0",
            "M7 10l6 0",
            "M10 7l0 6",
            "M21 21l-6 -6",
        ],
        ["zoom-out"] =
        [
            "M3 10a7 7 0 1 0 14 0a7 7 0 1 0 -14 0",
            "M7 10l6 0",
            "M21 21l-6 -6",
        ],
        ["folder"] =
        [
            "M5 4h4l3 3h7a2 2 0 0 1 2 2v8a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-11a2 2 0 0 1 2 -2",
        ],
        ["folder-open"] =
        [
            "M5 19l2.757 -7.351a1 1 0 0 1 .936 -.649h12.307a1 1 0 0 1 .986 1.164l-.996 5.211a2 2 0 0 1 -1.964 1.625h-14.026a2 2 0 0 1 -2 -2v-11a2 2 0 0 1 2 -2h4l3 3h7a2 2 0 0 1 2 2v2",
        ],
        ["folder-plus"] =
        [
            "M12 19h-7a2 2 0 0 1 -2 -2v-11a2 2 0 0 1 2 -2h4l3 3h7a2 2 0 0 1 2 2v3.5",
            "M16 19h6",
            "M19 16v6",
        ],
        ["file"] =
        [
            "M14 3v4a1 1 0 0 0 1 1h4",
            "M17 21h-10a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2h7l5 5v11a2 2 0 0 1 -2 2",
        ],
        ["file-text"] =
        [
            "M14 3v4a1 1 0 0 0 1 1h4",
            "M17 21h-10a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2h7l5 5v11a2 2 0 0 1 -2 2",
            "M9 9l1 0",
            "M9 13l6 0",
            "M9 17l6 0",
        ],
        ["file-plus"] =
        [
            "M14 3v4a1 1 0 0 0 1 1h4",
            "M17 21h-10a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2h7l5 5v11a2 2 0 0 1 -2 2",
            "M12 11l0 6",
            "M9 14l6 0",
        ],
        ["download"] =
        [
            "M4 17v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2 -2v-2",
            "M7 11l5 5l5 -5",
            "M12 4l0 12",
        ],
        ["upload"] =
        [
            "M4 17v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2 -2v-2",
            "M7 9l5 -5l5 5",
            "M12 4l0 12",
        ],
        ["terminal"] =
        [
            "M5 7l5 5l-5 5",
            "M12 19l7 0",
        ],
        ["code"] =
        [
            "M7 8l-4 4l4 4",
            "M17 8l4 4l-4 4",
            "M14 4l-4 16",
        ],
        ["git-branch"] =
        [
            "M5 18a2 2 0 1 0 4 0a2 2 0 1 0 -4 0",
            "M5 6a2 2 0 1 0 4 0a2 2 0 1 0 -4 0",
            "M15 6a2 2 0 1 0 4 0a2 2 0 1 0 -4 0",
            "M7 8l0 8",
            "M9 18h6a2 2 0 0 0 2 -2v-5",
            "M14 14l3 -3l3 3",
        ],
        ["brand-git"] =
        [
            "M15 12a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
            "M11 8a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
            "M11 16a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
            "M12 15v-6",
            "M15 11l-2 -2",
            "M11 7l-1.9 -1.9",
            "M13.446 2.6l7.955 7.954a2.045 2.045 0 0 1 0 2.892l-7.955 7.955a2.045 2.045 0 0 1 -2.892 0l-7.955 -7.955a2.045 2.045 0 0 1 0 -2.892l7.955 -7.955a2.045 2.045 0 0 1 2.892 0",
        ],
        ["brand-github"] =
        [
            "M9 19c-4.3 1.4 -4.3 -2.5 -6 -3m12 5v-3.5c0 -1 .1 -1.4 -.5 -2c2.8 -.3 5.5 -1.4 5.5 -6a4.6 4.6 0 0 0 -1.3 -3.2a4.2 4.2 0 0 0 -.1 -3.2s-1.1 -.3 -3.5 1.3a12.3 12.3 0 0 0 -6.2 0c-2.4 -1.6 -3.5 -1.3 -3.5 -1.3a4.2 4.2 0 0 0 -.1 3.2a4.6 4.6 0 0 0 -1.3 3.2c0 4.6 2.7 5.7 5.5 6c-.6 .6 -.6 1.2 -.5 2v3.5",
        ],
        ["mail"] =
        [
            "M3 7a2 2 0 0 1 2 -2h14a2 2 0 0 1 2 2v10a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-10",
            "M3 7l9 6l9 -6",
        ],
        ["send"] =
        [
            "M10 14l11 -11",
            "M21 3l-6.5 18a.55 .55 0 0 1 -1 0l-3.5 -7l-7 -3.5a.55 .55 0 0 1 0 -1l18 -6.5",
        ],
        ["info-circle"] =
        [
            "M3 12a9 9 0 1 0 18 0a9 9 0 0 0 -18 0",
            "M12 9h.01",
            "M11 12h1v4h1",
        ],
        ["link"] =
        [
            "M9 15l6 -6",
            "M11 6l.463 -.536a5 5 0 0 1 7.071 7.072l-.534 .464",
            "M13 18l-.397 .534a5.068 5.068 0 0 1 -7.127 0a4.972 4.972 0 0 1 0 -7.071l.524 -.463",
        ],
        ["lock"] =
        [
            "M5 13a2 2 0 0 1 2 -2h10a2 2 0 0 1 2 2v6a2 2 0 0 1 -2 2h-10a2 2 0 0 1 -2 -2v-6",
            "M11 16a1 1 0 1 0 2 0a1 1 0 0 0 -2 0",
            "M8 11v-4a4 4 0 1 1 8 0v4",
        ],
        ["key"] =
        [
            "M16.555 3.843l3.602 3.602a2.877 2.877 0 0 1 0 4.069l-2.643 2.643a2.877 2.877 0 0 1 -4.069 0l-.301 -.301l-6.558 6.558a2 2 0 0 1 -1.239 .578l-.175 .008h-1.172a1 1 0 0 1 -.993 -.883l-.007 -.117v-1.172a2 2 0 0 1 .467 -1.284l.119 -.13l.414 -.414h2v-2h2v-2l2.144 -2.144l-.301 -.301a2.877 2.877 0 0 1 0 -4.069l2.643 -2.643a2.877 2.877 0 0 1 4.069 0",
            "M15 9h.01",
        ],
        ["command"] =
        [
            "M7 9a2 2 0 1 1 2 -2v10a2 2 0 1 1 -2 -2h10a2 2 0 1 1 -2 2v-10a2 2 0 1 1 2 2h-10",
        ],
        ["text-size"] =
        [
            "M3 7v-2h13v2",
            "M10 5v14",
            "M12 19h-4",
            "M15 13v-1h6v1",
            "M18 12v7",
            "M17 19h2",
        ],
        ["edit"] =
        [
            "M7 7h-1a2 2 0 0 0 -2 2v9a2 2 0 0 0 2 2h9a2 2 0 0 0 2 -2v-1",
            "M20.385 6.585a2.1 2.1 0 0 0 -2.97 -2.97l-8.415 8.385v3h3l8.385 -8.415",
            "M16 5l3 3",
        ],
        ["pencil"] =
        [
            "M4 20h4l10.5 -10.5a2.828 2.828 0 1 0 -4 -4l-10.5 10.5v4",
            "M13.5 6.5l4 4",
        ],
        ["trash"] =
        [
            "M4 7l16 0",
            "M10 11l0 6",
            "M14 11l0 6",
            "M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12",
            "M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3",
        ],
        ["archive"] =
        [
            "M3 6a2 2 0 0 1 2 -2h14a2 2 0 0 1 2 2a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2",
            "M5 8v10a2 2 0 0 0 2 2h10a2 2 0 0 0 2 -2v-10",
            "M10 12l4 0",
        ],
        ["settings"] =
        [
            "M10.325 4.317c.426 -1.756 2.924 -1.756 3.35 0a1.724 1.724 0 0 0 2.573 1.066c1.543 -.94 3.31 .826 2.37 2.37a1.724 1.724 0 0 0 1.065 2.572c1.756 .426 1.756 2.924 0 3.35a1.724 1.724 0 0 0 -1.066 2.573c.94 1.543 -.826 3.31 -2.37 2.37a1.724 1.724 0 0 0 -2.572 1.065c-.426 1.756 -2.924 1.756 -3.35 0a1.724 1.724 0 0 0 -2.573 -1.066c-1.543 .94 -3.31 -.826 -2.37 -2.37a1.724 1.724 0 0 0 -1.065 -2.572c-1.756 -.426 -1.756 -2.924 0 -3.35a1.724 1.724 0 0 0 1.066 -2.573c-.94 -1.543 .826 -3.31 2.37 -2.37c1 .608 2.296 .07 2.572 -1.065",
            "M9 12a3 3 0 1 0 6 0a3 3 0 0 0 -6 0",
        ],
        ["adjustments-horizontal"] =
        [
            "M12 6a2 2 0 1 0 4 0a2 2 0 1 0 -4 0",
            "M4 6l8 0",
            "M16 6l4 0",
            "M6 12a2 2 0 1 0 4 0a2 2 0 1 0 -4 0",
            "M4 12l2 0",
            "M10 12l10 0",
            "M15 18a2 2 0 1 0 4 0a2 2 0 1 0 -4 0",
            "M4 18l11 0",
            "M19 18l1 0",
        ],
        ["camera"] =
        [
            "M5 7h1a2 2 0 0 0 2 -2a1 1 0 0 1 1 -1h6a1 1 0 0 1 1 1a2 2 0 0 0 2 2h1a2 2 0 0 1 2 2v9a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-9a2 2 0 0 1 2 -2",
            "M9 13a3 3 0 1 0 6 0a3 3 0 0 0 -6 0",
        ],
        ["photo"] =
        [
            "M15 8h.01",
            "M3 6a3 3 0 0 1 3 -3h12a3 3 0 0 1 3 3v12a3 3 0 0 1 -3 3h-12a3 3 0 0 1 -3 -3v-12",
            "M3 16l5 -5c.928 -.893 2.072 -.893 3 0l5 5",
            "M14 14l1 -1c.928 -.893 2.072 -.893 3 0l3 3",
        ],
        ["video"] =
        [
            "M15 10l4.553 -2.276a1 1 0 0 1 1.447 .894v6.764a1 1 0 0 1 -1.447 .894l-4.553 -2.276v-4",
            "M3 8a2 2 0 0 1 2 -2h8a2 2 0 0 1 2 2v8a2 2 0 0 1 -2 2h-8a2 2 0 0 1 -2 -2l0 -8",
        ],
        ["screen-share"] =
        [
            "M21 12v3a1 1 0 0 1 -1 1h-16a1 1 0 0 1 -1 -1v-10a1 1 0 0 1 1 -1h9",
            "M7 20l10 0",
            "M9 16l0 4",
            "M15 16l0 4",
            "M17 4h4v4",
            "M16 9l5 -5",
        ],
        ["device-desktop"] =
        [
            "M3 5a1 1 0 0 1 1 -1h16a1 1 0 0 1 1 1v10a1 1 0 0 1 -1 1h-16a1 1 0 0 1 -1 -1v-10",
            "M7 20h10",
            "M9 16v4",
            "M15 16v4",
        ],
        ["device-laptop"] =
        [
            "M3 19l18 0",
            "M5 7a1 1 0 0 1 1 -1h12a1 1 0 0 1 1 1v8a1 1 0 0 1 -1 1h-12a1 1 0 0 1 -1 -1l0 -8",
        ],
        ["window"] =
        [
            "M12 3c-3.866 0 -7 3.272 -7 7v10a1 1 0 0 0 1 1h12a1 1 0 0 0 1 -1v-10c0 -3.728 -3.134 -7 -7 -7",
            "M5 13l14 0",
            "M12 3l0 18",
        ],
        ["layout-dashboard"] =
        [
            "M5 4h4a1 1 0 0 1 1 1v6a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1v-6a1 1 0 0 1 1 -1",
            "M5 16h4a1 1 0 0 1 1 1v2a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1v-2a1 1 0 0 1 1 -1",
            "M15 12h4a1 1 0 0 1 1 1v6a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1v-6a1 1 0 0 1 1 -1",
            "M15 4h4a1 1 0 0 1 1 1v2a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1v-2a1 1 0 0 1 1 -1",
        ],
        ["calculator"] =
        [
            "M4 5a2 2 0 0 1 2 -2h12a2 2 0 0 1 2 2v14a2 2 0 0 1 -2 2h-12a2 2 0 0 1 -2 -2l0 -14",
            "M8 8a1 1 0 0 1 1 -1h6a1 1 0 0 1 1 1v1a1 1 0 0 1 -1 1h-6a1 1 0 0 1 -1 -1l0 -1",
            "M8 14l0 .01",
            "M12 14l0 .01",
            "M16 14l0 .01",
            "M8 17l0 .01",
            "M12 17l0 .01",
            "M16 17l0 .01",
        ],
        ["notes"] =
        [
            "M5 5a2 2 0 0 1 2 -2h10a2 2 0 0 1 2 2v14a2 2 0 0 1 -2 2h-10a2 2 0 0 1 -2 -2l0 -14",
            "M9 7l6 0",
            "M9 11l6 0",
            "M9 15l4 0",
        ],
        ["calendar"] =
        [
            "M4 7a2 2 0 0 1 2 -2h12a2 2 0 0 1 2 2v12a2 2 0 0 1 -2 2h-12a2 2 0 0 1 -2 -2v-12",
            "M16 3v4",
            "M8 3v4",
            "M4 11h16",
            "M11 15h1",
            "M12 15v3",
        ],
        ["clock"] =
        [
            "M3 12a9 9 0 1 0 18 0a9 9 0 0 0 -18 0",
            "M12 7v5l3 3",
        ],
        ["bell"] =
        [
            "M10 5a2 2 0 1 1 4 0a7 7 0 0 1 4 6v3a4 4 0 0 0 2 3h-16a4 4 0 0 0 2 -3v-3a7 7 0 0 1 4 -6",
            "M9 17v1a3 3 0 0 0 6 0v-1",
        ],
        ["star"] =
        [
            "M12 17.75l-6.172 3.245l1.179 -6.873l-5 -4.867l6.9 -1l3.086 -6.253l3.086 6.253l6.9 1l-5 4.867l1.179 6.873l-6.158 -3.245",
        ],
        ["heart"] =
        [
            "M19.5 12.572l-7.5 7.428l-7.5 -7.428a5 5 0 1 1 7.5 -6.566a5 5 0 1 1 7.5 6.572",
        ],
        ["bookmark"] =
        [
            "M18 7v14l-6 -4l-6 4v-14a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4",
        ],
        ["home"] =
        [
            "M5 12l-2 0l9 -9l9 9l-2 0",
            "M5 12v7a2 2 0 0 0 2 2h10a2 2 0 0 0 2 -2v-7",
            "M9 21v-6a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2v6",
        ],
        ["user"] =
        [
            "M8 7a4 4 0 1 0 8 0a4 4 0 0 0 -8 0",
            "M6 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2",
        ],
        ["users"] =
        [
            "M5 7a4 4 0 1 0 8 0a4 4 0 1 0 -8 0",
            "M3 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2",
            "M16 3.13a4 4 0 0 1 0 7.75",
            "M21 21v-2a4 4 0 0 0 -3 -3.85",
        ],
        ["message"] =
        [
            "M8 9h8",
            "M8 13h6",
            "M18 4a3 3 0 0 1 3 3v8a3 3 0 0 1 -3 3h-5l-5 3v-3h-2a3 3 0 0 1 -3 -3v-8a3 3 0 0 1 3 -3h12",
        ],
        ["messages"] =
        [
            "M21 14l-3 -3h-7a1 1 0 0 1 -1 -1v-6a1 1 0 0 1 1 -1h9a1 1 0 0 1 1 1v10",
            "M14 15v2a1 1 0 0 1 -1 1h-7l-3 3v-10a1 1 0 0 1 1 -1h2",
        ],
        ["microphone"] =
        [
            "M9 5a3 3 0 0 1 3 -3a3 3 0 0 1 3 3v5a3 3 0 0 1 -3 3a3 3 0 0 1 -3 -3l0 -5",
            "M5 10a7 7 0 0 0 14 0",
            "M8 21l8 0",
            "M12 17l0 4",
        ],
        ["volume"] =
        [
            "M15 8a5 5 0 0 1 0 8",
            "M17.7 5a9 9 0 0 1 0 14",
            "M6 15h-2a1 1 0 0 1 -1 -1v-4a1 1 0 0 1 1 -1h2l3.5 -4.5a.8 .8 0 0 1 1.5 .5v14a.8 .8 0 0 1 -1.5 .5l-3.5 -4.5",
        ],
        ["volume-2"] =
        [
            "M15 8a5 5 0 0 1 0 8",
            "M6 15h-2a1 1 0 0 1 -1 -1v-4a1 1 0 0 1 1 -1h2l3.5 -4.5a.8 .8 0 0 1 1.5 .5v14a.8 .8 0 0 1 -1.5 .5l-3.5 -4.5",
        ],
        ["music"] =
        [
            "M3 17a3 3 0 1 0 6 0a3 3 0 0 0 -6 0",
            "M13 17a3 3 0 1 0 6 0a3 3 0 0 0 -6 0",
            "M9 17v-13h10v13",
            "M9 8h10",
        ],
        ["brand-youtube"] =
        [
            "M2 8a4 4 0 0 1 4 -4h12a4 4 0 0 1 4 4v8a4 4 0 0 1 -4 4h-12a4 4 0 0 1 -4 -4v-8",
            "M10 9l5 3l-5 3l0 -6",
        ],
        ["brand-chrome"] =
        [
            "M3 12a9 9 0 1 0 18 0a9 9 0 1 0 -18 0",
            "M9 12a3 3 0 1 0 6 0a3 3 0 1 0 -6 0",
            "M12 9h8.4",
            "M14.598 13.5l-4.2 7.275",
            "M9.402 13.5l-4.2 -7.275",
        ],
        ["brand-openai"] =
        [
            "M11.217 19.384a3.501 3.501 0 0 0 6.783 -1.217v-5.167l-6 -3.35",
            "M5.214 15.014a3.501 3.501 0 0 0 4.446 5.266l4.34 -2.534v-6.946",
            "M6 7.63c-1.391 -.236 -2.787 .395 -3.534 1.689a3.474 3.474 0 0 0 1.271 4.745l4.263 2.514l6 -3.348",
            "M12.783 4.616a3.501 3.501 0 0 0 -6.783 1.217v5.067l6 3.45",
            "M18.786 8.986a3.501 3.501 0 0 0 -4.446 -5.266l-4.34 2.534v6.946",
            "M18 16.302c1.391 .236 2.787 -.395 3.534 -1.689a3.474 3.474 0 0 0 -1.271 -4.745l-4.308 -2.514l-5.955 3.42",
        ],
        ["brand-google"] =
        [
            "M20.945 11a9 9 0 1 1 -3.284 -5.997l-2.655 2.392a5.5 5.5 0 1 0 2.119 6.605h-4.125v-3h7.945",
        ],
        ["brand-gmail"] =
        [
            "M16 20h3a1 1 0 0 0 1 -1v-14a1 1 0 0 0 -1 -1h-3v16",
            "M5 20h3v-16h-3a1 1 0 0 0 -1 1v14a1 1 0 0 0 1 1",
            "M16 4l-4 4l-4 -4",
            "M4 6.5l8 7.5l8 -7.5",
        ],
        ["brand-windows"] =
        [
            "M17.8 20l-12 -1.5c-1 -.1 -1.8 -.9 -1.8 -1.9v-9.2c0 -1 .8 -1.8 1.8 -1.9l12 -1.5c1.2 -.1 2.2 .8 2.2 1.9v12.1c0 1.2 -1.1 2.1 -2.2 1.9l0 .1",
            "M12 5l0 14",
            "M4 12l16 0",
        ],
        ["brand-visual-studio"] =
        [
            "M4 8l2 -1l10 13l4 -2v-12l-4 -2l-10 13l-2 -1l0 -8",
        ],
        ["palette"] =
        [
            "M12 21a9 9 0 0 1 0 -18c4.97 0 9 3.582 9 8c0 1.06 -.474 2.078 -1.318 2.828c-.844 .75 -1.989 1.172 -3.182 1.172h-2.5a2 2 0 0 0 -1 3.75a1.3 1.3 0 0 1 -1 2.25",
            "M7.5 10.5a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
            "M11.5 7.5a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
            "M15.5 10.5a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
        ],
        ["color-picker"] =
        [
            "M11 7l6 6",
            "M4 16l11.7 -11.7a1 1 0 0 1 1.4 0l2.6 2.6a1 1 0 0 1 0 1.4l-11.7 11.7h-4v-4",
        ],
        ["crop"] =
        [
            "M8 5v10a1 1 0 0 0 1 1h10",
            "M5 8h10a1 1 0 0 1 1 1v10",
        ],
        ["scan"] =
        [
            "M5 12h14",
            "M3 7v-2a2 2 0 0 1 2 -2h2",
            "M3 17v2a2 2 0 0 0 2 2h2",
            "M17 3h2a2 2 0 0 1 2 2v2",
            "M17 21h2a2 2 0 0 0 2 -2v-2",
        ],
        ["screenshot"] =
        [
            "M7 19a2 2 0 0 1 -2 -2",
            "M5 13v-2",
            "M5 7a2 2 0 0 1 2 -2",
            "M11 5h2",
            "M17 5a2 2 0 0 1 2 2",
            "M19 11v2",
            "M19 17v4",
            "M21 19h-4",
            "M13 19h-2",
        ],
        ["language"] =
        [
            "M9 6.371c0 4.418 -2.239 6.629 -5 6.629",
            "M4 6.371h7",
            "M5 9c0 2.144 2.252 3.908 6 4",
            "M12 20l4 -9l4 9",
            "M19.1 18h-6.2",
            "M6.694 3l.793 .582",
        ],
        ["regex"] =
        [
            "M6.5 15a2.5 2.5 0 1 1 0 5a2.5 2.5 0 0 1 0 -5",
            "M17 7.875l3 -1.687",
            "M17 7.875v3.375",
            "M17 7.875l-3 -1.687",
            "M17 7.875l3 1.688",
            "M17 4.5v3.375",
            "M17 7.875l-3 1.688",
        ],
        ["hash"] =
        [
            "M5 9l14 0",
            "M5 15l14 0",
            "M11 4l-4 16",
            "M17 4l-4 16",
        ],
        ["number"] =
        [
            "M4 17v-10l7 10v-10",
            "M15 17h5",
            "M15 10a2.5 3 0 1 0 5 0a2.5 3 0 1 0 -5 0",
        ],
        ["list"] =
        [
            "M9 6l11 0",
            "M9 12l11 0",
            "M9 18l11 0",
            "M5 6l0 .01",
            "M5 12l0 .01",
            "M5 18l0 .01",
        ],
        ["chevron-up"] =
        [
            "M6 15l6 -6l6 6",
        ],
        ["chevron-down"] =
        [
            "M6 9l6 6l6 -6",
        ],
        ["chevron-left"] =
        [
            "M15 6l-6 6l6 6",
        ],
        ["chevron-right"] =
        [
            "M9 6l6 6l-6 6",
        ],
        ["external-link"] =
        [
            "M12 6h-6a2 2 0 0 0 -2 2v10a2 2 0 0 0 2 2h10a2 2 0 0 0 2 -2v-6",
            "M11 13l9 -9",
            "M15 4h5v5",
        ],
        ["arrow-up"] =
        [
            "M12 5l0 14",
            "M18 11l-6 -6",
            "M6 11l6 -6",
        ],
        ["arrow-down"] =
        [
            "M12 5l0 14",
            "M18 13l-6 6",
            "M6 13l6 6",
        ],
        ["arrow-left"] =
        [
            "M5 12l14 0",
            "M5 12l6 6",
            "M5 12l6 -6",
        ],
        ["arrow-right"] =
        [
            "M5 12l14 0",
            "M13 18l6 -6",
            "M13 6l6 6",
        ],
        ["refresh"] =
        [
            "M20 11a8.1 8.1 0 0 0 -15.5 -2m-.5 -4v4h4",
            "M4 13a8.1 8.1 0 0 0 15.5 2m.5 4v-4h-4",
        ],
        ["reload"] =
        [
            "M19.933 13.041a8 8 0 1 1 -9.925 -8.788c3.899 -1 7.935 1.007 9.425 4.747",
            "M20 4v5h-5",
        ],
        ["bolt"] =
        [
            "M13 3l0 7l6 0l-8 11l0 -7l-6 0l8 -11",
        ],
        ["wand"] =
        [
            "M6 21l15 -15l-3 -3l-15 15l3 3",
            "M15 6l3 3",
            "M9 3a2 2 0 0 0 2 2a2 2 0 0 0 -2 2a2 2 0 0 0 -2 -2a2 2 0 0 0 2 -2",
            "M19 13a2 2 0 0 0 2 2a2 2 0 0 0 -2 2a2 2 0 0 0 -2 -2a2 2 0 0 0 2 -2",
        ],
        ["robot"] =
        [
            "M6 6a2 2 0 0 1 2 -2h8a2 2 0 0 1 2 2v4a2 2 0 0 1 -2 2h-8a2 2 0 0 1 -2 -2l0 -4",
            "M12 2v2",
            "M9 12v9",
            "M15 12v9",
            "M5 16l4 -2",
            "M15 14l4 2",
            "M9 18h6",
            "M10 8v.01",
            "M14 8v.01",
        ],
        ["brain"] =
        [
            "M15.5 13a3.5 3.5 0 0 0 -3.5 3.5v1a3.5 3.5 0 0 0 7 0v-1.8",
            "M8.5 13a3.5 3.5 0 0 1 3.5 3.5v1a3.5 3.5 0 0 1 -7 0v-1.8",
            "M17.5 16a3.5 3.5 0 0 0 0 -7h-.5",
            "M19 9.3v-2.8a3.5 3.5 0 0 0 -7 0",
            "M6.5 16a3.5 3.5 0 0 1 0 -7h.5",
            "M5 9.3v-2.8a3.5 3.5 0 0 1 7 0v10",
        ],
        ["cpu"] =
        [
            "M5 6a1 1 0 0 1 1 -1h12a1 1 0 0 1 1 1v12a1 1 0 0 1 -1 1h-12a1 1 0 0 1 -1 -1l0 -12",
            "M9 9h6v6h-6l0 -6",
            "M3 10h2",
            "M3 14h2",
            "M10 3v2",
            "M14 3v2",
            "M21 10h-2",
            "M21 14h-2",
            "M14 21v-2",
            "M10 21v-2",
        ],
        ["database"] =
        [
            "M4 6a8 3 0 1 0 16 0a8 3 0 1 0 -16 0",
            "M4 6v6a8 3 0 0 0 16 0v-6",
            "M4 12v6a8 3 0 0 0 16 0v-6",
        ],
        ["cloud"] =
        [
            "M6.657 18c-2.572 0 -4.657 -2.007 -4.657 -4.483c0 -2.475 2.085 -4.482 4.657 -4.482c.393 -1.762 1.794 -3.2 3.675 -3.773c1.88 -.572 3.956 -.193 5.444 1c1.488 1.19 2.162 3.007 1.77 4.769h.99c1.913 0 3.464 1.56 3.464 3.486c0 1.927 -1.551 3.487 -3.465 3.487h-11.878",
        ],
        ["wifi"] =
        [
            "M12 18l.01 0",
            "M9.172 15.172a4 4 0 0 1 5.656 0",
            "M6.343 12.343a8 8 0 0 1 11.314 0",
            "M3.515 9.515c4.686 -4.687 12.284 -4.687 17 0",
        ],
        ["wifi-off"] =
        [
            "M12 18l.01 0",
            "M9.172 15.172a4 4 0 0 1 5.656 0",
            "M6.343 12.343a7.963 7.963 0 0 1 3.864 -2.14m4.163 .155a7.965 7.965 0 0 1 3.287 2",
            "M3.515 9.515a12 12 0 0 1 3.544 -2.455m3.101 -.92a12 12 0 0 1 10.325 3.374",
            "M3 3l18 18",
        ],
        ["plug"] =
        [
            "M9.785 6l8.215 8.215l-2.054 2.054a5.81 5.81 0 1 1 -8.215 -8.215l2.054 -2.054",
            "M4 20l3.5 -3.5",
            "M15 4l-3.5 3.5",
            "M20 9l-3.5 3.5",
        ],
        ["power"] =
        [
            "M7 6a7.75 7.75 0 1 0 10 0",
            "M12 4l0 8",
        ],
        ["logout"] =
        [
            "M14 8v-2a2 2 0 0 0 -2 -2h-7a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h7a2 2 0 0 0 2 -2v-2",
            "M9 12h12l-3 -3",
            "M18 15l3 -3",
        ],
        ["login"] =
        [
            "M15 8v-2a2 2 0 0 0 -2 -2h-7a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h7a2 2 0 0 0 2 -2v-2",
            "M21 12h-13l3 -3",
            "M11 15l-3 -3",
        ],
    };

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["app"] = "apps",
        ["play"] = "player-play",
        ["pause"] = "player-pause",
        ["stop"] = "player-stop",
        ["rotate"] = "rotate-clockwise",
        ["paste"] = "clipboard",
        ["save"] = "device-floppy",
        ["undo"] = "arrow-back-up",
        ["redo"] = "arrow-forward-up",
        ["select"] = "select-all",
        ["desktop"] = "device-desktop",
        ["laptop"] = "device-laptop",
        ["git"] = "brand-git",
        ["github"] = "brand-github",
        ["gmail"] = "brand-gmail",
        ["youtube"] = "brand-youtube",
        ["chrome"] = "brand-chrome",
        ["openai"] = "brand-openai",
        ["google"] = "brand-google",
        ["windows"] = "brand-windows",
        ["vscode"] = "brand-visual-studio",
        ["info"] = "info-circle",
        ["text"] = "text-size",
        ["sliders"] = "adjustments-horizontal",
        ["dashboard"] = "layout-dashboard",
        ["web"] = "world-www",
        ["url"] = "world-www",
        ["hotkey"] = "command",
        ["shortcut"] = "command",
        ["ai"] = "sparkles",
        ["magic"] = "wand",
        ["folder-add"] = "folder-plus",
    };

    private static string? _customIconDirectory;

    public static IReadOnlyList<string> AvailableKeys => GetAvailableIcons()
        .Select(icon => icon.Key)
        .ToList();

    public static IReadOnlyList<IconOption> AvailableIcons => GetAvailableIcons();

    public static void ConfigureCustomIconDirectory(string directory)
    {
        _customIconDirectory = directory;
        Directory.CreateDirectory(directory);
    }

    public static IReadOnlyList<IconOption> GetAvailableIcons()
    {
        var builtInIcons = Icons.Keys
            .Concat(Aliases.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key)
            .Select(key => new IconOption(key, GetPaths(key)))
            .Where(icon => icon.Paths.Count > 0);

        return builtInIcons
            .Concat(GetCustomIcons())
            .ToList();
    }

    public static string? GetImagePath(string? key)
    {
        var filePath = ResolveCustomFilePath(key);
        if (filePath is null || IsSvg(filePath))
        {
            return null;
        }

        return filePath;
    }

    public static IReadOnlyList<string> GetPaths(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return [];
        }

        var customFilePath = ResolveCustomFilePath(key);
        if (customFilePath is not null)
        {
            return IsSvg(customFilePath)
                ? ReadSvgPaths(customFilePath)
                : [];
        }

        var normalized = Normalize(key);
        if (Aliases.TryGetValue(normalized, out var alias))
        {
            normalized = alias;
        }

        return Icons.TryGetValue(normalized, out var paths)
            ? paths
            : [];
    }

    public static bool HasIcon(string? key) => GetPaths(key).Count > 0 || GetImagePath(key) is not null;

    private static IReadOnlyList<IconOption> GetCustomIcons()
    {
        if (string.IsNullOrWhiteSpace(_customIconDirectory) || !Directory.Exists(_customIconDirectory))
        {
            return [];
        }

        return Directory.EnumerateFiles(_customIconDirectory)
            .Where(IsSupportedCustomIcon)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(filePath =>
            {
                var key = ToCustomKey(Path.GetFileName(filePath));
                return IsSvg(filePath)
                    ? new IconOption(key, ReadSvgPaths(filePath))
                    : new IconOption(key, [], filePath);
            })
            .Where(icon => icon.HasImage || icon.HasPaths)
            .ToList();
    }

    private static string? ResolveCustomFilePath(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (File.Exists(key))
        {
            return key;
        }

        if (string.IsNullOrWhiteSpace(_customIconDirectory) ||
            !key.StartsWith("custom:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var fileName = key["custom:".Length..];
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        var path = Path.Combine(_customIconDirectory, fileName);
        return File.Exists(path) ? path : null;
    }

    private static IReadOnlyList<string> ReadSvgPaths(string path)
    {
        try
        {
            var svg = File.ReadAllText(path);
            var matches = System.Text.RegularExpressions.Regex.Matches(
                svg,
                "<path\\b[^>]*\\bd\\s*=\\s*['\"]([^'\"]+)['\"][^>]*>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return matches
                .Select(match => match.Groups[1].Value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Where(value => !string.Equals(value, "M0 0h24v24H0z", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static bool IsSupportedCustomIcon(string path) =>
        IsSvg(path) || IsRaster(path);

    private static bool IsSvg(string path) =>
        string.Equals(Path.GetExtension(path), ".svg", StringComparison.OrdinalIgnoreCase);

    private static bool IsRaster(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToCustomKey(string fileName) =>
        $"custom:{fileName}";

    private static string Normalize(string key) =>
        key.Trim()
            .ToLowerInvariant()
            .Replace("_", "-", StringComparison.Ordinal)
            .Replace(" ", "-", StringComparison.Ordinal);
}
