using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.ViewModels;

public sealed class ActionEditorRowViewModel : ViewModelBase
{
    public ActionEditorRowViewModel(OrbitAction action, List<OrbitAction> owner, ActionEditorRowViewModel? parent, int depth)
    {
        Action = action;
        Owner = owner;
        Parent = parent;
        Depth = depth;
    }

    public OrbitAction Action { get; }
    public List<OrbitAction> Owner { get; }
    public ActionEditorRowViewModel? Parent { get; }
    public int Depth { get; }
    public double Indent => Depth * 22;
    public bool IsChild => Depth > 0;
    public bool HasParent => Parent is not null;
    public string ParentTitle => Parent?.Title ?? "";
    public int ChildCount => Action.Children?.Count ?? 0;
    public bool CanChangeType => ChildCount == 0;
    public string FolderBadgeText => ChildCount == 1 ? "1 alt" : $"{ChildCount} alt";
    public string RowSubtitle =>
        IsChild
            ? $"{TypeLabel} - {ParentTitle} içinde"
            : IsFolder
                ? $"{FolderBadgeText} aksiyon"
                : TypeLabel;

    private bool _isDropTarget;

    public bool IsDropTarget
    {
        get => _isDropTarget;
        set => SetProperty(ref _isDropTarget, value);
    }

    public string Id
    {
        get => Action.Id;
        set
        {
            if (Action.Id == value)
            {
                return;
            }

            Action.Id = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public string Title
    {
        get => Action.Title;
        set
        {
            if (Action.Title == value)
            {
                return;
            }

            Action.Title = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(RowSubtitle));
        }
    }

    public string Icon
    {
        get => Action.Icon;
        set
        {
            if (Action.Icon == value)
            {
                return;
            }

            Action.Icon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayIcon));
            OnPropertyChanged(nameof(IconImagePath));
            OnPropertyChanged(nameof(HasIconImage));
            OnPropertyChanged(nameof(IconPaths));
            OnPropertyChanged(nameof(HasIconPaths));
            OnPropertyChanged(nameof(HasFallbackIcon));
        }
    }

    public string Type
    {
        get => Action.Type;
        set
        {
            if (Action.Type == value)
            {
                return;
            }

            Action.Type = value;
            Action.Children ??= [];
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFolder));
            OnPropertyChanged(nameof(IsNotFolder));
            OnPropertyChanged(nameof(IsRunCommand));
            OnPropertyChanged(nameof(IsOpenUrl));
            OnPropertyChanged(nameof(IsTargetEditable));
            OnPropertyChanged(nameof(TargetBoxMinHeight));
            OnPropertyChanged(nameof(TypeLabel));
            OnPropertyChanged(nameof(TargetLabel));
            OnPropertyChanged(nameof(ArgumentsLabel));
            OnPropertyChanged(nameof(TypeHelp));
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(RowSubtitle));
            OnPropertyChanged(nameof(FolderBadgeText));
        }
    }

    public string Target
    {
        get => Action.Target;
        set
        {
            if (Action.Target == value)
            {
                return;
            }

            Action.Target = value;
            OnPropertyChanged();
        }
    }

    public string Arguments
    {
        get => Action.Arguments;
        set
        {
            if (Action.Arguments == value)
            {
                return;
            }

            Action.Arguments = value;
            OnPropertyChanged();
        }
    }

    public string Browser
    {
        get => Action.Browser;
        set
        {
            if (Action.Browser == value)
            {
                return;
            }

            Action.Browser = value;
            OnPropertyChanged();
        }
    }

    public string Shortcut
    {
        get => Action.Shortcut;
        set
        {
            if (Action.Shortcut == value)
            {
                return;
            }

            Action.Shortcut = value;
            OnPropertyChanged();
        }
    }

    public bool IsFolder => Action.IsFolder;
    public bool IsNotFolder => !IsFolder;
    public bool IsRunCommand => string.Equals(Type, "run_command", StringComparison.OrdinalIgnoreCase);
    public bool IsOpenUrl => string.Equals(Type, "open_url", StringComparison.OrdinalIgnoreCase);
    public bool IsTargetEditable => !IsFolder;
    public double TargetBoxMinHeight => string.Equals(Type, "type_text", StringComparison.OrdinalIgnoreCase) ? 88 : 36;
    public string? IconImagePath => IconCatalog.GetImagePath(Icon);
    public bool HasIconImage => IconImagePath is not null;
    public IReadOnlyList<string> IconPaths => IconCatalog.GetPaths(Action.Icon);
    public bool HasIconPaths => !HasIconImage && IconPaths.Count > 0;
    public bool HasFallbackIcon => !HasIconImage && !HasIconPaths;
    public string DisplayIcon => string.IsNullOrWhiteSpace(Icon) ? "." : Icon;
    public string TypeLabel => ActionDefinitionCatalog.GetTypeOption(Type).Label;
    public string TargetLabel => ActionDefinitionCatalog.GetTypeOption(Type).TargetLabel;
    public string ArgumentsLabel => ActionDefinitionCatalog.GetTypeOption(Type).ArgumentsLabel;
    public string TypeHelp => ActionDefinitionCatalog.GetTypeOption(Type).Help;
    public string DisplayText => $"{Title} - {TypeLabel}";
}
