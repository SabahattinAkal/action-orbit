using System.ComponentModel;
using System.Windows.Input;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.ViewModels;

public sealed class HotkeySettingsViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly IHotkeyRegistrar _hotkeyRegistrar;
    private readonly LogService _logService;
    private readonly Action<string> _setStatus;
    private readonly Action<bool, bool> _setSaveState;
    private string _hotkeyDisplay = "";
    private string _hotkeyInput = "";
    private string _hotkeyIssueMessage = "";
    private bool _isHotkeyRegistered;

    public HotkeySettingsViewModel(
        ConfigService configService,
        IHotkeyRegistrar hotkeyRegistrar,
        LogService logService,
        Action<string> setStatus,
        Action<bool, bool> setSaveState)
    {
        _configService = configService;
        _hotkeyRegistrar = hotkeyRegistrar;
        _logService = logService;
        _setStatus = setStatus;
        _setSaveState = setSaveState;

        SaveHotkeyCommand = new RelayCommand(SaveHotkey);
        UseSuggestedHotkeyCommand = new RelayCommand(parameter =>
            UseSuggestedHotkey(parameter?.ToString()));
    }

    public IReadOnlyList<string> Suggestions { get; } =
        ["Ctrl+Alt+Shift+R", "F13", "F14", "Ctrl+Space"];

    public ICommand SaveHotkeyCommand { get; }
    public ICommand UseSuggestedHotkeyCommand { get; }

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

    public string HotkeyIssueMessage
    {
        get => _hotkeyIssueMessage;
        private set
        {
            if (SetProperty(ref _hotkeyIssueMessage, value))
            {
                OnPropertyChanged(nameof(HasHotkeyIssue));
            }
        }
    }

    public bool HasHotkeyIssue => !string.IsNullOrWhiteSpace(HotkeyIssueMessage);
    public string HotkeyStateText => IsHotkeyRegistered ? "Aktif" : "Pasif";
    public string HotkeyBadgeBackground => IsHotkeyRegistered ? "#EAF8EF" : "#FFF1F2";
    public string HotkeyBadgeForeground => IsHotkeyRegistered ? "#166534" : "#9F1239";
    public string HotkeyDotBrush => IsHotkeyRegistered ? "#22C55E" : "#F43F5E";

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

    public void RefreshFromConfig()
    {
        HotkeyDisplay = _configService.CurrentConfig.Hotkey.Display;
        HotkeyInput = _configService.CurrentConfig.Hotkey.Display;
    }

    public void RegisterConfiguredHotkey()
    {
        var configuredHotkey = _configService.CurrentConfig.Hotkey;
        try
        {
            _hotkeyRegistrar.Register(configuredHotkey);
            IsHotkeyRegistered = _hotkeyRegistrar.IsRegistered;
            RefreshFromConfig();
            HotkeyIssueMessage = "";
            _setStatus($"Kısayol aktif: {HotkeyDisplay}");
        }
        catch (Exception ex)
        {
            IsHotkeyRegistered = _hotkeyRegistrar.IsRegistered;
            HotkeyIssueMessage = BuildHotkeyIssueMessage(configuredHotkey.Display, ex);
            _setStatus(HotkeyIssueMessage);
            _logService.Error("Hotkey registration failed.", ex);
        }
    }

    public HotkeyConfig SnapshotConfiguredHotkey() => Clone(_configService.CurrentConfig.Hotkey);

    public bool TryActivateCandidate(HotkeyConfig hotkey, out string issueMessage)
    {
        try
        {
            _hotkeyRegistrar.Register(hotkey);
            IsHotkeyRegistered = _hotkeyRegistrar.IsRegistered;
            HotkeyIssueMessage = "";
            issueMessage = "";
            return true;
        }
        catch (Exception ex)
        {
            IsHotkeyRegistered = _hotkeyRegistrar.IsRegistered;
            issueMessage = BuildHotkeyIssueMessage(hotkey.Display, ex);
            HotkeyIssueMessage = issueMessage;
            _logService.Error("Candidate hotkey registration failed.", ex);
            return false;
        }
    }

    public void RestoreRegistration(HotkeyConfig hotkey)
    {
        try
        {
            _hotkeyRegistrar.Register(hotkey);
            IsHotkeyRegistered = _hotkeyRegistrar.IsRegistered;
        }
        catch (Exception ex)
        {
            IsHotkeyRegistered = false;
            _logService.Error("Previous hotkey restore failed.", ex);
        }
    }

    public void CompleteExternalConfigChange()
    {
        RefreshFromConfig();
        IsHotkeyRegistered = _hotkeyRegistrar.IsRegistered;
        HotkeyIssueMessage = "";
    }

    private void SaveHotkey()
    {
        if (!HotkeyParser.TryParseDisplay(HotkeyInput, out var parsedHotkey, out var errorMessage))
        {
            HotkeyIssueMessage = $"Kısayol okunamadı: {errorMessage}";
            _setStatus(HotkeyIssueMessage);
            return;
        }

        var previousHotkey = SnapshotConfiguredHotkey();
        var registeredCandidate = false;
        _configService.CurrentConfig.Hotkey = parsedHotkey;
        RefreshFromConfig();

        try
        {
            _hotkeyRegistrar.Register(parsedHotkey);
            registeredCandidate = true;
            IsHotkeyRegistered = _hotkeyRegistrar.IsRegistered;
            _configService.Save(_configService.CurrentConfig);
            _setSaveState(false, false);
            HotkeyIssueMessage = "";
            _setStatus($"Kısayol güncellendi: {parsedHotkey.Display}");
        }
        catch (Exception ex)
        {
            _configService.CurrentConfig.Hotkey = previousHotkey;
            RefreshFromConfig();

            if (registeredCandidate)
            {
                RestoreRegistration(previousHotkey);
            }
            else
            {
                IsHotkeyRegistered = _hotkeyRegistrar.IsRegistered;
            }

            HotkeyIssueMessage = $"{BuildHotkeyIssueMessage(parsedHotkey.Display, ex)} Eski kısayol korundu.";
            _setStatus(HotkeyIssueMessage);
            _logService.Error("Hotkey update failed.", ex);
        }
    }

    private void UseSuggestedHotkey(string? suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion))
        {
            return;
        }

        HotkeyInput = suggestion;
        SaveHotkey();
    }

    internal static string BuildHotkeyIssueMessage(string display, Exception exception)
    {
        if (exception is Win32Exception { NativeErrorCode: 1409 })
        {
            return $"{display} başka bir uygulama tarafından kullanılıyor. Eski Action Orbit açıksa sistem tepsisinden kapat veya aşağıdaki alternatiflerden birini dene.";
        }

        return $"{display} etkinleştirilemedi. Farklı bir kısayol dene. Teknik ayrıntı: {exception.Message}";
    }

    private static HotkeyConfig Clone(HotkeyConfig hotkey) => new()
    {
        Display = hotkey.Display,
        Key = hotkey.Key,
        Modifiers = [.. hotkey.Modifiers]
    };
}
