namespace ActionOrbit.App.Models;

public sealed class OrbitAction
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Type { get; set; } = "";
    public string Target { get; set; } = "";
    public string Arguments { get; set; } = "";
    public List<OrbitAction> Children { get; set; } = [];

    public bool IsFolder => string.Equals(Type, "folder", StringComparison.OrdinalIgnoreCase);
}
