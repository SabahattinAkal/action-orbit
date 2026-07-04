using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ActionOrbit.App.Services;
using ActionOrbit.App.ViewModels;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace ActionOrbit.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly HotkeyService _hotkeyService;
    private readonly Forms.NotifyIcon _trayIcon;
    private System.Windows.Point _actionDragStartPoint;
    private ActionEditorRowViewModel? _actionDragSource;
    private ActionEditorRowViewModel? _actionDropTarget;
    private bool _allowClose;

    public MainWindow(MainWindowViewModel viewModel, HotkeyService hotkeyService)
    {
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
        DataContext = _viewModel;
        InitializeComponent();
        _trayIcon = CreateTrayIcon();
        _viewModel.UserNotificationRequested += ShowUserNotification;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        _hotkeyService.Initialize(this);
        _viewModel.RegisterHotkey();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose && _viewModel.CloseToTray)
        {
            e.Cancel = true;
            HideToTray(showTip: true);
            return;
        }

        _allowClose = true;

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.UserNotificationRequested -= ShowUserNotification;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.OnClosed(e);
    }

    private Forms.NotifyIcon CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Action Orbit'i Aç", null, (_, _) => ShowFromTray());
        menu.Items.Add("Menüyü Göster", null, (_, _) =>
        {
            ShowFromTray();
            if (_viewModel.ShowOverlayCommand.CanExecute(null))
            {
                _viewModel.ShowOverlayCommand.Execute(null);
            }
        });
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Çıkış", null, (_, _) => ExitFromTray());

        var trayIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = Drawing.SystemIcons.Application,
            Text = "Action Orbit",
            Visible = true
        };

        trayIcon.DoubleClick += (_, _) => ShowFromTray();
        return trayIcon;
    }

    private void RunInBackground_Click(object sender, RoutedEventArgs e) =>
        HideToTray(showTip: true);

    private void HideToTray(bool showTip)
    {
        Hide();

        if (showTip)
        {
            _trayIcon.ShowBalloonTip(
                1400,
                "Action Orbit",
                "Arka planda çalışıyor. Kısayol aktif kalır.",
                Forms.ToolTipIcon.Info);
        }
    }

    private void ShowFromTray()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void ShowUserNotification(string message, bool isError)
    {
        _trayIcon.ShowBalloonTip(
            2200,
            "Action Orbit",
            message,
            isError ? Forms.ToolTipIcon.Warning : Forms.ToolTipIcon.Info);
    }

    private void ExitFromTray()
    {
        _allowClose = true;
        _trayIcon.Visible = false;
        System.Windows.Application.Current.Shutdown();
    }

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
        if (Math.Abs(position.X - _actionDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _actionDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var source = _actionDragSource;
        DragDrop.DoDragDrop(ActionList, new System.Windows.DataObject(typeof(ActionEditorRowViewModel), source), System.Windows.DragDropEffects.Move);
        _actionDragSource = null;
        SetActionDropTarget(null);
    }

    private void ActionList_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        var source = e.Data.GetData(typeof(ActionEditorRowViewModel)) as ActionEditorRowViewModel;
        var target = FindActionRow(e.OriginalSource as DependencyObject);

        if (_viewModel.CanMoveActionIntoFolder(source, target))
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
        if (position.X < 0 ||
            position.Y < 0 ||
            position.X > ActionList.ActualWidth ||
            position.Y > ActionList.ActualHeight)
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
            _viewModel.MoveActionIntoFolder(source, target);
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
