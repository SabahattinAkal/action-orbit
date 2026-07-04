namespace ActionOrbit.App.Models;

public sealed class AppConfig
{
    public int ConfigVersion { get; set; }
    public HotkeyConfig Hotkey { get; set; } = new();
    public string DefaultProfileId { get; set; } = "default";
    public ThemeConfig Theme { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
    public List<ProfileConfig> Profiles { get; set; } = [];
}
