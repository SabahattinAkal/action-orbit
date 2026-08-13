using System.Security.Cryptography;
using System.Net;
using System.Text;
using System.Text.Json;
using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public sealed class OrbitLinkStore
{
    private const long MaxStateBytes = 256 * 1024;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ActionOrbit.OrbitLink.v1");
    private readonly string _path;
    private readonly LogService _logService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        MaxDepth = 8
    };

    public OrbitLinkStore(string appDirectory, LogService logService)
    {
        _path = Path.Combine(appDirectory, "orbit-link.json");
        _logService = logService;
    }

    public OrbitLinkState Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return Normalize(new OrbitLinkState());
            }

            var info = new FileInfo(_path);
            if (info.Length <= 0 || info.Length > MaxStateBytes)
            {
                throw new InvalidDataException("Orbit Link durum dosyası geçersiz boyutta.");
            }

            var state = JsonSerializer.Deserialize<OrbitLinkState>(
                File.ReadAllText(_path),
                _jsonOptions) ?? new OrbitLinkState();
            return Normalize(state);
        }
        catch (Exception ex)
        {
            _logService.Error("Orbit Link state load failed.", ex);
            return Normalize(new OrbitLinkState());
        }
    }

    public void Save(OrbitLinkState state)
    {
        try
        {
            Normalize(state);
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            if (Encoding.UTF8.GetByteCount(json) > MaxStateBytes)
            {
                throw new InvalidDataException("Orbit Link durum dosyası boyut sınırını aşıyor.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _path, overwrite: true);
        }
        catch (Exception ex)
        {
            _logService.Error("Orbit Link state save failed.", ex);
        }
    }

    public static string ProtectKey(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != 32)
        {
            throw new ArgumentException("Orbit Link anahtarı 32 bayt olmalı.", nameof(key));
        }

        var protectedBytes = ProtectedData.Protect(key, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static bool TryUnprotectKey(string protectedKey, out byte[] key)
    {
        key = [];
        try
        {
            var protectedBytes = Convert.FromBase64String(protectedKey);
            key = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
            return key.Length == 32;
        }
        catch
        {
            key = [];
            return false;
        }
    }

    private static OrbitLinkState Normalize(OrbitLinkState state)
    {
        state.Version = 1;
        state.DeviceId = NormalizeId(state.DeviceId) ?? Guid.NewGuid().ToString("N");
        state.DeviceName = NormalizeName(state.DeviceName, Environment.MachineName);
        state.ListenPort = Math.Clamp(state.ListenPort, 1024, 65535);
        state.Peers = state.Peers?
            .Where(peer => peer is not null)
            .Select(peer =>
            {
                peer.Id = NormalizeId(peer.Id) ?? "";
                peer.Name = NormalizeName(peer.Name, "Eşleşen cihaz");
                peer.Host = NormalizeHost(peer.Host);
                peer.Port = Math.Clamp(peer.Port, 1024, 65535);
                peer.ProtectedKey = (peer.ProtectedKey ?? "").Trim();
                return peer;
            })
            .Where(peer => peer.Id.Length == 32 && peer.Host.Length > 0 && peer.ProtectedKey.Length > 0)
            .GroupBy(peer => peer.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(peer => peer.LastSeenUtc).First())
            .Take(16)
            .ToList() ?? [];
        return state;
    }

    internal static string NormalizeName(string? value, string fallback)
    {
        var normalized = new string((value ?? "")
            .Where(character => !char.IsControl(character))
            .Take(64)
            .ToArray())
            .Trim();
        return normalized.Length == 0 ? fallback : normalized;
    }

    internal static string NormalizeHost(string? value)
    {
        var normalized = (value ?? "").Trim();
        if (IPAddress.TryParse(normalized.Trim('[', ']'), out var address))
        {
            return NormalizeAddress(address).ToString();
        }

        return normalized.Length <= 255 && normalized.All(character =>
            char.IsLetterOrDigit(character) || character is '.' or ':' or '-' or '[' or ']')
            ? normalized.Trim('[', ']')
            : "";
    }

    internal static IPAddress NormalizeAddress(IPAddress address) =>
        address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

    private static string? NormalizeId(string? value)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant();
        return normalized.Length == 32 && normalized.All(Uri.IsHexDigit) ? normalized : null;
    }
}
