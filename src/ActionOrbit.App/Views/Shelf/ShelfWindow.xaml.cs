using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ActionOrbit.App.ViewModels;
using ActionOrbit.App.Services;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDataObject = System.Windows.IDataObject;
using WpfDragDrop = System.Windows.DragDrop;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;
using WpfBrush = System.Windows.Media.Brush;

namespace ActionOrbit.App.Views.Shelf;

public partial class ShelfWindow : Window
{
    private WpfPoint _dragStart;
    private ShelfItemViewModel? _dragItem;
    private WpfPoint _compactDragStart;
    private bool _compactWasDragged;
    private PixelRect? _expandedBounds;
    private PixelRect? _compactBounds;
    private double _expandedWidth, _expandedHeight, _expandedMinWidth, _expandedMinHeight, _expandedMaxHeight;
    public bool IsCompact { get; private set; }

    public ShelfWindow()
    {
        InitializeComponent();
        StateChanged += (_, _) =>
        {
            if (WindowState != WindowState.Minimized) return;
            WindowState = WindowState.Normal;
            CollapseToIcon();
        };
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => CollapseToIcon();

    internal void CollapseToIcon()
    {
        if (IsCompact) return;
        _expandedBounds = FloatingWindowPlacementService.GetBounds(this);
        (_expandedWidth, _expandedHeight) = (Width, Height);
        (_expandedMinWidth, _expandedMinHeight, _expandedMaxHeight) = (MinWidth, MinHeight, MaxHeight);
        IsCompact = true;
        _dragItem = null;
        FullShelfPanel.Visibility = Visibility.Collapsed;
        CompactShelfButton.Visibility = Visibility.Visible;
        ResizeMode = ResizeMode.NoResize;
        MinWidth = MinHeight = 64;
        Width = Height = 64;
        UpdateLayout();
        ClampToCurrentScreen(_compactBounds);
    }

    internal void ExpandFromIcon()
    {
        if (!IsCompact) return;
        var iconBounds = FloatingWindowPlacementService.GetBounds(this);
        _compactBounds = iconBounds;
        IsCompact = false;
        FullShelfPanel.Visibility = Visibility.Visible;
        CompactShelfButton.Visibility = Visibility.Collapsed;
        MaxHeight = _expandedMaxHeight;
        MinWidth = _expandedMinWidth;
        MinHeight = _expandedMinHeight;
        Width = _expandedWidth;
        Height = _expandedHeight;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        UpdateLayout();
        ClampToCurrentScreen(iconBounds);
    }

    internal PixelRect? GetExpandedBounds()
    {
        var current = FloatingWindowPlacementService.GetBounds(this);
        if (!IsCompact || current is null || _expandedBounds is null) return current;
        return new PixelRect(current.Value.Left, current.Value.Top,
            current.Value.Left + _expandedBounds.Value.Width, current.Value.Top + _expandedBounds.Value.Height);
    }

    private void ClampToCurrentScreen(PixelRect? anchorBounds = null)
    {
        var bounds = anchorBounds ?? FloatingWindowPlacementService.GetBounds(this);
        if (bounds is not { } area) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        var desired = new PixelRect(area.Left, area.Top,
            area.Left + (int)Math.Ceiling(Width * dpi.DpiScaleX), area.Top + (int)Math.Ceiling(Height * dpi.DpiScaleY));
        FloatingWindowPlacementService.Place(this, previousBounds: desired,
            anchor: new PixelPoint(area.Left + area.Width / 2, area.Top + area.Height / 2));
    }

    private void CompactShelf_Click(object sender, RoutedEventArgs e)
    {
        if (_compactWasDragged && ReferenceEquals(sender, CompactShelfButton)) { _compactWasDragged = false; return; }
        _compactWasDragged = false;
        ExpandFromIcon();
    }

    private void CompactShelf_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _compactDragStart = e.GetPosition(this);
        _compactWasDragged = false;
    }

    private void CompactShelf_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (!IsCompact || e.LeftButton != MouseButtonState.Pressed || _compactWasDragged) return;
        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _compactDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(point.Y - _compactDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _compactWasDragged = true;
        CompactShelfButton.ReleaseMouseCapture();
        DragMove();
        ClampToCurrentScreen();
        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Shelf_DragEnter(object sender, WpfDragEventArgs e)
    {
        if (IsCompact && CanAccept(e.Data)) ExpandFromIcon();
        UpdateDropFeedback(e);
    }

    private void Shelf_DragOver(object sender, WpfDragEventArgs e) => UpdateDropFeedback(e);

    private void UpdateDropFeedback(WpfDragEventArgs e)
    {
        e.Effects = CanAccept(e.Data) ? WpfDragDropEffects.Copy : WpfDragDropEffects.None;
        DropZone.Opacity = e.Effects == WpfDragDropEffects.Copy ? 1 : 0.58;
        DropZone.BorderBrush = e.Effects == WpfDragDropEffects.Copy
            ? (WpfBrush)FindResource("PrimaryActionBrush")
            : (WpfBrush)FindResource("InfoBorderBrush");
        e.Handled = true;
    }

    private void Shelf_DragLeave(object sender, WpfDragEventArgs e) => ResetDropFeedback();

    private async void Shelf_Drop(object sender, WpfDragEventArgs e)
    {
        ResetDropFeedback();
        if (DataContext is ShelfViewModel viewModel && CanAccept(e.Data))
        {
            e.Handled = true;
            await viewModel.HandleDropAsync(e.Data);
        }
    }

    private void ResetDropFeedback()
    {
        DropZone.Opacity = 1;
        DropZone.BorderBrush = (WpfBrush)FindResource("InfoBorderBrush");
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
