using System.Windows.Threading;
using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public sealed class ActivationCoordinator : IDisposable
{
    private readonly HotkeyService _hotkeyService;
    private readonly Func<ActivationSettings> _getSettings;
    private readonly Func<bool> _showOverlay;
    private readonly Action _commitOrCloseOverlay;
    private readonly DispatcherTimer _holdTimer;
    private DateTime _lastPressUtc = DateTime.MinValue;
    private bool _holdOpenedOverlay;

    public ActivationCoordinator(
        HotkeyService hotkeyService,
        Func<ActivationSettings> getSettings,
        Func<bool> showOverlay,
        Action commitOrCloseOverlay)
    {
        _hotkeyService = hotkeyService;
        _getSettings = getSettings;
        _showOverlay = showOverlay;
        _commitOrCloseOverlay = commitOrCloseOverlay;
        _holdTimer = new DispatcherTimer(DispatcherPriority.Input);
        _holdTimer.Tick += OnHoldTimerTick;
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.HotkeyReleased += OnHotkeyReleased;
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        var settings = _getSettings();
        switch (settings.Mode)
        {
            case "hold":
                _holdOpenedOverlay = false;
                _holdTimer.Stop();
                _holdTimer.Interval = TimeSpan.FromMilliseconds(settings.HoldDelayMilliseconds);
                _holdTimer.Start();
                break;
            case "double_press":
                var now = DateTime.UtcNow;
                if (now - _lastPressUtc <= TimeSpan.FromMilliseconds(settings.DoublePressWindowMilliseconds))
                {
                    _lastPressUtc = DateTime.MinValue;
                    _showOverlay();
                }
                else
                {
                    _lastPressUtc = now;
                }
                break;
            default:
                _showOverlay();
                break;
        }
    }

    private void OnHotkeyReleased(object? sender, EventArgs e)
    {
        if (!string.Equals(_getSettings().Mode, "hold", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _holdTimer.Stop();
        if (_holdOpenedOverlay)
        {
            _holdOpenedOverlay = false;
            _commitOrCloseOverlay();
        }
    }

    private void OnHoldTimerTick(object? sender, EventArgs e)
    {
        _holdTimer.Stop();
        _holdOpenedOverlay = _showOverlay();
    }

    public void Dispose()
    {
        _holdTimer.Stop();
        _holdTimer.Tick -= OnHoldTimerTick;
        _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
        _hotkeyService.HotkeyReleased -= OnHotkeyReleased;
    }
}
