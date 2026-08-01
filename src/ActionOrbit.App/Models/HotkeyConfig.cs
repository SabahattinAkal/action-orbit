namespace ActionOrbit.App.Models;

public sealed class HotkeyConfig
{
    public string Display { get; set; } = "Ctrl+Alt+Shift+P";
    public List<string> Modifiers { get; set; } = ["Control", "Alt", "Shift"];
    public string Key { get; set; } = "P";
}
