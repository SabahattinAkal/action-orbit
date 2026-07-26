using System.Text.Json;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.Services;

public sealed class ConfigService : IConfigPersistence
{
    private readonly LogService _logService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ConfigService(LogService logService, string? appDirectory = null)
    {
        _logService = logService;
        AppDirectory = string.IsNullOrWhiteSpace(appDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ActionOrbit")
            : appDirectory;
        Directory.CreateDirectory(AppDirectory);
        Directory.CreateDirectory(IconDirectory);
        IconCatalog.ConfigureCustomIconDirectory(IconDirectory);
    }

    public string AppDirectory { get; }

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
        Validate(CurrentConfig);
        var json = JsonSerializer.Serialize(CurrentConfig, _jsonOptions);
        WriteAllTextAtomic(targetPath, json);
    }

    public AppConfig ImportConfig(string sourcePath)
    {
        var config = ReadConfigForImport(sourcePath);
        Save(config);
        _logService.Info($"Config imported from {sourcePath}.");
        return CurrentConfig;
    }

    public AppConfig ReadConfigForImport(string sourcePath)
    {
        var json = File.ReadAllText(sourcePath);
        var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Config dosyası boş.");

        Validate(config);
        UpgradeConfig(config);
        return config;
    }

    public void ExportProfile(ProfileConfig profile, string targetPath)
    {
        var exportProfile = ProfileCopyService.Copy(profile, profile.Id, profile.Name);
        NormalizeProfile(exportProfile);
        ValidateActionTypes([exportProfile]);
        var json = JsonSerializer.Serialize(exportProfile, _jsonOptions);
        WriteAllTextAtomic(targetPath, json);
    }

    public ProfileConfig ImportProfile(string sourcePath)
    {
        var json = File.ReadAllText(sourcePath);
        var profile = JsonSerializer.Deserialize<ProfileConfig>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Profil dosyası boş.");

        NormalizeProfile(profile);
        ValidateActionTypes([profile]);
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
            Validate(loaded);
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
        if (config.ConfigVersion > DefaultConfigFactory.CurrentVersion)
        {
            throw new InvalidOperationException(
                $"Config sürümü desteklenmiyor: {config.ConfigVersion}. Bu dosya daha yeni bir Action Orbit sürümü gerektiriyor.");
        }

        if (config.Hotkey is null)
        {
            throw new InvalidOperationException("hotkey is required.");
        }

        config.Hotkey.Modifiers ??= [];

        if (string.IsNullOrWhiteSpace(config.Hotkey.Key))
        {
            throw new InvalidOperationException("hotkey.key is required.");
        }

        if (!HotkeyParser.TryParse(config.Hotkey, out _, out _))
        {
            throw new InvalidOperationException("hotkey contains an unsupported key or modifier.");
        }

        config.Hotkey.Display = BuildHotkeyDisplay(config.Hotkey);

        config.Profiles = config.Profiles?.OfType<ProfileConfig>().ToList() ?? [];

        if (config.Profiles.Count == 0)
        {
            throw new InvalidOperationException("profiles must contain at least one profile.");
        }

        foreach (var profile in config.Profiles)
        {
            NormalizeProfile(profile);
        }

        EnsureUniqueProfileIds(config.Profiles);

        if (string.IsNullOrWhiteSpace(config.DefaultProfileId) ||
            config.Profiles.All(profile =>
                !string.Equals(profile.Id, config.DefaultProfileId, StringComparison.OrdinalIgnoreCase)))
        {
            config.DefaultProfileId = config.Profiles[0].Id;
        }

        config.Theme ??= new ThemeConfig();
        config.Settings ??= new AppSettings();
        NormalizeTheme(config.Theme);
        ValidateActionTypes(config.Profiles);
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

        profile.Matches = profile.Matches?
            .OfType<ProfileMatch>()
            .Where(match => !string.IsNullOrWhiteSpace(match.ProcessName))
            .GroupBy(match => match.ProcessName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProfileMatch { ProcessName = group.Key })
            .ToList() ?? [];
        profile.Actions = profile.Actions?.OfType<OrbitAction>().ToList() ?? [];
        NormalizeActions(profile.Actions);
        EnsureUniqueActionIds(profile.Actions);
    }

