using System.Windows;
using System.Windows.Input;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Views.Actions;

public partial class RingPreviewView : System.Windows.Controls.UserControl
{
    private System.Windows.Point _dragStartPoint;
    private RingPreviewSlotViewModel? _dragSource;

    public RingPreviewView()
    {
        InitializeComponent();
    }

    private ActionEditorViewModel? ViewModel => DataContext as ActionEditorViewModel;

    private void RingSlot_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        _dragSource = (sender as FrameworkElement)?.DataContext as RingPreviewSlotViewModel;
    }

    private void RingSlot_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragSource?.ActionRow is null)
        {
            return;
        }

        var position = e.GetPosition(this);
        if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var source = _dragSource;
        System.Windows.DragDrop.DoDragDrop(
            this,
            new System.Windows.DataObject(typeof(RingPreviewSlotViewModel), source),
            System.Windows.DragDropEffects.Move);
        _dragSource = null;
    }

    private void RingSlot_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        var source = e.Data.GetData(typeof(RingPreviewSlotViewModel)) as RingPreviewSlotViewModel;
        var target = (sender as FrameworkElement)?.DataContext as RingPreviewSlotViewModel;
        e.Effects = ViewModel?.CanReorderRingPreviewSlot(source, target) == true
            ? System.Windows.DragDropEffects.Move
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void RingSlot_Drop(object sender, System.Windows.DragEventArgs e)
    {
        var source = e.Data.GetData(typeof(RingPreviewSlotViewModel)) as RingPreviewSlotViewModel;
        var target = (sender as FrameworkElement)?.DataContext as RingPreviewSlotViewModel;

        if (source is not null && target is not null)
        {
            ViewModel?.ReorderRingPreviewSlot(source, target);
        }

        _dragSource = null;
        e.Handled = true;
    }

    private void RingSlot_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0)
        {
            return;
        }

        var direction = e.Key switch
        {
            System.Windows.Input.Key.Left => -1,
            System.Windows.Input.Key.Right => 1,
            _ => 0
        };

        if (direction == 0)
        {
            return;
        }

        ViewModel?.MoveRingPreviewSlot(
            (sender as FrameworkElement)?.DataContext as RingPreviewSlotViewModel,
            direction);
        e.Handled = true;
    }
}
