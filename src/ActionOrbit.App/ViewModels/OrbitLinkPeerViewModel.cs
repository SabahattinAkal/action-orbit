using ActionOrbit.App.Models;
using System.Net;
using System.Net.Sockets;

namespace ActionOrbit.App.ViewModels;

public sealed class OrbitLinkPeerViewModel(OrbitLinkPeer peer, bool reverseRouteReady = false)
{
    public OrbitLinkPeer Peer { get; } = peer;
    public string Id => Peer.Id;
    public string Name => Peer.Name;
    public string Endpoint => IPAddress.TryParse(Peer.Host, out var address)
        && address.AddressFamily == AddressFamily.InterNetworkV6
            ? $"[{Peer.Host}]:{Peer.Port}"
            : $"{Peer.Host}:{Peer.Port}";
    public string LastSeenLabel => Peer.LastSeenUtc == DateTime.MinValue
        ? "Henüz aktarım yapılmadı"
        : $"Son bağlantı: {Peer.LastSeenUtc.ToLocalTime():dd.MM HH:mm}";
    public bool ReverseRouteReady { get; } = reverseRouteReady;
    public string ConnectionLabel => ReverseRouteReady
        ? "Çift yönlü bağlantı hazır"
        : "Doğrudan bağlantı";
    public string DisplayName => ReverseRouteReady
        ? $"{Name} · çift yönlü"
        : Name;
}
