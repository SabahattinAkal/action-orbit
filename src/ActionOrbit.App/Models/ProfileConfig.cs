namespace ActionOrbit.App.Models;

public sealed class ProfileConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string MainRingName { get; set; } = "Ana Halka";
    public List<ProfileMatch> Matches { get; set; } = [];
    public List<OrbitAction> Actions { get; set; } = [];
    public List<RingSetConfig> RingSets { get; set; } = [];
}
