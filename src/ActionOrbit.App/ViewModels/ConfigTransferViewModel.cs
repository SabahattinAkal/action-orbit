using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.ViewModels;

public sealed class ConfigTransferViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly LogService _logService;
    private readonly HotkeySettingsViewModel _hotkey;
    private readonly Func<ProfileConfig?> _getSelectedProfile;
    private readonly Action _reloadEditors;
    private readonly Action<ProfileConfig> _addImportedProfile;
    private readonly Action _markDirty;
    private readonly Action<bool, bool> _setSaveState;
    private readonly Action<string> _setStatus;

    public ConfigTransferViewModel(
        ConfigService configService,
        LogService logService,
        HotkeySettingsViewModel hotkey,
        Func<ProfileConfig?> getSelectedProfile,
        Action reloadEditors,
        Action<ProfileConfig> addImportedProfile,
        Action markDirty,
        Action<bool, bool> setSaveState,
        Action<string> setStatus)
    {
        _configService = configService;
        _logService = logService;
        _hotkey = hotkey;
        _getSelectedProfile = getSelectedProfile;
        _reloadEditors = reloadEditors;
        _addImportedProfile = addImportedProfile;
        _markDirty = markDirty;
        _setSaveState = setSaveState;
        _setStatus = setStatus;

        OpenConfigCommand = new RelayCommand(() => OpenPath(_configService.ConfigPath));
        OpenLogCommand = new RelayCommand(OpenLog);
        OpenConfigFolderCommand = new RelayCommand(() => OpenPath(_configService.AppDirectory));
        ExportConfigCommand = new RelayCommand(ExportConfig);
        ImportConfigCommand = new RelayCommand(ImportConfig);
        ExportProfileCommand = new RelayCommand(ExportSelectedProfile);
        ImportProfileCommand = new RelayCommand(ImportProfile);
        ReloadConfigCommand = new RelayCommand(ReloadConfig);
    }

    public ICommand OpenConfigCommand { get; }
    public ICommand OpenLogCommand { get; }
    public ICommand OpenConfigFolderCommand { get; }
    public ICommand ExportConfigCommand { get; }
    public ICommand ImportConfigCommand { get; }
    public ICommand ExportProfileCommand { get; }
    public ICommand ImportProfileCommand { get; }
    public ICommand ReloadConfigCommand { get; }

    public string ConfigPath => _configService.ConfigPath;
    public string ConfigFolderPath => _configService.AppDirectory;
    public string LogPath => _logService.LogPath;

    private void ReloadConfig()
    {
        try
        {
            _configService.Reload();
            _hotkey.RefreshFromConfig();
            _reloadEditors();
            _hotkey.RegisterConfiguredHotkey();
            _setStatus($"Config yenilendi. Kısayol: {_hotkey.HotkeyDisplay}");
        }
        catch (Exception ex)
        {
            _setStatus($"Config yenilenemedi: {ex.Message}");
            _logService.Error("Config reload failed.", ex);
        }
    }

    private void OpenLog()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_logService.LogPath)!);
        if (!File.Exists(_logService.LogPath))
        {
            File.WriteAllText(_logService.LogPath, "");
        }

        OpenPath(_logService.LogPath);
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
            _setStatus($"Config dışa aktarıldı: {Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex)
        {
            _setStatus($"Config dışa aktarılamadı: {ex.Message}");
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
            _setStatus("Config içe aktarma iptal edildi.");
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
            var importedConfig = _configService.ReadConfigForImport(dialog.FileName);
            var previousHotkey = _hotkey.SnapshotConfiguredHotkey();

            if (!_hotkey.TryActivateCandidate(importedConfig.Hotkey, out var hotkeyIssue))
            {
                _setStatus($"Config uygulanmadı. {hotkeyIssue}");
                return;
            }

            try
            {
                _configService.Save(importedConfig);
            }
            catch
            {
                _hotkey.RestoreRegistration(previousHotkey);
                throw;
            }

            _hotkey.CompleteExternalConfigChange();
            _reloadEditors();
            _setSaveState(false, false);
            _setStatus($"Config içe aktarıldı: {Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex)
        {
            _setStatus($"Config içe aktarılamadı: {ex.Message}");
            _logService.Error("Config import failed.", ex);
        }
    }

    private void ExportSelectedProfile()
    {
        var selectedProfile = _getSelectedProfile();
        if (selectedProfile is null)
        {
            _setStatus("Önce dışa aktarılacak profili seç.");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Profil dışa aktar",
            FileName = $"{NormalizeId(selectedProfile.Name, selectedProfile.Id)}.profile.json",
            Filter = "Action Orbit profil JSON (*.json)|*.json|Tüm dosyalar (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _configService.ExportProfile(selectedProfile, dialog.FileName);
            _setStatus($"Profil dışa aktarıldı: {selectedProfile.Name}");
        }
        catch (Exception ex)
        {
            _setStatus($"Profil dışa aktarılamadı: {ex.Message}");
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
            _addImportedProfile(profile);
            _markDirty();
            _setStatus($"Profil içe aktarıldı: {profile.Name}");
        }
        catch (Exception ex)
        {
            _setStatus($"Profil içe aktarılamadı: {ex.Message}");
            _logService.Error("Profile import failed.", ex);
        }
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
            _setStatus($"Dosya açılamadı: {ex.Message}");
            _logService.Error($"Could not open path: {path}", ex);
        }
    }
}
