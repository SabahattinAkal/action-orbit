using ActionOrbit.App.Models;

namespace ActionOrbit.App.ViewModels;

public sealed class ShelfItemViewModel
{
    public ShelfItemViewModel(ShelfItem item) => Item = item;

    public ShelfItem Item { get; }
    public string Id => Item.Id;
    public string DisplayName => Item.DisplayName;
    public string Kind => Item.Kind;
    public string KindLabel => Kind switch
    {
        "image" => "Görsel",
        "folder" => "Klasör",
        "url" => "Bağlantı",
        "text" => "Metin",
        _ => "Dosya"
    };
    public string Glyph => Kind switch
    {
        "image" => "▧",
        "folder" => "▰",
        "url" => "↗",
        "text" => "¶",
        _ => "◇"
    };
    public string Subtitle => Item.SizeBytes > 0
        ? $"{KindLabel} · {FormatBytes(Item.SizeBytes)}"
        : KindLabel;
    public bool IsImage => Kind == "image" && File.Exists(Item.LocalPath);
    public bool HasLocalPath => File.Exists(Item.LocalPath) || Directory.Exists(Item.LocalPath);
    public string PreviewPath => IsImage ? Item.LocalPath : "";

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1024L * 1024 * 1024 => $"{bytes / (1024d * 1024 * 1024):0.##} GB",
        >= 1024L * 1024 => $"{bytes / (1024d * 1024):0.##} MB",
        >= 1024 => $"{bytes / 1024d:0.##} KB",
        _ => $"{bytes} B"
    };
}
