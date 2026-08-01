namespace ActionOrbit.App.Models;

public sealed class RingSetConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<OrbitAction> Actions { get; set; } = [];
}
