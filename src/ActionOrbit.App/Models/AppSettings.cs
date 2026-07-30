namespace ActionOrbit.App.Models;

public sealed class AppSettings
{
    public bool RunAtStartup { get; set; }
    public bool CloseToTray { get; set; } = true;
    public bool AllowCommandActions { get; set; }
}
