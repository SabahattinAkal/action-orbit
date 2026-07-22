namespace ActionOrbit.App.Services;

public readonly record struct PixelPoint(int X, int Y);
public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Math.Max(0, Right - Left);
    public int Height => Math.Max(0, Bottom - Top);
}

public readonly record struct OverlayPlacement(int Left, int Top, int Width, int Height);

public static class OverlayPlacementCalculator
{
    public static OverlayPlacement Calculate(
        PixelPoint cursor,
        PixelRect workArea,
        double windowWidthDip,
        double windowHeightDip,
        double centerXDip,
        double centerYDip,
        uint dpiX,
        uint dpiY)
    {
        var scaleX = Math.Max(1, dpiX) / 96d;
        var scaleY = Math.Max(1, dpiY) / 96d;
        var width = Math.Max(1, (int)Math.Ceiling(windowWidthDip * scaleX));
        var height = Math.Max(1, (int)Math.Ceiling(windowHeightDip * scaleY));
        var desiredLeft = (int)Math.Round(cursor.X - centerXDip * scaleX);
        var desiredTop = (int)Math.Round(cursor.Y - centerYDip * scaleY);

        return new OverlayPlacement(
            Clamp(desiredLeft, workArea.Left, workArea.Right - width),
            Clamp(desiredTop, workArea.Top, workArea.Bottom - height),
            width,
            height);
    }

    private static int Clamp(int value, int min, int max) =>
        max < min ? min : Math.Min(Math.Max(value, min), max);
}
