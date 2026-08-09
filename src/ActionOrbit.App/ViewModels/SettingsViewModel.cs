using System.Windows.Input;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly IStartupRegistration _startupService;
    private readonly LogService _logService;
    private readonly Action _markDirty;
    private readonly Action<string> _setStatus;
    private readonly Action<bool, bool> _setSaveState;
    private string _themeMode = "dark";
    private string _accentInput = "#A51E39";
    private double _overlayButtonSize;
    private double _overlayRadiusX;
    private double _overlayRadiusY;
    private bool _isSyncingFields;
    private bool _startupWithWindows;
    private bool _closeToTray = true;
    private bool _allowCommandActions;
    private bool _overlayAnimation = true;
    private string _activationMode = "toggle";
    private double _holdDelayMilliseconds = 260;
    private double _doublePressWindowMilliseconds = 380;
    private bool _cancelWhenPointerLeaves;
    private string _suppressedProcessesText = "";
    private bool _shelfEnabled = true;
    private bool _rememberRecentShelves;
    private double _shelfMaxItems = 20;
    private double _shelfRetentionHours = 24;

    public SettingsViewModel(
        ConfigService configService,
        IStartupRegistration startupService,
        LogService logService,
        Action markDirty,
        Action<string> setStatus,
        Action<bool, bool> setSaveState)
    {
        _configService = configService;
        _startupService = startupService;
        _logService = logService;
        _markDirty = markDirty;
        _setStatus = setStatus;
        _setSaveState = setSaveState;
        ApplyThemeSettingsCommand = new RelayCommand(ApplyThemeSettings);
        UseAccentPresetCommand = new RelayCommand(parameter => UseAccentPreset(parameter?.ToString()));
        ResetVisualSettingsCommand = new RelayCommand(ResetVisualSettings);
    }

    public IReadOnlyList<ThemeModeOption> ThemeModeOptions { get; } =
    [
        new("system", "Windows ile aynı"),
        new("dark", "Koyu"),
        new("light", "Açık")
    ];
    public IReadOnlyList<AccentPresetOption> AccentPresets { get; } =
    [
        new("Orbit", "#A51E39"),
        new("Mavi", "#2563EB"),
        new("Mor", "#7C3AED"),
        new("Yeşil", "#15803D"),
        new("Turuncu", "#C2410C")
    ];
    public IReadOnlyList<ActivationModeOption> ActivationModeOptions { get; } =
    [
        new("toggle", "Basınca aç / tekrar basınca kapat"),
        new("hold", "Basılı tut / bırakınca çalıştır"),
        new("double_press", "Çift basınca aç")
    ];
    public ICommand ApplyThemeSettingsCommand { get; }
    public ICommand UseAccentPresetCommand { get; }
    public ICommand ResetVisualSettingsCommand { get; }
    public bool IsHoldMode => string.Equals(ActivationMode, "hold", StringComparison.Ordinal);
    public bool IsDoublePressMode => string.Equals(ActivationMode, "double_press", StringComparison.Ordinal);
    public bool HasAccentIssue => !IsValidAccent(AccentInput);
    public bool CanApplyVisualSettings => !HasAccentIssue;
    public string AccentIssueMessage => HasAccentIssue
        ? "Renk #RRGGBB biçiminde olmalı. Örn: #A51E39"
        : "";

    public bool StartupWithWindows
    {
        get => _startupWithWindows;
        set
        {
            if (!SetProperty(ref _startupWithWindows, value) || _isSyncingFields)
            {
                return;
            }

            try
            {
                _startupService.SetEnabled(value);
                _configService.CurrentConfig.Settings.RunAtStartup = value;
                _markDirty();
                _setStatus(value
                    ? "Windows başlangıcında çalışma açıldı."
                    : "Windows başlangıcında çalışma kapatıldı.");
            }
            catch (Exception ex)
            {
                _isSyncingFields = true;
                SetProperty(ref _startupWithWindows, !value, nameof(StartupWithWindows));
                _isSyncingFields = false;
                _setStatus($"Başlangıç ayarı değiştirilemedi: {ex.Message}");
                _logService.Error("Startup setting update failed.", ex);
            }
        }
    }

    public bool CloseToTray
    {
        get => _closeToTray;
        set
        {
            if (!SetProperty(ref _closeToTray, value) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.CloseToTray = value;
            _markDirty();
            _setStatus(value
                ? "Kapat düğmesi artık uygulamayı tray'e alacak."
                : "Kapat düğmesi artık uygulamadan tamamen çıkacak.");
        }
    }

    public bool AllowCommandActions
    {
        get => _allowCommandActions;
        set
        {
            if (!SetProperty(ref _allowCommandActions, value) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.AllowCommandActions = value;
            _markDirty();
            _setStatus(value
                ? "Komut aksiyonları açıldı. Her çalıştırmada ayrıca onay istenecek."
                : "Komut aksiyonları kapatıldı.");
        }
    }

    public bool OverlayAnimation
    {
        get => _overlayAnimation;
        set
        {
            if (!SetProperty(ref _overlayAnimation, value) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Theme.Animation = value;
            _markDirty();
        }
    }

    public string ActivationMode
    {
        get => _activationMode;
        set
        {
            var normalized = value is "hold" or "double_press" ? value : "toggle";
            if (!SetProperty(ref _activationMode, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(IsHoldMode));
            OnPropertyChanged(nameof(IsDoublePressMode));
            if (_isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.Activation.Mode = normalized;
            _markDirty();
        }
    }

    public double HoldDelayMilliseconds
    {
        get => _holdDelayMilliseconds;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 100, 1200));
            if (!SetProperty(ref _holdDelayMilliseconds, clamped) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.Activation.HoldDelayMilliseconds = (int)clamped;
            _markDirty();
        }
    }

    public double DoublePressWindowMilliseconds
    {
        get => _doublePressWindowMilliseconds;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 200, 900));
            if (!SetProperty(ref _doublePressWindowMilliseconds, clamped) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.Activation.DoublePressWindowMilliseconds = (int)clamped;
            _markDirty();
        }
    }

    public bool CancelWhenPointerLeaves
    {
        get => _cancelWhenPointerLeaves;
        set
        {
            if (!SetProperty(ref _cancelWhenPointerLeaves, value) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.Activation.CancelWhenPointerLeaves = value;
            _markDirty();
        }
    }

    public string SuppressedProcessesText
    {
        get => _suppressedProcessesText;
        set
        {
            if (!SetProperty(ref _suppressedProcessesText, value) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.Activation.SuppressedProcesses = ParseProcesses(value);
            _markDirty();
        }
    }

    public bool ShelfEnabled
    {
        get => _shelfEnabled;
        set
        {
            if (!SetProperty(ref _shelfEnabled, value) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.Shelf.Enabled = value;
            _markDirty();
            _setStatus(value ? "Orbit Shelf etkinleştirildi." : "Orbit Shelf devre dışı bırakıldı.");
        }
    }

    public bool RememberRecentShelves
    {
        get => _rememberRecentShelves;
        set
        {
            if (!SetProperty(ref _rememberRecentShelves, value) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.Shelf.RememberRecentShelves = value;
            _markDirty();
            _setStatus(value
                ? "Son kullanılan raflar uygulama kapanınca da korunacak."
                : "Yalnızca sabitlenen raflar uygulama kapanınca korunacak.");
        }
    }

    public double ShelfMaxItems
    {
        get => _shelfMaxItems;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 5, 100));
            if (!SetProperty(ref _shelfMaxItems, clamped) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.Shelf.MaxItemsPerShelf = (int)clamped;
            _markDirty();
        }
    }

    public double ShelfRetentionHours
    {
        get => _shelfRetentionHours;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 1, 168));
            if (!SetProperty(ref _shelfRetentionHours, clamped) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.Shelf.RetentionHours = (int)clamped;
            _markDirty();
        }
    }

    public string ThemeMode
    {
        get => _themeMode;
        set
        {
            var normalized = NormalizeThemeMode(value);
            if (!SetProperty(ref _themeMode, normalized) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Theme.Mode = normalized;
            _markDirty();
        }
    }

    public string AccentInput
    {
        get => _accentInput;
        set
        {
            if (!SetProperty(ref _accentInput, value ?? ""))
            {
                return;
            }

            OnPropertyChanged(nameof(HasAccentIssue));
            OnPropertyChanged(nameof(CanApplyVisualSettings));
            OnPropertyChanged(nameof(AccentIssueMessage));
        }
    }

    public double OverlayButtonSize
    {
        get => _overlayButtonSize;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 54, 96));
            if (!SetProperty(ref _overlayButtonSize, clamped) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Theme.ButtonSize = clamped;
            _markDirty();
        }
    }

    public double OverlayRadiusX
    {
        get => _overlayRadiusX;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 96, 190));
            if (!SetProperty(ref _overlayRadiusX, clamped) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Theme.RadiusX = clamped;
            _markDirty();
        }
    }

    public double OverlayRadiusY
    {
        get => _overlayRadiusY;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 82, 168));
            if (!SetProperty(ref _overlayRadiusY, clamped) || _isSyncingFields)
            {
                return;
            }

            _configService.CurrentConfig.Theme.RadiusY = clamped;
            _markDirty();
        }
    }

    public void RefreshFromConfig()
    {
        _configService.CurrentConfig.Settings ??= new AppSettings();
        _configService.CurrentConfig.Settings.Activation ??= new ActivationSettings();
        _configService.CurrentConfig.Settings.Shelf ??= new ShelfSettings();
        _configService.CurrentConfig.Theme ??= new ThemeConfig();

        _isSyncingFields = true;
        try
        {
            StartupWithWindows = _configService.CurrentConfig.Settings.RunAtStartup;
            CloseToTray = _configService.CurrentConfig.Settings.CloseToTray;
            AllowCommandActions = _configService.CurrentConfig.Settings.AllowCommandActions;
            ThemeMode = NormalizeThemeMode(_configService.CurrentConfig.Theme.Mode);
            AccentInput = string.IsNullOrWhiteSpace(_configService.CurrentConfig.Theme.Accent)
                ? "#A51E39"
                : _configService.CurrentConfig.Theme.Accent;
            OverlayButtonSize = Math.Clamp(_configService.CurrentConfig.Theme.ButtonSize, 54, 96);
            OverlayRadiusX = Math.Clamp(_configService.CurrentConfig.Theme.RadiusX, 96, 190);
            OverlayRadiusY = Math.Clamp(_configService.CurrentConfig.Theme.RadiusY, 82, 168);
            OverlayAnimation = _configService.CurrentConfig.Theme.Animation;
            ActivationMode = _configService.CurrentConfig.Settings.Activation.Mode;
            HoldDelayMilliseconds = _configService.CurrentConfig.Settings.Activation.HoldDelayMilliseconds;
            DoublePressWindowMilliseconds = _configService.CurrentConfig.Settings.Activation.DoublePressWindowMilliseconds;
            CancelWhenPointerLeaves = _configService.CurrentConfig.Settings.Activation.CancelWhenPointerLeaves;
            SuppressedProcessesText = string.Join(", ", _configService.CurrentConfig.Settings.Activation.SuppressedProcesses);
            ShelfEnabled = _configService.CurrentConfig.Settings.Shelf.Enabled;
            RememberRecentShelves = _configService.CurrentConfig.Settings.Shelf.RememberRecentShelves;
            ShelfMaxItems = _configService.CurrentConfig.Settings.Shelf.MaxItemsPerShelf;
            ShelfRetentionHours = _configService.CurrentConfig.Settings.Shelf.RetentionHours;
        }
        finally
        {
            _isSyncingFields = false;
        }
    }

    public bool IsStartupRegistrationEnabled() =>
        _startupService.IsEnabled();

    public bool TryApplyStartupRegistration(bool enabled, out string issueMessage)
    {
        try
        {
            _startupService.SetEnabled(enabled);
            issueMessage = "";
            return true;
        }
        catch (Exception ex)
        {
            issueMessage = $"Windows başlangıç ayarı uygulanamadı: {ex.Message}";
            _logService.Error("Imported startup setting update failed.", ex);
            return false;
        }
    }

    public void RestoreStartupRegistration(bool enabled)
    {
        try
        {
            _startupService.SetEnabled(enabled);
        }
        catch (Exception ex)
        {
            _logService.Error("Previous startup setting could not be restored.", ex);
        }
    }

    public void CompleteExternalConfigChange()
    {
        RefreshFromConfig();
        ThemeService.ApplyApplicationTheme(ThemeMode, AccentInput);
    }

    private void ApplyThemeSettings()
    {
        if (!IsValidAccent(AccentInput))
        {
            _setStatus("Accent rengi #RRGGBB formatında olmalı. Örn: #A51E39");
            return;
        }

        _configService.CurrentConfig.Theme.Mode = NormalizeThemeMode(ThemeMode);
        _configService.CurrentConfig.Theme.Accent = AccentInput.Trim();
        _configService.CurrentConfig.Theme.ButtonSize = OverlayButtonSize;
        _configService.CurrentConfig.Theme.RadiusX = OverlayRadiusX;
        _configService.CurrentConfig.Theme.RadiusY = OverlayRadiusY;
        _configService.CurrentConfig.Theme.Animation = OverlayAnimation;

        try
        {
            _configService.Save(_configService.CurrentConfig);
            _setSaveState(false, false);
            RefreshFromConfig();
            ThemeService.ApplyApplicationTheme(ThemeMode, AccentInput);
            _setStatus("Tema ve overlay ayarları kaydedildi. Önizle ile kontrol edebilirsin.");
        }
        catch (Exception ex)
        {
            _setStatus($"Tema ayarları kaydedilemedi: {ex.Message}");
            _logService.Error("Theme settings save failed.", ex);
        }
    }

    private void UseAccentPreset(string? color)
    {
        if (!IsValidAccent(color ?? ""))
        {
            return;
        }

        AccentInput = color!.ToUpperInvariant();
        ApplyThemeSettings();
    }

    private void ResetVisualSettings()
    {
        var defaults = DefaultConfigFactory.Create().Theme;
        ThemeMode = defaults.Mode;
        AccentInput = defaults.Accent;
        OverlayButtonSize = defaults.ButtonSize;
        OverlayRadiusX = defaults.RadiusX;
        OverlayRadiusY = defaults.RadiusY;
        OverlayAnimation = defaults.Animation;
        ApplyThemeSettings();
    }

    private static string NormalizeThemeMode(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "light" => "light",
            "dark" => "dark",
            _ => "system"
        };

    private static bool IsValidAccent(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length == 7
        && value[0] == '#'
        && value[1..].All(Uri.IsHexDigit);

    private static List<string> ParseProcesses(string? value) =>
        (value ?? "")
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(process => process.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? process : $"{process}.exe")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(ConfigService.MaxMatchesPerProfile)
            .ToList();
}

public sealed record ActivationModeOption(string Key, string Label);
public sealed record ThemeModeOption(string Key, string Label);
public sealed record AccentPresetOption(string Label, string Color);
