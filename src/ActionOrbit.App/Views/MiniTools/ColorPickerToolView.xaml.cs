using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ActionOrbit.App.Services.MiniTools;

namespace ActionOrbit.App.Views.MiniTools;

public partial class ColorPickerToolView : System.Windows.Controls.UserControl, IDisposable
{
    private readonly DispatcherTimer _timer;
    private bool _isTracking;

    public ColorPickerToolView()
    {
        InitializeComponent();
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(70)
        };
        _timer.Tick += OnTick;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private void Track_Click(object sender, RoutedEventArgs e) => ToggleTracking();

    private void View_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Space && _isTracking)
        {
            ToggleTracking();
            e.Handled = true;
        }
    }

    private void ToggleTracking()
    {
        _isTracking = !_isTracking;
        if (_isTracking)
        {
            Sample();
            _timer.Start();
            TrackButton.Content = "Rengi dondur (Space)";
            HintText.Text = "İmleci istediğin piksele götür ve Space'e bas";
            TrackButton.Focus();
        }
        else
        {
            _timer.Stop();
            TrackButton.Content = "Yeniden izlemeye başla";
            HintText.Text = "Renk donduruldu; HEX veya RGB değerini kopyalayabilirsin";
        }
    }

    private void OnTick(object? sender, EventArgs e) => Sample();

    private void Sample()
    {
        if (!ScreenColorSampler.TrySample(out var red, out var green, out var blue))
        {
            HintText.Text = "Bu pikselin rengi okunamadı";
            return;
        }

        var color = System.Windows.Media.Color.FromRgb(red, green, blue);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        ColorSwatch.Background = brush;
        HexText.Text = $"#{red:X2}{green:X2}{blue:X2}";
        RgbText.Text = $"{red}, {green}, {blue}";
    }

    private void CopyHex_Click(object sender, RoutedEventArgs e) => Copy(HexText.Text);

    private void CopyRgb_Click(object sender, RoutedEventArgs e) => Copy($"rgb({RgbText.Text})");

    private void Copy(string value)
    {
        try
        {
            System.Windows.Clipboard.SetText(value);
            HintText.Text = $"{value} panoya kopyalandı";
        }
        catch (Exception)
        {
            HintText.Text = "Pano şu anda kullanılıyor";
        }
    }
}
