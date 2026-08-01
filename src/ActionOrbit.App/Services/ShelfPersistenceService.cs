using System.Text.Json;
using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public sealed class ShelfPersistenceService
{
    private const long MaxStateBytes = 4 * 1024 * 1024;
    private readonly string _statePath;
    private readonly LogService _logService;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        MaxDepth = 16
    };

    public ShelfPersistenceService(string appDirectory, LogService logService)
    {
        _statePath = Path.Combine(appDirectory, "shelves.json");
        _logService = logService;
    }

    public IReadOnlyList<ShelfBoard> Load(ShelfSettings settings)
    {
        if (!File.Exists(_statePath))
        {
            return [];
        }

        try
        {
            var info = new FileInfo(_statePath);
            if (info.Length > MaxStateBytes)
            {
                throw new InvalidDataException("Raf durum dosyası boyut sınırını aşıyor.");
            }

            var document = JsonSerializer.Deserialize<ShelfStoreDocument>(
                File.ReadAllText(_statePath),
                _options) ?? new ShelfStoreDocument();
            var expiry = DateTime.UtcNow.AddHours(-settings.RetentionHours);
            return document.Shelves
                .Where(shelf => shelf.IsPinned || (settings.RememberRecentShelves && shelf.LastUsedUtc >= expiry))
                .Take(32)
                .Select(Normalize)
                .ToList();
        }
        catch (Exception ex)
        {
            _logService.Error("Shelf state load failed.", ex);
            return [];
        }
    }

    public void Save(IEnumerable<ShelfBoard> shelves, ShelfSettings settings)
    {
        try
        {
            var retained = shelves
                .Where(shelf => shelf.IsPinned || settings.RememberRecentShelves)
                .OrderByDescending(shelf => shelf.IsPinned)
                .ThenByDescending(shelf => shelf.LastUsedUtc)
                .Take(32)
                .ToList();
            var json = JsonSerializer.Serialize(new ShelfStoreDocument { Shelves = retained }, _options);
            if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxStateBytes)
            {
                throw new InvalidDataException("Raf durumu kaydetme sınırını aşıyor.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
            var temporaryPath = $"{_statePath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _statePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logService.Error("Shelf state save failed.", ex);
        }
    }

    private static ShelfBoard Normalize(ShelfBoard shelf)
    {
        shelf.Id = string.IsNullOrWhiteSpace(shelf.Id) ? Guid.NewGuid().ToString("N") : shelf.Id;
        shelf.Name = string.IsNullOrWhiteSpace(shelf.Name) ? "Kurtarılan Raf" : shelf.Name.Trim();
        shelf.Items = shelf.Items?
            .Where(item => item is not null)
            .Take(100)
            .Where(IsRestorableItem)
            .ToList() ?? [];
        return shelf;
    }

    private static bool IsRestorableItem(ShelfItem item)
    {
        if (item.Kind is "text" or "url")
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(item.LocalPath) ||
            (!File.Exists(item.LocalPath) && !Directory.Exists(item.LocalPath)))
        {
            return false;
        }

        return item.Kind != "image" || ImageProcessingService.TryValidateImageDimensions(item.LocalPath, out _);
    }

    private sealed class ShelfStoreDocument
    {
        public int Version { get; set; } = 1;
        public List<ShelfBoard> Shelves { get; set; } = [];
    }
}
