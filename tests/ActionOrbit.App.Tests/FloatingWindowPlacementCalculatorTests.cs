using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class FloatingWindowPlacementCalculatorTests
{
    [Theory]
    [InlineData(96u)]
    [InlineData(120u)]
    [InlineData(144u)]
    public void Shelf_StaysOnNegativeOriginMonitorAtEachScale(uint dpi)
    {
        var area = new PixelRect(-1920, 0, 0, 1032);
        var placement = FloatingWindowPlacementCalculator.Calculate(new PixelPoint(-20, 1000), area,
            460, 620, 380, 420, dpi, dpi, alignRight: true);
        Assert.InRange(placement.Left, area.Left, area.Right - placement.Width);
        Assert.InRange(placement.Top, area.Top, area.Bottom - placement.Height);
        Assert.Equal((int)Math.Ceiling(460 * dpi / 96d), placement.Width);
    }

    [Theory]
    [InlineData(96u)]
    [InlineData(120u)]
    [InlineData(144u)]
    public void OversizedWindow_AndOffscreenPreviousBounds_FitSmallWorkArea(uint dpi)
    {
        var area = new PixelRect(-800, -500, 0, 0);
        var placement = FloatingWindowPlacementCalculator.Calculate(new PixelPoint(-1, -1), area,
            1200, 1200, 1000, 1000, dpi, dpi, previousBounds: new PixelRect(2000, 2000, 3200, 3200));
        Assert.True(placement.Width > 0 && placement.Width <= area.Width);
        Assert.True(placement.Height > 0 && placement.Height <= area.Height);
        Assert.InRange(placement.Left, area.Left, area.Right - placement.Width);
        Assert.InRange(placement.Top, area.Top, area.Bottom - placement.Height);
    }
}
