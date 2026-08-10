using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public sealed record ActionTypeOption(
    string Key,
    string Label,
    string TargetLabel,
    string ArgumentsLabel,
    string Help);

public sealed record ActionPresetOption(
    string Id,
    string Title,
    string Description,
    string Icon,
    string Type,
    string Target,
    string Arguments = "")
{
    public IReadOnlyList<string> IconPaths => IconCatalog.GetPaths(Icon);
    public string? ImagePath => IconCatalog.GetImagePath(Icon);
    public bool HasImage => ImagePath is not null;
    public bool HasPaths => !HasImage && IconPaths.Count > 0;
    public string TypeLabel => ActionDefinitionCatalog.GetTypeOption(Type).Label;
}

public static class ActionDefinitionCatalog
{
    public static IReadOnlyList<ActionTypeOption> TypeOptions { get; } =
    [
        new("send_hotkey", "Kısayol gönder", "Kısayol", "Ek bilgi", "Örn: Ctrl+C, Ctrl+Shift+T, Win+Shift+S. Hazır eylemler bunu senin yerine doldurur."),
        new("open_app", "Uygulama aç", "Uygulama veya .exe", "Başlatma argümanları", "Notepad.exe, wt.exe veya tam uygulama yolu yazabilirsin."),
        new("open_folder", "Klasör aç", "Klasör yolu", "Ek bilgi", "%USERPROFILE%\\Downloads gibi Windows değişkenleri desteklenir."),
        new("open_file", "Dosya aç", "Dosya yolu", "Ek bilgi", "Dosyayı varsayılan uygulamasıyla açar."),
        new("open_url", "Web adresi aç", "Web adresi", "Ek bilgi", "https://... ile başlayan adresleri varsayılan tarayıcıda açar."),
        new("mini_tool", "Mini araç aç", "Mini araç", "Ek bilgi", "Action Orbit içindeki izinli mini araçlardan birini açar."),
        new("type_text", "Metin yaz", "Yazılacak metin", "Ek bilgi", "Seçili pencereye düz metin yazar."),
        new("run_command", "Komut çalıştır", "Komut", "Argümanlar", "Gelişmiş kullanım içindir; cmd üzerinden çalışır."),
        new("folder", "Klasör / alt menü", "Hedef yok", "Ek bilgi", "Bu aksiyon çalışmaz; içine alt aksiyonlar koyar.")
    ];

