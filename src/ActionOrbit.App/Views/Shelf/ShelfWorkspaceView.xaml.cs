using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ActionOrbit.App.ViewModels;
using WpfDataObject = System.Windows.IDataObject;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDragDrop = System.Windows.DragDrop;
using WpfDragDropEffects = System.Windows.DragDropEffects;

namespace ActionOrbit.App.Views.Shelf;

public partial class ShelfWorkspaceView : WpfUserControl
{
    private WpfPoint _dragStart;
    private ShelfItemViewModel? _dragItem;

    public ShelfWorkspaceView() => InitializeComponent();

    private void Shelf_DragEnter(object sender, WpfDragEventArgs e) => UpdateDropFeedback(e);
    private void Shelf_DragOver(object sender, WpfDragEventArgs e) => UpdateDropFeedback(e);

    private void UpdateDropFeedback(WpfDragEventArgs e)
    {
        e.Effects = CanAccept(e.Data) ? WpfDragDropEffects.Copy : WpfDragDropEffects.None;
        DropZone.Opacity = e.Effects == WpfDragDropEffects.Copy ? 1 : 0.68;
        e.Handled = true;
    }

    private void Shelf_DragLeave(object sender, WpfDragEventArgs e) => DropZone.Opacity = 1;

    private async void Shelf_Drop(object sender, WpfDragEventArgs e)
    {
        DropZone.Opacity = 1;
        if (DataContext is ShelfViewModel viewModel && CanAccept(e.Data))
        {
            e.Handled = true;
            await viewModel.HandleDropAsync(e.Data);
        }
    }

    private void Shelf_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _dragItem = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject)?.DataContext as ShelfItemViewModel;
    }

    private void Shelf_PreviewMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragItem is null || DataContext is not ShelfViewModel viewModel)
        {
            return;
        }

        var position = e.GetPosition(this);
        if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var item = _dragItem;
        _dragItem = null;
        WpfDragDrop.DoDragDrop(this, viewModel.BuildDragData(item), WpfDragDropEffects.Copy);
    }

    private static bool CanAccept(WpfDataObject data) =>
        data.GetDataPresent(WpfDataFormats.FileDrop) ||
        data.GetDataPresent(WpfDataFormats.Bitmap) ||
        data.GetDataPresent(WpfDataFormats.Html) ||
        data.GetDataPresent(WpfDataFormats.UnicodeText) ||
        data.GetDataPresent(WpfDataFormats.Text);

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
