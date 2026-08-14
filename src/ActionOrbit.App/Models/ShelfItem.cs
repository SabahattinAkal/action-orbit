namespace ActionOrbit.App.Models;

public sealed class ShelfItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Kind { get; set; } = "file";
    public string DisplayName { get; set; } = "";
    public string Source { get; set; } = "";
    public string LocalPath { get; set; } = "";
    public string TextContent { get; set; } = "";
    public long SizeBytes { get; set; }
    public bool IsTemporary { get; set; }
    public string TransferId { get; set; } = "";
    public string LastTransferPeerId { get; set; } = "";
    public string LastTransferPeerName { get; set; } = "";
    public string LastTransferState { get; set; } = "";
    public string LastTransferMessage { get; set; } = "";
    public DateTime? LastTransferUpdatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
