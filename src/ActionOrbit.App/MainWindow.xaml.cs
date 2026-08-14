using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using ActionOrbit.App.Services;
using ActionOrbit.App.ViewModels;
using ActionOrbit.App.Views.Settings;
using ActionOrbit.App.Views.Shelf;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace ActionOrbit.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly HotkeyService _hotkeyService;
    private readonly Drawing.Icon? _applicationIcon;
    private readonly Forms.NotifyIcon _trayIcon;
    private bool _allowClose;
    private bool _hasShownTrayTip;

    public MainWindow(MainWindowViewModel viewModel, HotkeyService hotkeyService)
    {
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
        DataContext = _viewModel;
        InitializeComponent();
        _applicationIcon = LoadApplicationIcon();
        _trayIcon = CreateTrayIcon();
        _viewModel.Status.UserNotificationRequested += ShowUserNotification;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        _hotkeyService.Initialize(this);
        _viewModel.RegisterHotkey();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose && _viewModel.Settings.CloseToTray)
        {
            e.Cancel = true;
            HideToTray(showTip: true);
            return;
        }

        if (!_viewModel.FlushPendingChanges())
        {
            var confirmation = System.Windows.MessageBox.Show(
                "Bekleyen değişiklikler kaydedilemedi. Yine de uygulamadan çıkılsın mı?",
                "Kaydetme hatası",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirmation != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        _allowClose = true;

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Status.UserNotificationRequested -= ShowUserNotification;
        _viewModel.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _applicationIcon?.Dispose();
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
        menu.Items.Add("Orbit Shelf'i Aç", null, (_, _) =>
            _viewModel.Shelf.OpenFloatingShelfCommand.Execute(null));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Çıkış", null, (_, _) => ExitFromTray());

        var trayIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = _applicationIcon ?? Drawing.SystemIcons.Application,
            Text = "Action Orbit Pro",
            Visible = true
        };

        trayIcon.DoubleClick += (_, _) => ShowFromTray();
        return trayIcon;
    }

    private static Drawing.Icon? LoadApplicationIcon()
    {
        try
        {
            return string.IsNullOrWhiteSpace(Environment.ProcessPath)
                ? null
                : Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);
        }
        catch
        {
            return null;
        }
    }

    private void RunInBackground_Click(object sender, RoutedEventArgs e) =>
        HideToTray(showTip: true);

    private void HideToTray(bool showTip)
    {
        Hide();

        if (showTip && !_hasShownTrayTip)
        {
            _hasShownTrayTip = true;
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

    internal void RestoreFromExternalRequest()
    {
        ShowFromTray();
        Topmost = true;
        Topmost = false;
        Focus();
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
        // ContextMenuStrip tıklama olayı sürerken NotifyIcon'u dispose etmek,
        // Windows'un açılır menüyü ekranda hayalet olarak bırakmasına neden
        // olabiliyor. Önce menüyü kapat, WPF kapanışını sonraki UI turunda yap.
        _trayIcon.ContextMenuStrip?.Close(Forms.ToolStripDropDownCloseReason.ItemClicked);
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _allowClose = true;
            Close();

            if (IsLoaded)
            {
                _allowClose = false;
            }
        }));
    }

    internal IReadOnlyDictionary<string, bool> RunReleaseSmokeChecks()
    {
        var checks = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["mainWindowXaml"] = IsLoaded && Content is not null,
            ["trayIcon"] = _trayIcon.Visible && _trayIcon.Icon is not null
        };

        _viewModel.NavigateWorkspaceCommand.Execute("settings");
        UpdateLayout();
        checks["settingsXaml"] = _viewModel.IsSettingsWorkspace
            && FindVisualChild<SettingsView>(this) is not null;

        var shelfWindow = new ShelfWindow
        {
            DataContext = _viewModel.Shelf,
            ShowActivated = false,
            ShowInTaskbar = false,
            Opacity = 0,
            Left = -32000,
            Top = -32000
        };
        shelfWindow.Show();
        shelfWindow.UpdateLayout();
        checks["orbitShelfXaml"] = shelfWindow is { IsLoaded: true, Content: not null };
        shelfWindow.Close();

        return checks;
    }

    internal void ExitFromTrayForReleaseSmoke() => ExitFromTray();

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) return match;
            var nested = FindVisualChild<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }

}
