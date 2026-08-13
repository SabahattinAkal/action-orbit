namespace ActionOrbit.App.Models;

public sealed class OrbitLinkPeer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "Eşleşen cihaz";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 48731;
    public string ProtectedKey { get; set; } = "";
    public DateTime PairedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.MinValue;
}
