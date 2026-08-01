namespace ActionOrbit.App.Models;

public sealed class ShelfSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxItemsPerShelf { get; set; } = 20;
    public long MaxItemBytes { get; set; } = 50 * 1024 * 1024;
    public long MaxTotalBytes { get; set; } = 100 * 1024 * 1024;
    public int RetentionHours { get; set; } = 24;
    public bool RememberRecentShelves { get; set; }
}