    private static void NormalizeActions(List<OrbitAction> actions)
    {
        foreach (var action in actions)
        {
            action.Id = action.Id?.Trim() ?? "";
            action.Title = action.Title?.Trim() ?? "";
            action.Icon = action.Icon?.Trim() ?? "";
            action.Type = action.Type?.Trim().ToLowerInvariant() ?? "";
            action.Target ??= "";
            action.Arguments ??= "";
            action.Children = action.Children?.OfType<OrbitAction>().ToList() ?? [];
            NormalizeActions(action.Children);
            if (action.Children.Count > 0
                && !string.Equals(action.Type, "folder", StringComparison.OrdinalIgnoreCase)
                && ActionDefinitionCatalog.TypeOptions.Any(option =>
                    string.Equals(option.Key, action.Type, StringComparison.OrdinalIgnoreCase)))
            {
                action.Type = "folder";
            }
        }
    }

    private static void NormalizeTheme(ThemeConfig theme)
    {
        theme.Mode = theme.Mode?.Trim().ToLowerInvariant() switch
        {
            "light" => "light",
            "dark" => "dark",
            _ => "system"
        };

        var accent = theme.Accent?.Trim() ?? "";
        theme.Accent = accent.Length == 7
            && accent[0] == '#'
            && accent[1..].All(Uri.IsHexDigit)
                ? accent
                : "#A51E39";
        theme.ButtonSize = ClampFinite(theme.ButtonSize, 54, 96, 60);
        theme.RadiusX = ClampFinite(theme.RadiusX, 96, 190, 116);
        theme.RadiusY = ClampFinite(theme.RadiusY, 82, 168, 98);
    }

    private static double ClampFinite(double value, double min, double max, double fallback) =>
        double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;

    private static string BuildHotkeyDisplay(HotkeyConfig hotkey)
    {
        var modifiers = hotkey.Modifiers.Select(modifier =>
            modifier.Trim().ToLowerInvariant() switch
            {
                "control" or "ctrl" => "Ctrl",
                "alt" => "Alt",
                "shift" => "Shift",
                "win" or "windows" => "Win",
                _ => modifier.Trim()
            });

        return string.Join("+", modifiers.Append(hotkey.Key.Trim()));
    }

    private static void EnsureUniqueProfileIds(IEnumerable<ProfileConfig> profiles)
    {
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in profiles)
        {
            profile.Id = CreateUniqueId(profile.Id, "profile", usedIds);
        }
    }

    private static void EnsureUniqueActionIds(IEnumerable<OrbitAction> actions)
    {
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        EnsureUniqueActionIds(actions, usedIds);
    }

    private static void EnsureUniqueActionIds(
        IEnumerable<OrbitAction> actions,
        HashSet<string> usedIds)
    {
        foreach (var action in actions)
        {
            var fallback = action.IsFolder ? "folder" : "action";
            action.Id = CreateUniqueId(action.Id, fallback, usedIds);
            EnsureUniqueActionIds(action.Children, usedIds);
        }
    }

    private static string CreateUniqueId(string? requestedId, string fallback, HashSet<string> usedIds)
    {
        var baseId = string.IsNullOrWhiteSpace(requestedId) ? fallback : requestedId.Trim();
        var candidate = baseId;
        var suffix = 2;

        while (!usedIds.Add(candidate))
        {
            candidate = $"{baseId}_{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static void ValidateActionTypes(IEnumerable<ProfileConfig> profiles)
    {
        var supportedTypes = ActionDefinitionCatalog.TypeOptions
            .Select(option => option.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in profiles)
        {
            ValidateActionTypes(profile.Actions, profile.Name, supportedTypes);
        }
    }

    private static void ValidateActionTypes(
        IEnumerable<OrbitAction> actions,
        string profileName,
        HashSet<string> supportedTypes)
    {
        foreach (var action in actions)
        {
            if (!supportedTypes.Contains(action.Type))
            {
                throw new InvalidOperationException(
                    $"{profileName} profilindeki '{action.Title}' aksiyonunun türü desteklenmiyor: {action.Type}");
            }

            ValidateActionTypes(action.Children, profileName, supportedTypes);
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
