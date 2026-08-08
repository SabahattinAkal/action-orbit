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
        new("color_picker", "Renk Seçici", "İmlecin altındaki ekran rengini yakalar")
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
