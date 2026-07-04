namespace ActionOrbit.App.Models;

public sealed class ThemeConfig
{
    public string Mode { get; set; } = "dark";
    public string Accent { get; set; } = "#9F1D3D";
    public double ButtonSize { get; set; } = 82;
    public double RadiusX { get; set; } = 190;
    public double RadiusY { get; set; } = 155;
    public bool Animation { get; set; } = true;
}
