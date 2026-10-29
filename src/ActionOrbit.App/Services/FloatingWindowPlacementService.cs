using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.Services;

internal static class FloatingWindowPlacementService
{
    internal static void Place(Window window, bool alignRight = false, PixelRect? previousBounds = null)
    {
        if (!NativeMethods.GetCursorPos(out var cursor)) return;
        var monitor = NativeMethods.MonitorFromPoint(cursor, NativeMethods.MonitorDefaultToNearest);
        var info = new NativeMethods.MonitorInfo { Size = Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (monitor == IntPtr.Zero || !NativeMethods.GetMonitorInfo(monitor, ref info)) return;
        uint dpiX = 96, dpiY = 96;
        if (NativeMethods.GetDpiForMonitor(monitor, NativeMethods.MonitorDpiType.EffectiveDpi, out var x, out var y) == 0)
        { dpiX = Math.Max(96u, x); dpiY = Math.Max(96u, y); }
        var area = new PixelRect(info.WorkArea.Left, info.WorkArea.Top, info.WorkArea.Right, info.WorkArea.Bottom);
        var maxWidth = Math.Max(1, area.Width * 96d / dpiX - 24);
        var maxHeight = Math.Max(1, area.Height * 96d / dpiY - 24);
        window.MinWidth = Math.Min(window.MinWidth, maxWidth);
        window.MinHeight = Math.Min(window.MinHeight, maxHeight);
        window.MaxHeight = Math.Min(window.MaxHeight, maxHeight);
        var width = double.IsFinite(window.Width) ? window.Width : Math.Max(window.MinWidth, window.ActualWidth);
        var height = window.ActualHeight > 0 ? window.ActualHeight :
            double.IsFinite(window.Height) ? window.Height : Math.Min(680, maxHeight);
        var placement = FloatingWindowPlacementCalculator.Calculate(new PixelPoint(cursor.X, cursor.Y), area,
            width, height, window.MinWidth, window.MinHeight, dpiX, dpiY, alignRight, previousBounds);
        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
            NativeMethods.SetWindowPos(handle, IntPtr.Zero, placement.Left, placement.Top, placement.Width, placement.Height,
                NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
    }

    internal static PixelRect? GetBounds(Window window)
    {
        if (window.WindowState == WindowState.Minimized) return null;
        var handle = new WindowInteropHelper(window).Handle;
        return handle != IntPtr.Zero && NativeMethods.GetWindowRect(handle, out var rect)
            ? new PixelRect(rect.Left, rect.Top, rect.Right, rect.Bottom) : null;
    }
}
