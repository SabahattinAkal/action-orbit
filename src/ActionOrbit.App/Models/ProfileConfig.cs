namespace ActionOrbit.App.Models;

public sealed class ProfileConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<ProfileMatch> Matches { get; set; } = [];
    public List<OrbitAction> Actions { get; set; } = [];
}
