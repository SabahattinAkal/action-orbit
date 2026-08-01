namespace ActionOrbit.App.Models;

public sealed class ActivationSettings
{
    public string Mode { get; set; } = "toggle";
    public int HoldDelayMilliseconds { get; set; } = 260;
    public int DoublePressWindowMilliseconds { get; set; } = 380;
    public bool CancelWhenPointerLeaves { get; set; }
    public List<string> SuppressedProcesses { get; set; } = [];
}
