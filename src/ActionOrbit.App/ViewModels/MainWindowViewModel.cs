using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly HotkeyService _hotkeyService;
    private readonly ActiveWindowService _activeWindowService;
    private readonly ProfileService _profileService;
    private readonly OverlayService _overlayService;
    private readonly ActionExecutionService _actionExecutionService;
    private readonly StartupService _startupService;
    private readonly LogService _logService;
    private readonly DispatcherTimer _autosaveTimer;
    private readonly DispatcherTimer _activeProcessTimer;
    private readonly string _ownProcessName = $"{Process.GetCurrentProcess().ProcessName}.exe";
    private string _hotkeyDisplay = "";
    private string _statusMessage = "";
    private string _activeProcessName = "";
    private string _activeProfileName = "";
    private string _hotkeyInput = "";
    private string _themeMode = "dark";
    private string _accentInput = "#A51E39";
    private int _profileCount;
    private double _overlayButtonSize;
    private double _overlayRadiusX;
    private double _overlayRadiusY;
    private bool _isHotkeyRegistered;
    private bool _isSyncingProfileFields;
    private bool _isSyncingSettingsFields;
    private bool _isReloadingEditor;
    private bool _hasUnsavedChanges;
    private bool _startupWithWindows;
    private bool _closeToTray = true;
    private ProfileConfig? _selectedProfile;
    private ActionEditorRowViewModel? _selectedAction;
    private ActionPresetOption? _selectedPreset;
    private RunningAppOption? _selectedRunningApp;
    private string _selectedProfileId = "";
    private string _selectedProfileName = "";
    private string _selectedProfileMatchesText = "";

    public MainWindowViewModel(
        ConfigService configService,
        HotkeyService hotkeyService,
        ActiveWindowService activeWindowService,
        ProfileService profileService,
        OverlayService overlayService,
        ActionExecutionService actionExecutionService,
        StartupService startupService,
        LogService logService)
    {
        _configService = configService;
        _hotkeyService = hotkeyService;
        _activeWindowService = activeWindowService;
        _profileService = profileService;
        _overlayService = overlayService;
        _actionExecutionService = actionExecutionService;
        _startupService = startupService;
        _logService = logService;

        OpenConfigCommand = new RelayCommand(OpenConfig);
        OpenLogCommand = new RelayCommand(OpenLog);
        OpenConfigFolderCommand = new RelayCommand(OpenConfigFolder);
        ExportConfigCommand = new RelayCommand(ExportConfig);
        ImportConfigCommand = new RelayCommand(ImportConfig);
        ExportProfileCommand = new RelayCommand(ExportSelectedProfile);
        ImportProfileCommand = new RelayCommand(ImportProfile);
        ReloadConfigCommand = new RelayCommand(ReloadConfig);
        ShowOverlayCommand = new RelayCommand(ShowOverlay);
        DetectProfileCommand = new RelayCommand(DetectProfile);
        RefreshRunningAppsCommand = new RelayCommand(RefreshRunningApps);
        AddSelectedRunningAppToProfileCommand = new RelayCommand(AddSelectedRunningAppToProfile);
        RemoveProfileMatchCommand = new RelayCommand(parameter => RemoveProfileMatch(parameter?.ToString()));
        SaveHotkeyCommand = new RelayCommand(SaveHotkey);
        ApplyThemeSettingsCommand = new RelayCommand(ApplyThemeSettings);
        AddActiveProcessToProfileCommand = new RelayCommand(AddActiveProcessToProfile);
        SaveConfigCommand = new RelayCommand(SaveConfig);
        AddActionCommand = new RelayCommand(AddAction);
        AddFolderCommand = new RelayCommand(AddFolder);
        AddChildActionCommand = new RelayCommand(AddChildAction);
        AddProfileCommand = new RelayCommand(AddProfile);
        DeleteProfileCommand = new RelayCommand(DeleteProfile);
        ApplyPresetCommand = new RelayCommand(ApplySelectedPreset);
        ImportIconCommand = new RelayCommand(ImportIcon);
        DeleteActionCommand = new RelayCommand(DeleteAction);
        BrowseActionTargetCommand = new RelayCommand(BrowseActionTarget);
        TestActionCommand = new RelayCommand(TestSelectedAction);
        MoveActionUpCommand = new RelayCommand(() => MoveSelectedAction(-1));
        MoveActionDownCommand = new RelayCommand(() => MoveSelectedAction(1));
        MoveActionOutOfFolderCommand = new RelayCommand(MoveSelectedActionOutOfFolder);

        _actionExecutionService.ActionExecuted += OnActionExecuted;
        _hotkeyService.HotkeyPressed += (_, _) =>
            System.Windows.Application.Current.Dispatcher.Invoke(_overlayService.ShowOverlay);

        RefreshConfigSummary();
        RefreshSettingsFromConfig();
        ReloadEditorFromConfig();
        RefreshAvailableIcons();
        UpdateActiveProcessPreview();
        RefreshRunningApps();
        SelectedPreset = ActionPresets.FirstOrDefault();

        _autosaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _autosaveTimer.Tick += (_, _) => SaveDirtyConfig();
        _autosaveTimer.Start();

        _activeProcessTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        _activeProcessTimer.Tick += (_, _) => UpdateActiveProcessPreview();
        _activeProcessTimer.Start();
        StatusMessage = "Hazır. Pencere açılınca global kısayol kaydedilecek.";
    }

    public event Action<string, bool>? UserNotificationRequested;

    public ObservableCollection<ProfileConfig> Profiles { get; } = [];
    public ObservableCollection<ActionEditorRowViewModel> ActionRows { get; } = [];
    public ObservableCollection<IconOption> AvailableIcons { get; } = [];
    public ObservableCollection<string> SelectedProfileMatchChips { get; } = [];
    public ObservableCollection<RunningAppOption> RunningApps { get; } = [];
    public IReadOnlyList<string> ThemeModeOptions { get; } = ["system", "light", "dark"];
    public IReadOnlyList<ActionTypeOption> ActionTypeOptions { get; } = ActionDefinitionCatalog.TypeOptions;
    public IReadOnlyList<ActionPresetOption> ActionPresets { get; } = ActionDefinitionCatalog.Presets;
    public IReadOnlyList<string> AvailableIconKeys => IconCatalog.AvailableKeys;

    public ICommand OpenConfigCommand { get; }
    public ICommand OpenLogCommand { get; }
    public ICommand OpenConfigFolderCommand { get; }
    public ICommand ExportConfigCommand { get; }
    public ICommand ImportConfigCommand { get; }
    public ICommand ExportProfileCommand { get; }
    public ICommand ImportProfileCommand { get; }
    public ICommand ReloadConfigCommand { get; }
    public ICommand ShowOverlayCommand { get; }
    public ICommand DetectProfileCommand { get; }
    public ICommand RefreshRunningAppsCommand { get; }
    public ICommand AddSelectedRunningAppToProfileCommand { get; }
    public ICommand RemoveProfileMatchCommand { get; }
    public ICommand SaveHotkeyCommand { get; }
    public ICommand ApplyThemeSettingsCommand { get; }
    public ICommand AddActiveProcessToProfileCommand { get; }
    public ICommand SaveConfigCommand { get; }
    public ICommand AddActionCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand AddChildActionCommand { get; }
    public ICommand AddProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }
    public ICommand ApplyPresetCommand { get; }
    public ICommand ImportIconCommand { get; }
    public ICommand DeleteActionCommand { get; }
    public ICommand BrowseActionTargetCommand { get; }
    public ICommand TestActionCommand { get; }
    public ICommand MoveActionUpCommand { get; }
    public ICommand MoveActionDownCommand { get; }
    public ICommand MoveActionOutOfFolderCommand { get; }

    public ProfileConfig? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value))
            {
                SyncProfileFieldsFromSelectedProfile();
                RebuildActionRows();
                OnPropertyChanged(nameof(SelectedProfileIsDefault));
            }
        }
    }

    public ActionEditorRowViewModel? SelectedAction
    {
        get => _selectedAction;
        set
        {
            if (SetProperty(ref _selectedAction, value))
            {
                RefreshSelectedActionState();
            }
        }
    }

    public ActionPresetOption? SelectedPreset
    {
        get => _selectedPreset;
        set => SetProperty(ref _selectedPreset, value);
    }

    public RunningAppOption? SelectedRunningApp
    {
        get => _selectedRunningApp;
        set => SetProperty(ref _selectedRunningApp, value);
    }

    public string HotkeyDisplay
    {
        get => _hotkeyDisplay;
        private set => SetProperty(ref _hotkeyDisplay, value);
    }

    public string HotkeyInput
    {
        get => _hotkeyInput;
        set => SetProperty(ref _hotkeyInput, value);
    }

    public string HotkeyStateText => IsHotkeyRegistered ? "Aktif" : "Pasif";
    public string HotkeyBadgeBackground => IsHotkeyRegistered ? "#EAF8EF" : "#FFF1F2";
    public string HotkeyBadgeForeground => IsHotkeyRegistered ? "#166534" : "#9F1239";
    public string HotkeyDotBrush => IsHotkeyRegistered ? "#22C55E" : "#F43F5E";

    public string ConfigPath => _configService.ConfigPath;
    public string ConfigFolderPath => _configService.AppDirectory;
    public string LogPath => _logService.LogPath;

    public bool StartupWithWindows
    {
        get => _startupWithWindows;
        set
        {
            if (!SetProperty(ref _startupWithWindows, value))
            {
                return;
            }

            if (_isSyncingSettingsFields)
            {
                return;
            }

            try
            {
                _startupService.SetEnabled(value);
                _configService.CurrentConfig.Settings.RunAtStartup = value;
                MarkDirty();
                StatusMessage = value
                    ? "Windows başlangıcında çalışma açıldı."
                    : "Windows başlangıcında çalışma kapatıldı.";
            }
            catch (Exception ex)
            {
                _isSyncingSettingsFields = true;
                SetProperty(ref _startupWithWindows, !value, nameof(StartupWithWindows));
                _isSyncingSettingsFields = false;
                StatusMessage = $"Başlangıç ayarı değiştirilemedi: {ex.Message}";
                _logService.Error("Startup setting update failed.", ex);
            }
        }
    }

    public bool CloseToTray
    {
        get => _closeToTray;
        set
        {
            if (!SetProperty(ref _closeToTray, value))
            {
                return;
            }

            if (_isSyncingSettingsFields)
            {
                return;
            }

            _configService.CurrentConfig.Settings.CloseToTray = value;
            MarkDirty();
            StatusMessage = value
                ? "Kapat düğmesi artık uygulamayı tray'e alacak."
                : "Kapat düğmesi artık uygulamadan tamamen çıkacak.";
        }
    }

    public string ThemeMode
    {
        get => _themeMode;
        set
        {
            var normalized = NormalizeThemeMode(value);
            if (!SetProperty(ref _themeMode, normalized))
            {
                return;
            }

            if (_isSyncingSettingsFields)
            {
                return;
            }

            _configService.CurrentConfig.Theme.Mode = normalized;
            MarkDirty();
        }
    }

    public string AccentInput
    {
        get => _accentInput;
        set => SetProperty(ref _accentInput, value);
    }

    public double OverlayButtonSize
    {
        get => _overlayButtonSize;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 54, 96));
            if (!SetProperty(ref _overlayButtonSize, clamped))
            {
                return;
            }

            if (_isSyncingSettingsFields)
            {
                return;
            }

            _configService.CurrentConfig.Theme.ButtonSize = clamped;
            MarkDirty();
        }
    }

    public double OverlayRadiusX
    {
        get => _overlayRadiusX;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 96, 190));
            if (!SetProperty(ref _overlayRadiusX, clamped))
            {
                return;
            }

            if (_isSyncingSettingsFields)
            {
                return;
            }

            _configService.CurrentConfig.Theme.RadiusX = clamped;
            MarkDirty();
        }
    }

    public double OverlayRadiusY
    {
        get => _overlayRadiusY;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 82, 168));
            if (!SetProperty(ref _overlayRadiusY, clamped))
            {
                return;
            }

            if (_isSyncingSettingsFields)
            {
                return;
            }

            _configService.CurrentConfig.Theme.RadiusY = clamped;
            MarkDirty();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ActiveProcessName
    {
        get => _activeProcessName;
        private set => SetProperty(ref _activeProcessName, value);
    }

    public string ActiveProfileName
    {
        get => _activeProfileName;
        private set => SetProperty(ref _activeProfileName, value);
    }

    public int ProfileCount
    {
        get => _profileCount;
        private set => SetProperty(ref _profileCount, value);
    }

    public bool IsHotkeyRegistered
    {
        get => _isHotkeyRegistered;
        private set
        {
            if (SetProperty(ref _isHotkeyRegistered, value))
            {
                OnPropertyChanged(nameof(HotkeyStateText));
                OnPropertyChanged(nameof(HotkeyBadgeBackground));
                OnPropertyChanged(nameof(HotkeyBadgeForeground));
                OnPropertyChanged(nameof(HotkeyDotBrush));
            }
        }
    }

    public bool SelectedProfileIsDefault =>
        SelectedProfile is not null &&
        string.Equals(SelectedProfile.Id, _configService.CurrentConfig.DefaultProfileId, StringComparison.OrdinalIgnoreCase);

    public bool HasSelectedAction => SelectedAction is not null;
    public bool HasNoSelectedAction => SelectedAction is null;
    public bool SelectedActionIsFolder => SelectedAction?.IsFolder == true;
    public bool SelectedActionIsChild => SelectedAction?.IsChild == true;
    public bool CanMoveSelectedActionOutOfFolder => SelectedAction?.Parent is not null;
    public string SelectedActionFolderSummary =>
        SelectedAction is { IsFolder: true } action
            ? $"{action.Title} klasöründe {action.ChildCount} alt aksiyon var. Yeni alt aksiyon ekleyebilir veya listedeki aksiyonları bu klasörün üstüne sürükleyebilirsin."
            : "";
    public string SelectedActionParentSummary =>
        SelectedAction is { Parent: not null } action
            ? $"{action.Title}, {action.ParentTitle} klasörünün içinde. Gerekirse aksiyonu ana seviyeye çıkarabilirsin."
            : "";
    public bool CanBrowseSelectedAction =>
        SelectedAction?.Type is "open_app" or "open_file" or "open_folder";
    public bool HasSelectedActionValidation =>
        !string.IsNullOrWhiteSpace(SelectedActionValidationMessage);
    public string SelectedActionValidationMessage =>
        SelectedAction is null
            ? ""
            : ValidateAction(SelectedAction, allowCommandWarning: true, out var message)
                ? ""
                : message;

    public string SelectedProfileId
    {
        get => _selectedProfileId;
        set
        {
            var normalized = NormalizeId(value, "profile");
            if (!SetProperty(ref _selectedProfileId, normalized))
            {
                return;
            }

            if (_isSyncingProfileFields || SelectedProfile is null)
            {
                return;
            }

            SelectedProfile.Id = normalized;
            RefreshProfileList();
            MarkDirty();
        }
    }

    public string SelectedProfileName
    {
        get => _selectedProfileName;
        set
        {
            if (!SetProperty(ref _selectedProfileName, value))
            {
                return;
            }

            if (_isSyncingProfileFields || SelectedProfile is null)
            {
                return;
            }

            SelectedProfile.Name = string.IsNullOrWhiteSpace(value) ? "Yeni Profil" : value.Trim();
            RefreshProfileList();
            MarkDirty();
        }
    }

    public string SelectedProfileMatchesText
    {
        get => _selectedProfileMatchesText;
        set
        {
            if (!SetProperty(ref _selectedProfileMatchesText, value))
            {
                return;
            }

            if (_isSyncingProfileFields || SelectedProfile is null)
            {
                return;
            }

            SelectedProfile.Matches = ParseProfileMatches(value);
            RefreshSelectedProfileMatchChips();
            MarkDirty();
        }
    }

    public void RegisterHotkey()
    {
        try
        {
            _hotkeyService.Register(_configService.CurrentConfig.Hotkey);
            IsHotkeyRegistered = _hotkeyService.IsRegistered;
            HotkeyInput = _configService.CurrentConfig.Hotkey.Display;
            StatusMessage = $"Kısayol aktif: {HotkeyDisplay}";
        }
        catch (Exception ex)
        {
            IsHotkeyRegistered = false;
            StatusMessage = $"Kısayol kaydedilemedi: {ex.Message}";
            _logService.Error("Hotkey registration failed.", ex);
        }
    }

    private void ReloadConfig()
    {
        try
        {
            _configService.Reload();
            RefreshConfigSummary();
            RefreshSettingsFromConfig();
            ReloadEditorFromConfig();
            RegisterHotkey();
            DetectProfile();
            StatusMessage = $"Config yenilendi. Kısayol: {HotkeyDisplay}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Config yenilenemedi: {ex.Message}";
            _logService.Error("Config reload failed.", ex);
        }
    }

    private void OpenConfig() =>
        OpenPath(_configService.ConfigPath);

    private void OpenConfigFolder() =>
        OpenPath(_configService.AppDirectory);

    private void OpenLog()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_logService.LogPath)!);
        if (!File.Exists(_logService.LogPath))
        {
            File.WriteAllText(_logService.LogPath, "");
        }

        OpenPath(_logService.LogPath);
    }

    private void ShowOverlay() =>
        _overlayService.ShowOverlay();

    private void DetectProfile()
    {
        UpdateActiveProcessPreview();
        RefreshRunningApps();
    }

    private void AddActiveProcessToProfile()
    {
        UpdateActiveProcessPreview();
        AddProcessNameToSelectedProfile(ActiveProcessName, "Aktif uygulama");
    }

    private void AddSelectedRunningAppToProfile()
    {
        if (SelectedRunningApp is null)
        {
            StatusMessage = "Önce listeden çalışan bir uygulama seç.";
            return;
        }

        AddProcessNameToSelectedProfile(SelectedRunningApp.ProcessName, "Seçili uygulama");
    }

    private void AddProcessNameToSelectedProfile(string processName, string sourceLabel)
    {
        if (SelectedProfile is null)
        {
            StatusMessage = "Önce bir profil seç.";
            return;
        }

        if (string.IsNullOrWhiteSpace(processName))
        {
            StatusMessage = $"{sourceLabel} algılanamadı.";
            return;
        }

        if (string.Equals(processName, _ownProcessName, StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Action Orbit kendi penceresini profile eklemez.";
            return;
        }

        if (SelectedProfile.Matches.Any(match =>
            string.Equals(match.ProcessName, processName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = $"{processName} zaten bu profile bağlı.";
            return;
        }

        SelectedProfile.Matches.Add(new ProfileMatch { ProcessName = processName });
        SelectedProfileMatchesText = string.Join(", ", SelectedProfile.Matches
            .Select(match => match.ProcessName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        RefreshSelectedProfileMatchChips();
        MarkDirty();
        StatusMessage = $"{processName} seçili profile eklendi.";
    }

    private void RemoveProfileMatch(string? processName)
    {
        if (SelectedProfile is null || string.IsNullOrWhiteSpace(processName))
        {
            return;
        }

        var removed = SelectedProfile.Matches.RemoveAll(match =>
            string.Equals(match.ProcessName, processName, StringComparison.OrdinalIgnoreCase));

        if (removed == 0)
        {
            StatusMessage = $"{processName} bu profile bağlı değil.";
            return;
        }

        SelectedProfileMatchesText = string.Join(", ", SelectedProfile.Matches
            .Select(match => match.ProcessName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        RefreshSelectedProfileMatchChips();
        MarkDirty();
        StatusMessage = $"{processName} profil eşleşmelerinden kaldırıldı.";
    }

    private void SaveHotkey()
    {
        if (!HotkeyParser.TryParseDisplay(HotkeyInput, out var parsedHotkey, out var errorMessage))
        {
            StatusMessage = $"Kısayol kaydedilemedi: {errorMessage}";
            return;
        }

        var previousHotkey = CloneHotkey(_configService.CurrentConfig.Hotkey);
        _configService.CurrentConfig.Hotkey = parsedHotkey;
        RefreshConfigSummary();

        try
        {
            _hotkeyService.Register(parsedHotkey);
            IsHotkeyRegistered = _hotkeyService.IsRegistered;
            _configService.Save(_configService.CurrentConfig);
            _hasUnsavedChanges = false;
            StatusMessage = $"Kısayol güncellendi: {parsedHotkey.Display}";
        }
        catch (Exception ex)
        {
            _configService.CurrentConfig.Hotkey = previousHotkey;
            RefreshConfigSummary();
            TryRestorePreviousHotkey(previousHotkey);
            StatusMessage = $"Kısayol kaydedilemedi, eski kısayol korundu: {ex.Message}";
            _logService.Error("Hotkey update failed.", ex);
        }
    }

    private void ExportConfig()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Config dışa aktar",
            FileName = $"action-orbit-config-{DateTime.Now:yyyyMMdd-HHmm}.json",
            Filter = "Action Orbit JSON (*.json)|*.json|Tüm dosyalar (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _configService.ExportConfig(dialog.FileName);
            StatusMessage = $"Config dışa aktarıldı: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Config dışa aktarılamadı: {ex.Message}";
            _logService.Error("Config export failed.", ex);
        }
    }

    private void ImportConfig()
    {
        var confirmation = System.Windows.MessageBox.Show(
            "Seçtiğin config mevcut profilleri ve ayarları değiştirecek. Devam edilsin mi?",
            "Config içe aktar",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            StatusMessage = "Config içe aktarma iptal edildi.";
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Config içe aktar",
            Filter = "Action Orbit JSON (*.json)|*.json|Tüm dosyalar (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _configService.ImportConfig(dialog.FileName);
            RefreshConfigSummary();
            RefreshSettingsFromConfig();
            ReloadEditorFromConfig();
            RegisterHotkey();
            DetectProfile();
            _hasUnsavedChanges = false;
            StatusMessage = $"Config içe aktarıldı: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Config içe aktarılamadı: {ex.Message}";
            _logService.Error("Config import failed.", ex);
        }
    }

    private void ExportSelectedProfile()
    {
        if (SelectedProfile is null)
        {
            StatusMessage = "Önce dışa aktarılacak profili seç.";
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Profil dışa aktar",
            FileName = $"{NormalizeId(SelectedProfile.Name, SelectedProfile.Id)}.profile.json",
            Filter = "Action Orbit profil JSON (*.json)|*.json|Tüm dosyalar (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _configService.ExportProfile(SelectedProfile, dialog.FileName);
            StatusMessage = $"Profil dışa aktarıldı: {SelectedProfile.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Profil dışa aktarılamadı: {ex.Message}";
            _logService.Error("Profile export failed.", ex);
        }
    }

    private void ImportProfile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Profil içe aktar",
            Filter = "Action Orbit profil JSON (*.json)|*.json|Tüm dosyalar (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var profile = _configService.ImportProfile(dialog.FileName);
            profile.Id = CreateUniqueImportedProfileId(profile.Id);
            _configService.CurrentConfig.Profiles.Add(profile);
            Profiles.Add(profile);
            SelectedProfile = profile;
            RefreshConfigSummary();
            MarkDirty();
            StatusMessage = $"Profil içe aktarıldı: {profile.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Profil içe aktarılamadı: {ex.Message}";
            _logService.Error("Profile import failed.", ex);
        }
    }

    private void ApplyThemeSettings()
    {
        if (!IsValidAccent(AccentInput))
        {
            StatusMessage = "Accent rengi #RRGGBB formatında olmalı. Örn: #A51E39";
            return;
        }

        _configService.CurrentConfig.Theme.Mode = NormalizeThemeMode(ThemeMode);
        _configService.CurrentConfig.Theme.Accent = AccentInput.Trim();
        _configService.CurrentConfig.Theme.ButtonSize = OverlayButtonSize;
        _configService.CurrentConfig.Theme.RadiusX = OverlayRadiusX;
        _configService.CurrentConfig.Theme.RadiusY = OverlayRadiusY;

        try
        {
            _configService.Save(_configService.CurrentConfig);
            _hasUnsavedChanges = false;
            RefreshSettingsFromConfig();
            StatusMessage = "Tema ve overlay ayarları kaydedildi. Önizle ile kontrol edebilirsin.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Tema ayarları kaydedilemedi: {ex.Message}";
            _logService.Error("Theme settings save failed.", ex);
        }
    }

    private void UpdateActiveProcessPreview()
    {
        var processName = _activeWindowService.GetActiveProcessName(_ownProcessName);
        if (string.IsNullOrWhiteSpace(processName))
        {
            return;
        }

        ActiveProcessName = processName;
        var profile = _profileService.ResolveProfile(_configService.CurrentConfig, ActiveProcessName);
        ActiveProfileName = profile.Name;
    }

    private void RefreshRunningApps()
    {
        var previousSelection = SelectedRunningApp?.ProcessName;
        var options = GetRunningAppOptions()
            .GroupBy(option => option.ProcessName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(option => option.WindowTitle.Length)
                .First())
            .OrderBy(option => option.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(ActiveProcessName) &&
            !string.Equals(ActiveProcessName, _ownProcessName, StringComparison.OrdinalIgnoreCase) &&
            options.All(option => !string.Equals(option.ProcessName, ActiveProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            options.Insert(0, new RunningAppOption(ActiveProcessName, "Aktif pencere"));
        }

        RunningApps.Clear();
        foreach (var option in options)
        {
            RunningApps.Add(option);
        }

        SelectedRunningApp =
            RunningApps.FirstOrDefault(option => string.Equals(option.ProcessName, previousSelection, StringComparison.OrdinalIgnoreCase))
            ?? RunningApps.FirstOrDefault(option => string.Equals(option.ProcessName, ActiveProcessName, StringComparison.OrdinalIgnoreCase))
            ?? RunningApps.FirstOrDefault();

        StatusMessage = RunningApps.Count == 0
            ? "Çalışan uygulama penceresi bulunamadı."
            : $"{RunningApps.Count} çalışan uygulama listelendi.";
    }

    private IEnumerable<RunningAppOption> GetRunningAppOptions()
    {
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                var processName = $"{process.ProcessName}.exe";
                if (string.Equals(processName, _ownProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return new RunningAppOption(processName, process.MainWindowTitle);
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private void OnActionExecuted(object? sender, ActionExecutionCompletedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (e.Result.Succeeded)
            {
                StatusMessage = $"Aksiyon çalıştı: {e.Action.Title}";
                return;
            }

            var message = $"Aksiyon çalışmadı: {e.Action.Title} - {e.Result.Message}";
            StatusMessage = message;
            UserNotificationRequested?.Invoke(message, true);
        }));
    }

    private void MarkDirty()
    {
        if (_isReloadingEditor)
        {
            return;
        }

        _hasUnsavedChanges = true;
        StatusMessage = "Değişiklikler otomatik kaydedilecek.";
    }

    private void SaveDirtyConfig()
    {
        if (!_hasUnsavedChanges)
        {
            return;
        }

        try
        {
            _configService.Save(_configService.CurrentConfig);
            _hasUnsavedChanges = false;
            RefreshConfigSummary();
            StatusMessage = $"Otomatik kaydedildi: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Otomatik kaydedilemedi: {ex.Message}";
            _logService.Error("Autosave failed.", ex);
        }
    }

    private void RefreshAvailableIcons()
    {
        AvailableIcons.Clear();
        foreach (var icon in IconCatalog.GetAvailableIcons())
        {
            AvailableIcons.Add(icon);
        }

        OnPropertyChanged(nameof(AvailableIconKeys));
    }

    private void RefreshConfigSummary()
    {
        HotkeyDisplay = _configService.CurrentConfig.Hotkey.Display;
        HotkeyInput = _configService.CurrentConfig.Hotkey.Display;
        ProfileCount = _configService.CurrentConfig.Profiles.Count;
        OnPropertyChanged(nameof(ConfigPath));
        OnPropertyChanged(nameof(ConfigFolderPath));
        OnPropertyChanged(nameof(LogPath));
        OnPropertyChanged(nameof(SelectedProfileIsDefault));
    }

    private void RefreshSettingsFromConfig()
    {
        _configService.CurrentConfig.Settings ??= new AppSettings();
        _configService.CurrentConfig.Theme ??= new ThemeConfig();

        _isSyncingSettingsFields = true;
        try
        {
            StartupWithWindows = _configService.CurrentConfig.Settings.RunAtStartup;
            CloseToTray = _configService.CurrentConfig.Settings.CloseToTray;
            ThemeMode = NormalizeThemeMode(_configService.CurrentConfig.Theme.Mode);
            AccentInput = string.IsNullOrWhiteSpace(_configService.CurrentConfig.Theme.Accent)
                ? "#A51E39"
                : _configService.CurrentConfig.Theme.Accent;
            OverlayButtonSize = Math.Clamp(_configService.CurrentConfig.Theme.ButtonSize, 54, 96);
            OverlayRadiusX = Math.Clamp(_configService.CurrentConfig.Theme.RadiusX, 96, 190);
            OverlayRadiusY = Math.Clamp(_configService.CurrentConfig.Theme.RadiusY, 82, 168);
        }
        finally
        {
            _isSyncingSettingsFields = false;
        }
    }

    private void ReloadEditorFromConfig()
    {
        _isReloadingEditor = true;
        try
        {
            var selectedId = SelectedProfile?.Id;
            Profiles.Clear();
            foreach (var profile in _configService.CurrentConfig.Profiles)
            {
                Profiles.Add(profile);
            }

            SelectedProfile = Profiles.FirstOrDefault(profile =>
                string.Equals(profile.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                ?? Profiles.FirstOrDefault();
            RebuildActionRows();
        }
        finally
        {
            _isReloadingEditor = false;
        }
    }

    private void RebuildActionRows()
    {
        var selectedId = SelectedAction?.Action.Id;
        ActionRows.Clear();

        if (SelectedProfile is null)
        {
            SelectedAction = null;
            return;
        }

        AddRows(SelectedProfile.Actions, owner: SelectedProfile.Actions, parent: null, depth: 0);
        SelectedAction = ActionRows.FirstOrDefault(row =>
            string.Equals(row.Action.Id, selectedId, StringComparison.OrdinalIgnoreCase))
            ?? ActionRows.FirstOrDefault();
    }

    private void AddRows(
        List<OrbitAction> actions,
        List<OrbitAction> owner,
        ActionEditorRowViewModel? parent,
        int depth)
    {
        foreach (var action in actions)
        {
            action.Children ??= [];
            var row = new ActionEditorRowViewModel(action, owner, parent, depth);
            row.PropertyChanged += OnActionRowPropertyChanged;
            ActionRows.Add(row);

            if (action.Children.Count > 0)
            {
                AddRows(action.Children, action.Children, row, depth + 1);
            }
        }
    }

    public bool CanMoveActionIntoFolder(ActionEditorRowViewModel? source, ActionEditorRowViewModel? target)
    {
        if (source is null ||
            target is null ||
            ReferenceEquals(source, target) ||
            !target.IsFolder ||
            ReferenceEquals(source.Parent?.Action, target.Action))
        {
            return false;
        }

        return !IsDescendantOf(target, source);
    }

    public void MoveActionIntoFolder(ActionEditorRowViewModel source, ActionEditorRowViewModel target)
    {
        if (!CanMoveActionIntoFolder(source, target))
        {
            StatusMessage = "Alt aksiyon yapmak icin bir klasorun ustune birak.";
            return;
        }

        var movedAction = source.Action;
        var targetFolder = target.Action;
        targetFolder.Children ??= [];

        if (!source.Owner.Remove(movedAction))
        {
            StatusMessage = "Aksiyon tasinamadi.";
            return;
        }

        targetFolder.Children.Add(movedAction);
        RebuildActionRows();
        SelectedAction = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, movedAction));
        MarkDirty();
        StatusMessage = $"{movedAction.Title}, {targetFolder.Title} klasorune tasindi.";
    }

    private void MoveSelectedActionOutOfFolder()
    {
        if (SelectedAction?.Parent is null)
        {
            StatusMessage = "Bu aksiyon zaten ana halkada.";
            return;
        }

        var row = SelectedAction;
        var parent = row.Parent;
        var movedAction = row.Action;
        var sourceOwner = row.Owner;
        var destinationOwner = parent.Owner;

        if (!sourceOwner.Remove(movedAction))
        {
            StatusMessage = "Aksiyon klasörden çıkarılamadı.";
            return;
        }

        var parentIndex = destinationOwner.IndexOf(parent.Action);
        var insertIndex = parentIndex >= 0 ? parentIndex + 1 : destinationOwner.Count;
        destinationOwner.Insert(insertIndex, movedAction);

        RebuildActionRows();
        SelectedAction = ActionRows.FirstOrDefault(candidate => ReferenceEquals(candidate.Action, movedAction));
        MarkDirty();
        StatusMessage = $"{movedAction.Title} klasörden çıkarıldı.";
    }

    private static bool IsDescendantOf(ActionEditorRowViewModel row, ActionEditorRowViewModel possibleAncestor)
    {
        var parent = row.Parent;
        while (parent is not null)
        {
            if (ReferenceEquals(parent.Action, possibleAncestor.Action))
            {
                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }

    private void OnActionRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ActionEditorRowViewModel.Icon) ||
            e.PropertyName is nameof(ActionEditorRowViewModel.Title) ||
            e.PropertyName is nameof(ActionEditorRowViewModel.Type) ||
            e.PropertyName is nameof(ActionEditorRowViewModel.Target) ||
            e.PropertyName is nameof(ActionEditorRowViewModel.Arguments) ||
            e.PropertyName is nameof(ActionEditorRowViewModel.Id))
        {
            MarkDirty();
            if (sender is ActionEditorRowViewModel row && ReferenceEquals(row, SelectedAction))
            {
                RefreshSelectedActionState();
            }
        }
    }

    private void SaveConfig()
    {
        try
        {
            _configService.Save(_configService.CurrentConfig);
            _hasUnsavedChanges = false;
            RefreshConfigSummary();
            UpdateActiveProcessPreview();
            StatusMessage = "Config kaydedildi.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Config kaydedilemedi: {ex.Message}";
            _logService.Error("Config save failed.", ex);
        }
    }

    private void AddAction() =>
        AddActionTo(SelectedProfile?.Actions, type: "open_app");

    private void AddFolder() =>
        AddActionTo(SelectedProfile?.Actions, type: "folder");

    private void AddChildAction()
    {
        if (SelectedAction is null)
        {
            StatusMessage = "Önce bir klasör aksiyonu seç.";
            return;
        }

        if (!SelectedAction.IsFolder)
        {
            SelectedAction.Type = "folder";
        }

        SelectedAction.Action.Children ??= [];
        AddActionTo(SelectedAction.Action.Children, type: "open_app");
    }

    private void ApplySelectedPreset()
    {
        if (SelectedPreset is null)
        {
            StatusMessage = "Önce hazır bir eylem seç.";
            return;
        }

        var row = SelectedAction;
        if (row is null)
        {
            if (SelectedProfile is null)
            {
                StatusMessage = "Önce bir profil seç.";
                return;
            }

            var action = ActionDefinitionCatalog.CreateActionFromPreset(
                SelectedPreset,
                CreateUniqueActionId(SelectedProfile.Actions, NormalizeId(SelectedPreset.Id, "action")));

            SelectedProfile.Actions.Add(action);
            RebuildActionRows();
            row = ActionRows.FirstOrDefault(candidate => ReferenceEquals(candidate.Action, action));
        }

        if (row is null)
        {
            return;
        }

        row.Title = SelectedPreset.Title;
        row.Icon = SelectedPreset.Icon;
        row.Type = SelectedPreset.Type;
        row.Target = SelectedPreset.Target;
        row.Arguments = SelectedPreset.Arguments;

        if (string.IsNullOrWhiteSpace(row.Id) ||
            row.Id.StartsWith("action_", StringComparison.OrdinalIgnoreCase) ||
            row.Id.StartsWith("folder_", StringComparison.OrdinalIgnoreCase) ||
            row.Id.StartsWith("new_", StringComparison.OrdinalIgnoreCase))
        {
            row.Id = CreateUniqueActionId(row.Owner, NormalizeId(SelectedPreset.Id, "action"), row.Action);
        }

        SelectedAction = row;
        RefreshActionList();
        MarkDirty();
        StatusMessage = $"Hazır eylem uygulandı: {SelectedPreset.Title}";
    }

    private void ImportIcon()
    {
        if (SelectedAction is null)
        {
            StatusMessage = "Önce ikon atanacak bir aksiyon seç.";
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "İkon seç",
            Filter = "İkon dosyaları (*.png;*.jpg;*.jpeg;*.svg)|*.png;*.jpg;*.jpeg;*.svg|Tüm dosyalar (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(_configService.IconDirectory);
            var targetPath = CreateUniqueIconPath(dialog.FileName);
            File.Copy(dialog.FileName, targetPath, overwrite: false);

            var key = $"custom:{Path.GetFileName(targetPath)}";
            if (!IconCatalog.HasIcon(key))
            {
                File.Delete(targetPath);
                StatusMessage = "Bu SVG desteklenmedi. Path tabanlı SVG veya PNG/JPG kullan.";
                return;
            }

            SelectedAction.Icon = key;
            RefreshAvailableIcons();
            RefreshActionList();
            MarkDirty();
            StatusMessage = $"İkon içe aktarıldı: {Path.GetFileName(targetPath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"İkon eklenemedi: {ex.Message}";
            _logService.Error("Icon import failed.", ex);
        }
    }

    private void BrowseActionTarget()
    {
        if (SelectedAction is null)
        {
            StatusMessage = "Önce bir aksiyon seç.";
            return;
        }

        try
        {
            switch (SelectedAction.Type)
            {
                case "open_app":
                    BrowseFileForSelectedAction(
                        "Uygulama seç",
                        "Uygulamalar (*.exe)|*.exe|Tüm dosyalar (*.*)|*.*");
                    break;
                case "open_file":
                    BrowseFileForSelectedAction(
                        "Dosya seç",
                        "Tüm dosyalar (*.*)|*.*");
                    break;
                case "open_folder":
                    BrowseFolderForSelectedAction();
                    break;
                default:
                    StatusMessage = "Bu aksiyon türü için gözat seçeneği yok.";
                    break;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hedef seçilemedi: {ex.Message}";
            _logService.Error("Action target browse failed.", ex);
        }
    }

    private async void TestSelectedAction()
    {
        if (SelectedAction is null)
        {
            StatusMessage = "Önce test edilecek aksiyonu seç.";
            return;
        }

        if (!ValidateAction(SelectedAction, allowCommandWarning: false, out var validationMessage))
        {
            StatusMessage = $"Aksiyon test edilemedi: {validationMessage}";
            return;
        }

        if (SelectedAction.IsFolder)
        {
            StatusMessage = "Klasör aksiyonları önizleme halkasında açılır.";
            ShowOverlay();
            return;
        }

        try
        {
            var title = SelectedAction.Title;
            var result = await _actionExecutionService.ExecuteAsync(SelectedAction.Action);
            StatusMessage = result.Succeeded
                ? $"Aksiyon test edildi: {title}"
                : $"Aksiyon çalışmadı: {result.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Aksiyon çalışmadı: {ex.Message}";
            _logService.Error("Action test failed.", ex);
        }
    }

    private void AddProfile()
    {
        var id = CreateUniqueProfileId("profile");
        var profile = new ProfileConfig
        {
            Id = id,
            Name = "Yeni Profil",
            Matches = [],
            Actions =
            [
                new OrbitAction
                {
                    Id = "new_action",
                    Title = "Yeni Aksiyon",
                    Icon = "app",
                    Type = "open_app",
                    Target = "",
                    Arguments = ""
                }
            ]
        };

        _configService.CurrentConfig.Profiles.Add(profile);
        Profiles.Add(profile);
        SelectedProfile = profile;
        RefreshConfigSummary();
        MarkDirty();
        StatusMessage = "Yeni profil eklendi. Çalışan uygulama listesinden bir uygulama bağlayabilirsin.";
    }

    private void DeleteProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        if (_configService.CurrentConfig.Profiles.Count <= 1)
        {
            StatusMessage = "Son profil silinemez.";
            return;
        }

        var profile = SelectedProfile;
        var confirmation = System.Windows.MessageBox.Show(
            $"{profile.Name} profili silinsin mi?",
            "Profil sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            StatusMessage = "Profil silme iptal edildi.";
            return;
        }

        _configService.CurrentConfig.Profiles.Remove(profile);
        Profiles.Remove(profile);

        if (string.Equals(_configService.CurrentConfig.DefaultProfileId, profile.Id, StringComparison.OrdinalIgnoreCase))
        {
            _configService.CurrentConfig.DefaultProfileId = _configService.CurrentConfig.Profiles.FirstOrDefault()?.Id ?? "default";
        }

        SelectedProfile = Profiles.FirstOrDefault();
        RefreshConfigSummary();
        MarkDirty();
        StatusMessage = "Profil silindi.";
    }

    private void AddActionTo(List<OrbitAction>? actions, string type)
    {
        if (actions is null)
        {
            StatusMessage = "Önce bir profil seç.";
            return;
        }

        var action = new OrbitAction
        {
            Id = CreateUniqueActionId(actions, type == "folder" ? "folder" : "action"),
            Title = type == "folder" ? "Yeni Klasör" : "Yeni Aksiyon",
            Icon = type == "folder" ? "folder" : "app",
            Type = type,
            Target = "",
            Arguments = ""
        };

        actions.Add(action);
        RebuildActionRows();
        SelectedAction = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, action));
        MarkDirty();
        StatusMessage = type == "folder" ? "Klasör eklendi." : "Aksiyon eklendi.";
    }

    private void DeleteAction()
    {
        if (SelectedAction is null)
        {
            return;
        }

        var action = SelectedAction;
        var confirmation = System.Windows.MessageBox.Show(
            $"{action.Title} aksiyonu silinsin mi?",
            "Aksiyon sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            StatusMessage = "Aksiyon silme iptal edildi.";
            return;
        }

        action.Owner.Remove(action.Action);
        RebuildActionRows();
        MarkDirty();
        StatusMessage = "Aksiyon silindi.";
    }

    private void MoveSelectedAction(int direction)
    {
        if (SelectedAction is null)
        {
            return;
        }

        var owner = SelectedAction.Owner;
        var index = owner.IndexOf(SelectedAction.Action);
        var targetIndex = index + direction;
        if (index < 0 || targetIndex < 0 || targetIndex >= owner.Count)
        {
            return;
        }

        owner.RemoveAt(index);
        owner.Insert(targetIndex, SelectedAction.Action);
        var moved = SelectedAction.Action;
        RebuildActionRows();
        SelectedAction = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, moved));
        MarkDirty();
    }

    private void SyncProfileFieldsFromSelectedProfile()
    {
        _isSyncingProfileFields = true;
        try
        {
            SelectedProfileId = SelectedProfile?.Id ?? "";
            SelectedProfileName = SelectedProfile?.Name ?? "";
            SelectedProfileMatchesText = SelectedProfile is null
                ? ""
                : string.Join(", ", SelectedProfile.Matches.Select(match => match.ProcessName).Where(name => !string.IsNullOrWhiteSpace(name)));
        }
        finally
        {
            _isSyncingProfileFields = false;
        }

        RefreshSelectedProfileMatchChips();
    }

    private void BrowseFileForSelectedAction(string title, string filter)
    {
        if (SelectedAction is null)
        {
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = filter,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        SelectedAction.Target = dialog.FileName;
        RefreshSelectedActionState();
        MarkDirty();
        StatusMessage = "Aksiyon hedefi güncellendi.";
    }

    private void BrowseFolderForSelectedAction()
    {
        if (SelectedAction is null)
        {
            return;
        }

        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Klasör seç",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        SelectedAction.Target = dialog.SelectedPath;
        RefreshSelectedActionState();
        MarkDirty();
        StatusMessage = "Klasör hedefi güncellendi.";
    }

    private void RefreshSelectedProfileMatchChips()
    {
        SelectedProfileMatchChips.Clear();
        var names = SelectedProfile?.Matches
            .Select(match => match.ProcessName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            ?? Enumerable.Empty<string>();

        foreach (var name in names)
        {
            SelectedProfileMatchChips.Add(name);
        }
    }

    private void RefreshSelectedActionState()
    {
        OnPropertyChanged(nameof(HasSelectedAction));
        OnPropertyChanged(nameof(HasNoSelectedAction));
        OnPropertyChanged(nameof(SelectedActionIsFolder));
        OnPropertyChanged(nameof(SelectedActionIsChild));
        OnPropertyChanged(nameof(CanMoveSelectedActionOutOfFolder));
        OnPropertyChanged(nameof(SelectedActionFolderSummary));
        OnPropertyChanged(nameof(SelectedActionParentSummary));
        OnPropertyChanged(nameof(CanBrowseSelectedAction));
        OnPropertyChanged(nameof(SelectedActionValidationMessage));
        OnPropertyChanged(nameof(HasSelectedActionValidation));
    }

    private void RefreshProfileList()
    {
        System.Windows.Data.CollectionViewSource.GetDefaultView(Profiles)?.Refresh();
    }

    private void RefreshActionList()
    {
        System.Windows.Data.CollectionViewSource.GetDefaultView(ActionRows)?.Refresh();
    }

    private string CreateUniqueProfileId(string prefix)
    {
        var index = _configService.CurrentConfig.Profiles.Count + 1;
        var id = $"{prefix}_{index}";
        while (_configService.CurrentConfig.Profiles.Any(profile => string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            index++;
            id = $"{prefix}_{index}";
        }

        return id;
    }

    private string CreateUniqueImportedProfileId(string requestedId)
    {
        var baseId = NormalizeId(requestedId, "imported_profile");
        var id = baseId;
        var index = 2;

        while (_configService.CurrentConfig.Profiles.Any(profile =>
            string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            id = $"{baseId}_{index}";
            index++;
        }

        return id;
    }

    private string CreateUniqueIconPath(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var baseName = NormalizeId(Path.GetFileNameWithoutExtension(sourcePath), "icon");
        var fileName = $"{baseName}{extension}";
        var targetPath = Path.Combine(_configService.IconDirectory, fileName);
        var index = 2;

        while (File.Exists(targetPath))
        {
            fileName = $"{baseName}_{index}{extension}";
            targetPath = Path.Combine(_configService.IconDirectory, fileName);
            index++;
        }

        return targetPath;
    }

    private static string CreateUniqueActionId(List<OrbitAction> actions, string prefix, OrbitAction? ignoredAction = null)
    {
        var index = actions.Count + 1;
        var id = $"{prefix}_{index}";
        while (actions.Any(action =>
            !ReferenceEquals(action, ignoredAction) &&
            string.Equals(action.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            index++;
            id = $"{prefix}_{index}";
        }

        return id;
    }

    private static string NormalizeId(string value, string fallback)
    {
        var normalized = new string((value ?? "")
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray())
            .Trim('_');

        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string NormalizeThemeMode(string? value)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant();
        return normalized is "system" or "light" or "dark" ? normalized : "dark";
    }

    private static bool IsValidAccent(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length != 7 || trimmed[0] != '#')
        {
            return false;
        }

        return trimmed.Skip(1).All(Uri.IsHexDigit);
    }

    private static List<ProfileMatch> ParseProfileMatches(string value) =>
        (value ?? "")
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(process => new ProfileMatch { ProcessName = process })
            .ToList();

    private static HotkeyConfig CloneHotkey(HotkeyConfig hotkey) =>
        new()
        {
            Display = hotkey.Display,
            Key = hotkey.Key,
            Modifiers = [.. hotkey.Modifiers]
        };

    private void TryRestorePreviousHotkey(HotkeyConfig previousHotkey)
    {
        try
        {
            _hotkeyService.Register(previousHotkey);
            IsHotkeyRegistered = _hotkeyService.IsRegistered;
        }
        catch (Exception restoreException)
        {
            IsHotkeyRegistered = false;
            _logService.Error("Previous hotkey restore failed.", restoreException);
        }
    }

    private static bool ValidateAction(
        ActionEditorRowViewModel row,
        bool allowCommandWarning,
        out string message)
    {
        message = "";

        if (string.IsNullOrWhiteSpace(row.Title))
        {
            message = "Aksiyon adı boş olamaz.";
            return false;
        }

        var target = row.Target?.Trim() ?? "";
        var expandedTarget = Environment.ExpandEnvironmentVariables(target);

        switch (row.Type)
        {
            case "folder":
                if (row.Action.Children.Count == 0)
                {
                    message = "Klasörün içinde en az bir alt aksiyon olmalı.";
                    return false;
                }

                return true;

            case "open_app":
                if (string.IsNullOrWhiteSpace(target))
                {
                    message = "Uygulama hedefi boş olamaz.";
                    return false;
                }

                if (LooksLikePath(expandedTarget) && !File.Exists(expandedTarget))
                {
                    message = "Uygulama dosyası bulunamadı.";
                    return false;
                }

                return true;

            case "open_file":
                if (string.IsNullOrWhiteSpace(target))
                {
                    message = "Dosya yolu boş olamaz.";
                    return false;
                }

                if (!File.Exists(expandedTarget))
                {
                    message = "Dosya bulunamadı.";
                    return false;
                }

                return true;

            case "open_folder":
                if (string.IsNullOrWhiteSpace(target))
                {
                    message = "Klasör yolu boş olamaz.";
                    return false;
                }

                if (!Directory.Exists(expandedTarget))
                {
                    message = "Klasör bulunamadı.";
                    return false;
                }

                return true;

            case "open_url":
                if (!Uri.TryCreate(target, UriKind.Absolute, out var uri) ||
                    uri.Scheme is not ("http" or "https"))
                {
                    message = "Web adresi http:// veya https:// ile başlamalı.";
                    return false;
                }

                return true;

            case "send_hotkey":
                if (!ValidateSendHotkey(target, out message))
                {
                    return false;
                }

                return true;

            case "type_text":
                if (string.IsNullOrEmpty(row.Target))
                {
                    message = "Yazılacak metin boş olamaz.";
                    return false;
                }

                return true;

            case "run_command":
                if (string.IsNullOrWhiteSpace(target))
                {
                    message = "Komut boş olamaz.";
                    return false;
                }

                if (allowCommandWarning)
                {
                    message = "";
                }

                return true;

            default:
                message = $"Bilinmeyen aksiyon türü: {row.Type}";
                return false;
        }
    }

    private static bool ValidateSendHotkey(string hotkey, out string message)
    {
        message = "";

        var tokens = (hotkey ?? "")
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (tokens.Count == 0)
        {
            message = "Kısayol boş olamaz. Örn: Ctrl+C";
            return false;
        }

        foreach (var token in tokens)
        {
            if (!KeyTokenParser.TryParseVirtualKey(token, out _))
            {
                message = $"Kısayol parçası okunamadı: {token}";
                return false;
            }
        }

        return true;
    }

    private static bool LooksLikePath(string value) =>
        Path.IsPathFullyQualified(value) ||
        value.Contains('\\', StringComparison.Ordinal) ||
        value.Contains('/', StringComparison.Ordinal);

    private void OpenPath(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Dosya açılamadı: {ex.Message}";
            _logService.Error($"Could not open path: {path}", ex);
        }
    }
}
