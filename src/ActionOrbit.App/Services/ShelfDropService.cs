using System.Text.RegularExpressions;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;
using ActionOrbit.App.Models;
using WpfDataFormats = System.Windows.DataFormats;

namespace ActionOrbit.App.Services;

public sealed class ShelfDropService
{
    private const long MaxInlineImageBytes = 20L * 1024 * 1024;
    private static readonly Regex HtmlImageSourcePattern = new(
        "<img\\b[^>]*\\bsrc\\s*=\\s*[\"'](?<source>[^\"']+)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline,
        TimeSpan.FromMilliseconds(100));
    private readonly SafeRemoteImageService _remoteImages;
    private readonly string _cacheDirectory;

    public ShelfDropService(SafeRemoteImageService remoteImages, string cacheDirectory)
    {
        _remoteImages = remoteImages;
        _cacheDirectory = cacheDirectory;
    }

    public async Task<ShelfImportResult> ImportAsync(
        System.Windows.IDataObject data,
        ShelfSettings settings,
        int remainingItemSlots,
        long remainingBytes,
        CancellationToken cancellationToken = default)
    {
        if (remainingItemSlots <= 0)
        {
            return ShelfImportResult.Failure("Bu rafın öğe sınırına ulaşıldı.");
        }

        if (data.GetDataPresent(WpfDataFormats.FileDrop) &&
            data.GetData(WpfDataFormats.FileDrop) is string[] paths)
        {
            var fileDrop = ImportPaths(paths, settings, remainingItemSlots, remainingBytes);
            if (fileDrop.Succeeded)
            {
                return fileDrop;
            }
        }

        if (data.GetDataPresent(WpfDataFormats.Bitmap) &&
            data.GetData(WpfDataFormats.Bitmap) is BitmapSource bitmap)
        {
            return ImportBitmap(bitmap, settings, remainingBytes);
        }

        var html = data.GetDataPresent(WpfDataFormats.Html)
            ? data.GetData(WpfDataFormats.Html) as string
            : null;
        var text = data.GetDataPresent(WpfDataFormats.UnicodeText)
            ? data.GetData(WpfDataFormats.UnicodeText) as string
            : data.GetDataPresent(WpfDataFormats.Text)
                ? data.GetData(WpfDataFormats.Text) as string
                : null;

        var imageSource = TryExtractImageSource(html);
        var inlineImage = ImportInlineDataImage(
            imageSource ?? text,
            settings,
            remainingBytes);
        if (inlineImage is not null)
        {
            return inlineImage;
        }

        var remoteUri = TryParseHttpUri(imageSource) ?? TryParseHttpUri(text);
        if (remoteUri is not null && LooksLikeImage(remoteUri, html))
        {
            var maxBytes = Math.Min(settings.MaxItemBytes, remainingBytes);
            var remote = await _remoteImages.DownloadAsync(
                remoteUri,
                _cacheDirectory,
                maxBytes,
                cancellationToken);
            if (!remote.Succeeded)
            {
                return ShelfImportResult.Failure(remote.Message);
            }

            return ShelfImportResult.Success(
            [
                new ShelfItem
                {
                    Kind = "image",
                    DisplayName = GetRemoteDisplayName(remote.Source!, remote.Path),
                    Source = remote.Source!.GetLeftPart(UriPartial.Path),
                    LocalPath = remote.Path,
                    SizeBytes = remote.SizeBytes,
                    IsTemporary = true
                }
            ]);
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            var normalized = text.Trim();
            if (normalized.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                return ShelfImportResult.Failure("Chrome görsel verisi çözülemedi; ham base64 metni rafa eklenmedi.");
            }

            if (normalized.Length > 256 * 1024)
            {
                return ShelfImportResult.Failure("Metin 256 KB sınırını aşıyor.");
            }

            var uri = TryParseHttpUri(normalized);
            return ShelfImportResult.Success(
            [
                new ShelfItem
                {
                    Kind = uri is null ? "text" : "url",
                    DisplayName = uri is null ? BuildTextTitle(normalized) : uri.Host,
                    Source = uri?.GetLeftPart(UriPartial.Path) ?? "",
                    TextContent = normalized,
                    SizeBytes = System.Text.Encoding.UTF8.GetByteCount(normalized)
                }
            ]);
        }

        return ShelfImportResult.Failure("Bu sürükleme biçimi henüz desteklenmiyor.");
    }

