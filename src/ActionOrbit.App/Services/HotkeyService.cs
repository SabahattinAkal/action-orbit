using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.Services;

public sealed class HotkeyService : IHotkeyRegistrar, IDisposable
{
    private const int MainHotkeyId = 0x4150;

    private readonly LogService _logService;
    private HwndSource? _source;
    private IntPtr _windowHandle;
    private bool _registered;
    private HotkeyConfig? _registeredHotkey;

    public HotkeyService(LogService logService)
    {
        _logService = logService;
    }

    public event EventHandler? HotkeyPressed;

    public bool IsRegistered => _registered;

    public void Initialize(Window window)
    {
        if (_source is not null)
        {
            return;
        }

        _windowHandle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(WndProc);
    }

    public void Register(HotkeyConfig hotkey)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Hotkey service is not initialized with a window handle.");
        }

        var previousHotkey = _registeredHotkey is null ? null : Clone(_registeredHotkey);
        UnregisterCore(clearRegisteredHotkey: false);

        try
        {
            RegisterCore(hotkey);
        }
        catch
        {
            if (previousHotkey is not null)
            {
                try
                {
                    RegisterCore(previousHotkey);
                }
                catch (Exception restoreException)
                {
                    _registered = false;
                    _registeredHotkey = null;
                    _logService.Error("Previous hotkey could not be restored.", restoreException);
                }
            }

            throw;
        }
    }

    public void Unregister() =>
        UnregisterCore(clearRegisteredHotkey: true);

    private void RegisterCore(HotkeyConfig hotkey)
    {
        if (!HotkeyParser.TryParse(hotkey, out var modifiers, out var virtualKey))
        {
            throw new InvalidOperationException($"Hotkey key could not be parsed: {hotkey.Key}");
        }

        if (!NativeMethods.RegisterHotKey(_windowHandle, MainHotkeyId, modifiers, virtualKey))
        {
            var error = Marshal.GetLastWin32Error();
            throw new Win32Exception(error, $"Could not register hotkey {hotkey.Display}.");
        }

        _registered = true;
        _registeredHotkey = Clone(hotkey);
        _logService.Info($"Hotkey registered: {hotkey.Display}.");
    }

    private void UnregisterCore(bool clearRegisteredHotkey)
    {
        if (!_registered || _windowHandle == IntPtr.Zero)
        {
            _registered = false;
            if (clearRegisteredHotkey)
            {
                _registeredHotkey = null;
            }
            return;
        }

        if (!NativeMethods.UnregisterHotKey(_windowHandle, MainHotkeyId))
        {
            _logService.Warn("Hotkey unregister returned false.");
        }

        _registered = false;
        if (clearRegisteredHotkey)
        {
            _registeredHotkey = null;
        }
    }

    public void Dispose()
    {
        Unregister();
        _source?.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WmHotkey && wParam.ToInt32() == MainHotkeyId)
        {
            handled = true;
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    private static HotkeyConfig Clone(HotkeyConfig hotkey) =>
        new()
        {
            Display = hotkey.Display,
            Key = hotkey.Key,
            Modifiers = [.. hotkey.Modifiers]
        };
}
