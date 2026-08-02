using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
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
    private Action? _openShelf;

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

    public bool TryShowOverlay(out string errorMessage)
    {
        if (TryGetSuppressedForegroundProcess(out var suppressedProcess))
        {
            errorMessage = $"Action Orbit, {suppressedProcess} öndeyken devre dışı.";
            return false;
        }

        try
        {
            ShowOverlayCore();
            errorMessage = "";
            return true;
        }
        catch (Exception ex)
        {
            _logService.Error("Overlay could not be opened.", ex);
            errorMessage = $"Overlay açılamadı: {ex.Message}";
            return false;
        }
    }

    public void CommitHoveredActionOrClose()
    {
        if (_currentWindow is not { IsVisible: true } window)
        {
            return;
        }

        window.Dispatcher.BeginInvoke(window.ExecuteHoveredActionOrClose);
    }

    public void CloseCurrentOverlay() => _currentWindow?.Close();

    public void SetShelfOpener(Action openShelf) =>
        _openShelf = openShelf ?? throw new ArgumentNullException(nameof(openShelf));

    private bool TryGetSuppressedForegroundProcess(out string processName)
    {
        var ownProcessName = $"{Process.GetCurrentProcess().ProcessName}.exe";
        processName = _activeWindowService.GetProcessNameForWindow(
            NativeMethods.GetForegroundWindow(),
            ownProcessName);
        var resolvedProcessName = processName;
        return _configService.CurrentConfig.Settings.Activation.SuppressedProcesses.Any(candidate =>
            string.Equals(candidate, resolvedProcessName, StringComparison.OrdinalIgnoreCase));
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
        var actionTargetWindow = _activeWindowService.GetLastExternalWindowHandle();
        var profile = _profileService.ResolveProfile(config, processName);
        var defaultProfile = _profileService.GetDefaultProfile(config);
        var cursor = GetCursorPosition();

        _currentWindow = new OverlayWindow(
            profile,
            defaultProfile,
            config.Theme,
            config.Settings.Activation,
            _actionExecutionService,
            _logService,
            actionTargetWindow,
            _openShelf)
        {
            WindowStartupLocation = WindowStartupLocation.Manual
        };

        _currentWindow.SourceInitialized += (_, _) => PositionWindow(_currentWindow, cursor);
        _currentWindow.Closed += (_, _) => _currentWindow = null;
        _currentWindow.Show();
        _currentWindow.Dispatcher.BeginInvoke(() => PositionWindow(_currentWindow, cursor));
        _logService.Info(
            $"Overlay opened for profile {LogService.SafeValue(profile.Name)} at {cursor.X:0},{cursor.Y:0}.");
    }

    private static NativeMethods.Point GetCursorPosition()
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            point = new NativeMethods.Point
            {
                X = (int)Math.Round(SystemParameters.PrimaryScreenWidth / 2),
                Y = (int)Math.Round(SystemParameters.PrimaryScreenHeight / 2)
            };
        }

        return point;
    }

    private static void PositionWindow(OverlayWindow window, NativeMethods.Point cursor)
    {
        var monitor = NativeMethods.MonitorFromPoint(cursor, NativeMethods.MonitorDefaultToNearest);
        var monitorInfo = new NativeMethods.MonitorInfo
        {
            Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfo>()
        };

        if (monitor == IntPtr.Zero || !NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
        {
            window.Left = cursor.X - window.OrbitCenterX;
            window.Top = cursor.Y - window.OrbitCenterY;
            return;
        }

        var dpiX = 96u;
        var dpiY = 96u;
        try
        {
            if (NativeMethods.GetDpiForMonitor(monitor, NativeMethods.MonitorDpiType.EffectiveDpi, out var resolvedX, out var resolvedY) == 0)
            {
                dpiX = resolvedX;
                dpiY = resolvedY;
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // Windows 10/11 includes shcore; 96 DPI is a safe fallback for older systems.
        }

        var placement = OverlayPlacementCalculator.Calculate(
            new PixelPoint(cursor.X, cursor.Y),
            new PixelRect(
                monitorInfo.WorkArea.Left,
                monitorInfo.WorkArea.Top,
                monitorInfo.WorkArea.Right,
                monitorInfo.WorkArea.Bottom),
            window.Width,
            window.Height,
            window.OrbitCenterX,
            window.OrbitCenterY,
            dpiX,
            dpiY);

        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
        {
            NativeMethods.SetWindowPos(
                handle,
                IntPtr.Zero,
                placement.Left,
                placement.Top,
                placement.Width,
                placement.Height,
                NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
        }

    }
}
