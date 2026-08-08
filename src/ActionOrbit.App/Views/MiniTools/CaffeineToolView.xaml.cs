using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ActionOrbit.App.Services.MiniTools;

namespace ActionOrbit.App.Views.MiniTools;

public partial class CaffeineToolView : System.Windows.Controls.UserControl, IDisposable
{
    private readonly AwakeController _awakeController = new();
    private readonly DispatcherTimer _timer;
    private DateTimeOffset? _expiresAt;

    public CaffeineToolView()
    {
        InitializeComponent();
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTick;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        _awakeController.Dispose();
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (_awakeController.IsActive)
        {
            StopAwake("Uyku engeli kapalı");
            return;
        }

        if (!_awakeController.Activate())
        {
            StateText.Text = "Başlatılamadı";
            RemainingText.Text = "Windows isteği kabul etmedi";
            return;
        }

        var minutes = GetSelectedMinutes();
        _expiresAt = minutes > 0 ? DateTimeOffset.Now.AddMinutes(minutes) : null;
        _timer.Start();
        DurationBox.IsEnabled = false;
        ToggleButton.Content = "Uyanık tutmayı durdur";
        StateText.Text = "Bilgisayar uyanık tutuluyor";
        StateDot.Background = (System.Windows.Media.Brush)FindResource("PrimaryActionBrush");
        UpdateRemaining();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_expiresAt is not null && DateTimeOffset.Now >= _expiresAt.Value)
        {
            StopAwake("Süre tamamlandı");
            return;
        }

        UpdateRemaining();
    }

    private void StopAwake(string state)
    {
        _awakeController.Deactivate();
        _timer.Stop();
        _expiresAt = null;
        DurationBox.IsEnabled = true;
        ToggleButton.Content = "Uyanık tutmayı başlat";
        StateText.Text = state;
        RemainingText.Text = "Windows güç ayarları normal çalışıyor";
        StateDot.Background = (System.Windows.Media.Brush)FindResource("NavigationBrush");
    }

    private void UpdateRemaining()
    {
        if (_expiresAt is null)
        {
            RemainingText.Text = "Sen kapatana kadar aktif";
            return;
        }

        var remaining = _expiresAt.Value - DateTimeOffset.Now;
        if (remaining < TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
        }

        RemainingText.Text = remaining.TotalHours >= 1
            ? $"{(int)remaining.TotalHours} sa {remaining.Minutes:00} dk kaldı"
            : $"{Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))} dk kaldı";
    }

    private int GetSelectedMinutes() =>
        DurationBox.SelectedItem is ComboBoxItem { Tag: string tag } && int.TryParse(tag, out var minutes)
            ? minutes
            : 60;
}
