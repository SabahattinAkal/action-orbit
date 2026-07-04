using System.Text.Json;
using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public sealed class ConfigService
{
    private readonly LogService _logService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ConfigService(LogService logService)
    {
        _logService = logService;
        Directory.CreateDirectory(AppDirectory);
        Directory.CreateDirectory(IconDirectory);
        IconCatalog.ConfigureCustomIconDirectory(IconDirectory);
    }

    public string AppDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ActionOrbit");

    public string ConfigPath =>
        Path.Combine(AppDirectory, "config.json");

    public string IconDirectory =>
        Path.Combine(AppDirectory, "icons");

    private string LastGoodConfigPath =>
        Path.Combine(AppDirectory, "config.lastgood.json");

    public AppConfig CurrentConfig { get; private set; } = DefaultConfigFactory.Create();

    public AppConfig Load()
    {
        Directory.CreateDirectory(AppDirectory);

        if (!File.Exists(ConfigPath))
        {
            CurrentConfig = DefaultConfigFactory.Create();
            Save(CurrentConfig);
            _logService.Info($"Default config created at {ConfigPath}.");
            return CurrentConfig;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);

            if (config is null)
            {
                throw new InvalidOperationException("Config file is empty.");
            }

            Validate(config);
            var upgraded = UpgradeConfig(config);
            CurrentConfig = config;
            if (upgraded)
            {
                Save(CurrentConfig);
                _logService.Info($"Config upgraded to version {DefaultConfigFactory.CurrentVersion}.");
            }

            _logService.Info("Config loaded.");
            return CurrentConfig;
        }
        catch (Exception ex)
        {
            BackupBrokenConfig(ex);
            if (TryLoadLastGoodConfig(out var lastGoodConfig))
            {
                CurrentConfig = lastGoodConfig;
                Save(CurrentConfig);
                _logService.Warn("Broken config was replaced by last good config.");
                return CurrentConfig;
            }

            CurrentConfig = DefaultConfigFactory.Create();
            Save(CurrentConfig);
            _logService.Warn("Broken config was replaced by default config.");
            return CurrentConfig;
        }
    }

    public AppConfig Reload() => Load();

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(AppDirectory);
        Validate(config);
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        WriteAllTextAtomic(ConfigPath, json);

        try
        {
            WriteAllTextAtomic(LastGoodConfigPath, json);
        }
        catch (Exception ex)
        {
            _logService.Error("Last good config save failed.", ex);
        }

        CurrentConfig = config;
    }

    public void ExportConfig(string targetPath)
    {
        var json = JsonSerializer.Serialize(CurrentConfig, _jsonOptions);
        WriteAllTextAtomic(targetPath, json);
    }

    public AppConfig ImportConfig(string sourcePath)
    {
        var json = File.ReadAllText(sourcePath);
        var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Config dosyası boş.");

        Validate(config);
        UpgradeConfig(config);
        Save(config);
        _logService.Info($"Config imported from {sourcePath}.");
        return CurrentConfig;
    }

    public void ExportProfile(ProfileConfig profile, string targetPath)
    {
        NormalizeProfile(profile);
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        WriteAllTextAtomic(targetPath, json);
    }

    public ProfileConfig ImportProfile(string sourcePath)
    {
        var json = File.ReadAllText(sourcePath);
        var profile = JsonSerializer.Deserialize<ProfileConfig>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Profil dosyası boş.");

        NormalizeProfile(profile);
        _logService.Info($"Profile imported from {sourcePath}.");
        return profile;
    }

    private bool TryLoadLastGoodConfig(out AppConfig config)
    {
        config = DefaultConfigFactory.Create();
        if (!File.Exists(LastGoodConfigPath))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(LastGoodConfigPath);
            var loaded = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
            if (loaded is null)
            {
                return false;
            }

            Validate(loaded);
            UpgradeConfig(loaded);
            config = loaded;
            return true;
        }
        catch (Exception ex)
        {
            _logService.Error("Last good config load failed.", ex);
            return false;
        }
    }

    private static void Validate(AppConfig config)
    {
        if (config.Hotkey is null)
        {
            throw new InvalidOperationException("hotkey is required.");
        }

        config.Hotkey.Modifiers ??= [];

        if (string.IsNullOrWhiteSpace(config.Hotkey.Key))
        {
            throw new InvalidOperationException("hotkey.key is required.");
        }

        config.Profiles ??= [];

        if (config.Profiles.Count == 0)
        {
            throw new InvalidOperationException("profiles must contain at least one profile.");
        }

        foreach (var profile in config.Profiles)
        {
            NormalizeProfile(profile);
        }

        if (string.IsNullOrWhiteSpace(config.DefaultProfileId))
        {
            config.DefaultProfileId = config.Profiles[0].Id;
        }

        config.Theme ??= new ThemeConfig();
        config.Settings ??= new AppSettings();
    }

    private static void NormalizeProfile(ProfileConfig profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            profile.Id = "profile";
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            profile.Name = "İçe Aktarılan Profil";
        }

        profile.Matches ??= [];
        profile.Actions ??= [];
        NormalizeActions(profile.Actions);
    }

    private static void NormalizeActions(List<OrbitAction> actions)
    {
        foreach (var action in actions)
        {
            action.Children ??= [];
            NormalizeActions(action.Children);
        }
    }

    private static bool UpgradeConfig(AppConfig config)
    {
        if (config.ConfigVersion >= DefaultConfigFactory.CurrentVersion)
        {
            return false;
        }

        var defaults = DefaultConfigFactory.Create();

        if (IsKnownDefaultTheme(config.Theme))
        {
            config.Theme.Accent = defaults.Theme.Accent;
            config.Theme.ButtonSize = defaults.Theme.ButtonSize;
            config.Theme.RadiusX = defaults.Theme.RadiusX;
            config.Theme.RadiusY = defaults.Theme.RadiusY;
            config.Theme.Animation = defaults.Theme.Animation;
        }

        foreach (var defaultProfile in defaults.Profiles)
        {
            var profile = config.Profiles.FirstOrDefault(item =>
                string.Equals(item.Id, defaultProfile.Id, StringComparison.OrdinalIgnoreCase));
            if (profile is null)
            {
                continue;
            }

            profile.Name = defaultProfile.Name;
            ApplyActionDefaults(profile.Actions, defaultProfile.Actions);
        }

        config.ConfigVersion = DefaultConfigFactory.CurrentVersion;
        return true;
    }

    private static bool IsKnownDefaultTheme(ThemeConfig theme) =>
        string.Equals(theme.Accent, "#9F1D3D", StringComparison.OrdinalIgnoreCase)
        || string.Equals(theme.Accent, "#A51E39", StringComparison.OrdinalIgnoreCase)
        || Math.Abs(theme.ButtonSize - 82) < 0.1
        || Math.Abs(theme.ButtonSize - 66) < 0.1
        || Math.Abs(theme.ButtonSize - 72) < 0.1
        || Math.Abs(theme.RadiusX - 190) < 0.1
        || Math.Abs(theme.RadiusX - 134) < 0.1
        || Math.Abs(theme.RadiusX - 152) < 0.1
        || Math.Abs(theme.RadiusY - 155) < 0.1
        || Math.Abs(theme.RadiusY - 112) < 0.1
        || Math.Abs(theme.RadiusY - 126) < 0.1;

    private static void ApplyActionDefaults(List<OrbitAction> actions, List<OrbitAction> defaultActions)
    {
        foreach (var defaultAction in defaultActions)
        {
            var action = actions.FirstOrDefault(item =>
                string.Equals(item.Id, defaultAction.Id, StringComparison.OrdinalIgnoreCase));
            if (action is null)
            {
                continue;
            }

            action.Title = defaultAction.Title;
            action.Icon = defaultAction.Icon;

            if (action.Children.Count > 0 && defaultAction.Children.Count > 0)
            {
                ApplyActionDefaults(action.Children, defaultAction.Children);
            }
        }
    }

    private void BackupBrokenConfig(Exception exception)
    {
        _logService.Error("Config load failed.", exception);

        if (!File.Exists(ConfigPath))
        {
            return;
        }

        try
        {
            var backupPath = Path.Combine(
                AppDirectory,
                $"config.broken.{DateTime.Now:yyyyMMddHHmmss}.json");
            File.Copy(ConfigPath, backupPath, overwrite: true);
            _logService.Warn($"Broken config backed up to {backupPath}.");
        }
        catch (Exception backupException)
        {
            _logService.Error("Broken config backup failed.", backupException);
        }
    }

    private static void WriteAllTextAtomic(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = Path.Combine(
            directory ?? AppContext.BaseDirectory,
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(tempPath, contents);

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
