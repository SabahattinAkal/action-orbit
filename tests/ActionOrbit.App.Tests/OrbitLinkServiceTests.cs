using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class OrbitLinkServiceTests
{
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("10.20.30.40", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.254", true)]
    [InlineData("192.168.1.20", true)]
    [InlineData("169.254.1.2", true)]
    [InlineData("100.64.0.1", true)]
    [InlineData("100.127.255.254", true)]
    [InlineData("fd7a:115c:a1e0::1", true)]
    [InlineData("1.1.1.1", false)]
    [InlineData("8.8.8.8", false)]
    public void LocalNetworkPolicy_AllowsOnlyPrivateAndLocalAddresses(string value, bool expected) =>
        Assert.Equal(expected, OrbitLinkService.IsLocalNetworkAddress(IPAddress.Parse(value)));

    [Fact]
    public void Store_NormalizesIpv4MappedTailscaleAddress()
    {
        Assert.Equal(
            "100.64.0.10",
            OrbitLinkStore.NormalizeHost("::ffff:100.64.0.10"));
    }

    [Fact]
    public void Store_ProtectsPeerKeyForCurrentWindowsUser()
    {
        var key = Enumerable.Range(0, 32).Select(index => (byte)index).ToArray();

        var protectedKey = OrbitLinkStore.ProtectKey(key);
        var succeeded = OrbitLinkStore.TryUnprotectKey(protectedKey, out var unprotected);

        Assert.True(succeeded);
        Assert.Equal(key, unprotected);
        Assert.DoesNotContain(Convert.ToBase64String(key), protectedKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PairAndTransfer_TextArrivesAsTemporarySharedData()
    {
        using var receiverDirectory = new TemporaryDirectory();
        using var senderDirectory = new TemporaryDirectory();
        var receiverPort = ReservePort();
        var senderPort = ReservePort(receiverPort);
        WriteState(receiverDirectory.Path, receiverPort, "Alıcı PC");
        WriteState(senderDirectory.Path, senderPort, "RDP PC");
        using var receiver = new OrbitLinkService(receiverDirectory.Path, new LogService(receiverDirectory.Path));
        using var sender = new OrbitLinkService(senderDirectory.Path, new LogService(senderDirectory.Path));
        var received = new TaskCompletionSource<OrbitLinkItemReceivedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        var returned = new TaskCompletionSource<OrbitLinkItemReceivedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        receiver.ItemReceived += (_, args) => received.TrySetResult(args);
        sender.ItemReceived += (_, args) => returned.TrySetResult(args);

        Assert.True((await receiver.SetEnabledAsync(true)).Succeeded);
        Assert.True((await sender.SetEnabledAsync(true)).Succeeded);
        var offer = receiver.BeginPairing();
        var pairResult = await sender.PairAsync(
            $"127.0.0.1:{receiverPort}",
            offer.Code,
            TestContext.Current.CancellationToken);
        Assert.True(pairResult.Succeeded, pairResult.Message);
        var peer = Assert.Single(sender.Peers);

        var sendResult = await sender.SendItemAsync(peer.Id, new ShelfItem
        {
            Kind = "text",
            DisplayName = "RDP notu",
            TextContent = "Orbit Link güvenli aktarım testi"
        }, TestContext.Current.CancellationToken);
        Assert.True(sendResult.Succeeded, sendResult.Message);

        var args = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal("RDP PC", args.Peer.Name);
        Assert.Equal("text", args.Item.Kind);
        Assert.Equal("RDP notu", args.Item.DisplayName);
        Assert.Equal("Orbit Link güvenli aktarım testi", args.Item.TextContent);
        Assert.NotEmpty(args.Item.TransferId);
        Assert.Empty(args.Item.LocalPath);

        var receiverPeer = Assert.Single(receiver.Peers);
        Assert.Equal("127.0.0.1", receiverPeer.Host);
        var returnResult = await receiver.SendItemAsync(receiverPeer.Id, new ShelfItem
        {
            Kind = "text",
            DisplayName = "Ana PC yanıtı",
            TextContent = "Çift yönlü aktarım"
        }, TestContext.Current.CancellationToken);
        Assert.True(returnResult.Succeeded, returnResult.Message);
        var returnedArgs = await returned.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);
        Assert.Equal("Çift yönlü aktarım", returnedArgs.Item.TextContent);
    }

    [Fact]
    public async Task ReverseRoute_DeliversWhenOnlyOfficeCanOpenTheConnection()
    {
        using var homeDirectory = new TemporaryDirectory();
        using var officeDirectory = new TemporaryDirectory();
        var homePort = ReservePort();
        var officePort = ReservePort(homePort);
        WriteState(homeDirectory.Path, homePort, "Ev PC");
        WriteState(officeDirectory.Path, officePort, "Ofis PC");
        using var home = new OrbitLinkService(homeDirectory.Path, new LogService(homeDirectory.Path));
        using var office = new OrbitLinkService(officeDirectory.Path, new LogService(officeDirectory.Path));
        var receivedAtOffice = new TaskCompletionSource<OrbitLinkItemReceivedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        office.ItemReceived += (_, args) => receivedAtOffice.TrySetResult(args);

        Assert.True((await home.SetEnabledAsync(true)).Succeeded);
        Assert.True((await office.SetEnabledAsync(true)).Succeeded);
        var offer = home.BeginPairing();
        var pairResult = await office.PairAsync(
            $"127.0.0.1:{homePort}",
            offer.Code,
            TestContext.Current.CancellationToken);
        Assert.True(pairResult.Succeeded, pairResult.Message);

        // Ofis bilgisayarı ev bilgisayarına dışarı yönlü sorgu açar. Ev tarafı,
        // ofisin gelen portuna bağlanmadan bu sorgunun dönüşünü aktarım kanalı yapar.
        Assert.True(await WaitUntilAsync(
            () => home.HasReverseRoute(office.DeviceId),
            TimeSpan.FromSeconds(8),
            TestContext.Current.CancellationToken));

        var officePeer = Assert.Single(home.Peers);
        var sendResult = await home.SendItemAsync(officePeer.Id, new ShelfItem
        {
            Kind = "text",
            DisplayName = "Evden ofise",
            TextContent = "Gelen bağlantı açmadan teslim"
        }, TestContext.Current.CancellationToken);

        Assert.True(sendResult.Succeeded, sendResult.Message);
        Assert.Contains("aktarım sırasına", sendResult.Message, StringComparison.OrdinalIgnoreCase);
        var received = await receivedAtOffice.Task.WaitAsync(
            TimeSpan.FromSeconds(8),
            TestContext.Current.CancellationToken);
        Assert.Equal("Ev PC", received.Peer.Name);
        Assert.Equal("Gelen bağlantı açmadan teslim", received.Item.TextContent);
    }

    [Fact]
    public async Task OfflineTransfer_PersistsEncryptedAndResumesAfterRestart()
    {
        using var senderDirectory = new TemporaryDirectory();
        using var receiverDirectory = new TemporaryDirectory();
        var ports = ReservePorts(2);
        var senderId = Guid.NewGuid().ToString("N");
        var receiverId = Guid.NewGuid().ToString("N");
        var key = RandomNumberGenerator.GetBytes(32);
        WriteState(senderDirectory.Path, ports[0], "Gönderen", enabled: true,
        [
            CreatePeer(receiverId, "Alıcı", ports[1], key)
        ], senderId);
        WriteState(receiverDirectory.Path, ports[1], "Alıcı", enabled: true,
        [
            CreatePeer(senderId, "Gönderen", ports[0], key)
        ], receiverId);

        const string privateText = "yeniden başlatmada korunacak özel içerik";
        string transferId;
        using (var firstSender = new OrbitLinkService(
                   senderDirectory.Path,
                   new LogService(senderDirectory.Path)))
        {
            var result = await firstSender.SendItemAsync(receiverId, new ShelfItem
            {
                Kind = "text",
                DisplayName = "Bekleyen not",
                TextContent = privateText
            }, TestContext.Current.CancellationToken);
            Assert.True(result.Succeeded, result.Message);
            Assert.Equal(OrbitLinkTransferState.Queued, result.TransferState);
            transferId = result.TransferId;
            Assert.Single(firstSender.PendingTransfers);
        }

        var queuePath = Path.Combine(senderDirectory.Path, "orbit-link-queue.json");
        var queueJson = await File.ReadAllTextAsync(queuePath, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(privateText, queueJson, StringComparison.Ordinal);
        Assert.DoesNotContain(Convert.ToBase64String(key), queueJson, StringComparison.Ordinal);

        using var receiver = new OrbitLinkService(receiverDirectory.Path, new LogService(receiverDirectory.Path));
        var received = new TaskCompletionSource<OrbitLinkItemReceivedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        receiver.ItemReceived += (_, args) => received.TrySetResult(args);
        Assert.True(receiver.StartIfEnabled().Succeeded);

        using var restartedSender = new OrbitLinkService(senderDirectory.Path, new LogService(senderDirectory.Path));
        Assert.Equal(transferId, Assert.Single(restartedSender.PendingTransfers).TransferId);
        Assert.True(restartedSender.StartIfEnabled().Succeeded);

        var delivered = await received.Task.WaitAsync(
            TimeSpan.FromSeconds(12),
            TestContext.Current.CancellationToken);
        Assert.Equal(privateText, delivered.Item.TextContent);
        Assert.True(await WaitUntilAsync(
            () => restartedSender.PendingTransfers.Count == 0,
            TimeSpan.FromSeconds(8),
            TestContext.Current.CancellationToken));
        Assert.False(File.Exists(queuePath));
    }

    [Fact]
    public async Task OfflineTransferQueue_IsBoundedAndCanBeCanceled()
    {
        using var directory = new TemporaryDirectory();
        var peerId = Guid.NewGuid().ToString("N");
        var key = RandomNumberGenerator.GetBytes(32);
        WriteState(directory.Path, ReservePort(), "Gönderen", enabled: true,
        [
            CreatePeer(peerId, "Kapalı cihaz", ReservePort(), key)
        ]);
        using var service = new OrbitLinkService(directory.Path, new LogService(directory.Path));

        var first = await service.SendItemAsync(peerId, new ShelfItem
        {
            Kind = "text",
            DisplayName = "Bir",
            TextContent = "bir"
        }, TestContext.Current.CancellationToken);
        var second = await service.SendItemAsync(peerId, new ShelfItem
        {
            Kind = "text",
            DisplayName = "İki",
            TextContent = "iki"
        }, TestContext.Current.CancellationToken);
        var third = await service.SendItemAsync(peerId, new ShelfItem
        {
            Kind = "text",
            DisplayName = "Üç",
            TextContent = "üç"
        }, TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.False(third.Succeeded);
        Assert.Equal(OrbitLinkQueueStore.MaxQueuedTransfers, service.PendingTransfers.Count);
        Assert.Equal(OrbitLinkTransferState.Canceled, service.CancelTransfer(first.TransferId).TransferState);
        Assert.Single(service.PendingTransfers);
        Assert.Equal(OrbitLinkTransferState.Canceled, service.CancelTransfer(second.TransferId).TransferState);
        Assert.Empty(service.PendingTransfers);
        Assert.False(File.Exists(Path.Combine(directory.Path, "orbit-link-queue.json")));
    }

    [Fact]
    public async Task ReceiverWithoutShelf_RejectsTransferAndClearsSenderQueue()
    {
        using var receiverDirectory = new TemporaryDirectory();
        using var senderDirectory = new TemporaryDirectory();
        var ports = ReservePorts(2);
        WriteState(receiverDirectory.Path, ports[0], "Alıcı");
        WriteState(senderDirectory.Path, ports[1], "Gönderen");
        using var receiver = new OrbitLinkService(receiverDirectory.Path, new LogService(receiverDirectory.Path));
        using var sender = new OrbitLinkService(senderDirectory.Path, new LogService(senderDirectory.Path));

        Assert.True((await receiver.SetEnabledAsync(true)).Succeeded);
        Assert.True((await sender.SetEnabledAsync(true)).Succeeded);
        var offer = receiver.BeginPairing();
        Assert.True((await sender.PairAsync(
            $"127.0.0.1:{ports[0]}",
            offer.Code,
            TestContext.Current.CancellationToken)).Succeeded);

        var result = await sender.SendItemAsync(Assert.Single(sender.Peers).Id, new ShelfItem
        {
            Kind = "url",
            DisplayName = "Örnek bağlantı",
            TextContent = "https://example.com/orbit-link-test"
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(OrbitLinkTransferState.Failed, result.TransferState);
        Assert.Contains("kabul etmedi", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(sender.PendingTransfers);
    }

    [Fact]
    public async Task ReplayedEncryptedTransfer_IsAcceptedWithoutAddingASecondCopy()
    {
        using var senderDirectory = new TemporaryDirectory();
        using var receiverDirectory = new TemporaryDirectory();
        var ports = ReservePorts(2);
        var senderId = Guid.NewGuid().ToString("N");
        var receiverId = Guid.NewGuid().ToString("N");
        var key = RandomNumberGenerator.GetBytes(32);
        WriteState(senderDirectory.Path, ports[0], "Gönderen", enabled: true,
        [
            CreatePeer(receiverId, "Alıcı", ports[1], key)
        ], senderId);
        WriteState(receiverDirectory.Path, ports[1], "Alıcı", enabled: true,
        [
            CreatePeer(senderId, "Gönderen", ports[0], key)
        ], receiverId);
        using var sender = new OrbitLinkService(senderDirectory.Path, new LogService(senderDirectory.Path));
        var queuedResult = await sender.SendItemAsync(receiverId, new ShelfItem
        {
            Kind = "text",
            DisplayName = "Tek kopya",
            TextContent = "Aynı şifreli paket iki kez"
        }, TestContext.Current.CancellationToken);
        Assert.True(queuedResult.Succeeded);

        var queued = Assert.Single(new OrbitLinkQueueStore(
            senderDirectory.Path,
            new LogService(senderDirectory.Path)).Load(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { receiverId }));
        using var receiver = new OrbitLinkService(receiverDirectory.Path, new LogService(receiverDirectory.Path));
        var receivedCount = 0;
        receiver.ItemReceived += (_, _) => receivedCount++;

        var first = InvokeTransfer(receiver, queued.Transfer);
        var replay = InvokeTransfer(receiver, queued.Transfer);

        Assert.True(first.Success, first.Message);
        Assert.True(replay.Success, replay.Message);
        Assert.Contains("daha önce", replay.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, receivedCount);
    }

    [Fact]
    public async Task Dispose_DoesNotBlockWhileReversePollWaitsForPeerResponse()
    {
        using var directory = new TemporaryDirectory();
        var key = RandomNumberGenerator.GetBytes(32);
        using var unresponsivePeer = new TcpListener(IPAddress.Loopback, 0);
        unresponsivePeer.Start();
        var peerPort = ((IPEndPoint)unresponsivePeer.LocalEndpoint).Port;
        var ownPort = ReservePort(peerPort);
        WriteState(directory.Path, ownPort, "Kapanan PC", enabled: true,
        [
            new OrbitLinkPeer
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Yanıt vermeyen cihaz",
                Host = "127.0.0.1",
                Port = peerPort,
                ProtectedKey = OrbitLinkStore.ProtectKey(key)
            }
        ]);
        var service = new OrbitLinkService(directory.Path, new LogService(directory.Path));
        Assert.True(service.StartIfEnabled().Succeeded);

        using var acceptedClient = await unresponsivePeer.AcceptTcpClientAsync(
                TestContext.Current.CancellationToken)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var started = System.Diagnostics.Stopwatch.StartNew();
        service.Dispose();
        started.Stop();

        Assert.True(
            started.Elapsed < TimeSpan.FromMilliseconds(300),
            $"Orbit Link kapanışı UI'ı {started.Elapsed.TotalMilliseconds:0} ms bekletti.");
    }

    [Fact]
    public async Task PairAndTransfer_FileIsCopiedIntoReceiversCache()
    {
        using var receiverDirectory = new TemporaryDirectory();
        using var senderDirectory = new TemporaryDirectory();
        var receiverPort = ReservePort();
        var senderPort = ReservePort(receiverPort);
        WriteState(receiverDirectory.Path, receiverPort, "Ana PC");
        WriteState(senderDirectory.Path, senderPort, "Uzak PC");
        var sourcePath = Path.Combine(senderDirectory.Path, "örnek.txt");
        await File.WriteAllTextAsync(sourcePath, "dosya içeriği", TestContext.Current.CancellationToken);
        using var receiver = new OrbitLinkService(receiverDirectory.Path, new LogService(receiverDirectory.Path));
        using var sender = new OrbitLinkService(senderDirectory.Path, new LogService(senderDirectory.Path));
        var received = new TaskCompletionSource<OrbitLinkItemReceivedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        receiver.ItemReceived += (_, args) => received.TrySetResult(args);

        Assert.True((await receiver.SetEnabledAsync(true)).Succeeded);
        Assert.True((await sender.SetEnabledAsync(true)).Succeeded);
        var offer = receiver.BeginPairing();
        Assert.True((await sender.PairAsync(
            $"127.0.0.1:{receiverPort}",
            offer.Code,
            TestContext.Current.CancellationToken)).Succeeded);

        var send = await sender.SendItemAsync(Assert.Single(sender.Peers).Id, new ShelfItem
        {
            Kind = "file",
            DisplayName = "örnek.txt",
            LocalPath = sourcePath,
            SizeBytes = new FileInfo(sourcePath).Length
        }, TestContext.Current.CancellationToken);
        Assert.True(send.Succeeded, send.Message);

        var item = (await received.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken)).Item;
        Assert.True(item.IsTemporary);
        Assert.StartsWith(Path.Combine(receiverDirectory.Path, "shelf-cache"), item.LocalPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("dosya içeriği", await File.ReadAllTextAsync(item.LocalPath, TestContext.Current.CancellationToken));
        Assert.NotEqual(sourcePath, item.LocalPath);
    }

    [Fact]
    public async Task PairingCode_CanBeUsedOnlyOnce()
    {
        using var receiverDirectory = new TemporaryDirectory();
        using var firstDirectory = new TemporaryDirectory();
        using var secondDirectory = new TemporaryDirectory();
        var ports = ReservePorts(3);
        WriteState(receiverDirectory.Path, ports[0], "Alıcı");
        WriteState(firstDirectory.Path, ports[1], "Birinci");
        WriteState(secondDirectory.Path, ports[2], "İkinci");
        using var receiver = new OrbitLinkService(receiverDirectory.Path, new LogService(receiverDirectory.Path));
        using var first = new OrbitLinkService(firstDirectory.Path, new LogService(firstDirectory.Path));
        using var second = new OrbitLinkService(secondDirectory.Path, new LogService(secondDirectory.Path));
        Assert.True((await receiver.SetEnabledAsync(true)).Succeeded);
        Assert.True((await first.SetEnabledAsync(true)).Succeeded);
        Assert.True((await second.SetEnabledAsync(true)).Succeeded);
        var offer = receiver.BeginPairing();

        var firstResult = await first.PairAsync(
            $"127.0.0.1:{ports[0]}",
            offer.Code,
            TestContext.Current.CancellationToken);
        var secondResult = await second.PairAsync(
            $"127.0.0.1:{ports[0]}",
            offer.Code,
            TestContext.Current.CancellationToken);

        Assert.True(firstResult.Succeeded, firstResult.Message);
        Assert.False(secondResult.Succeeded);
        Assert.Single(receiver.Peers);
    }

    [Fact]
    public async Task SendItem_RejectsFoldersAndOversizedFilesBeforeNetworkTransfer()
    {
        using var temp = new TemporaryDirectory();
        var key = RandomNumberGenerator.GetBytes(32);
        var peerId = Guid.NewGuid().ToString("N");
        WriteState(temp.Path, ReservePort(), "Test PC", enabled: true,
        [
            new OrbitLinkPeer
            {
                Id = peerId,
                Name = "Hedef",
                Host = "127.0.0.1",
                Port = ReservePort(),
                ProtectedKey = OrbitLinkStore.ProtectKey(key)
            }
        ]);
        var service = new OrbitLinkService(temp.Path, new LogService(temp.Path));

        var folderResult = await service.SendItemAsync(peerId, new ShelfItem
        {
            Kind = "folder",
            LocalPath = temp.Path
        }, TestContext.Current.CancellationToken);
        var largePath = Path.Combine(temp.Path, "large.bin");
        await using (var stream = new FileStream(largePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.SetLength(OrbitLinkService.MaxTransferBytes + 1);
        }
        var largeResult = await service.SendItemAsync(peerId, new ShelfItem
        {
            Kind = "file",
            DisplayName = "large.bin",
            LocalPath = largePath
        }, TestContext.Current.CancellationToken);

        Assert.False(folderResult.Succeeded);
        Assert.Contains("Klasör", folderResult.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(largeResult.Succeeded);
        Assert.Contains("25 MB", largeResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteState(
        string directory,
        int port,
        string deviceName,
        bool enabled = false,
        List<OrbitLinkPeer>? peers = null,
        string? deviceId = null)
    {
        var state = new OrbitLinkState
        {
            DeviceId = deviceId ?? Guid.NewGuid().ToString("N"),
            DeviceName = deviceName,
            ListenPort = port,
            Enabled = enabled,
            Peers = peers ?? []
        };
        File.WriteAllText(
            Path.Combine(directory, "orbit-link.json"),
            JsonSerializer.Serialize(state, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }

    private static OrbitLinkPeer CreatePeer(
        string id,
        string name,
        int port,
        byte[] key) => new()
    {
        Id = id,
        Name = name,
        Host = "127.0.0.1",
        Port = port,
        ProtectedKey = OrbitLinkStore.ProtectKey(key),
        PairedUtc = DateTime.UtcNow
    };

    private static OrbitLinkWireResponse InvokeTransfer(
        OrbitLinkService service,
        OrbitLinkEncryptedTransfer transfer)
    {
        var method = typeof(OrbitLinkService).GetMethod(
            "HandleTransferRequest",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<OrbitLinkWireResponse>(method.Invoke(service, [transfer]));
    }

    private static int ReservePort(int excluded = 0)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            if (port != excluded) return port;
        }
        throw new InvalidOperationException("Test için boş port bulunamadı.");
    }

    private static int[] ReservePorts(int count)
    {
        var listeners = new List<TcpListener>();
        try
        {
            for (var index = 0; index < count; index++)
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                listeners.Add(listener);
            }
            return listeners.Select(listener => ((IPEndPoint)listener.LocalEndpoint).Port).ToArray();
        }
        finally
        {
            foreach (var listener in listeners) listener.Stop();
        }
    }

    private static async Task<bool> WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            await Task.Delay(100, cancellationToken);
        }
        return condition();
    }
}
