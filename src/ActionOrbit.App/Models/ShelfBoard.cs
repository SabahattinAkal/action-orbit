namespace ActionOrbit.App.Models;

public sealed class ShelfBoard
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Yeni Raf";
    public bool IsPinned { get; set; }
    public DateTime LastUsedUtc { get; set; } = DateTime.UtcNow;
    public List<ShelfItem> Items { get; set; } = [];
}
