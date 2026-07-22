using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Views.Actions;

public partial class ActionListView : System.Windows.Controls.UserControl
{
    private System.Windows.Point _actionDragStartPoint;
    private ActionEditorRowViewModel? _actionDragSource;
    private ActionEditorRowViewModel? _actionDropTarget;

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
        SetActionDropTarget(null);
    }

    private void ActionList_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        var source = e.Data.GetData(typeof(ActionEditorRowViewModel)) as ActionEditorRowViewModel;
        var target = FindActionRow(e.OriginalSource as DependencyObject);

        if (ViewModel?.CanMoveActionIntoFolder(source, target) == true)
        {
            e.Effects = System.Windows.DragDropEffects.Move;
            SetActionDropTarget(target);
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
            SetActionDropTarget(null);
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
            SetActionDropTarget(null);
        }
    }

    private void ActionList_Drop(object sender, System.Windows.DragEventArgs e)
    {
        var source = e.Data.GetData(typeof(ActionEditorRowViewModel)) as ActionEditorRowViewModel;
        var target = FindActionRow(e.OriginalSource as DependencyObject);

        if (source is not null && target is not null)
        {
            ViewModel?.MoveActionIntoFolder(source, target);
        }

        _actionDragSource = null;
        SetActionDropTarget(null);
        e.Handled = true;
    }

    private void SetActionDropTarget(ActionEditorRowViewModel? row)
    {
        if (ReferenceEquals(_actionDropTarget, row))
        {
            return;
        }

        if (_actionDropTarget is not null)
        {
            _actionDropTarget.IsDropTarget = false;
        }

        _actionDropTarget = row;

        if (_actionDropTarget is not null)
        {
            _actionDropTarget.IsDropTarget = true;
        }
    }

    private static ActionEditorRowViewModel? FindActionRow(DependencyObject? source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is ListBoxItem { DataContext: ActionEditorRowViewModel row })
            {
                return row;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
