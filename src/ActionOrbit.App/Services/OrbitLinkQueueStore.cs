using System.Text.Json;

namespace ActionOrbit.App.Services;

internal sealed class OrbitLinkQueuedTransfer
{
    public string PeerId { get; set; } = "";
    public string ShelfItemId { get; set; } = "";
    public OrbitLinkEncryptedTransfer Transfer { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime NextAttemptUtc { get; set; } = DateTime.UtcNow;
    public int AttemptCount { get; set; }
}

internal sealed class OrbitLinkQueueStore
{
    internal const int MaxQueuedTransfers = 2;
    internal static readonly TimeSpan TransferLifetime = TimeSpan.FromHours(24);
    private const long MaxStateBytes = 100L * 1024 * 1024;
    private readonly string _statePath;
    private readonly LogService _logService;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        MaxDepth = 12
    };

    public OrbitLinkQueueStore(string appDirectory, LogService logService)
    {
        _statePath = Path.Combine(appDirectory, "orbit-link-queue.json");
        _logService = logService;
    }

    public IReadOnlyList<OrbitLinkQueuedTransfer> Load(IReadOnlySet<string> peerIds)
    {
        if (!File.Exists(_statePath)) return [];

        try
        {
            if (new FileInfo(_statePath).Length is <= 0 or > MaxStateBytes)
            {
                throw new InvalidDataException("Orbit Link kuyruk dosyası boyut sınırını aşıyor.");
            }

            var document = JsonSerializer.Deserialize<QueueDocument>(
                File.ReadAllText(_statePath),
                _options) ?? new QueueDocument();
            var expiry = DateTime.UtcNow.Subtract(TransferLifetime);
            var retained = document.Transfers
                .Where(item => item is not null
                    && peerIds.Contains(item.PeerId)
                    && item.CreatedUtc >= expiry
                    && item.CreatedUtc <= DateTime.UtcNow.AddMinutes(5)
                    && item.Transfer is not null
                    && IsHexId(item.Transfer.TransferId)
                    && IsHexId(item.Transfer.SenderId)
                    && item.Transfer.Ciphertext.Length <= 64 * 1024 * 1024
                    && item.Transfer.Nonce.Length <= 64
                    && item.Transfer.Tag.Length <= 64)
                .OrderBy(item => item.CreatedUtc)
                .Take(MaxQueuedTransfers)
                .ToList();
            if (retained.Count != document.Transfers.Count)
            {
                Save(retained);
            }
            return retained;
        }
        catch (Exception ex)
        {
            _logService.Error("Orbit Link queue load failed.", ex);
            return [];
        }
    }

    public void Save(IEnumerable<OrbitLinkQueuedTransfer> transfers)
    {
        try
        {
            var retained = transfers
                .OrderBy(item => item.CreatedUtc)
                .Take(MaxQueuedTransfers)
                .ToList();
            if (retained.Count == 0)
            {
                if (File.Exists(_statePath)) File.Delete(_statePath);
                return;
            }

            var json = JsonSerializer.Serialize(
                new QueueDocument { Transfers = retained },
                _options);
            if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxStateBytes)
            {
                throw new InvalidDataException("Orbit Link kuyruk dosyası boyut sınırını aşıyor.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
            var temporaryPath = $"{_statePath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _statePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logService.Error("Orbit Link queue save failed.", ex);
            throw;
        }
    }

    private static bool IsHexId(string? value) =>
        value?.Length == 32 && value.All(Uri.IsHexDigit);

    private sealed class QueueDocument
    {
        public int Version { get; set; } = 1;
        public List<OrbitLinkQueuedTransfer> Transfers { get; set; } = [];
    }
}
