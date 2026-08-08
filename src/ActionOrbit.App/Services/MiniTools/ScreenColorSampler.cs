using System.Runtime.InteropServices;

namespace ActionOrbit.App.Services.MiniTools;

internal static class ScreenColorSampler
{
    public static bool TrySample(out byte red, out byte green, out byte blue)
    {
        red = green = blue = 0;
        if (!GetCursorPos(out var point))
        {
            return false;
        }

        var deviceContext = GetDC(nint.Zero);
        if (deviceContext == nint.Zero)
        {
            return false;
        }

        try
        {
            var colorReference = GetPixel(deviceContext, point.X, point.Y);
            if (colorReference == uint.MaxValue)
            {
                return false;
            }

            red = (byte)(colorReference & 0xFF);
            green = (byte)((colorReference >> 8) & 0xFF);
            blue = (byte)((colorReference >> 16) & 0xFF);
            return true;
        }
        finally
        {
            ReleaseDC(nint.Zero, deviceContext);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint windowHandle);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint windowHandle, nint deviceContext);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(nint deviceContext, int x, int y);
}
