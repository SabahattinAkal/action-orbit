namespace ActionOrbit.App.Services;

public static class FloatingWindowPlacementCalculator
{
    public static OverlayPlacement Calculate(PixelPoint cursor, PixelRect workArea,
        double widthDip, double heightDip, double minWidthDip, double minHeightDip,
        uint dpiX, uint dpiY, bool alignRight = false, PixelRect? previousBounds = null)
    {
        var scaleX = Math.Max(96u, dpiX) / 96d;
        var scaleY = Math.Max(96u, dpiY) / 96d;
        var marginX = Math.Min((int)Math.Ceiling(12 * scaleX), workArea.Width / 4);
        var marginY = Math.Min((int)Math.Ceiling(12 * scaleY), workArea.Height / 4);
        var availableWidth = Math.Max(1, workArea.Width - 2 * marginX);
        var availableHeight = Math.Max(1, workArea.Height - 2 * marginY);
        var minWidth = Math.Min(availableWidth, Math.Max(1, (int)Math.Ceiling(minWidthDip * scaleX)));
        var minHeight = Math.Min(availableHeight, Math.Max(1, (int)Math.Ceiling(minHeightDip * scaleY)));
        var width = Math.Clamp(previousBounds?.Width ?? (int)Math.Ceiling(widthDip * scaleX), minWidth, availableWidth);
        var height = Math.Clamp(previousBounds?.Height ?? (int)Math.Ceiling(heightDip * scaleY), minHeight, availableHeight);
        var left = previousBounds?.Left ?? (alignRight ? workArea.Right - width - marginX : cursor.X + marginX);
        var top = previousBounds?.Top ?? (alignRight ? workArea.Top + (workArea.Height - height) / 2 : cursor.Y + marginY);
        return new OverlayPlacement(
            Math.Clamp(left, workArea.Left + marginX, workArea.Right - width - marginX),
            Math.Clamp(top, workArea.Top + marginY, workArea.Bottom - height - marginY), width, height);
    }
}
