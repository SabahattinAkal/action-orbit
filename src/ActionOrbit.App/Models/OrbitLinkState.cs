namespace ActionOrbit.App.Models;

public sealed class OrbitLinkState
{
    public int Version { get; set; } = 1;
    public string DeviceId { get; set; } = Guid.NewGuid().ToString("N");
    public string DeviceName { get; set; } = Environment.MachineName;
    public bool Enabled { get; set; }
    public int ListenPort { get; set; } = 48731;
    public List<OrbitLinkPeer> Peers { get; set; } = [];
}
