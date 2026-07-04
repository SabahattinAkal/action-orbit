using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using ActionOrbit.App.Overlay;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.Services;

public sealed class OverlayService
{
    private readonly ConfigService _configService;
    private readonly ActiveWindowService _activeWindowService;
    private readonly ProfileService _profileService;
    private readonly ActionExecutionService _actionExecutionService;
    private readonly LogService _logService;
    private OverlayWindow? _currentWindow;

    public OverlayService(
        ConfigService configService,
        ActiveWindowService activeWindowService,
        ProfileService profileService,
        ActionExecutionService actionExecutionService,
        LogService logService)
    {
        _configService = configService;
        _activeWindowService = activeWindowService;
        _profileService = profileService;
        _actionExecutionService = actionExecutionService;
        _logService = logService;
    }

    public void ShowOverlay()
    {
        try
        {
            ShowOverlayCore();
        }
        catch (Exception ex)
        {
            _logService.Error("Overlay could not be opened.", ex);
        }
    }

    private void ShowOverlayCore()
    {
        if (_currentWindow is { IsVisible: true })
        {
            _currentWindow.Close();
            return;
        }

        var config = _configService.CurrentConfig;
        var previousForegroundWindow = NativeMethods.GetForegroundWindow();
        var ownProcessName = $"{Process.GetCurrentProcess().ProcessName}.exe";
        var processName = _activeWindowService.GetProcessNameForWindow(previousForegroundWindow, ownProcessName);
        var profile = _profileService.ResolveProfile(config, processName);
        var defaultProfile = _profileService.GetDefaultProfile(config);
        var cursor = GetCursorPosition();

        _currentWindow = new OverlayWindow(profile, defaultProfile, config.Theme, _actionExecutionService, _logService, previousForegroundWindow)
        {
            WindowStartupLocation = WindowStartupLocation.Manual
        };

        PositionWindow(_currentWindow, cursor);
        _currentWindow.Closed += (_, _) => _currentWindow = null;
        _currentWindow.Show();
        _logService.Info($"Overlay opened for profile {profile.Name} at {cursor.X:0},{cursor.Y:0}.");
    }

    private static System.Windows.Point GetCursorPosition()
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            return new System.Windows.Point(SystemParameters.PrimaryScreenWidth / 2, SystemParameters.PrimaryScreenHeight / 2);
        }

        var wpfPoint = new System.Windows.Point(point.X, point.Y);
        var source = PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow);
        return source?.CompositionTarget?.TransformFromDevice.Transform(wpfPoint) ?? wpfPoint;
    }

    private static void PositionWindow(OverlayWindow window, System.Windows.Point cursor)
    {
        var centerX = window.OrbitCenterX;
        var centerY = window.OrbitCenterY;
        var left = cursor.X - centerX;
        var top = cursor.Y - centerY;

        var minLeft = SystemParameters.VirtualScreenLeft;
        var minTop = SystemParameters.VirtualScreenTop;
        var maxLeft = minLeft + SystemParameters.VirtualScreenWidth - window.Width;
        var maxTop = minTop + SystemParameters.VirtualScreenHeight - window.Height;

        window.Left = Clamp(left, minLeft, maxLeft);
        window.Top = Clamp(top, minTop, maxTop);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (max < min)
        {
            return min;
        }

        return Math.Min(Math.Max(value, min), max);
    }
}
