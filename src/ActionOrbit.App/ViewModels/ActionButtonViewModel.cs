using System.Windows.Input;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.ViewModels;

public sealed class ActionButtonViewModel
{
    public required OrbitAction Action { get; init; }
    public required ICommand Command { get; init; }
    public string Title => Action.Title;
    public string Icon => string.IsNullOrWhiteSpace(Action.Icon)
        ? Title[..Math.Min(1, Title.Length)].ToUpperInvariant()
        : Action.Icon;
    public string? IconImagePath => IconCatalog.GetImagePath(Icon);
    public bool HasIconImage => IconImagePath is not null;
    public IReadOnlyList<string> IconPaths => IconCatalog.GetPaths(Icon);
    public bool HasIconPaths => !HasIconImage && IconPaths.Count > 0;
    public bool HasFallbackIcon => !HasIconImage && !HasIconPaths;
    public string Type => Action.Type;
    public bool IsFolder => Action.IsFolder;
    public bool IsSatellite { get; init; }
    public bool IsActiveFolder { get; init; }
    public bool ShowFolderLob => IsFolder && !IsActiveFolder;
    public double FolderLobWidth => Diameter * 0.64;
    public double FolderLobHeight => Diameter * 0.64;
    public double FolderLobX => Diameter / 2 - FolderLobWidth / 2 + Math.Cos(AngleRadians) * Diameter * 0.26;
    public double FolderLobY => Diameter / 2 - FolderLobHeight / 2 + Math.Sin(AngleRadians) * Diameter * 0.26;
    public double FolderLobAngle => AngleDegrees;
    public string EffectiveIcon => IsActiveFolder ? "x" : Icon;
    public string? EffectiveIconImagePath => IconCatalog.GetImagePath(EffectiveIcon);
    public bool HasEffectiveIconImage => EffectiveIconImagePath is not null;
    public IReadOnlyList<string> EffectiveIconPaths => IconCatalog.GetPaths(EffectiveIcon);
    public bool HasEffectiveIconPaths => !HasEffectiveIconImage && EffectiveIconPaths.Count > 0;
    public bool HasEffectiveFallbackIcon => !HasEffectiveIconImage && !HasEffectiveIconPaths;
    public double X { get; init; }
    public double Y { get; init; }
    public double CenterX { get; init; }
    public double CenterY { get; init; }
    public double Diameter { get; init; }
    public double IconFontSize { get; init; }
    public double AngleDegrees { get; init; }
    public string ToolTipText => string.Equals(Type, "overflow", StringComparison.OrdinalIgnoreCase)
        ? Title
        : $"{Title} - {Type}";
    private double AngleRadians => AngleDegrees * Math.PI / 180.0;
}
