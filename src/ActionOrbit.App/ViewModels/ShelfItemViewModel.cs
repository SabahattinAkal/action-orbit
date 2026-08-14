using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.ViewModels;

public sealed class ShelfItemViewModel : ViewModelBase
{
    public ShelfItemViewModel(ShelfItem item) => Item = item;

    public ShelfItem Item { get; }
    public string Id => Item.Id;
    public string DisplayName => Item.DisplayName;
    public string Kind => Item.Kind;
    public string KindLabel => Kind switch
    {
        "image" => "Görsel",
        "folder" => "Klasör",
        "url" => "Bağlantı",
        "text" => "Metin",
        _ => "Dosya"
    };
    public string Glyph => Kind switch
    {
        "image" => "▧",
        "folder" => "▰",
        "url" => "↗",
        "text" => "¶",
        _ => "◇"
    };
    public string Subtitle => Item.SizeBytes > 0
        ? $"{KindLabel} · {FormatBytes(Item.SizeBytes)}"
        : KindLabel;
    public bool IsImage => Kind == "image" && File.Exists(Item.LocalPath);
    public bool HasLocalPath => File.Exists(Item.LocalPath) || Directory.Exists(Item.LocalPath);
    public string PreviewPath => IsImage ? Item.LocalPath : "";
    public bool HasTransferStatus => !string.IsNullOrWhiteSpace(Item.LastTransferState);
    public bool IsTransferPending => TransferState is OrbitLinkTransferState.Queued or OrbitLinkTransferState.Sending;
    public bool CanRetryTransfer => TransferState is OrbitLinkTransferState.Queued or OrbitLinkTransferState.Failed;
    public string TransferStatusText => TransferState switch
    {
        OrbitLinkTransferState.Queued => $"◷ {Item.LastTransferMessage}",
        OrbitLinkTransferState.Sending => $"↗ {Item.LastTransferMessage}",
        OrbitLinkTransferState.Delivered => $"✓ {Item.LastTransferMessage}",
        OrbitLinkTransferState.Failed => $"! {Item.LastTransferMessage}",
        OrbitLinkTransferState.Canceled => "Aktarım iptal edildi.",
        _ => ""
    };

    public void ApplyTransferStatus(OrbitLinkTransferStatus status)
    {
        Item.TransferId = status.TransferId;
        Item.LastTransferPeerId = status.PeerId;
        Item.LastTransferPeerName = status.PeerName;
        Item.LastTransferState = status.State.ToString();
        Item.LastTransferMessage = status.Message;
        Item.LastTransferUpdatedUtc = status.UpdatedUtc;
        OnPropertyChanged(nameof(HasTransferStatus));
        OnPropertyChanged(nameof(IsTransferPending));
        OnPropertyChanged(nameof(CanRetryTransfer));
        OnPropertyChanged(nameof(TransferStatusText));
    }

    private OrbitLinkTransferState? TransferState =>
        Enum.TryParse<OrbitLinkTransferState>(Item.LastTransferState, ignoreCase: true, out var state)
            ? state
            : null;

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1024L * 1024 * 1024 => $"{bytes / (1024d * 1024 * 1024):0.##} GB",
        >= 1024L * 1024 => $"{bytes / (1024d * 1024):0.##} MB",
        >= 1024 => $"{bytes / 1024d:0.##} KB",
        _ => $"{bytes} B"
    };
}
