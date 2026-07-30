using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;

namespace ActionOrbit.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly ConfigService _configService;
    private readonly HotkeyService _hotkeyService;
    private readonly ActiveWindowService _activeWindowService;
    private readonly ProfileService _profileService;
    private readonly OverlayService _overlayService;
    private readonly ActionExecutionService _actionExecutionService;
    private readonly StartupService _startupService;
    private readonly LogService _logService;
    private readonly UndoManager _undoManager = new();
    private readonly DispatcherTimer _activeProcessTimer;
    private bool _isReloadingEditor;
    private string _selectedWorkspace = "home";
    private string _actionLibrarySearchText = "";
    private ActionTypeOption? _selectedActionLibraryCategory;

    public MainWindowViewModel(
        ConfigService configService,
        HotkeyService hotkeyService,
        ActiveWindowService activeWindowService,
        ProfileService profileService,
        OverlayService overlayService,
        ActionExecutionService actionExecutionService,
        StartupService startupService,
        LogService logService,
        IUserConfirmationService? confirmationService = null)
    {
        _configService = configService;
        _hotkeyService = hotkeyService;
        _activeWindowService = activeWindowService;
        _profileService = profileService;
        _overlayService = overlayService;
        _actionExecutionService = actionExecutionService;
        _startupService = startupService;
        _logService = logService;
        Status = new StatusCenterViewModel();
        Autosave = new AutosaveViewModel(
            _configService,
            _logService,
            (message, tone) => Status.SetMessage(message, tone),
            AfterConfigSaved,
            startTimer: false);

        ProfileEditor = new ProfileEditorViewModel(
            _configService,
            _activeWindowService,
            _profileService,
            MarkDirty,
            message => Status.SetMessage(message),
            RegisterUndo,
            () =>
            {
                OnPropertyChanged(nameof(SelectedProfile));
                OnPropertyChanged(nameof(SelectedProfileIsDefault));
                OnPropertyChanged(nameof(CanSetDefaultProfile));
                ActionEditor?.ReloadForSelectedProfile();
            });
        Settings = new SettingsViewModel(
            _configService,
            _startupService,
            _logService,
            MarkDirty,
            message => Status.SetMessage(message),
            Autosave.SetState);
        Hotkey = new HotkeySettingsViewModel(
            _configService,
            _hotkeyService,
            _logService,
            message => Status.SetMessage(message),
            Autosave.SetState);
        ActionEditor = new ActionEditorViewModel(
            _configService,
            _actionExecutionService,
            _logService,
            () => ProfileEditor.SelectedProfile,
            MarkDirty,
            message => Status.SetMessage(message),
            RegisterUndo,
            ShowOverlay);
        Transfers = new ConfigTransferViewModel(
            _configService,
            _logService,
            Hotkey,
            Settings,
            () => ProfileEditor.SelectedProfile,
            ReloadAfterExternalConfigChange,
            AddImportedProfileToEditor,
            MarkDirty,
            Autosave.SetState,
            message => Status.SetMessage(message),
            confirmationService);

        ShowOverlayCommand = new RelayCommand(ShowOverlay);
        NavigateWorkspaceCommand = new RelayCommand(parameter => NavigateWorkspace(parameter?.ToString()));
        UndoCommand = new RelayCommand(UndoLastChange);
        _undoManager.StateChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(UndoButtonText));
        };

        _actionExecutionService.ActionExecuted += OnActionExecuted;
        _hotkeyService.HotkeyPressed += (_, _) =>
            System.Windows.Application.Current.Dispatcher.Invoke(ShowOverlay);

        RefreshConfigSummary();
        Hotkey.RefreshFromConfig();
        Settings.RefreshFromConfig();
        ReloadEditorFromConfig();
        ActionEditor.RefreshAvailableIcons();
        ProfileEditor.UpdateActiveProcessPreview();
        ProfileEditor.RefreshRunningApps();
        ActionLibraryCategories =
        [
            new("all", "Tümü", "", "", "Tüm hazır aksiyonlar"),
            .. ActionEditor.ActionTypeOptions.Where(option => option.Key != "folder")
        ];
        SelectedActionLibraryCategory = ActionLibraryCategories[0];
        FilteredActionPresets = CollectionViewSource.GetDefaultView(ActionEditor.ActionPresets);
        FilteredActionPresets.Filter = MatchesActionLibraryFilter;

        _activeProcessTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        _activeProcessTimer.Tick += (_, _) => ProfileEditor.UpdateActiveProcessPreview();
        _activeProcessTimer.Start();
        Autosave.Start();
        Status.SetMessage("Hazır. Pencere açılınca global kısayol kaydedilecek.");
    }

    public ObservableCollection<ProfileConfig> Profiles => ProfileEditor.Profiles;
    public ObservableCollection<string> SelectedProfileMatchChips => ProfileEditor.SelectedProfileMatchChips;
    public ObservableCollection<RunningAppOption> RunningApps => ProfileEditor.RunningApps;
    public SettingsViewModel Settings { get; }
    public StatusCenterViewModel Status { get; }
    public AutosaveViewModel Autosave { get; }
    public HotkeySettingsViewModel Hotkey { get; }
    public ConfigTransferViewModel Transfers { get; }
    public ProfileEditorViewModel ProfileEditor { get; }
    public ActionEditorViewModel ActionEditor { get; }
    public IReadOnlyList<ActionTypeOption> ActionLibraryCategories { get; }
    public ICollectionView FilteredActionPresets { get; }

    public ICommand ShowOverlayCommand { get; }
    public ICommand DetectProfileCommand => ProfileEditor.DetectProfileCommand;
    public ICommand RefreshRunningAppsCommand => ProfileEditor.RefreshRunningAppsCommand;
    public ICommand AddSelectedRunningAppToProfileCommand => ProfileEditor.AddSelectedRunningAppToProfileCommand;
    public ICommand RemoveProfileMatchCommand => ProfileEditor.RemoveProfileMatchCommand;
    public ICommand AddActiveProcessToProfileCommand => ProfileEditor.AddActiveProcessToProfileCommand;
    public ICommand AddProfileCommand => ProfileEditor.AddProfileCommand;
    public ICommand DuplicateProfileCommand => ProfileEditor.DuplicateProfileCommand;
    public ICommand SetDefaultProfileCommand => ProfileEditor.SetDefaultProfileCommand;
    public ICommand DeleteProfileCommand => ProfileEditor.DeleteProfileCommand;
    public ICommand NavigateWorkspaceCommand { get; }
    public ICommand UndoCommand { get; }

    public string SelectedWorkspace
    {
        get => _selectedWorkspace;
        private set
        {
            if (SetProperty(ref _selectedWorkspace, value))
            {
                OnPropertyChanged(nameof(IsHomeWorkspace));
                OnPropertyChanged(nameof(IsEditorWorkspace));
                OnPropertyChanged(nameof(IsLibraryWorkspace));
                OnPropertyChanged(nameof(IsSettingsWorkspace));
            }
        }
    }

    public bool IsHomeWorkspace => SelectedWorkspace == "home";
    public bool IsEditorWorkspace => SelectedWorkspace == "editor";
    public bool IsLibraryWorkspace => SelectedWorkspace == "library";
    public bool IsSettingsWorkspace => SelectedWorkspace == "settings";

    public ProfileConfig? SelectedProfile
    {
        get => ProfileEditor.SelectedProfile;
        set => ProfileEditor.SelectedProfile = value;
    }

    public string ActionLibrarySearchText
    {
        get => _actionLibrarySearchText;
        set
        {
            if (SetProperty(ref _actionLibrarySearchText, value))
            {
                FilteredActionPresets.Refresh();
            }
        }
    }

    public ActionTypeOption? SelectedActionLibraryCategory
    {
        get => _selectedActionLibraryCategory;
        set
        {
            if (SetProperty(ref _selectedActionLibraryCategory, value) && FilteredActionPresets is not null)
            {
                FilteredActionPresets.Refresh();
            }
        }
    }

    public bool CanUndo => _undoManager.CanUndo;
    public string UndoButtonText => _undoManager.ButtonText;

    public RunningAppOption? SelectedRunningApp
    {
        get => ProfileEditor.SelectedRunningApp;
        set => ProfileEditor.SelectedRunningApp = value;
    }

    public string ActiveProcessName => ProfileEditor.ActiveProcessName;
    public string ActiveProfileName => ProfileEditor.ActiveProfileName;
    public int ProfileCount => ProfileEditor.ProfileCount;

    public bool SelectedProfileIsDefault => ProfileEditor.SelectedProfileIsDefault;
    public bool CanSetDefaultProfile => ProfileEditor.CanSetDefaultProfile;

    public string SelectedProfileId
    {
        get => ProfileEditor.SelectedProfileId;
        set => ProfileEditor.SelectedProfileId = value;
    }

    public string SelectedProfileName
    {
        get => ProfileEditor.SelectedProfileName;
        set => ProfileEditor.SelectedProfileName = value;
    }

    public string SelectedProfileMatchesText
    {
        get => ProfileEditor.SelectedProfileMatchesText;
        set => ProfileEditor.SelectedProfileMatchesText = value;
    }

    public void RegisterHotkey() => Hotkey.RegisterConfiguredHotkey();

    private void ShowOverlay()
    {
        if (!_overlayService.TryShowOverlay(out var errorMessage))
        {
            Status.ReportFailure(errorMessage);
        }
    }

    private void NavigateWorkspace(string? workspace)
    {
        if (workspace is not ("home" or "editor" or "library" or "settings"))
        {
            return;
        }

        SelectedWorkspace = workspace;
    }

    private bool MatchesActionLibraryFilter(object item)
    {
        if (item is not ActionPresetOption preset)
        {
            return false;
        }

        var category = SelectedActionLibraryCategory?.Key ?? "all";
        if (category != "all" && !string.Equals(preset.Type, category, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var search = ActionLibrarySearchText.Trim();
        return search.Length == 0 ||
               preset.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
               preset.Description.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
               preset.Target.Contains(search, StringComparison.CurrentCultureIgnoreCase);
    }

    private void OnActionExecuted(object? sender, ActionExecutionCompletedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            Status.ReportActionResult(e.Action, e.Result)));
    }

    private void MarkDirty()
    {
        if (_isReloadingEditor)
        {
            return;
        }

        Autosave.MarkDirty();
    }

    private void RegisterUndo(string description, Action undoAction)
        => _undoManager.Register(description, undoAction);

    private void UndoLastChange()
    {
        var description = _undoManager.Undo();
        if (description is null)
        {
            return;
        }
        MarkDirty();
        Status.SetMessage($"Geri alındı: {description}", StatusTone.Warning);
    }

    private void RefreshConfigSummary()
    {
        ProfileEditor.NotifyProfilesChanged();
        OnPropertyChanged(nameof(SelectedProfileIsDefault));
    }

    private void AfterConfigSaved()
    {
        RefreshConfigSummary();
        ProfileEditor.UpdateActiveProcessPreview();
    }

    private void ReloadAfterExternalConfigChange()
    {
        RefreshConfigSummary();
        Settings.CompleteExternalConfigChange();
        ReloadEditorFromConfig();
        ProfileEditor.DetectProfile();
    }

    private void AddImportedProfileToEditor(ProfileConfig profile)
    {
        Profiles.Add(profile);
        SelectedProfile = profile;
        ProfileEditor.NotifyProfilesChanged();
        OnPropertyChanged(nameof(SelectedProfileIsDefault));
        OnPropertyChanged(nameof(CanSetDefaultProfile));
    }


    private void ReloadEditorFromConfig()
    {
        _undoManager.Clear();
        _isReloadingEditor = true;
        try
        {
            ProfileEditor.ReloadFromConfig();
            ActionEditor.ReloadForSelectedProfile();
        }
        finally
        {
            _isReloadingEditor = false;
        }
    }

    public void Dispose()
    {
        Autosave.Dispose();
        _activeProcessTimer.Stop();
        _actionExecutionService.ActionExecuted -= OnActionExecuted;
    }

    public bool FlushPendingChanges() =>
        Autosave.FlushPendingChanges();

}