    public static IReadOnlyList<ActionPresetOption> Presets { get; } =
    [
        new("copy", "Kopyala", "Seçili içeriği panoya kopyalar", "copy", "send_hotkey", "Ctrl+C"),
        new("paste", "Yapıştır", "Panodaki içeriği yapıştırır", "paste", "send_hotkey", "Ctrl+V"),
        new("cut", "Kes", "Seçili içeriği keser", "cut", "send_hotkey", "Ctrl+X"),
        new("select_all", "Tümünü Seç", "Aktif alandaki tüm içeriği seçer", "select-all", "send_hotkey", "Ctrl+A"),
        new("undo", "Geri Al", "Son işlemi geri alır", "undo", "send_hotkey", "Ctrl+Z"),
        new("redo", "Yinele", "Geri alınan işlemi tekrarlar", "redo", "send_hotkey", "Ctrl+Y"),
        new("save", "Kaydet", "Aktif belgeyi kaydeder", "save", "send_hotkey", "Ctrl+S"),
        new("find", "Bul", "Aktif uygulamada arama açar", "search", "send_hotkey", "Ctrl+F"),
        new("print", "Yazdır", "Yazdırma ekranını açar", "printer", "send_hotkey", "Ctrl+P"),
        new("new_tab", "Yeni Sekme", "Tarayıcıda yeni sekme açar", "plus", "send_hotkey", "Ctrl+T"),
        new("close_tab", "Sekmeyi Kapat", "Aktif sekmeyi kapatır", "x", "send_hotkey", "Ctrl+W"),
        new("restore_tab", "Kapalı Sekmeyi Aç", "Son kapatılan sekmeyi geri getirir", "rotate", "send_hotkey", "Ctrl+Shift+T"),
        new("refresh", "Yenile", "Sayfayı veya görünümü yeniler", "refresh", "send_hotkey", "Ctrl+R"),
        new("screenshot", "Ekran Al", "Windows ekran alıntısı aracını açar", "screenshot", "send_hotkey", "Win+Shift+S"),
        new("emoji", "Emoji Paneli", "Windows emoji panelini açar", "sparkles", "send_hotkey", "Win+."),
        new("lock", "Ekranı Kilitle", "Oturumu kilitler", "lock", "send_hotkey", "Win+L"),
        new("task_manager", "Görev Yöneticisi", "Windows Görev Yöneticisi'ni açar", "dashboard", "open_app", "taskmgr.exe"),
        new("terminal", "Terminal", "Windows Terminal açar", "terminal", "open_app", "wt.exe"),
        new("notepad", "Not Defteri", "Not Defteri açar", "notes", "open_app", "notepad.exe"),
        new("calculator", "Hesap Makinesi", "Hesap makinesini açar", "calculator", "open_app", "calc.exe"),
        new("mini_timer", "Zamanlayıcı", "Kompakt Action Orbit zamanlayıcısını açar", "clock", "mini_tool", "timer"),
        new("mini_caffeine", "Uyanık Tut", "Ekranı ve bilgisayarı geçici olarak uyanık tutar", "coffee", "mini_tool", "caffeine"),
        new("mini_system_glance", "Sistem Durumu", "İşlemci, bellek ve pil durumunu gösterir", "layout-dashboard", "mini_tool", "system_glance"),
        new("mini_calculator", "Mini Hesap Makinesi", "Güvenli ifade hesaplayıcısını açar", "calculator", "mini_tool", "calculator"),
        new("mini_color_picker", "Renk Seçici", "İmlecin altındaki ekran rengini yakalar", "color-picker", "mini_tool", "color_picker"),
        new("mini_stopwatch", "Kronometre", "Tur ve toplam süre takibi yapar", "clock", "mini_tool", "stopwatch"),
        new("mini_quick_note", "Hızlı Not", "Yerel ve otomatik kaydedilen not alanını açar", "notes", "mini_tool", "quick_note"),
        new("mini_unit_converter", "Birim Dönüştürücü", "Yaygın birimler arasında dönüşüm yapar", "refresh", "mini_tool", "unit_converter"),
        new("mini_text_tools", "Metin Araçları", "Metni temizler, dönüştürür ve sayar", "text", "mini_tool", "text_tools"),
        new("mini_password_generator", "Parola Üretici", "Güçlü ve yerel parola üretir", "key", "mini_tool", "password_generator"),
        new("downloads", "İndirilenler", "İndirilenler klasörünü açar", "download", "open_folder", "%USERPROFILE%\\Downloads"),
        new("documents", "Belgeler", "Belgeler klasörünü açar", "file-text", "open_folder", "%USERPROFILE%\\Documents"),
        new("desktop", "Masaüstü", "Masaüstü klasörünü açar", "desktop", "open_folder", "%USERPROFILE%\\Desktop"),
        new("chatgpt", "ChatGPT", "ChatGPT web uygulamasını açar", "openai", "open_url", "https://chatgpt.com"),
        new("youtube", "YouTube", "YouTube'u açar", "youtube", "open_url", "https://youtube.com"),
        new("google", "Google", "Google aramasını açar", "google", "open_url", "https://google.com"),
        new("open_url", "Web Sitesi Aç", "Hedefe kendi adresini yaz", "url", "open_url", "https://"),
        new("open_app", "Uygulama Aç", "Hedefe exe adı veya uygulama yolu yaz", "app", "open_app", ""),
        new("open_folder", "Klasör Aç", "Hedefe klasör yolu yaz", "folder-open", "open_folder", ""),
        new("type_text", "Metin Yaz", "Hedefe yazılacak metni gir", "text", "type_text", ""),
        new("run_command", "Komut Çalıştır", "Gelişmiş komut çalıştırma", "terminal", "run_command", ""),
        new("folder", "Alt Menü Klasörü", "İçine başka aksiyonlar koy", "folder", "folder", "")
    ];

    public static ActionTypeOption GetTypeOption(string? key) =>
        TypeOptions.FirstOrDefault(option => string.Equals(option.Key, key, StringComparison.OrdinalIgnoreCase))
        ?? TypeOptions[0];

    public static OrbitAction CreateActionFromPreset(ActionPresetOption preset, string id) =>
        new()
        {
            Id = id,
            Title = preset.Title,
            Icon = preset.Icon,
            Type = preset.Type,
            Target = preset.Target,
            Arguments = preset.Arguments,
            Children = []
        };
}
