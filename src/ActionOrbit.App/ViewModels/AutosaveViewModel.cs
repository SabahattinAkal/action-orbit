using System.Windows.Input;
using System.Windows.Threading;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.ViewModels;

public sealed class AutosaveViewModel : ViewModelBase, IDisposable
{
    private readonly IConfigPersistence _configPersistence;
    private readonly LogService _logService;
    private readonly Action<string, StatusTone> _setStatus;
    private readonly Action _afterSave;
    private readonly Func<DateTime> _now;
    private readonly DispatcherTimer _timer;
    private bool _hasUnsavedChanges;
    private bool _lastSaveFailed;
    private bool _disposed;

    public AutosaveViewModel(
        IConfigPersistence configPersistence,
        LogService logService,
        Action<string, StatusTone> setStatus,
        Action afterSave,
        TimeSpan? interval = null,
        Func<DateTime>? now = null,
        bool startTimer = true)
    {
        _configPersistence = configPersistence;
        _logService = logService;
        _setStatus = setStatus;
        _afterSave = afterSave;
        _now = now ?? (() => DateTime.Now);
        _timer = new DispatcherTimer
        {
            Interval = interval ?? TimeSpan.FromSeconds(1.5)
        };
        _timer.Tick += OnTimerTick;
        SaveNowCommand = new RelayCommand(() => { SaveNow(); });

        if (startTimer)
        {
            Start();
        }
    }

    public ICommand SaveNowCommand { get; }
    public bool HasUnsavedChanges => _hasUnsavedChanges;
    public bool LastSaveFailed => _lastSaveFailed;

    public string StateText => LastSaveFailed
        ? "Kaydetme hatası"
        : HasUnsavedChanges
            ? "Değişiklikler bekliyor"
            : "Tüm değişiklikler kaydedildi";

    public string StateBackground => LastSaveFailed
        ? "#FFF1F2"
        : HasUnsavedChanges
            ? "#FFF7E6"
            : "#EAF8EF";

    public string StateForeground => LastSaveFailed
        ? "#9F1239"
        : HasUnsavedChanges
            ? "#92400E"
            : "#166534";

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
    }

    public void MarkDirty()
    {
        SetState(hasUnsavedChanges: true);
        _setStatus("Değişiklikler otomatik kaydedilecek.", StatusTone.Warning);
    }

    public void SetState(bool hasUnsavedChanges, bool failed = false)
    {
        _hasUnsavedChanges = hasUnsavedChanges;
        _lastSaveFailed = failed;
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(LastSaveFailed));
        OnPropertyChanged(nameof(StateText));
        OnPropertyChanged(nameof(StateBackground));
        OnPropertyChanged(nameof(StateForeground));
    }

    public bool FlushPendingChanges()
    {
        if (!HasUnsavedChanges)
        {
            return true;
        }

        return SaveCore(isAutomatic: true);
    }

    public bool SaveNow() => SaveCore(isAutomatic: false);

    private bool SaveCore(bool isAutomatic)
    {
        try
        {
            _configPersistence.Save(_configPersistence.CurrentConfig);
            SetState(hasUnsavedChanges: false);
            _afterSave();
            _setStatus(
                isAutomatic
                    ? $"Otomatik kaydedildi: {_now():HH:mm:ss}"
                    : "Config kaydedildi.",
                StatusTone.Success);
            return true;
        }
        catch (Exception ex)
        {
            SetState(hasUnsavedChanges: true, failed: true);
            _setStatus(
                isAutomatic
                    ? $"Otomatik kaydedilemedi: {ex.Message}"
                    : $"Config kaydedilemedi: {ex.Message}",
                StatusTone.Error);
            _logService.Error(isAutomatic ? "Autosave failed." : "Config save failed.", ex);
            return false;
        }
    }

    private void OnTimerTick(object? sender, EventArgs e) => FlushPendingChanges();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}
