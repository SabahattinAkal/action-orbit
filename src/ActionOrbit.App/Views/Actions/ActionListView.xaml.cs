using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Views.Actions;

public partial class ActionListView : System.Windows.Controls.UserControl
{
    private const double FolderDropZoneStart = 0.3;
    private const double FolderDropZoneEnd = 0.7;
    private const double AutoScrollEdge = 42;
    private const double AutoScrollStep = 24;

    private System.Windows.Point _actionDragStartPoint;
    private ActionEditorRowViewModel? _actionDragSource;
    private ActionEditorRowViewModel? _actionDropTarget;
    private ActionDropMode _actionDropMode;

    public ActionListView()
    {
        InitializeComponent();
    }

    private ActionEditorViewModel? ViewModel => DataContext as ActionEditorViewModel;

    private void ActionList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _actionDragStartPoint = e.GetPosition(ActionList);
        _actionDragSource = FindActionRow(e.OriginalSource as DependencyObject);
    }

    private void ActionList_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _actionDragSource is null)
        {
            return;
        }

        var position = e.GetPosition(ActionList);
        if (Math.Abs(position.X - _actionDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - _actionDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var source = _actionDragSource;
        System.Windows.DragDrop.DoDragDrop(
            ActionList,
            new System.Windows.DataObject(typeof(ActionEditorRowViewModel), source),
            System.Windows.DragDropEffects.Move);
        _actionDragSource = null;
        SetActionDropTarget(null, ActionDropMode.None);
    }

    private void ActionList_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        AutoScrollActionList(e);
        var source = e.Data.GetData(typeof(ActionEditorRowViewModel)) as ActionEditorRowViewModel;
        var targetContainer = FindActionContainer(e.OriginalSource as DependencyObject);
        var target = targetContainer?.DataContext as ActionEditorRowViewModel;
        var dropMode = ResolveDropMode(source, target, targetContainer, e);

        if (dropMode != ActionDropMode.None)
        {
            e.Effects = System.Windows.DragDropEffects.Move;
            SetActionDropTarget(target, dropMode);
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
            SetActionDropTarget(null, ActionDropMode.None);
        }

        e.Handled = true;
    }

    private void ActionList_DragLeave(object sender, System.Windows.DragEventArgs e)
    {
        var position = e.GetPosition(ActionList);
        if (position.X < 0
            || position.Y < 0
            || position.X > ActionList.ActualWidth
            || position.Y > ActionList.ActualHeight)
        {
            SetActionDropTarget(null, ActionDropMode.None);
        }
    }

    private void ActionList_Drop(object sender, System.Windows.DragEventArgs e)
    {
        var source = e.Data.GetData(typeof(ActionEditorRowViewModel)) as ActionEditorRowViewModel;
        var targetContainer = FindActionContainer(e.OriginalSource as DependencyObject);
        var target = targetContainer?.DataContext as ActionEditorRowViewModel;
        var dropMode = ResolveDropMode(source, target, targetContainer, e);

        if (source is not null && target is not null)
        {
            if (dropMode == ActionDropMode.IntoFolder)
            {
                ViewModel?.MoveActionIntoFolder(source, target);
            }
            else if (dropMode is ActionDropMode.Before or ActionDropMode.After)
            {
                ViewModel?.ReorderAction(source, target, dropMode == ActionDropMode.After);
            }
        }

        _actionDragSource = null;
        SetActionDropTarget(null, ActionDropMode.None);
        e.Handled = true;
    }

    private void ActionList_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0)
        {
            return;
        }

        var command = e.Key switch
        {
            Key.Up => ViewModel?.MoveActionUpCommand,
            Key.Down => ViewModel?.MoveActionDownCommand,
            _ => null
        };

        if (command?.CanExecute(null) != true)
        {
            return;
        }

        command.Execute(null);
        e.Handled = true;
    }

    private ActionDropMode ResolveDropMode(
        ActionEditorRowViewModel? source,
        ActionEditorRowViewModel? target,
        ListBoxItem? targetContainer,
        System.Windows.DragEventArgs e)
    {
        if (source is null || target is null || targetContainer is null)
        {
            return ActionDropMode.None;
        }

        var relativeY = e.GetPosition(targetContainer).Y / Math.Max(1, targetContainer.ActualHeight);
        var canMoveIntoFolder = ViewModel?.CanMoveActionIntoFolder(source, target) == true;
        if (target.IsFolder
            && canMoveIntoFolder
            && relativeY >= FolderDropZoneStart
            && relativeY <= FolderDropZoneEnd)
        {
            return ActionDropMode.IntoFolder;
        }

        if (!ActionEditorViewModel.CanReorderAction(source, target))
        {
            return canMoveIntoFolder ? ActionDropMode.IntoFolder : ActionDropMode.None;
        }

        return relativeY < 0.5 ? ActionDropMode.Before : ActionDropMode.After;
    }

    private void AutoScrollActionList(System.Windows.DragEventArgs e)
    {
        var scrollViewer = FindVisualChild<ScrollViewer>(ActionList);
        if (scrollViewer is null || scrollViewer.ScrollableHeight <= 0)
        {
            return;
        }

        var position = e.GetPosition(ActionList);
        if (position.Y <= AutoScrollEdge)
        {
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, scrollViewer.VerticalOffset - AutoScrollStep));
        }
        else if (position.Y >= ActionList.ActualHeight - AutoScrollEdge)
        {
            scrollViewer.ScrollToVerticalOffset(
                Math.Min(scrollViewer.ScrollableHeight, scrollViewer.VerticalOffset + AutoScrollStep));
        }
    }

    private void SetActionDropTarget(ActionEditorRowViewModel? row, ActionDropMode mode)
    {
        if (ReferenceEquals(_actionDropTarget, row) && _actionDropMode == mode)
        {
            return;
        }

        if (_actionDropTarget is not null)
        {
            _actionDropTarget.IsDropTarget = false;
        }

        _actionDropTarget = row;
        _actionDropMode = mode;

        if (_actionDropTarget is not null)
        {
            _actionDropTarget.IsDropTarget = true;
        }
    }

    private static ActionEditorRowViewModel? FindActionRow(DependencyObject? source)
    {
        return FindActionContainer(source)?.DataContext as ActionEditorRowViewModel;
    }

    private static ListBoxItem? FindActionContainer(DependencyObject? source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is ListBoxItem { DataContext: ActionEditorRowViewModel })
            {
                return (ListBoxItem)current;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private enum ActionDropMode
    {
        None,
        Before,
        After,
        IntoFolder
    }
}
