using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

internal sealed class OrbitLinkWireRequest
{
    public string Type { get; set; } = "";
    public OrbitLinkPairRequest? Pair { get; set; }
    public OrbitLinkEncryptedTransfer? Transfer { get; set; }
    public OrbitLinkPullRequest? Pull { get; set; }
}

internal sealed class OrbitLinkWireResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public OrbitLinkPairResponse? Pair { get; set; }
    public OrbitLinkEncryptedTransfer? Transfer { get; set; }
    public string TransferId { get; set; } = "";
    public string Proof { get; set; } = "";
}

internal sealed class OrbitLinkPullRequest
{
    public string RequesterId { get; set; } = "";
    public string Nonce { get; set; } = "";
    public string AcknowledgedTransferId { get; set; } = "";
    public bool AcknowledgedSuccess { get; set; }
    public string AcknowledgedMessage { get; set; } = "";
    public string Proof { get; set; } = "";
}

internal sealed class OrbitLinkPairRequest
{
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public int ListenPort { get; set; }
    public string Nonce { get; set; } = "";
    public string Proof { get; set; } = "";
}

internal sealed class OrbitLinkPairResponse
{
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public int ListenPort { get; set; }
    public string Nonce { get; set; } = "";
    public string Ciphertext { get; set; } = "";
    public string Tag { get; set; } = "";
}

internal sealed class OrbitLinkEncryptedTransfer
{
    public string SenderId { get; set; } = "";
    public string TransferId { get; set; } = "";
    public string Nonce { get; set; } = "";
    public string Ciphertext { get; set; } = "";
    public string Tag { get; set; } = "";
}

internal sealed class OrbitLinkTransferPayload
{
    public string TransferId { get; set; } = "";
    public string Kind { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string TextContent { get; set; } = "";
    public string Extension { get; set; } = "";
    public string ContentBase64 { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = "";
}

public sealed record OrbitLinkPairingOffer(
    string Code,
    string Address,
    DateTime ExpiresUtc);

public enum OrbitLinkTransferState
{
    Queued,
    Sending,
    Delivered,
    Failed,
    Canceled
}

public sealed record OrbitLinkTransferStatus(
    string ShelfItemId,
    string TransferId,
    string PeerId,
    string PeerName,
    OrbitLinkTransferState State,
    string Message,
    DateTime UpdatedUtc);

public sealed class OrbitLinkTransferStatusChangedEventArgs(OrbitLinkTransferStatus status) : EventArgs
{
    public OrbitLinkTransferStatus Status { get; } = status;
}

public sealed record OrbitLinkOperationResult(
    bool Succeeded,
    string Message,
    string TransferId = "",
    OrbitLinkTransferState? TransferState = null)
{
    public static OrbitLinkOperationResult Success(
        string message,
        string transferId = "",
        OrbitLinkTransferState? transferState = null) =>
        new(true, message, transferId, transferState);

    public static OrbitLinkOperationResult Failure(
        string message,
        string transferId = "",
        OrbitLinkTransferState? transferState = null) =>
        new(false, message, transferId, transferState);
}

public sealed class OrbitLinkItemReceivedEventArgs(OrbitLinkPeer peer, ShelfItem item) : EventArgs
{
    public OrbitLinkPeer Peer { get; } = peer;
    public ShelfItem Item { get; } = item;
    public bool Accepted { get; private set; } = true;
    public bool IsDuplicate { get; private set; }
    public string RejectionMessage { get; private set; } = "";

    public void Reject(string message, bool isDuplicate = false)
    {
        Accepted = false;
        IsDuplicate = isDuplicate;
        RejectionMessage = string.IsNullOrWhiteSpace(message) ? "Alıcı öğeyi kabul etmedi." : message;
    }
}
