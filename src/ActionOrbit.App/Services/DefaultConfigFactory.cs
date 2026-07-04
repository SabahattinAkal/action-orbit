using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public static class DefaultConfigFactory
{
    public const int CurrentVersion = 6;

    public static AppConfig Create() =>
        new()
        {
            ConfigVersion = CurrentVersion,
            Hotkey = new HotkeyConfig
            {
                Display = "Ctrl+Alt+Shift+R",
                Modifiers = ["Control", "Alt", "Shift"],
                Key = "R"
            },
            DefaultProfileId = "default",
            Settings = new AppSettings
            {
                CloseToTray = true,
                RunAtStartup = false
            },
            Theme = new ThemeConfig
            {
                Mode = "dark",
                Accent = "#A51E39",
                ButtonSize = 60,
                RadiusX = 116,
                RadiusY = 98,
                Animation = true
            },
            Profiles =
            [
                BuildDefaultProfile(),
                BuildBrowserProfile(),
                BuildVsCodeProfile(),
                BuildExplorerProfile()
            ]
        };

    private static ProfileConfig BuildDefaultProfile() =>
        new()
        {
            Id = "default",
            Name = "Varsayılan",
            Actions =
            [
                Folder(
                    "apps",
                    "Uygulamalar",
                    "play",
                    Action("notepad", "Not Defteri", "file", "open_app", "notepad.exe"),
                    Action("taskmgr", "Görevler", "app", "open_app", "taskmgr.exe"),
                    Action("terminal", "Terminal", "terminal", "open_app", "wt.exe")),
                Folder(
                    "folders",
                    "Klasörler",
                    "folder",
                    Action("downloads", "İndirilenler", "download", "open_folder", "%USERPROFILE%\\Downloads"),
                    Action("documents", "Belgeler", "file", "open_folder", "%USERPROFILE%\\Documents"),
                    Action("desktop", "Masaüstü", "app", "open_folder", "%USERPROFILE%\\Desktop")),
                Action("screenshot", "Ekran Al", "scissors", "send_hotkey", "Win+Shift+S"),
                Action("chatgpt", "ChatGPT", "sparkles", "open_url", "https://chatgpt.com"),
                Action("copy", "Kopyala", "copy", "send_hotkey", "Ctrl+C"),
                Action("paste", "Yapıştır", "clipboard", "send_hotkey", "Ctrl+V")
            ]
        };

    private static ProfileConfig BuildBrowserProfile() =>
        new()
        {
            Id = "browser",
            Name = "Tarayıcı",
            Matches =
            [
                new ProfileMatch { ProcessName = "chrome.exe" },
                new ProfileMatch { ProcessName = "msedge.exe" },
                new ProfileMatch { ProcessName = "firefox.exe" }
            ],
            Actions =
            [
                Action("new_tab", "Yeni Sekme", "plus", "send_hotkey", "Ctrl+T"),
                Action("close_tab", "Kapat", "x", "send_hotkey", "Ctrl+W"),
                Action("reopen_tab", "Geri Aç", "rotate", "send_hotkey", "Ctrl+Shift+T"),
                Action("devtools", "Geliştirici", "code", "send_hotkey", "F12"),
                Folder(
                    "sites",
                    "Siteler",
                    "sparkles",
                    Action("youtube", "YouTube", "play", "open_url", "https://youtube.com"),
                    Action("gmail", "Gmail", "mail", "open_url", "https://mail.google.com"),
                    Action("chatgpt_browser", "ChatGPT", "sparkles", "open_url", "https://chatgpt.com")),
                Action("downloads_page", "İndirilenler", "download", "send_hotkey", "Ctrl+J")
            ]
        };

    private static ProfileConfig BuildVsCodeProfile() =>
        new()
        {
            Id = "vscode",
            Name = "VS Code",
            Matches =
            [
                new ProfileMatch { ProcessName = "Code.exe" }
            ],
            Actions =
            [
                Action("command_palette", "Palet", "command", "send_hotkey", "Ctrl+Shift+P"),
                Action("terminal", "Terminal", "terminal", "send_hotkey", "Ctrl+`"),
                Action("format", "Formatla", "sparkles", "send_hotkey", "Shift+Alt+F"),
                Action("search", "Ara", "search", "send_hotkey", "Ctrl+Shift+F"),
                Folder(
                    "git",
                    "Git",
                    "git",
                    Action("source_control", "Kaynak", "git", "send_hotkey", "Ctrl+Shift+G"),
                    Action("commit_text", "Commit", "text", "type_text", "feat: ")),
                Action("quick_open", "Dosyalar", "file", "send_hotkey", "Ctrl+P")
            ]
        };

    private static ProfileConfig BuildExplorerProfile() =>
        new()
        {
            Id = "explorer",
            Name = "Gezgin",
            Matches =
            [
                new ProfileMatch { ProcessName = "explorer.exe" }
            ],
            Actions =
            [
                Action("new_folder", "Yeni Klasör", "folder", "send_hotkey", "Ctrl+Shift+N"),
                Action("copy_path", "Yolu Kopyala", "link", "send_hotkey", "Ctrl+Shift+C"),
                Action("properties", "Özellikler", "info", "send_hotkey", "Alt+Enter"),
                Action("terminal_here", "Terminal", "terminal", "run_command", "wt.exe"),
                Action("desktop_folder", "Masaüstü", "app", "open_folder", "%USERPROFILE%\\Desktop"),
                Action("downloads_folder", "İndirilenler", "download", "open_folder", "%USERPROFILE%\\Downloads")
            ]
        };

    private static OrbitAction Action(string id, string title, string icon, string type, string target, string arguments = "") =>
        new()
        {
            Id = id,
            Title = title,
            Icon = icon,
            Type = type,
            Target = target,
            Arguments = arguments
        };

    private static OrbitAction Folder(string id, string title, string icon, params OrbitAction[] children) =>
        new()
        {
            Id = id,
            Title = title,
            Icon = icon,
            Type = "folder",
            Children = [.. children]
        };
}