    private static ShelfImportResult ImportPaths(
        IEnumerable<string> paths,
        ShelfSettings settings,
        int remainingItemSlots,
        long remainingBytes)
    {
        var items = new List<ShelfItem>();
        var skipped = 0;
        long acceptedBytes = 0;
        foreach (var path in paths.Take(remainingItemSlots))
        {
            if (string.IsNullOrWhiteSpace(path) || (!File.Exists(path) && !Directory.Exists(path)))
            {
                skipped++;
                continue;
            }

            var isDirectory = Directory.Exists(path);
            var isImage = !isDirectory && IsImageFile(path);
            if (isImage && !ImageProcessingService.TryValidateImageDimensions(path, out _))
            {
                skipped++;
                continue;
            }

            var size = isDirectory ? 0 : new FileInfo(path).Length;
            if (size > settings.MaxItemBytes || acceptedBytes + size > remainingBytes)
            {
                skipped++;
                continue;
            }

            acceptedBytes += size;
            items.Add(new ShelfItem
            {
                Kind = isDirectory ? "folder" : isImage ? "image" : "file",
                DisplayName = isDirectory
                    ? new DirectoryInfo(path).Name
                    : Path.GetFileName(path),
                Source = path,
                LocalPath = path,
                SizeBytes = size
            });
        }

        return items.Count == 0
            ? ShelfImportResult.Failure("Dosyalar bulunamadı veya raf boyut sınırını aştı.")
            : ShelfImportResult.Success(items, skipped);
    }

    private ShelfImportResult ImportBitmap(BitmapSource bitmap, ShelfSettings settings, long remainingBytes)
    {
        if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0 ||
            bitmap.PixelWidth > ImageProcessingService.MaxImageDimension ||
            bitmap.PixelHeight > ImageProcessingService.MaxImageDimension ||
            (long)bitmap.PixelWidth * bitmap.PixelHeight > ImageProcessingService.MaxPixelCount)
        {
            return ShelfImportResult.Failure("Görsel boyutları güvenli sınırı aşıyor.");
        }

