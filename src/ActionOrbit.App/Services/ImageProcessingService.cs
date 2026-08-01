using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ActionOrbit.App.Services;

public sealed class ImageProcessingService
{
    public const long MaxPixelCount = 12_000_000;
    public const int MaxImageDimension = 16_384;
    private readonly string _cacheDirectory;

    public ImageProcessingService(string cacheDirectory) => _cacheDirectory = cacheDirectory;

    public ImageProcessResult ConvertToPng(string sourcePath) =>
        Encode(sourcePath, maxDimension: null, "png");

    public ImageProcessResult ResizeToFit(string sourcePath, int maxDimension) =>
        Encode(sourcePath, Math.Clamp(maxDimension, 128, 8192), "png");

    private ImageProcessResult Encode(string sourcePath, int? maxDimension, string format)
    {
        if (!File.Exists(sourcePath))
        {
            return ImageProcessResult.Failure("Görsel dosyası bulunamadı.");
        }

        try
        {
            if (!TryValidateImageDimensions(sourcePath, out var validationMessage))
            {
                return ImageProcessResult.Failure(validationMessage);
            }

            var frame = LoadFrame(sourcePath);
            BitmapSource output = frame;
            if (maxDimension is int limit && (frame.PixelWidth > limit || frame.PixelHeight > limit))
            {
                var scale = Math.Min(limit / (double)frame.PixelWidth, limit / (double)frame.PixelHeight);
                var transformed = new TransformedBitmap(frame, new ScaleTransform(scale, scale));
                transformed.Freeze();
                output = transformed;
            }

            Directory.CreateDirectory(_cacheDirectory);
            var targetPath = Path.Combine(_cacheDirectory, $"processed-{Guid.NewGuid():N}.{format}");
            BitmapEncoder encoder = format == "jpg" ? new JpegBitmapEncoder { QualityLevel = 90 } : new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(output));
            using var stream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            encoder.Save(stream);
            return ImageProcessResult.Success(targetPath, output.PixelWidth, output.PixelHeight, stream.Length);
        }
        catch (Exception ex)
        {
            return ImageProcessResult.Failure($"Görsel işlenemedi: {ex.Message}");
        }
    }

    internal static BitmapFrame LoadFrame(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        frame.Freeze();
        return frame;
    }

    internal static bool TryValidateImageDimensions(string path, out string message)
    {
        message = "";
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            message = "Görsel dosyası bulunamadı.";
            return false;
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.DelayCreation | BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnDemand);
            if (decoder.Frames.Count == 0)
            {
                message = "Dosyada okunabilir görsel karesi yok.";
                return false;
            }

            var frame = decoder.Frames[0];
            var width = frame.PixelWidth;
            var height = frame.PixelHeight;
            if (width <= 0 || height <= 0 || width > MaxImageDimension || height > MaxImageDimension ||
                (long)width * height > MaxPixelCount)
            {
                message = "Görsel piksel boyutları güvenli sınırı aşıyor.";
                return false;
            }

            return true;
        }
        catch
        {
            message = "Dosya desteklenen ve okunabilir bir görsel değil.";
            return false;
        }
    }
}

public sealed record ImageProcessResult(
    bool Succeeded,
    string Message,
    string Path,
    int PixelWidth,
    int PixelHeight,
    long SizeBytes)
{
    public static ImageProcessResult Success(string path, int width, int height, long sizeBytes) =>
        new(true, "", path, width, height, sizeBytes);
    public static ImageProcessResult Failure(string message) => new(false, message, "", 0, 0, 0);
}
