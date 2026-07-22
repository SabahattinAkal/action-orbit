using ActionOrbit.App.Services;

namespace ActionOrbit.App.ViewModels;

public sealed class RingPreviewSlotViewModel : ViewModelBase
{
    private bool _isSelected;

    public RingPreviewSlotViewModel(
        ActionEditorRowViewModel? actionRow,
        string title,
        string icon,
        double left,
        double top,
        bool isOverflow = false)
    {
        ActionRow = actionRow;
        Title = title;
        Icon = icon;
        Left = left;
        Top = top;
        IsOverflow = isOverflow;
    }

    public ActionEditorRowViewModel? ActionRow { get; }
    public string Title { get; }
    public string Icon { get; }
    public double Left { get; }
    public double Top { get; }
    public bool IsOverflow { get; }
    public string? ImagePath => IconCatalog.GetImagePath(Icon);
    public bool HasImage => ImagePath is not null;
    public IReadOnlyList<string> IconPaths => IconCatalog.GetPaths(Icon);
    public bool HasPaths => !HasImage && IconPaths.Count > 0;
    public bool HasFallback => !HasImage && !HasPaths;
    public string FallbackText => IsOverflow ? "+" : Title.FirstOrDefault().ToString().ToUpperInvariant();

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