        Directory.CreateDirectory(_cacheDirectory);
        var targetPath = Path.Combine(_cacheDirectory, $"bitmap-{Guid.NewGuid():N}.png");
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                encoder.Save(stream);
            }

            var size = new FileInfo(targetPath).Length;
            if (size > settings.MaxItemBytes || size > remainingBytes)
            {
                File.Delete(targetPath);
                return ShelfImportResult.Failure("Görsel raf boyut sınırını aşıyor.");
            }

            return ShelfImportResult.Success(
            [
                new ShelfItem
                {
                    Kind = "image",
                    DisplayName = $"Sürüklenen Görsel {DateTime.Now:HH-mm-ss}.png",
                    LocalPath = targetPath,
                    SizeBytes = size,
                    IsTemporary = true
                }
            ]);
        }
        catch
        {
            try { File.Delete(targetPath); } catch { }
            return ShelfImportResult.Failure("Görsel geçici PNG dosyasına dönüştürülemedi.");
        }
    }

    private ShelfImportResult? ImportInlineDataImage(
        string? value,
        ShelfSettings settings,
        long remainingBytes)
    {
        var normalized = WebUtility.HtmlDecode(value ?? "").Trim();
        if (!normalized.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var commaIndex = normalized.IndexOf(',');
        if (commaIndex <= 5)
        {
            return ShelfImportResult.Failure("Chrome görsel verisinin data URI başlığı okunamadı.");
        }

        var metadata = normalized[5..commaIndex];
        var metadataParts = metadata.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var mediaType = metadataParts.FirstOrDefault()?.ToLowerInvariant() ?? "";
        if (!metadataParts.Skip(1).Any(part => string.Equals(part, "base64", StringComparison.OrdinalIgnoreCase)))
        {
            return ShelfImportResult.Failure("Yalnızca base64 kodlu Chrome görselleri destekleniyor.");
        }

        var extension = mediaType switch
        {
            "image/png" => ".png",
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/webp" => ".webp",
            _ => ""
        };
        if (extension.Length == 0)
        {
            return ShelfImportResult.Failure("Bu inline görsel biçimi desteklenmiyor.");
        }

        var maxBytes = Math.Min(Math.Min(settings.MaxItemBytes, remainingBytes), MaxInlineImageBytes);
        var payload = normalized[(commaIndex + 1)..];
        if (payload.Length == 0 || payload.Length > ((maxBytes + 2) / 3 * 4) + 4096)
        {
            return ShelfImportResult.Failure("Inline görsel güvenli boyut sınırını aşıyor.");
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            return ShelfImportResult.Failure("Chrome görselinin base64 verisi bozuk.");
        }

        if (bytes.Length == 0 || bytes.LongLength > maxBytes)
        {
            return ShelfImportResult.Failure("Inline görsel güvenli boyut sınırını aşıyor.");
        }

        Directory.CreateDirectory(_cacheDirectory);
        var targetPath = Path.Combine(_cacheDirectory, $"inline-{Guid.NewGuid():N}{extension}");
        try
        {
            File.WriteAllBytes(targetPath, bytes);
            if (!SafeRemoteImageService.HasSupportedImageSignature(targetPath))
            {
                File.Delete(targetPath);
                return ShelfImportResult.Failure("Inline veri desteklenen bir görsel değil.");
            }

            if (!ImageProcessingService.TryValidateImageDimensions(targetPath, out var validationIssue))
            {
                File.Delete(targetPath);
                return ShelfImportResult.Failure(validationIssue);
            }

            return ShelfImportResult.Success(
            [
                new ShelfItem
                {
                    Kind = "image",
                    DisplayName = $"Chrome Görseli {DateTime.Now:HH-mm-ss}{extension}",
                    Source = $"inline:{mediaType}",
                    LocalPath = targetPath,
                    SizeBytes = bytes.LongLength,
                    IsTemporary = true
                }
            ]);
        }
        catch (Exception ex)
        {
            try { File.Delete(targetPath); } catch { }
            return ShelfImportResult.Failure($"Inline görsel oluşturulamadı: {ex.Message}");
        }
    }

    private static string? TryExtractImageSource(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var match = HtmlImageSourcePattern.Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["source"].Value).Trim() : null;
    }

    private static Uri? TryParseHttpUri(string? value) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
            ? uri
            : null;

    private static bool LooksLikeImage(Uri uri, string? html) =>
        IsImageExtension(Path.GetExtension(uri.AbsolutePath)) ||
        (!string.IsNullOrWhiteSpace(html) && html.Contains("<img", StringComparison.OrdinalIgnoreCase));

    private static bool IsImageFile(string path) => IsImageExtension(Path.GetExtension(path));

    private static bool IsImageExtension(string extension) => extension.ToLowerInvariant() is
        ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" or ".avif" or ".tif" or ".tiff";

    private static string GetRemoteDisplayName(Uri uri, string localPath)
    {
        var fileName = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));
        return string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(localPath) : fileName;
    }

    private static string BuildTextTitle(string value)
    {
        var oneLine = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return oneLine.Length <= 48 ? oneLine : $"{oneLine[..45]}…";
    }
}

public sealed record ShelfImportResult(bool Succeeded, string Message, IReadOnlyList<ShelfItem> Items, int SkippedCount)
{
    public static ShelfImportResult Success(IReadOnlyList<ShelfItem> items, int skippedCount = 0) =>
        new(true, "", items, skippedCount);
    public static ShelfImportResult Failure(string message) => new(false, message, [], 0);
}
