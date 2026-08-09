namespace ActionOrbit.App.Services.MiniTools;

public sealed record MiniToolDefinition(string Id, string Title, string Description);

public static class MiniToolCatalog
{
    public static IReadOnlyList<MiniToolDefinition> Tools { get; } =
    [
        new("timer", "Zamanlayıcı", "Odak oturumları ve kısa geri sayımlar"),
        new("caffeine", "Uyanık Tut", "Ekranı ve bilgisayarı seçilen süre boyunca açık tutar"),
        new("system_glance", "Sistem Durumu", "İşlemci, bellek ve pil özeti"),
        new("calculator", "Hesap Makinesi", "Yerel ve güvenli ifade hesaplayıcı"),
        new("color_picker", "Renk Seçici", "İmlecin altındaki ekran rengini yakalar"),
        new("stopwatch", "Kronometre", "Tur ve toplam süre takibi"),
        new("quick_note", "Hızlı Not", "Otomatik kaydedilen yerel karalama alanı"),
        new("unit_converter", "Birim Dönüştürücü", "Uzunluk, ağırlık, sıcaklık ve veri birimleri"),
        new("text_tools", "Metin Araçları", "Metni temizle, dönüştür, say ve kopyala"),
        new("password_generator", "Parola Üretici", "Yerel ve kriptografik güçlü parola üretimi")
    ];

    public static bool TryGet(string? id, out MiniToolDefinition definition)
    {
        definition = Tools.FirstOrDefault(tool =>
            string.Equals(tool.Id, id?.Trim(), StringComparison.OrdinalIgnoreCase))!;
        return definition is not null;
    }
}

public interface IMiniToolLauncher
{
    void Show(string toolId);
}
