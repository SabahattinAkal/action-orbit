using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ActionOrbit.App.Services.Windows;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace ActionOrbit.App.Services;

internal sealed class WindowPinService : IDisposable
{
    private readonly Dictionary<IntPtr, (uint ProcessId, Window Outline)> _pinned = [];
    private readonly DispatcherTimer _timer;
    private const int ExStyle = -20;
    private const long TopmostStyle = 0x8;
    private readonly string _ownershipProperty = "ActionOrbitPro.Pin." + Guid.NewGuid().ToString("N");
    internal bool LastToggleSucceeded { get; private set; }
    internal bool IsTracking(IntPtr target) => _pinned.ContainsKey(target);
    internal bool OutlinesAreClickThrough => _pinned.Count > 0 && _pinned.Values.All(item =>
        (GetWindowLongPtr(new WindowInteropHelper(item.Outline).Handle, ExStyle).ToInt64() & 0x08000020) == 0x08000020);
    internal static bool IsTopmost(IntPtr target) => (GetWindowLongPtr(target, ExStyle).ToInt64() & TopmostStyle) != 0;

    public WindowPinService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += (_, _) => Refresh();
    }

    public string ToggleForeground() => Toggle(NativeMethods.GetForegroundWindow(), allowOwnProcess: false);

    internal string Toggle(IntPtr target, bool allowOwnProcess)
    {
        LastToggleSucceeded = false;
        if (!NativeMethods.IsWindow(target) || !IsWindowVisible(target)) return "Sabitlenecek bir pencere seç.";
        NativeMethods.GetWindowThreadProcessId(target, out var processId);
        if (!allowOwnProcess && processId == (uint)Environment.ProcessId) return "Önce sabitlemek istediğin Windows penceresini seç.";
        var className = new System.Text.StringBuilder(128);
        NativeMethods.GetClassName(target, className, className.Capacity);
        if (className.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd")
            return "Masaüstü ve görev çubuğu sabitlenmez; bir uygulama penceresi seç.";

        var wasPinned = (GetWindowLongPtr(target, ExStyle).ToInt64() & TopmostStyle) != 0;
        if (!wasPinned) RemoveOutline(target);
        if (!wasPinned && !SetProp(target, _ownershipProperty, new IntPtr(1)))
            return "Pencere sabitlenemedi; bu pencereye erişim izni yok.";
        if (!NativeMethods.SetWindowPos(target, new IntPtr(wasPinned ? -2 : -1), 0, 0, 0, 0, 0x1 | 0x2 | NativeMethods.SwpNoActivate))
        {
            RemoveProp(target, _ownershipProperty);
            return "Pencere sabitlenemedi. Yönetici yetkili veya korunan pencereler bu işlemi engelleyebilir.";
        }
        if (wasPinned) RemoveOutline(target);
        LastToggleSucceeded = true;
        if (wasPinned) return "Pencere sabitlemesi kapatıldı.";

        var outline = new Window
        {
            WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false, ShowActivated = false,
            Topmost = true, Focusable = false, IsHitTestVisible = false,
            Opacity = allowOwnProcess ? 0 : 1,
            WindowStartupLocation = WindowStartupLocation.Manual, Left = -32000, Top = -32000,
            Width = 1, Height = 1,
            Content = new Border { BorderThickness = new Thickness(3), CornerRadius = new CornerRadius(5),
                BorderBrush = new SolidColorBrush(Color.FromRgb(165, 30, 57)), IsHitTestVisible = false }
        };
        outline.SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(outline).Handle;
            SetWindowLongPtr(handle, ExStyle, new IntPtr(GetWindowLongPtr(handle, ExStyle).ToInt64() | 0x20 | 0x08000000 | 0x80));
            HwndSource.FromHwnd(handle)?.AddHook((IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (message == 0x84) { handled = true; return new IntPtr(-1); }
                return IntPtr.Zero;
            });
        };
        _pinned[target] = (processId, outline);
        outline.Show();
        Refresh();
        _timer.Start();
        return "Pencere sabitlendi · Ctrl+Alt+Shift+T ile kapat.";
    }

    private void Refresh()
    {
        foreach (var pair in _pinned.ToArray())
        {
            NativeMethods.GetWindowThreadProcessId(pair.Key, out var processId);
            if (!NativeMethods.IsWindow(pair.Key) || processId != pair.Value.ProcessId || GetProp(pair.Key, _ownershipProperty) != new IntPtr(1) ||
                (GetWindowLongPtr(pair.Key, ExStyle).ToInt64() & TopmostStyle) == 0)
            { RemoveOutline(pair.Key); continue; }
            var outline = pair.Value.Outline;
            var cloaked = 0;
            DwmGetWindowAttribute(pair.Key, 14, out cloaked, sizeof(int));
            if (!IsWindowVisible(pair.Key) || IsIconic(pair.Key) || cloaked != 0) { outline.Hide(); continue; }
            if (!NativeMethods.GetWindowRect(pair.Key, out var bounds)) continue;
            if (DwmGetWindowAttributeRect(pair.Key, 9, out var frame, Marshal.SizeOf<NativeMethods.Rect>()) == 0) bounds = frame;
            if (!outline.IsVisible) outline.Show();
            NativeMethods.SetWindowPos(new WindowInteropHelper(outline).Handle, new IntPtr(-1),
                bounds.Left - 2, bounds.Top - 2, Math.Max(1, bounds.Right - bounds.Left + 4), Math.Max(1, bounds.Bottom - bounds.Top + 4),
                NativeMethods.SwpNoActivate);
        }
        if (_pinned.Count == 0) _timer.Stop();
    }

    private void RemoveOutline(IntPtr target)
    {
        RemoveProp(target, _ownershipProperty);
        if (_pinned.Remove(target, out var entry)) entry.Outline.Close();
    }

    public void Dispose()
    {
        _timer.Stop();
        foreach (var pair in _pinned.ToArray())
        {
            NativeMethods.GetWindowThreadProcessId(pair.Key, out var processId);
            if (processId == pair.Value.ProcessId && NativeMethods.IsWindow(pair.Key) && GetProp(pair.Key, _ownershipProperty) == new IntPtr(1))
                NativeMethods.SetWindowPos(pair.Key, new IntPtr(-2), 0, 0, 0, 0, 0x1 | 0x2 | NativeMethods.SwpNoActivate);
            RemoveOutline(pair.Key);
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SetPropW")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetProp(IntPtr window, string name, IntPtr value);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetPropW")] private static extern IntPtr GetProp(IntPtr window, string name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "RemovePropW")] private static extern IntPtr RemoveProp(IntPtr window, string name);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsIconic(IntPtr window);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);
    [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] private static extern int DwmGetWindowAttributeRect(IntPtr window, int attribute, out NativeMethods.Rect value, int size);
}
