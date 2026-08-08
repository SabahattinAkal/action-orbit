using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ActionOrbit.App.Views.MiniTools;

public partial class TimerToolView : System.Windows.Controls.UserControl, IDisposable
{
    private readonly DispatcherTimer _timer;
    private TimeSpan _selectedDuration = TimeSpan.FromMinutes(5);
    private TimeSpan _remaining = TimeSpan.FromMinutes(5);
    private DateTimeOffset? _endsAt;

    public TimerToolView()
    {
        InitializeComponent();
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _timer.Tick += OnTick;
        UpdateDisplay();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string value } || !int.TryParse(value, out var minutes))
        {
            return;
        }

        _timer.Stop();
        _endsAt = null;
        _selectedDuration = TimeSpan.FromMinutes(minutes);
        _remaining = _selectedDuration;
        StatusText.Text = $"{minutes} dakikalık süre hazır";
        StartButton.Content = "Başlat";
        UpdateDisplay();
    }

    private void StartPause_Click(object sender, RoutedEventArgs e)
    {
        if (_timer.IsEnabled)
        {
            UpdateRemaining();
            _timer.Stop();
            _endsAt = null;
            StartButton.Content = "Devam et";
            StatusText.Text = "Duraklatıldı";
            return;
        }

        if (_remaining <= TimeSpan.Zero)
        {
            _remaining = _selectedDuration;
        }

        _endsAt = DateTimeOffset.Now + _remaining;
        _timer.Start();
        StartButton.Content = "Duraklat";
        StatusText.Text = "Geri sayım sürüyor";
        UpdateDisplay();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _endsAt = null;
        _remaining = _selectedDuration;
        StartButton.Content = "Başlat";
        StatusText.Text = "Hazır";
        UpdateDisplay();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        UpdateRemaining();
        if (_remaining > TimeSpan.Zero)
        {
            return;
        }

        _timer.Stop();
        _endsAt = null;
        StartButton.Content = "Tekrar başlat";
        StatusText.Text = "Süre doldu";
        SystemSounds.Exclamation.Play();
    }

    private void UpdateRemaining()
    {
        if (_endsAt is not null)
        {
            _remaining = _endsAt.Value - DateTimeOffset.Now;
            if (_remaining < TimeSpan.Zero)
            {
                _remaining = TimeSpan.Zero;
            }
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        RemainingText.Text = _remaining.TotalHours >= 1
            ? $"{(int)_remaining.TotalHours:00}:{_remaining.Minutes:00}:{_remaining.Seconds:00}"
            : $"{(int)_remaining.TotalMinutes:00}:{_remaining.Seconds:00}";
        Progress.Value = _selectedDuration <= TimeSpan.Zero
            ? 0
            : Math.Clamp(100 * (1 - _remaining.TotalMilliseconds / _selectedDuration.TotalMilliseconds), 0, 100);
    }
}
