using ActionOrbit.App.Models;
using ActionOrbit.App.Services.Actions;

namespace ActionOrbit.App.ViewModels;

public enum StatusTone
{
    Info,
    Success,
    Warning,
    Error
}

public sealed class StatusCenterViewModel : ViewModelBase
{
    private string _message = "";
    private StatusTone _tone = StatusTone.Info;

    public event Action<string, bool>? UserNotificationRequested;

    public string Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public StatusTone Tone
    {
        get => _tone;
        private set
        {
            if (SetProperty(ref _tone, value))
            {
                OnPropertyChanged(nameof(Background));
                OnPropertyChanged(nameof(Foreground));
                OnPropertyChanged(nameof(DotBrush));
            }
        }
    }

    public string Background => Tone switch
    {
        StatusTone.Success => "#EAF8EF",
        StatusTone.Warning => "#FFF7E6",
        StatusTone.Error => "#FFF1F2",
        _ => "#EEF2F7"
    };

    public string Foreground => Tone switch
    {
        StatusTone.Success => "#166534",
        StatusTone.Warning => "#92400E",
        StatusTone.Error => "#9F1239",
        _ => "#475569"
    };

    public string DotBrush => Tone switch
    {
        StatusTone.Success => "#22C55E",
        StatusTone.Warning => "#F59E0B",
        StatusTone.Error => "#F43F5E",
        _ => "#64748B"
    };

    public void SetMessage(string message, StatusTone tone = StatusTone.Info)
    {
        Message = message;
        Tone = tone;
    }

    public void ReportActionResult(OrbitAction action, ActionExecutionResult result)
    {
        if (result.Succeeded)
        {
            SetMessage($"Aksiyon çalıştı: {action.Title}", StatusTone.Success);
            return;
        }

        var message = $"Aksiyon çalışmadı: {action.Title} - {result.Message}";
        SetMessage(message, StatusTone.Error);
        UserNotificationRequested?.Invoke(message, true);
    }

    public void ReportUnexpectedError()
    {
        const string message =
            "Beklenmeyen bir arayüz hatası oluştu. Ayrıntılar log dosyasına kaydedildi.";
        SetMessage(message, StatusTone.Error);
        UserNotificationRequested?.Invoke(message, true);
    }

    public void ReportFailure(string message)
    {
        SetMessage(message, StatusTone.Error);
        UserNotificationRequested?.Invoke(message, true);
    }
}
