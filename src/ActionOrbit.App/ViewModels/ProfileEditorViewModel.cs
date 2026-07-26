using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.ViewModels;

public sealed class ProfileEditorViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly ActiveWindowService _activeWindowService;
    private readonly ProfileService _profileService;
    private readonly Action _markDirty;
    private readonly Action<string> _setStatus;
    private readonly Action<string, Action> _registerUndo;
    private readonly Action _selectedProfileChanged;
    private readonly ActiveProfileResolutionCache _activeProfileResolutionCache = new();
    private readonly string _ownProcessName = $"{Process.GetCurrentProcess().ProcessName}.exe";
    private bool _isSyncingFields;
    private ProfileConfig? _selectedProfile;
    private RunningAppOption? _selectedRunningApp;
    private string _selectedProfileId = "";
    private string _selectedProfileName = "";
    private string _selectedProfileMatchesText = "";
    private string _activeProcessName = "";
    private string _activeProfileName = "";

    public ProfileEditorViewModel(
        ConfigService configService,
        ActiveWindowService activeWindowService,
        ProfileService profileService,
        Action markDirty,
        Action<string> setStatus,
        Action<string, Action> registerUndo,
        Action selectedProfileChanged)
    {
        _configService = configService;
        _activeWindowService = activeWindowService;
        _profileService = profileService;
        _markDirty = markDirty;
        _setStatus = setStatus;
        _registerUndo = registerUndo;
        _selectedProfileChanged = selectedProfileChanged;

        DetectProfileCommand = new RelayCommand(DetectProfile);
        RefreshRunningAppsCommand = new RelayCommand(RefreshRunningApps);
        AddSelectedRunningAppToProfileCommand = new RelayCommand(AddSelectedRunningAppToProfile);
        RemoveProfileMatchCommand = new RelayCommand(parameter => RemoveProfileMatch(parameter?.ToString()));
        AddActiveProcessToProfileCommand = new RelayCommand(AddActiveProcessToProfile);
        AddProfileCommand = new RelayCommand(AddProfile);
        DuplicateProfileCommand = new RelayCommand(DuplicateProfile);
        SetDefaultProfileCommand = new RelayCommand(SetDefaultProfile);
        DeleteProfileCommand = new RelayCommand(DeleteProfile);
    }

    public ObservableCollection<ProfileConfig> Profiles { get; } = [];
    public ObservableCollection<string> SelectedProfileMatchChips { get; } = [];
    public ObservableCollection<RunningAppOption> RunningApps { get; } = [];

    public ICommand DetectProfileCommand { get; }
    public ICommand RefreshRunningAppsCommand { get; }
    public ICommand AddSelectedRunningAppToProfileCommand { get; }
    public ICommand RemoveProfileMatchCommand { get; }
    public ICommand AddActiveProcessToProfileCommand { get; }
    public ICommand AddProfileCommand { get; }
    public ICommand DuplicateProfileCommand { get; }
    public ICommand SetDefaultProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }

    public int ProfileCount => Profiles.Count;

    public ProfileConfig? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (!SetProperty(ref _selectedProfile, value))
            {
                return;
            }

            SyncFieldsFromSelectedProfile();
            OnPropertyChanged(nameof(SelectedProfileIsDefault));
            OnPropertyChanged(nameof(CanSetDefaultProfile));
            _selectedProfileChanged();
        }
    }

    public RunningAppOption? SelectedRunningApp
    {
        get => _selectedRunningApp;
        set => SetProperty(ref _selectedRunningApp, value);
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

    public bool SelectedProfileIsDefault =>
        SelectedProfile is not null
        && string.Equals(
            SelectedProfile.Id,
            _configService.CurrentConfig.DefaultProfileId,
            StringComparison.OrdinalIgnoreCase);

    public bool CanSetDefaultProfile => SelectedProfile is not null && !SelectedProfileIsDefault;

    public string SelectedProfileId
    {
        get => _selectedProfileId;
        set
        {
            var normalized = NormalizeId(value, "profile");
            if (!SetProperty(ref _selectedProfileId, normalized)
                || _isSyncingFields
                || SelectedProfile is null)
            {
                return;
            }

            if (_configService.CurrentConfig.Profiles.Any(profile =>
                !ReferenceEquals(profile, SelectedProfile)
                && string.Equals(profile.Id, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                _setStatus($"{normalized} profil ID'si zaten kullanılıyor.");
                _selectedProfileId = SelectedProfile.Id;
                OnPropertyChanged(nameof(SelectedProfileId));
                return;
            }

            var wasDefault = SelectedProfileIsDefault;
            SelectedProfile.Id = normalized;
            if (wasDefault)
            {
                _configService.CurrentConfig.DefaultProfileId = normalized;
            }

            RefreshProfileList();
            OnPropertyChanged(nameof(SelectedProfileIsDefault));
            OnPropertyChanged(nameof(CanSetDefaultProfile));
            MarkProfilesDirty();
        }
    }

    public string SelectedProfileName
    {
        get => _selectedProfileName;
        set
        {
            if (!SetProperty(ref _selectedProfileName, value)
                || _isSyncingFields
                || SelectedProfile is null)
            {
                return;
            }

            SelectedProfile.Name = string.IsNullOrWhiteSpace(value) ? "Yeni Profil" : value.Trim();
            RefreshProfileList();
            MarkProfilesDirty();
        }
    }

    public string SelectedProfileMatchesText
    {
        get => _selectedProfileMatchesText;
        set
        {
            if (!SetProperty(ref _selectedProfileMatchesText, value)
                || _isSyncingFields
                || SelectedProfile is null)
            {
                return;
            }

            SelectedProfile.Matches = ParseProfileMatches(value);
            RefreshSelectedProfileMatchChips();
            MarkProfilesDirty();
        }
    }

    public void ReloadFromConfig()
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
        NotifyProfilesChanged();
    }

    public void NotifyProfilesChanged()
    {
        _activeProfileResolutionCache.Invalidate();
        OnPropertyChanged(nameof(ProfileCount));
        OnPropertyChanged(nameof(SelectedProfileIsDefault));
        OnPropertyChanged(nameof(CanSetDefaultProfile));
        RefreshProfileList();
    }

    public void DetectProfile()
    {
        _activeProfileResolutionCache.Invalidate();
        UpdateActiveProcessPreview();
        RefreshRunningApps();
    }

    public void UpdateActiveProcessPreview()
    {
        var processName = _activeWindowService.GetActiveProcessName(_ownProcessName);
        if (string.IsNullOrWhiteSpace(processName))
        {
            return;
        }

        ActiveProcessName = processName;
        if (!_activeProfileResolutionCache.RequiresResolution(processName))
        {
            return;
        }

        var profile = _profileService.ResolveProfile(_configService.CurrentConfig, processName);
        ActiveProfileName = profile.Name;
        _activeProfileResolutionCache.RecordResolution(processName);
    }

    public void RefreshRunningApps()
    {
        var previousSelection = SelectedRunningApp?.ProcessName;
        var options = GetRunningAppOptions()
            .GroupBy(option => option.ProcessName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(option => option.WindowTitle.Length)
                .First())
            .OrderBy(option => option.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(ActiveProcessName)
            && !string.Equals(ActiveProcessName, _ownProcessName, StringComparison.OrdinalIgnoreCase)
            && options.All(option => !string.Equals(option.ProcessName, ActiveProcessName, StringComparison.OrdinalIgnoreCase)))
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

        _setStatus(RunningApps.Count == 0
            ? "Çalışan uygulama penceresi bulunamadı."
            : $"{RunningApps.Count} çalışan uygulama listelendi.");
    }

    private IEnumerable<RunningAppOption> GetRunningAppOptions()
    {
        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch (Exception ex)
        {
            _setStatus($"Çalışan uygulamalar listelenemedi: {ex.Message}");
            yield break;
        }

        foreach (var process in processes)
        {
            RunningAppOption? option = null;
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

                option = new RunningAppOption(processName, process.MainWindowTitle);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
            {
                // Processes can exit or become inaccessible while the list is being read.
            }
            finally
            {
                process.Dispose();
            }

            if (option is not null)
            {
                yield return option;
            }
        }
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
            _setStatus("Önce listeden çalışan uygulama seç.");
            return;
        }

        AddProcessNameToSelectedProfile(SelectedRunningApp.ProcessName, "Seçili uygulama");
    }

    private void AddProcessNameToSelectedProfile(string processName, string sourceLabel)
    {
        if (SelectedProfile is null)
        {
            _setStatus("Önce bir profil seç.");
            return;
        }

        if (string.IsNullOrWhiteSpace(processName))
        {
            _setStatus($"{sourceLabel} algılanamadı.");
            return;
        }

        if (string.Equals(processName, _ownProcessName, StringComparison.OrdinalIgnoreCase))
        {
            _setStatus("Action Orbit kendi penceresini profile eklemez.");
            return;
        }

        if (SelectedProfile.Matches.Any(match =>
            string.Equals(match.ProcessName, processName, StringComparison.OrdinalIgnoreCase)))
        {
            _setStatus($"{processName} zaten bu profile bağlı.");
            return;
        }

        SelectedProfile.Matches.Add(new ProfileMatch { ProcessName = processName });
        SyncMatchTextFromProfile();
        MarkProfilesDirty();
        _setStatus($"{processName} seçili profile eklendi.");
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
            _setStatus($"{processName} bu profile bağlı değil.");
            return;
        }

        SyncMatchTextFromProfile();
        MarkProfilesDirty();
        _setStatus($"{processName} profil eşleşmelerinden kaldırıldı.");
    }

    private void SyncMatchTextFromProfile()
    {
        _isSyncingFields = true;
        SelectedProfileMatchesText = SelectedProfile is null
            ? ""
            : string.Join(", ", SelectedProfile.Matches
                .Select(match => match.ProcessName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase));
        _isSyncingFields = false;
        RefreshSelectedProfileMatchChips();
    }

    private void AddProfile()
    {
        var profile = new ProfileConfig
        {
            Id = CreateUniqueProfileId("profile"),
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
        NotifyProfilesChanged();
        MarkProfilesDirty();
        _setStatus("Yeni profil eklendi. Çalışan uygulama listesinden bir uygulama bağlayabilirsin.");
    }

    private void DuplicateProfile()
    {
        if (SelectedProfile is null)
        {
            _setStatus("Önce kopyalanacak profili seç.");
            return;
        }

        var source = SelectedProfile;
        var copy = ProfileCopyService.Copy(
            source,
            CreateUniqueImportedProfileId($"{source.Id}_copy"),
            $"{source.Name} Kopyası");

        _configService.CurrentConfig.Profiles.Add(copy);
        Profiles.Add(copy);
        SelectedProfile = copy;
        NotifyProfilesChanged();
        MarkProfilesDirty();
        _setStatus($"Profil kopyalandı: {copy.Name}");
    }

    private void SetDefaultProfile()
    {
        if (SelectedProfile is null || SelectedProfileIsDefault)
        {
            return;
        }

        _configService.CurrentConfig.DefaultProfileId = SelectedProfile.Id;
        OnPropertyChanged(nameof(SelectedProfileIsDefault));
        OnPropertyChanged(nameof(CanSetDefaultProfile));
        RefreshProfileList();
        MarkProfilesDirty();
        _setStatus($"Varsayılan profil değiştirildi: {SelectedProfile.Name}");
    }

    private void DeleteProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        if (_configService.CurrentConfig.Profiles.Count <= 1)
        {
            _setStatus("Son profil silinemez.");
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
            _setStatus("Profil silme iptal edildi.");
            return;
        }

        var configIndex = _configService.CurrentConfig.Profiles.IndexOf(profile);
        var viewIndex = Profiles.IndexOf(profile);
        var previousDefaultProfileId = _configService.CurrentConfig.DefaultProfileId;
        _configService.CurrentConfig.Profiles.Remove(profile);
        Profiles.Remove(profile);

        if (string.Equals(_configService.CurrentConfig.DefaultProfileId, profile.Id, StringComparison.OrdinalIgnoreCase))
        {
            _configService.CurrentConfig.DefaultProfileId = _configService.CurrentConfig.Profiles.FirstOrDefault()?.Id ?? "default";
        }

        SelectedProfile = Profiles.FirstOrDefault();
        NotifyProfilesChanged();
        _registerUndo($"{profile.Name} profilini silme", () =>
        {
            _configService.CurrentConfig.Profiles.Insert(
                Math.Clamp(configIndex, 0, _configService.CurrentConfig.Profiles.Count),
                profile);
            Profiles.Insert(Math.Clamp(viewIndex, 0, Profiles.Count), profile);
            _configService.CurrentConfig.DefaultProfileId = previousDefaultProfileId;
            SelectedProfile = profile;
            NotifyProfilesChanged();
        });
        MarkProfilesDirty();
        _setStatus("Profil silindi.");
    }

    private void SyncFieldsFromSelectedProfile()
    {
        _isSyncingFields = true;
        try
        {
            SelectedProfileId = SelectedProfile?.Id ?? "";
            SelectedProfileName = SelectedProfile?.Name ?? "";
            SelectedProfileMatchesText = SelectedProfile is null
                ? ""
                : string.Join(", ", SelectedProfile.Matches
                    .Select(match => match.ProcessName)
                    .Where(name => !string.IsNullOrWhiteSpace(name)));
        }
        finally
        {
            _isSyncingFields = false;
        }

        RefreshSelectedProfileMatchChips();
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

    private void RefreshProfileList() =>
        CollectionViewSource.GetDefaultView(Profiles)?.Refresh();

    private string CreateUniqueProfileId(string prefix)
    {
        var index = Profiles.Count + 1;
        var id = $"{prefix}_{index}";
        while (_configService.CurrentConfig.Profiles.Any(profile =>
            string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            index++;
            id = $"{prefix}_{index}";
        }

        return id;
    }

    private string CreateUniqueImportedProfileId(string requestedId)
    {
        var baseId = NormalizeId(requestedId, "imported_profile");
        var candidate = baseId;
        var index = 2;
        while (_configService.CurrentConfig.Profiles.Any(profile =>
            string.Equals(profile.Id, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{baseId}_{index}";
            index++;
        }

        return candidate;
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

    private static List<ProfileMatch> ParseProfileMatches(string value) =>
        (value ?? "")
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(process => new ProfileMatch { ProcessName = process })
            .ToList();

    private void MarkProfilesDirty()
    {
        _activeProfileResolutionCache.Invalidate();
        _markDirty();
    }
}
