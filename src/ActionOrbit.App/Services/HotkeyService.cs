using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.Services;

public sealed class HotkeyService : IHotkeyRegistrar, IDisposable
{
    private const int MainHotkeyId = 0x4150;
    private const int FirstActionHotkeyId = 0x4200;
    private const int MaxActionHotkeys = 128;

    private readonly LogService _logService;
    private HwndSource? _source;
    private IntPtr _windowHandle;
    private bool _registered;
    private HotkeyConfig? _registeredHotkey;
    private uint _registeredVirtualKey;
    private DispatcherTimer? _releaseTimer;
    private readonly Dictionary<int, string> _actionHotkeys = [];

    public HotkeyService(LogService logService)
    {
        _logService = logService;
    }

    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;
    public event EventHandler<ActionShortcutPressedEventArgs>? ActionShortcutPressed;

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
        _releaseTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(24),
            DispatcherPriority.Input,
            (_, _) => CheckForKeyRelease(),
            window.Dispatcher);
        _releaseTimer.Stop();
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

    public IReadOnlyList<string> RegisterActionShortcuts(IEnumerable<string> shortcutDisplays)
    {
        UnregisterActionShortcuts();
        var failures = new List<string>();
        if (_windowHandle == IntPtr.Zero)
        {
            failures.Add("Kısayol penceresi henüz hazır değil.");
            return failures;
        }

        var shortcuts = shortcutDisplays
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxActionHotkeys)
            .ToList();
        for (var index = 0; index < shortcuts.Count; index++)
        {
            var display = shortcuts[index];
            if (!HotkeyParser.TryParseDisplay(display, out var hotkey, out var parseError) ||
                !HotkeyParser.TryParse(hotkey, out var modifiers, out var virtualKey))
            {
                failures.Add($"{display}: {parseError}");
                continue;
            }

            var id = FirstActionHotkeyId + index;
            if (!NativeMethods.RegisterHotKey(_windowHandle, id, modifiers, virtualKey))
            {
                failures.Add($"{display}: başka bir uygulama veya aksiyon kullanıyor.");
                continue;
            }

            _actionHotkeys[id] = hotkey.Display;
        }

        _logService.Info($"Registered {_actionHotkeys.Count} direct action hotkeys.");
        return failures;
    }

    public void UnregisterActionShortcuts()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            foreach (var id in _actionHotkeys.Keys)
            {
                NativeMethods.UnregisterHotKey(_windowHandle, id);
            }
        }

        _actionHotkeys.Clear();
    }

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
        _registeredVirtualKey = virtualKey;
        _registeredHotkey = Clone(hotkey);
        _logService.Info($"Hotkey registered: {hotkey.Display}.");
    }

    private void UnregisterCore(bool clearRegisteredHotkey)
    {
        if (!_registered || _windowHandle == IntPtr.Zero)
        {
            _registered = false;
            _registeredVirtualKey = 0;
            _releaseTimer?.Stop();
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
        _registeredVirtualKey = 0;
        _releaseTimer?.Stop();
        if (clearRegisteredHotkey)
        {
            _registeredHotkey = null;
        }
    }

    public void Dispose()
    {
        Unregister();
        UnregisterActionShortcuts();
        _releaseTimer?.Stop();
        _source?.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WmHotkey && wParam.ToInt32() == MainHotkeyId)
        {
            handled = true;
            _releaseTimer?.Start();
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
        else if (message == NativeMethods.WmHotkey && _actionHotkeys.TryGetValue(wParam.ToInt32(), out var shortcut))
        {
            handled = true;
            ActionShortcutPressed?.Invoke(this, new ActionShortcutPressedEventArgs(shortcut));
        }

        return IntPtr.Zero;
    }

    private void CheckForKeyRelease()
    {
        if (_registeredVirtualKey == 0 ||
            (NativeMethods.GetAsyncKeyState((int)_registeredVirtualKey) & 0x8000) != 0)
        {
            return;
        }

        _releaseTimer?.Stop();
        HotkeyReleased?.Invoke(this, EventArgs.Empty);
    }

    private static HotkeyConfig Clone(HotkeyConfig hotkey) =>
        new()
        {
            Display = hotkey.Display,
            Key = hotkey.Key,
            Modifiers = [.. hotkey.Modifiers]
        };
}

public sealed class ActionShortcutPressedEventArgs : EventArgs
{
    public ActionShortcutPressedEventArgs(string shortcut) => Shortcut = shortcut;
    public string Shortcut { get; }
}
