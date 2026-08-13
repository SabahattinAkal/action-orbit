using System.Buffers.Binary;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public sealed class OrbitLinkService : IDisposable
{
    public const int DefaultPort = 48731;
    public const long MaxTransferBytes = 25L * 1024 * 1024;
    private const int MaxWireBytes = 48 * 1024 * 1024;
    private const int MaxPairAttempts = 6;
    private const int MaxQueuedReverseTransfersPerPeer = 2;
    private static readonly TimeSpan PairingLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ReverseRouteLifetime = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan ReversePollInterval = TimeSpan.FromSeconds(2);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        MaxDepth = 12
    };
    private readonly object _gate = new();
    private readonly OrbitLinkStore _store;
    private readonly LogService _logService;
    private readonly string _cacheDirectory;
    private readonly HashSet<string> _recentTransfers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> _recentTransferOrder = new();
    private readonly SemaphoreSlim _clientSlots = new(4, 4);
    private readonly Dictionary<string, Queue<OrbitLinkEncryptedTransfer>> _reverseQueues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _reverseRouteSeenUtc = new(StringComparer.OrdinalIgnoreCase);
    private OrbitLinkState _state;
    private TcpListener? _listener;
    private CancellationTokenSource? _listenerCancellation;
    private Task? _listenerTask;
    private Task? _reversePollTask;
    private PairingSession? _pairingSession;
    private bool _disposed;

    public OrbitLinkService(string appDirectory, LogService logService)
    {
        _store = new OrbitLinkStore(appDirectory, logService);
        _logService = logService;
        _cacheDirectory = Path.Combine(appDirectory, "shelf-cache");
        Directory.CreateDirectory(_cacheDirectory);
        _state = _store.Load();
    }

    public event EventHandler? StateChanged;
    public event EventHandler<OrbitLinkItemReceivedEventArgs>? ItemReceived;

    public string DeviceId => _state.DeviceId;
    public string DeviceName => _state.DeviceName;
    public int ListenPort => _state.ListenPort;
    public bool Enabled => _state.Enabled;
    public bool IsRunning => _listener is not null;
    public bool HasActivePairing
    {
        get
        {
            lock (_gate)
            {
                return _pairingSession is not null && _pairingSession.ExpiresUtc > DateTime.UtcNow;
            }
        }
    }
    public IReadOnlyList<OrbitLinkPeer> Peers
    {
        get
        {
            lock (_gate)
            {
                return _state.Peers.Select(ClonePeer).ToList();
            }
        }
    }

    public string LocalAddressSummary
    {
        get
        {
            var addresses = GetLocalAddresses().Take(3).Select(address => $"{address}:{ListenPort}").ToList();
            return addresses.Count == 0 ? $"127.0.0.1:{ListenPort}" : string.Join(" · ", addresses);
        }
    }

    public bool HasReverseRoute(string peerId)
    {
        lock (_gate)
        {
            return _reverseRouteSeenUtc.TryGetValue(peerId, out var seenUtc)
                && seenUtc >= DateTime.UtcNow.Subtract(ReverseRouteLifetime);
        }
    }

    public async Task<OrbitLinkOperationResult> SetEnabledAsync(bool enabled)
    {
        ThrowIfDisposed();
        if (!enabled)
        {
            lock (_gate)
            {
                _state.Enabled = false;
                _pairingSession = null;
                _store.Save(_state);
            }
            await StopListenerAsync();
            RaiseStateChanged();
            return OrbitLinkOperationResult.Success("Orbit Link kapatıldı.");
        }

        lock (_gate)
        {
            _state.Enabled = true;
            _store.Save(_state);
        }

        var start = StartListener();
        if (!start.Succeeded)
        {
            lock (_gate)
            {
                _state.Enabled = false;
                _store.Save(_state);
            }
        }
        RaiseStateChanged();
        return start;
    }

    public OrbitLinkOperationResult StartIfEnabled()
    {
        ThrowIfDisposed();
        return Enabled ? StartListener() : OrbitLinkOperationResult.Success("Orbit Link kapalı.");
    }

    public void UpdateDeviceName(string? name)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            _state.DeviceName = OrbitLinkStore.NormalizeName(name, Environment.MachineName);
            _store.Save(_state);
        }
        RaiseStateChanged();
    }

    public OrbitLinkPairingOffer BeginPairing()
    {
        ThrowIfDisposed();
        if (!Enabled || !IsRunning)
        {
            throw new InvalidOperationException("Önce Orbit Link'i etkinleştir.");
        }

        var code = CreatePairingCode();
        var expires = DateTime.UtcNow.Add(PairingLifetime);
        lock (_gate)
        {
            _pairingSession = new PairingSession(code, expires);
        }
        return new OrbitLinkPairingOffer(code, LocalAddressSummary, expires);
    }

    public async Task<OrbitLinkOperationResult> PairAsync(
        string endpoint,
        string code,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!Enabled || !IsRunning)
        {
            return OrbitLinkOperationResult.Failure("Önce Orbit Link'i etkinleştir.");
        }

        var parsed = await ParseAndValidateEndpointAsync(endpoint, cancellationToken);
        if (!parsed.Succeeded)
        {
            return OrbitLinkOperationResult.Failure(parsed.Message);
        }

        var normalizedCode = NormalizePairingCode(code);
        if (normalizedCode.Length < 20)
        {
            return OrbitLinkOperationResult.Failure("Eşleştirme kodu eksik veya geçersiz.");
        }

        try
        {
            var nonce = RandomNumberGenerator.GetBytes(16);
            var request = new OrbitLinkPairRequest
            {
                DeviceId = DeviceId,
                DeviceName = DeviceName,
                ListenPort = ListenPort,
                Nonce = Convert.ToBase64String(nonce)
            };
            var pairingKey = DerivePairingKey(normalizedCode);
            request.Proof = CreateHmac(pairingKey, PairRequestCanonical(request));
            var response = await SendRequestAsync(
                parsed.Address!,
                parsed.Port,
                new OrbitLinkWireRequest { Type = "pair", Pair = request },
                cancellationToken);
            if (!response.Success || response.Pair is null)
            {
                return OrbitLinkOperationResult.Failure(
                    string.IsNullOrWhiteSpace(response.Message) ? "Cihaz eşleştirilemedi." : response.Message);
            }

            var pair = response.Pair;
            var secret = DecryptPairSecret(pairingKey, pair);
            if (secret is null)
            {
                return OrbitLinkOperationResult.Failure("Eşleştirme yanıtı doğrulanamadı.");
            }

            var peer = new OrbitLinkPeer
            {
                Id = pair.DeviceId,
                Name = OrbitLinkStore.NormalizeName(pair.DeviceName, "Eşleşen cihaz"),
                Host = parsed.Address!.ToString(),
                Port = Math.Clamp(pair.ListenPort, 1024, 65535),
                ProtectedKey = OrbitLinkStore.ProtectKey(secret),
                PairedUtc = DateTime.UtcNow,
                LastSeenUtc = DateTime.UtcNow
            };
            UpsertPeer(peer);
            CryptographicOperations.ZeroMemory(secret);
            return OrbitLinkOperationResult.Success($"{peer.Name} eşleştirildi.");
        }
        catch (OperationCanceledException)
        {
            return OrbitLinkOperationResult.Failure("Eşleştirme iptal edildi.");
        }
        catch (Exception ex)
        {
            _logService.Error("Orbit Link pairing failed.", ex);
            return OrbitLinkOperationResult.Failure($"Cihaza ulaşılamadı: {ex.Message}");
        }
    }

    public async Task<OrbitLinkOperationResult> SendItemAsync(
        string peerId,
        ShelfItem item,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!Enabled)
        {
            return OrbitLinkOperationResult.Failure("Orbit Link kapalı; Ayarlar'dan etkinleştir.");
        }
        var peer = FindPeer(peerId);
        if (peer is null || !OrbitLinkStore.TryUnprotectKey(peer.ProtectedKey, out var key))
        {
            return OrbitLinkOperationResult.Failure("Eşleşen cihaz veya güvenlik anahtarı bulunamadı.");
        }

        try
        {
            var payloadResult = await BuildPayloadAsync(item, cancellationToken);
            if (payloadResult.Payload is null)
            {
                return OrbitLinkOperationResult.Failure(payloadResult.Message);
            }

            var payload = payloadResult.Payload;
            var transfer = EncryptTransferPayload(payload, key);
            if (HasReverseRoute(peer.Id))
            {
                return QueueReverseTransfer(peer, transfer, item.DisplayName);
            }

            var request = new OrbitLinkWireRequest { Type = "transfer", Transfer = transfer };
            var parsed = await ResolvePeerAddressAsync(peer, cancellationToken);
            if (parsed is null)
            {
                return OrbitLinkOperationResult.Failure("Cihaz adresi yerel ağda doğrulanamadı.");
            }

            var response = await SendRequestAsync(
                parsed,
                peer.Port,
                request,
                cancellationToken,
                TimeSpan.FromSeconds(6));
            if (!ValidateTransferResponse(response, key, payload.TransferId))
            {
                return OrbitLinkOperationResult.Failure("Cihaz yanıtı güvenlik doğrulamasından geçemedi.");
            }

            if (!response.Success)
            {
                return OrbitLinkOperationResult.Failure(response.Message);
            }

            peer.LastSeenUtc = DateTime.UtcNow;
            UpsertPeer(peer);
            return OrbitLinkOperationResult.Success($"{item.DisplayName}, {peer.Name} cihazına gönderildi.");
        }
        catch (OperationCanceledException)
        {
            return OrbitLinkOperationResult.Failure("Aktarım iptal edildi.");
        }
        catch (Exception ex)
        {
            _logService.Error("Orbit Link transfer failed.", ex);
            if (HasReverseRoute(peer.Id))
            {
                var payloadResult = await BuildPayloadAsync(item, cancellationToken);
                if (payloadResult.Payload is not null)
                {
                    return QueueReverseTransfer(
                        peer,
                        EncryptTransferPayload(payloadResult.Payload, key),
                        item.DisplayName);
                }
            }
            return OrbitLinkOperationResult.Failure($"Aktarım tamamlanamadı: {ex.Message}");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public void RemovePeer(string peerId)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            _state.Peers.RemoveAll(peer => string.Equals(peer.Id, peerId, StringComparison.OrdinalIgnoreCase));
            _store.Save(_state);
        }
        RaiseStateChanged();
    }

    internal static bool IsLocalNetworkAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10
                || bytes[0] == 127
                || bytes[0] == 169 && bytes[1] == 254
                || bytes[0] == 172 && bytes[1] is >= 16 and <= 31
                || bytes[0] == 192 && bytes[1] == 168
                || bytes[0] == 100 && bytes[1] is >= 64 and <= 127;
        }
        if (address.AddressFamily != AddressFamily.InterNetworkV6) return false;
        var ipv6Bytes = address.GetAddressBytes();
        return address.IsIPv6LinkLocal
            || address.IsIPv6SiteLocal
            || address.Equals(IPAddress.IPv6Loopback)
            || (ipv6Bytes[0] & 0xFE) == 0xFC;
    }

    private OrbitLinkOperationResult StartListener()
    {
        lock (_gate)
        {
            if (_listener is not null)
            {
                return OrbitLinkOperationResult.Success("Orbit Link zaten çalışıyor.");
            }

            try
            {
                _listenerCancellation = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.IPv6Any, ListenPort);
                _listener.Server.DualMode = true;
                _listener.Start(16);
                _listenerTask = ListenAsync(_listener, _listenerCancellation.Token);
                _reversePollTask = PollPeersAsync(_listenerCancellation.Token);
                _logService.Info($"Orbit Link listening on local port {ListenPort}.");
                return OrbitLinkOperationResult.Success("Orbit Link yerel ağda hazır.");
            }
            catch (Exception ex)
            {
                _listener = null;
                _listenerCancellation?.Dispose();
                _listenerCancellation = null;
                _logService.Error("Orbit Link listener failed to start.", ex);
                return OrbitLinkOperationResult.Failure($"Orbit Link başlatılamadı: {ex.Message}");
            }
        }
    }

    private async Task StopListenerAsync()
    {
        TcpListener? listener;
        CancellationTokenSource? cancellation;
        Task? task;
        Task? reversePollTask;
        lock (_gate)
        {
            listener = _listener;
            cancellation = _listenerCancellation;
            task = _listenerTask;
            reversePollTask = _reversePollTask;
            _listener = null;
            _listenerCancellation = null;
            _listenerTask = null;
            _reversePollTask = null;
        }

        cancellation?.Cancel();
        listener?.Stop();
        if (task is not null)
        {
            try { await task.WaitAsync(TimeSpan.FromSeconds(2)); } catch { }
        }
        if (reversePollTask is not null)
        {
            try { await reversePollTask.WaitAsync(TimeSpan.FromSeconds(2)); } catch { }
        }
        cancellation?.Dispose();
    }

    private async Task ListenAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                if (!_clientSlots.Wait(0))
                {
                    client.Dispose();
                    continue;
                }
                _ = HandleClientWithReleaseAsync(client, cancellationToken);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                _logService.Error("Orbit Link accept failed.", ex);
            }
        }
    }

    private async Task HandleClientWithReleaseAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try { await HandleClientAsync(client, cancellationToken); }
        finally { _clientSlots.Release(); }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = 15_000;
                client.SendTimeout = 15_000;
                using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                requestTimeout.CancelAfter(TimeSpan.FromSeconds(20));
                var requestToken = requestTimeout.Token;
                var remote = client.Client.RemoteEndPoint as IPEndPoint;
                var remoteAddress = remote is null ? null : OrbitLinkStore.NormalizeAddress(remote.Address);
                if (remoteAddress is null || !IsLocalNetworkAddress(remoteAddress))
                {
                    await WriteResponseAsync(client.GetStream(), new OrbitLinkWireResponse
                    {
                        Message = "Yalnızca yerel ağ ve VPN adreslerine izin verilir."
                    }, requestToken);
                    return;
                }

                var request = await ReadRequestAsync(client.GetStream(), requestToken);
                var response = request.Type switch
                {
                    "pair" when request.Pair is not null => HandlePairRequest(request.Pair, remoteAddress),
                    "transfer" when request.Transfer is not null => HandleTransferRequest(request.Transfer),
                    "pull" when request.Pull is not null => HandlePullRequest(request.Pull),
                    _ => new OrbitLinkWireResponse { Message = "Desteklenmeyen Orbit Link isteği." }
                };
                await WriteResponseAsync(client.GetStream(), response, requestToken);
            }
            catch (Exception ex)
            {
                _logService.Error("Orbit Link client request failed.", ex);
                try
                {
                    using var errorTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await WriteResponseAsync(client.GetStream(), new OrbitLinkWireResponse
                    {
                        Message = "Orbit Link isteği işlenemedi."
                    }, errorTimeout.Token);
                }
                catch { }
            }
        }
    }

    private OrbitLinkWireResponse HandlePairRequest(OrbitLinkPairRequest request, IPAddress remoteAddress)
    {
        PairingSession? session;
        lock (_gate)
        {
            session = _pairingSession;
            if (session is null || session.ExpiresUtc <= DateTime.UtcNow || session.Attempts >= MaxPairAttempts)
            {
                _pairingSession = null;
                return new OrbitLinkWireResponse { Message = "Eşleştirme kodu yok veya süresi dolmuş." };
            }
            session.Attempts++;
        }

        if (!IsValidDeviceId(request.DeviceId) || request.ListenPort is < 1024 or > 65535 ||
            !TryDecodeBase64(request.Nonce, 16, out _))
        {
            return new OrbitLinkWireResponse { Message = "Eşleştirme isteği geçersiz." };
        }

        var pairingKey = DerivePairingKey(session.Code);
        if (!FixedTimeHmacEquals(pairingKey, PairRequestCanonical(request), request.Proof))
        {
            return new OrbitLinkWireResponse { Message = "Eşleştirme kodu doğrulanamadı." };
        }

        lock (_gate)
        {
            if (!ReferenceEquals(_pairingSession, session))
            {
                return new OrbitLinkWireResponse { Message = "Eşleştirme kodu daha önce kullanıldı." };
            }
            _pairingSession = null;
        }

        var secret = RandomNumberGenerator.GetBytes(32);
        try
        {
            var responsePair = EncryptPairSecret(pairingKey, secret, DeviceId, DeviceName, ListenPort);
            var peer = new OrbitLinkPeer
            {
                Id = request.DeviceId.ToLowerInvariant(),
                Name = OrbitLinkStore.NormalizeName(request.DeviceName, "Eşleşen cihaz"),
                Host = remoteAddress.ToString(),
                Port = request.ListenPort,
                ProtectedKey = OrbitLinkStore.ProtectKey(secret),
                PairedUtc = DateTime.UtcNow,
                LastSeenUtc = DateTime.UtcNow
            };
            UpsertPeer(peer);
            return new OrbitLinkWireResponse
            {
                Success = true,
                Message = "Cihaz eşleştirildi.",
                Pair = responsePair
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            CryptographicOperations.ZeroMemory(pairingKey);
        }
    }

    private OrbitLinkWireResponse HandleTransferRequest(OrbitLinkEncryptedTransfer transfer)
    {
        var peer = FindPeer(transfer.SenderId);
        if (peer is null || !OrbitLinkStore.TryUnprotectKey(peer.ProtectedKey, out var key))
        {
            return new OrbitLinkWireResponse { Message = "Gönderen cihaz eşleşmemiş." };
        }

        try
        {
            if (!IsValidTransferId(transfer.TransferId)
                || !TryDecodeBase64(transfer.Nonce, 12, out var nonce)
                || !TryDecodeBase64(transfer.Tag, 16, out var tag))
            {
                return SignTransferResponse(key, transfer.TransferId, false, "Aktarım paketi geçersiz.");
            }

            byte[] ciphertext;
            try { ciphertext = Convert.FromBase64String(transfer.Ciphertext); }
            catch { return SignTransferResponse(key, transfer.TransferId, false, "Aktarım verisi çözülemedi."); }
            if (ciphertext.Length <= 0 || ciphertext.Length > MaxWireBytes)
            {
                return SignTransferResponse(key, transfer.TransferId, false, "Aktarım boyut sınırını aşıyor.");
            }

            var plaintext = new byte[ciphertext.Length];
            try
            {
                using var aes = new AesGcm(key, tag.Length);
                aes.Decrypt(nonce, ciphertext, tag, plaintext, TransferAad(transfer.SenderId, transfer.TransferId));
            }
            catch (CryptographicException)
            {
                return SignTransferResponse(key, transfer.TransferId, false, "Aktarım kimliği doğrulanamadı.");
            }

            OrbitLinkTransferPayload? payload;
            try { payload = JsonSerializer.Deserialize<OrbitLinkTransferPayload>(plaintext, JsonOptions); }
            catch { payload = null; }
            if (payload is null || !string.Equals(payload.TransferId, transfer.TransferId, StringComparison.OrdinalIgnoreCase))
            {
                return SignTransferResponse(key, transfer.TransferId, false, "Aktarım içeriği geçersiz.");
            }

            lock (_gate)
            {
                if (_recentTransfers.Contains(payload.TransferId))
                {
                    return SignTransferResponse(key, transfer.TransferId, true, "Aktarım daha önce alındı.");
                }
            }

            var imported = ImportPayload(payload, peer);
            if (imported.Item is null)
            {
                return SignTransferResponse(key, transfer.TransferId, false, imported.Message);
            }

            var receivedHandler = ItemReceived;
            if (receivedHandler is null)
            {
                DeleteImportedTemporaryItem(imported.Item);
                return SignTransferResponse(key, transfer.TransferId, false, "Alıcı Shelf hazır değil.");
            }
            var receivedArgs = new OrbitLinkItemReceivedEventArgs(peer, imported.Item);
            try { receivedHandler.Invoke(this, receivedArgs); }
            catch (Exception ex)
            {
                _logService.Error("Orbit Link receive callback failed.", ex);
                receivedArgs.Reject("Alıcı Shelf öğeyi işleyemedi.");
            }
            if (!receivedArgs.Accepted)
            {
                DeleteImportedTemporaryItem(imported.Item);
                return SignTransferResponse(key, transfer.TransferId, false, receivedArgs.RejectionMessage);
            }

            RememberTransfer(payload.TransferId);
            peer.LastSeenUtc = DateTime.UtcNow;
            UpsertPeer(peer);
            return SignTransferResponse(key, transfer.TransferId, true, "Öğe alındı.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private OrbitLinkWireResponse HandlePullRequest(OrbitLinkPullRequest request)
    {
        var peer = FindPeer(request.RequesterId);
        if (peer is null || !OrbitLinkStore.TryUnprotectKey(peer.ProtectedKey, out var key))
        {
            return new OrbitLinkWireResponse { Message = "Ters bağlantı isteyen cihaz eşleşmemiş." };
        }

        try
        {
            if (!TryDecodeBase64(request.Nonce, 16, out _)
                || !FixedTimeHmacEquals(key, PullRequestCanonical(request), request.Proof))
            {
                return new OrbitLinkWireResponse { Message = "Ters bağlantı isteği doğrulanamadı." };
            }

            OrbitLinkEncryptedTransfer? pending = null;
            var routeBecameReady = false;
            lock (_gate)
            {
                routeBecameReady = !_reverseRouteSeenUtc.TryGetValue(peer.Id, out var previousSeen)
                    || previousSeen < DateTime.UtcNow.Subtract(ReverseRouteLifetime);
                _reverseRouteSeenUtc[peer.Id] = DateTime.UtcNow;
                if (_reverseQueues.TryGetValue(peer.Id, out var queue))
                {
                    if (!string.IsNullOrWhiteSpace(request.AcknowledgedTransferId)
                        && queue.TryPeek(out var acknowledged)
                        && string.Equals(
                            acknowledged.TransferId,
                            request.AcknowledgedTransferId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        queue.Dequeue();
                        if (!request.AcknowledgedSuccess)
                        {
                            _logService.Warn(
                                $"Reverse transfer rejected by peer {LogService.SafeValue(peer.Id)}: " +
                                LogService.SafeValue(request.AcknowledgedMessage));
                        }
                    }

                    queue.TryPeek(out pending);
                    if (queue.Count == 0)
                    {
                        _reverseQueues.Remove(peer.Id);
                    }
                }
            }

            if (routeBecameReady) RaiseStateChanged();
            var transferId = pending?.TransferId ?? "";
            return new OrbitLinkWireResponse
            {
                Success = true,
                Message = pending is null ? "Ters bağlantı hazır." : "Bekleyen öğe gönderiliyor.",
                Transfer = pending,
                TransferId = transferId,
                Proof = CreateHmac(key, PullResponseCanonical(request.Nonce, transferId))
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private async Task PollPeersAsync(CancellationToken cancellationToken)
    {
        // View-model aboneliklerinin kurulmasına fırsat ver. Uygulama açılır açılmaz
        // bekleyen bir öğe gelirse Shelf alıcısı hazır olmadan tüketilmemeli.
        try { await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); }
        catch (OperationCanceledException) { return; }

        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var peer in Peers)
            {
                if (cancellationToken.IsCancellationRequested) break;
                try { await PollPeerAsync(peer, cancellationToken); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
                catch
                {
                    // Bir peer'in gelen bağlantısı kapalı olabilir. Sessizce sonraki turda yeniden denenir.
                }
            }

            ExpireReverseRoutes();

            try { await Task.Delay(ReversePollInterval, cancellationToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private void ExpireReverseRoutes()
    {
        var changed = false;
        var threshold = DateTime.UtcNow.Subtract(ReverseRouteLifetime);
        lock (_gate)
        {
            foreach (var peerId in _reverseRouteSeenUtc
                         .Where(pair => pair.Value < threshold)
                         .Select(pair => pair.Key)
                         .ToList())
            {
                _reverseRouteSeenUtc.Remove(peerId);
                changed = true;
            }
        }

        if (changed) RaiseStateChanged();
    }

    private async Task PollPeerAsync(OrbitLinkPeer peer, CancellationToken cancellationToken)
    {
        if (!OrbitLinkStore.TryUnprotectKey(peer.ProtectedKey, out var key)) return;
        try
        {
            var address = await ResolvePeerAddressAsync(peer, cancellationToken);
            if (address is null) return;

            var acknowledgedTransferId = "";
            var acknowledgedSuccess = false;
            var acknowledgedMessage = "";
            for (var index = 0; index < 3; index++)
            {
                var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
                var pull = new OrbitLinkPullRequest
                {
                    RequesterId = DeviceId,
                    Nonce = nonce,
                    AcknowledgedTransferId = acknowledgedTransferId,
                    AcknowledgedSuccess = acknowledgedSuccess,
                    AcknowledgedMessage = LogService.SafeValue(acknowledgedMessage, 240)
                };
                pull.Proof = CreateHmac(key, PullRequestCanonical(pull));
                var response = await SendRequestAsync(
                    address,
                    peer.Port,
                    new OrbitLinkWireRequest { Type = "pull", Pull = pull },
                    cancellationToken,
                    TimeSpan.FromSeconds(4));
                if (!response.Success
                    || !FixedTimeHmacEquals(
                        key,
                        PullResponseCanonical(nonce, response.TransferId),
                        response.Proof))
                {
                    return;
                }

                if (response.Transfer is null) return;

                var importResult = HandleTransferRequest(response.Transfer);
                acknowledgedTransferId = response.Transfer.TransferId;
                acknowledgedSuccess = importResult.Success;
                acknowledgedMessage = importResult.Message;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private OrbitLinkOperationResult QueueReverseTransfer(
        OrbitLinkPeer peer,
        OrbitLinkEncryptedTransfer transfer,
        string displayName)
    {
        lock (_gate)
        {
            if (!_reverseRouteSeenUtc.TryGetValue(peer.Id, out var seenUtc)
                || seenUtc < DateTime.UtcNow.Subtract(ReverseRouteLifetime))
            {
                return OrbitLinkOperationResult.Failure("Cihazın ters bağlantısı artık hazır değil.");
            }

            if (!_reverseQueues.TryGetValue(peer.Id, out var queue))
            {
                queue = new Queue<OrbitLinkEncryptedTransfer>();
                _reverseQueues[peer.Id] = queue;
            }
            if (queue.Count >= MaxQueuedReverseTransfersPerPeer)
            {
                return OrbitLinkOperationResult.Failure("Ters bağlantı aktarım sırası dolu; önce bekleyen öğelerin tamamlanmasını bekle.");
            }
            queue.Enqueue(transfer);
        }

        return OrbitLinkOperationResult.Success(
            $"{displayName}, {peer.Name} için güvenli aktarım sırasına alındı.");
    }

    private OrbitLinkEncryptedTransfer EncryptTransferPayload(OrbitLinkTransferPayload payload, byte[] key)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, TransferAad(DeviceId, payload.TransferId));
        return new OrbitLinkEncryptedTransfer
        {
            SenderId = DeviceId,
            TransferId = payload.TransferId,
            Nonce = Convert.ToBase64String(nonce),
            Ciphertext = Convert.ToBase64String(ciphertext),
            Tag = Convert.ToBase64String(tag)
        };
    }

    private (ShelfItem? Item, string Message) ImportPayload(OrbitLinkTransferPayload payload, OrbitLinkPeer peer)
    {
        if (!IsValidTransferId(payload.TransferId) || payload.Kind is not ("text" or "url" or "file" or "image"))
        {
            return (null, "Desteklenmeyen öğe türü.");
        }

        var displayName = SanitizeDisplayName(payload.DisplayName, payload.Kind == "text" ? "Paylaşılan metin" : "Paylaşılan öğe");
        if (payload.Kind is "text" or "url")
        {
            if (string.IsNullOrWhiteSpace(payload.TextContent)
                || Encoding.UTF8.GetByteCount(payload.TextContent) > 256 * 1024)
            {
                return (null, "Metin içeriği geçersiz veya çok büyük.");
            }
            return (new ShelfItem
            {
                Kind = payload.Kind,
                DisplayName = displayName,
                Source = $"Orbit Link · {peer.Name}",
                TextContent = payload.TextContent,
                SizeBytes = Encoding.UTF8.GetByteCount(payload.TextContent),
                TransferId = payload.TransferId
            }, "");
        }

        byte[] content;
        try { content = Convert.FromBase64String(payload.ContentBase64); }
        catch { return (null, "Dosya içeriği çözülemedi."); }
        if (content.Length <= 0 || content.Length > MaxTransferBytes || content.LongLength != payload.SizeBytes)
        {
            return (null, "Dosya boyutu doğrulanamadı.");
        }
        var hash = Convert.ToHexString(SHA256.HashData(content));
        if (!string.Equals(hash, payload.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            return (null, "Dosya bütünlük kontrolünden geçemedi.");
        }

        var extension = SanitizeExtension(payload.Extension);
        var path = Path.Combine(_cacheDirectory, $"orbit-link-{Guid.NewGuid():N}{extension}");
        try
        {
            File.WriteAllBytes(path, content);
            if (payload.Kind == "image" && !ImageProcessingService.TryValidateImageDimensions(path, out _))
            {
                File.Delete(path);
                return (null, "Görsel güvenlik doğrulamasından geçemedi.");
            }
        }
        catch (Exception ex)
        {
            try { File.Delete(path); } catch { }
            _logService.Error("Orbit Link received file could not be stored.", ex);
            return (null, "Alınan dosya yerel önbelleğe yazılamadı.");
        }

        return (new ShelfItem
        {
            Kind = payload.Kind,
            DisplayName = displayName,
            Source = $"Orbit Link · {peer.Name}",
            LocalPath = path,
            SizeBytes = content.LongLength,
            IsTemporary = true,
            TransferId = payload.TransferId
        }, "");
    }

    private static async Task<(OrbitLinkTransferPayload? Payload, string Message)> BuildPayloadAsync(
        ShelfItem item,
        CancellationToken cancellationToken)
    {
        var transferId = Guid.NewGuid().ToString("N");
        var kind = (item.Kind ?? "").Trim().ToLowerInvariant();
        var displayName = SanitizeDisplayName(item.DisplayName, "Paylaşılan öğe");
        if (kind is "text" or "url")
        {
            var text = item.TextContent ?? "";
            if (string.IsNullOrWhiteSpace(text) || Encoding.UTF8.GetByteCount(text) > 256 * 1024)
            {
                return (null, "Metin boş veya 256 KB sınırını aşıyor.");
            }
            return (new OrbitLinkTransferPayload
            {
                TransferId = transferId,
                Kind = kind,
                DisplayName = displayName,
                TextContent = text,
                SizeBytes = Encoding.UTF8.GetByteCount(text)
            }, "");
        }

        if (kind == "folder" || Directory.Exists(item.LocalPath))
        {
            return (null, "Klasör aktarımı bu ilk sürümde kapalı; içeriği dosya olarak gönder.");
        }
        if (kind is not ("file" or "image") || string.IsNullOrWhiteSpace(item.LocalPath) || !File.Exists(item.LocalPath))
        {
            return (null, "Gönderilecek yerel dosya bulunamadı.");
        }
        var info = new FileInfo(item.LocalPath);
        if (info.Length <= 0 || info.Length > MaxTransferBytes)
        {
            return (null, $"Orbit Link dosya sınırı {MaxTransferBytes / (1024 * 1024)} MB.");
        }

        var content = await File.ReadAllBytesAsync(item.LocalPath, cancellationToken);
        return (new OrbitLinkTransferPayload
        {
            TransferId = transferId,
            Kind = kind,
            DisplayName = displayName,
            Extension = SanitizeExtension(Path.GetExtension(item.LocalPath)),
            ContentBase64 = Convert.ToBase64String(content),
            SizeBytes = content.LongLength,
            Sha256 = Convert.ToHexString(SHA256.HashData(content))
        }, "");
    }

    private static OrbitLinkPairResponse EncryptPairSecret(
        byte[] pairingKey,
        byte[] secret,
        string deviceId,
        string deviceName,
        int listenPort)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[secret.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(pairingKey, tag.Length);
        aes.Encrypt(nonce, secret, ciphertext, tag, PairResponseAad(deviceId, deviceName, listenPort));
        return new OrbitLinkPairResponse
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            ListenPort = listenPort,
            Nonce = Convert.ToBase64String(nonce),
            Ciphertext = Convert.ToBase64String(ciphertext),
            Tag = Convert.ToBase64String(tag)
        };
    }

    private static byte[]? DecryptPairSecret(byte[] pairingKey, OrbitLinkPairResponse response)
    {
        try
        {
            if (!IsValidDeviceId(response.DeviceId)
                || response.ListenPort is < 1024 or > 65535
                || !TryDecodeBase64(response.Nonce, 12, out var nonce)
                || !TryDecodeBase64(response.Tag, 16, out var tag)
                || !TryDecodeBase64(response.Ciphertext, 32, out var ciphertext))
            {
                return null;
            }
            var secret = new byte[32];
            using var aes = new AesGcm(pairingKey, tag.Length);
            aes.Decrypt(
                nonce,
                ciphertext,
                tag,
                secret,
                PairResponseAad(response.DeviceId, response.DeviceName, response.ListenPort));
            return secret;
        }
        catch (CryptographicException) { return null; }
        finally { CryptographicOperations.ZeroMemory(pairingKey); }
    }

    private static OrbitLinkWireResponse SignTransferResponse(byte[] key, string transferId, bool success, string message)
    {
        var safeTransferId = IsValidTransferId(transferId) ? transferId.ToLowerInvariant() : "invalid";
        var safeMessage = LogService.SafeValue(message, 240);
        return new OrbitLinkWireResponse
        {
            Success = success,
            Message = safeMessage,
            TransferId = safeTransferId,
            Proof = CreateHmac(key, TransferResponseCanonical(safeTransferId, success, safeMessage))
        };
    }

    private static bool ValidateTransferResponse(OrbitLinkWireResponse response, byte[] key, string transferId) =>
        string.Equals(response.TransferId, transferId, StringComparison.OrdinalIgnoreCase)
        && FixedTimeHmacEquals(
            key,
            TransferResponseCanonical(transferId.ToLowerInvariant(), response.Success, response.Message),
            response.Proof);

    private async Task<OrbitLinkWireResponse> SendRequestAsync(
        IPAddress address,
        int port,
        OrbitLinkWireRequest request,
        CancellationToken cancellationToken,
        TimeSpan? requestTimeout = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(requestTimeout ?? TimeSpan.FromSeconds(30));
        using var client = new TcpClient(address.AddressFamily);
        await client.ConnectAsync(address, port, timeout.Token);
        using var stream = client.GetStream();
        await WriteMessageAsync(stream, request, timeout.Token);
        return await ReadResponseAsync(stream, timeout.Token);
    }

    private static async Task WriteMessageAsync<T>(NetworkStream stream, T value, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        if (payload.Length <= 0 || payload.Length > MaxWireBytes)
        {
            throw new InvalidDataException("Orbit Link mesajı boyut sınırını aşıyor.");
        }
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);
        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static Task WriteResponseAsync(NetworkStream stream, OrbitLinkWireResponse response, CancellationToken cancellationToken) =>
        WriteMessageAsync(stream, response, cancellationToken);

    private static async Task<OrbitLinkWireRequest> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken) =>
        JsonSerializer.Deserialize<OrbitLinkWireRequest>(await ReadMessageBytesAsync(stream, cancellationToken), JsonOptions)
        ?? throw new InvalidDataException("Orbit Link isteği boş.");

    private static async Task<OrbitLinkWireResponse> ReadResponseAsync(NetworkStream stream, CancellationToken cancellationToken) =>
        JsonSerializer.Deserialize<OrbitLinkWireResponse>(await ReadMessageBytesAsync(stream, cancellationToken), JsonOptions)
        ?? throw new InvalidDataException("Orbit Link yanıtı boş.");

    private static async Task<byte[]> ReadMessageBytesAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header, cancellationToken);
        var length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length <= 0 || length > MaxWireBytes)
        {
            throw new InvalidDataException("Orbit Link mesaj boyutu geçersiz.");
        }
        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload, cancellationToken);
        return payload;
    }

    private static async Task<(bool Succeeded, string Message, IPAddress? Address, int Port)> ParseAndValidateEndpointAsync(
        string endpoint,
        CancellationToken cancellationToken)
    {
        var value = (endpoint ?? "").Trim();
        if (value.Length == 0)
        {
            return (false, "Cihaz adresini IP:port biçiminde gir.", null, 0);
        }
        string host;
        int port = DefaultPort;
        if (value.StartsWith('['))
        {
            var end = value.IndexOf(']');
            if (end <= 1) return (false, "IPv6 adresi geçersiz.", null, 0);
            host = value[1..end];
            if (value.Length > end + 1 && (!value[(end + 1)..].StartsWith(':') || !int.TryParse(value[(end + 2)..], out port)))
            {
                return (false, "Port değeri geçersiz.", null, 0);
            }
        }
        else
        {
            var separator = value.LastIndexOf(':');
            if (separator > 0 && value.Count(character => character == ':') == 1)
            {
                host = value[..separator];
                if (!int.TryParse(value[(separator + 1)..], out port))
                {
                    return (false, "Port değeri geçersiz.", null, 0);
                }
            }
            else host = value;
        }
        host = OrbitLinkStore.NormalizeHost(host);
        if (host.Length == 0 || port is < 1024 or > 65535)
        {
            return (false, "Cihaz adresi veya port geçersiz.", null, 0);
        }
        IPAddress[] addresses;
        try { addresses = await Dns.GetHostAddressesAsync(host, cancellationToken); }
        catch { return (false, "Cihaz adresi çözümlenemedi.", null, 0); }
        var address = addresses
            .Select(OrbitLinkStore.NormalizeAddress)
            .FirstOrDefault(IsLocalNetworkAddress);
        return address is null
            ? (false, "Yalnızca yerel ağ, VPN ve localhost adresleri kullanılabilir.", null, 0)
            : (true, "", address, port);
    }

    private static async Task<IPAddress?> ResolvePeerAddressAsync(OrbitLinkPeer peer, CancellationToken cancellationToken)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(peer.Host, cancellationToken);
            return addresses
                .Select(OrbitLinkStore.NormalizeAddress)
                .FirstOrDefault(IsLocalNetworkAddress);
        }
        catch { return null; }
    }

    private void UpsertPeer(OrbitLinkPeer peer)
    {
        lock (_gate)
        {
            _state.Peers.RemoveAll(item => string.Equals(item.Id, peer.Id, StringComparison.OrdinalIgnoreCase));
            _state.Peers.Add(ClonePeer(peer));
            _state.Peers = _state.Peers.OrderByDescending(item => item.LastSeenUtc).Take(16).ToList();
            _store.Save(_state);
        }
        RaiseStateChanged();
    }

    private OrbitLinkPeer? FindPeer(string peerId)
    {
        lock (_gate)
        {
            var peer = _state.Peers.FirstOrDefault(item => string.Equals(item.Id, peerId, StringComparison.OrdinalIgnoreCase));
            return peer is null ? null : ClonePeer(peer);
        }
    }

    private void RememberTransfer(string transferId)
    {
        lock (_gate)
        {
            if (!_recentTransfers.Add(transferId)) return;
            _recentTransferOrder.Enqueue(transferId);
            while (_recentTransferOrder.Count > 256)
            {
                _recentTransfers.Remove(_recentTransferOrder.Dequeue());
            }
        }
    }

    private static void DeleteImportedTemporaryItem(ShelfItem item)
    {
        if (!item.IsTemporary || string.IsNullOrWhiteSpace(item.LocalPath)) return;
        try { File.Delete(item.LocalPath); } catch { }
    }

    private void RaiseStateChanged()
    {
        try { StateChanged?.Invoke(this, EventArgs.Empty); }
        catch (Exception ex) { _logService.Error("Orbit Link state callback failed.", ex); }
    }

    private static IEnumerable<IPAddress> GetLocalAddresses()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(network => network.OperationalStatus == OperationalStatus.Up
                    && network.NetworkInterfaceType is not NetworkInterfaceType.Loopback)
                .SelectMany(network => network.GetIPProperties().UnicastAddresses)
                .Select(address => address.Address)
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork && IsLocalNetworkAddress(address))
                .Distinct()
                .ToList();
        }
        catch { return []; }
    }

    private static byte[] DerivePairingKey(string code) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"ActionOrbit.Pair.v1|{NormalizePairingCode(code)}"));

    private static string PairRequestCanonical(OrbitLinkPairRequest request) =>
        $"pair-v1|{request.DeviceId.ToLowerInvariant()}|{request.DeviceName}|{request.ListenPort}|{request.Nonce}";

    private static string TransferResponseCanonical(string transferId, bool success, string message) =>
        $"ack-v1|{transferId.ToLowerInvariant()}|{(success ? 1 : 0)}|{message}";

    private static string PullRequestCanonical(OrbitLinkPullRequest request) =>
        $"pull-v1|{request.RequesterId?.ToLowerInvariant() ?? ""}|{request.Nonce ?? ""}|" +
        $"{request.AcknowledgedTransferId?.ToLowerInvariant() ?? ""}|{(request.AcknowledgedSuccess ? 1 : 0)}|" +
        (request.AcknowledgedMessage ?? "");

    private static string PullResponseCanonical(string requestNonce, string transferId) =>
        $"pull-response-v1|{requestNonce ?? ""}|{transferId?.ToLowerInvariant() ?? ""}";

    private static byte[] TransferAad(string senderId, string transferId) =>
        Encoding.UTF8.GetBytes($"ActionOrbit.Transfer.v1|{senderId.ToLowerInvariant()}|{transferId.ToLowerInvariant()}");

    private static byte[] PairResponseAad(string deviceId, string deviceName, int listenPort) =>
        Encoding.UTF8.GetBytes($"ActionOrbit.Pair.Response.v1|{deviceId.ToLowerInvariant()}|{deviceName}|{listenPort}");

    private static string CreateHmac(byte[] key, string value)
    {
        using var hmac = new HMACSHA256(key);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static bool FixedTimeHmacEquals(byte[] key, string value, string candidate)
    {
        try
        {
            var expected = Convert.FromBase64String(CreateHmac(key, value));
            var actual = Convert.FromBase64String(candidate);
            return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch { return false; }
    }

    private static string CreatePairingCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(20);
        var characters = bytes.Select(value => alphabet[value % alphabet.Length]).ToArray();
        return string.Join("-", Enumerable.Range(0, 5).Select(index => new string(characters, index * 4, 4)));
    }

    private static string NormalizePairingCode(string? code) =>
        new string((code ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static bool TryDecodeBase64(string value, int exactLength, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value);
            return bytes.Length == exactLength;
        }
        catch
        {
            bytes = [];
            return false;
        }
    }

    private static string SanitizeDisplayName(string? value, string fallback)
    {
        var fileName = Path.GetFileName(value ?? "");
        var clean = new string(fileName.Where(character => !char.IsControl(character)).Take(160).ToArray()).Trim();
        return clean.Length == 0 ? fallback : clean;
    }

    private static string SanitizeExtension(string? value)
    {
        var extension = (value ?? "").Trim().ToLowerInvariant();
        return extension.Length is > 1 and <= 12
            && extension[0] == '.'
            && extension[1..].All(char.IsLetterOrDigit)
            ? extension
            : "";
    }

    private static bool IsValidDeviceId(string? value) =>
        value?.Length == 32 && value.All(Uri.IsHexDigit);

    private static bool IsValidTransferId(string? value) => IsValidDeviceId(value);

    private static OrbitLinkPeer ClonePeer(OrbitLinkPeer peer) => new()
    {
        Id = peer.Id,
        Name = peer.Name,
        Host = peer.Host,
        Port = peer.Port,
        ProtectedKey = peer.ProtectedKey,
        PairedUtc = peer.PairedUtc,
        LastSeenUtc = peer.LastSeenUtc
    };

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Tray'deki Çıkış komutu WPF UI iş parçacığında çalışır. Burada ağ
        // görevlerini senkron beklemek menünün birkaç saniye donmasına, bir
        // aktarım UI teslimatındaysa karşılıklı beklemeye neden olabilir.
        // StopListenerAsync iptal sinyalini ve listener.Stop çağrısını ilk
        // await'ten önce yapar; kalan temizlik arka planda tamamlanabilir.
        _ = StopListenerAsync();
    }

    private sealed class PairingSession(string code, DateTime expiresUtc)
    {
        public string Code { get; } = code;
        public DateTime ExpiresUtc { get; } = expiresUtc;
        public int Attempts { get; set; }
    }
}
