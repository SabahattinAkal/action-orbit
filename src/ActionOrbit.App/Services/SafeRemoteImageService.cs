using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;

namespace ActionOrbit.App.Services;

public sealed class SafeRemoteImageService : IDisposable
{
    private const int MaxRedirects = 4;
    private readonly HttpClient _client;
    private readonly LogService _logService;
    private readonly Func<Uri, CancellationToken, Task<RemoteUriValidation>> _validateRemoteUri;

    public SafeRemoteImageService(LogService logService)
        : this(logService, CreateSecureHandler(), ValidateRemoteUriAsync)
    {
    }

    internal SafeRemoteImageService(
        LogService logService,
        HttpMessageHandler handler,
        Func<Uri, CancellationToken, Task<RemoteUriValidation>> validateRemoteUri)
    {
        _logService = logService;
        _validateRemoteUri = validateRemoteUri;
        _client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(12)
        };
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ActionOrbit", "2.0"));
    }

    public async Task<RemoteImageResult> DownloadAsync(
        Uri source,
        string cacheDirectory,
        long maxBytes,
        CancellationToken cancellationToken = default)
    {
        var current = source;
        for (var redirect = 0; redirect <= MaxRedirects; redirect++)
        {
            var validation = await _validateRemoteUri(current, cancellationToken);
            if (!validation.IsSafe)
            {
                return RemoteImageResult.Failure(validation.Message);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            using var response = await _client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (IsRedirect(response.StatusCode))
            {
                if (response.Headers.Location is null || redirect == MaxRedirects)
                {
                    return RemoteImageResult.Failure("Görsel yönlendirmesi güvenli biçimde tamamlanamadı.");
                }

                current = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(current, response.Headers.Location);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                return RemoteImageResult.Failure($"Görsel indirilemedi: HTTP {(int)response.StatusCode}.");
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
            if (!mediaType.StartsWith("image/", StringComparison.Ordinal))
            {
                return RemoteImageResult.Failure("Uzak içerik bir görsel değil.");
            }

            if (response.Content.Headers.ContentLength is long length && length > maxBytes)
            {
                return RemoteImageResult.Failure("Görsel izin verilen boyut sınırını aşıyor.");
            }

            Directory.CreateDirectory(cacheDirectory);
            var extension = ExtensionForMediaType(mediaType);
            var targetPath = Path.Combine(cacheDirectory, $"remote-{Guid.NewGuid():N}{extension}");
            try
            {
                var buffer = new byte[81920];
                long total = 0;
                await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var output = new FileStream(
                    targetPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    while (true)
                    {
                        var read = await input.ReadAsync(buffer, cancellationToken);
                        if (read == 0)
                        {
                            break;
                        }

                        total += read;
                        if (total > maxBytes)
                        {
                            throw new InvalidDataException("Görsel izin verilen boyut sınırını aşıyor.");
                        }

                        await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    }

                    await output.FlushAsync(cancellationToken);
                }

                if (!HasSupportedImageSignature(targetPath))
                {
                    throw new InvalidDataException("Dosya içeriği desteklenen bir görsel biçimi değil.");
                }

                if (!ImageProcessingService.TryValidateImageDimensions(targetPath, out var dimensionIssue))
                {
                    throw new InvalidDataException(dimensionIssue);
                }

                _logService.Info($"Remote shelf image downloaded from {current.Scheme}://{current.Host}.");
                return RemoteImageResult.Success(targetPath, total, current);
            }
            catch (Exception ex)
            {
                TryDelete(targetPath);
                return RemoteImageResult.Failure(ex.Message);
            }
        }

        return RemoteImageResult.Failure("Görsel yönlendirmesi çok uzun.");
    }

    internal static async Task<RemoteUriValidation> ValidateRemoteUriAsync(
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        if (!uri.IsAbsoluteUri || uri.Scheme is not ("http" or "https"))
        {
            return RemoteUriValidation.Failure("Yalnızca http ve https adresleri kabul edilir.");
        }

        if (string.IsNullOrWhiteSpace(uri.Host) ||
            string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return RemoteUriValidation.Failure("Yerel adreslerden içerik alınamaz.");
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch
        {
            return RemoteUriValidation.Failure("Görsel sunucusunun adresi çözülemedi.");
        }

        return addresses.Length > 0 && addresses.All(IsPublicAddress)
            ? RemoteUriValidation.Success
            : RemoteUriValidation.Failure("Özel, yerel veya bağlantı-yerel ağ adreslerinden içerik alınamaz.");
    }

    internal static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            return IsPublicAddress(address.MapToIPv4());
        }

        if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal)
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            return (bytes[0] & 0xFE) != 0xFC;
        }

        var octets = address.GetAddressBytes();
        return octets[0] != 0 &&
               octets[0] != 10 &&
               octets[0] != 127 &&
               !(octets[0] == 169 && octets[1] == 254) &&
               !(octets[0] == 172 && octets[1] is >= 16 and <= 31) &&
               !(octets[0] == 192 && octets[1] == 168) &&
               !(octets[0] >= 224);
    }

    private static SocketsHttpHandler CreateSecureHandler() => new()
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        ConnectTimeout = TimeSpan.FromSeconds(8),
        UseProxy = false,
        ConnectCallback = ConnectToPublicHostAsync
    };

    private static async ValueTask<Stream> ConnectToPublicHostAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(address => !IsPublicAddress(address)))
        {
            throw new HttpRequestException("Özel veya yerel ağ adresine bağlantı engellendi.");
        }

        Exception? lastError = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            try
            {
                await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (OperationCanceledException)
            {
                socket.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex;
                socket.Dispose();
            }
        }

        throw new HttpRequestException("Görsel sunucusuna güvenli bağlantı kurulamadı.", lastError);
    }

    private static bool HasSupportedImageSignature(string path)
    {
        Span<byte> header = stackalloc byte[16];
        using var stream = File.OpenRead(path);
        var read = stream.Read(header);
        return read >= 12 &&
               ((header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) ||
                (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) ||
                (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38) ||
                (header[0] == 0x42 && header[1] == 0x4D) ||
                (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                 header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) ||
                (header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70 &&
                 header[8] == 0x61 && header[9] == 0x76 && header[10] == 0x69 &&
                 header[11] is 0x66 or 0x73));
    }

    private static string ExtensionForMediaType(string mediaType) => mediaType switch
    {
        "image/jpeg" => ".jpg",
        "image/gif" => ".gif",
        "image/bmp" => ".bmp",
        "image/webp" => ".webp",
        "image/avif" => ".avif",
        _ => ".png"
    };

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or
            HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Best-effort cache cleanup.
        }
    }

    public void Dispose() => _client.Dispose();
}

public sealed record RemoteImageResult(bool Succeeded, string Message, string Path, long SizeBytes, Uri? Source)
{
    public static RemoteImageResult Success(string path, long sizeBytes, Uri source) =>
        new(true, "", path, sizeBytes, source);
    public static RemoteImageResult Failure(string message) => new(false, message, "", 0, null);
}

public sealed record RemoteUriValidation(bool IsSafe, string Message)
{
    public static RemoteUriValidation Success { get; } = new(true, "");
    public static RemoteUriValidation Failure(string message) => new(false, message);
}
