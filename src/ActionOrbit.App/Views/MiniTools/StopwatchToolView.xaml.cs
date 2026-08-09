using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace ActionOrbit.App.Views.MiniTools;

public partial class StopwatchToolView : System.Windows.Controls.UserControl, IDisposable
{
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _timer;
    private readonly ObservableCollection<string> _laps = [];

    public StopwatchToolView()
    {
        InitializeComponent();
        LapsList.ItemsSource = _laps;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _timer.Tick += OnTick;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        _stopwatch.Stop();
    }

    private void StartPause_Click(object sender, RoutedEventArgs e)
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
            _timer.Stop();
            StartButton.Content = "Devam et";
            LapButton.IsEnabled = false;
            StateText.Text = "Duraklatıldı";
            UpdateDisplay();
            return;
        }

        _stopwatch.Start();
        _timer.Start();
        StartButton.Content = "Duraklat";
        LapButton.IsEnabled = true;
        StateText.Text = "Ölçüm sürüyor";
    }

    private void Lap_Click(object sender, RoutedEventArgs e)
    {
        if (!_stopwatch.IsRunning)
        {
            return;
        }

        _laps.Insert(0, $"Tur {_laps.Count + 1:00}   {Format(_stopwatch.Elapsed)}");
        EmptyLapsText.Visibility = Visibility.Collapsed;
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _stopwatch.Reset();
        _timer.Stop();
        _laps.Clear();
        StartButton.Content = "Başlat";
        LapButton.IsEnabled = false;
        StateText.Text = "Hazır";
        EmptyLapsText.Visibility = Visibility.Visible;
        UpdateDisplay();
    }

    private void OnTick(object? sender, EventArgs e) => UpdateDisplay();

    private void UpdateDisplay() => ElapsedText.Text = Format(_stopwatch.Elapsed);

    private static string Format(TimeSpan elapsed) =>
        $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds / 10:00}";
}
