using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class OverlayPlacementCalculatorTests
{
    [Fact]
    public void Calculate_CentersOrbitOnCursorAtOneHundredPercentDpi()
    {
        var placement = OverlayPlacementCalculator.Calculate(
            new PixelPoint(960, 540),
            new PixelRect(0, 0, 1920, 1040),
            windowWidthDip: 600,
            windowHeightDip: 500,
            centerXDip: 300,
            centerYDip: 250,
            dpiX: 96,
            dpiY: 96);

        Assert.Equal(new OverlayPlacement(660, 290, 600, 500), placement);
    }

    [Fact]
    public void Calculate_ClampsToMonitorWorkAreaNearBottomRightEdge()
    {
        var placement = OverlayPlacementCalculator.Calculate(
            new PixelPoint(1915, 1035),
            new PixelRect(0, 0, 1920, 1040),
            600,
            500,
            300,
            250,
            96,
            96);

        Assert.Equal(1320, placement.Left);
        Assert.Equal(540, placement.Top);
    }

    [Fact]
    public void Calculate_SupportsNegativeCoordinatesAndPerMonitorDpi()
    {
        var placement = OverlayPlacementCalculator.Calculate(
            new PixelPoint(-1280, 500),
            new PixelRect(-2560, 0, 0, 1400),
            600,
            500,
            300,
            250,
            144,
            144);

        Assert.Equal(-1730, placement.Left);
        Assert.Equal(125, placement.Top);
        Assert.Equal(900, placement.Width);
        Assert.Equal(750, placement.Height);
    }

    [Fact]
    public void Calculate_AnchorsOversizedOverlayAtWorkAreaOrigin()
    {
        var placement = OverlayPlacementCalculator.Calculate(
            new PixelPoint(400, 300),
            new PixelRect(100, 50, 500, 350),
            800,
            600,
            400,
            300,
            96,
            96);

        Assert.Equal(100, placement.Left);
        Assert.Equal(50, placement.Top);
    }
}
