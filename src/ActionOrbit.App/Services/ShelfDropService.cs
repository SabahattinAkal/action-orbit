using System.Text.RegularExpressions;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;
using ActionOrbit.App.Models;
using WpfDataFormats = System.Windows.DataFormats;

namespace ActionOrbit.App.Services;

public sealed class ShelfDropService
{
    private static readonly Regex HtmlImageSourcePattern = new(
        "<img\\b[^>]*\\bsrc\\s*=\\s*[\"'](?<url>https?://[^\"']+)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
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

        var remoteUri = TryExtractRemoteUri(html) ?? TryParseHttpUri(text);
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

    private static Uri? TryExtractRemoteUri(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var match = HtmlImageSourcePattern.Match(html);
        return match.Success ? TryParseHttpUri(WebUtility.HtmlDecode(match.Groups["url"].Value)) : null;
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
