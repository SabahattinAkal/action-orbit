using System.Windows.Controls;
using System.Windows.Threading;
using ActionOrbit.App.Services.MiniTools;

namespace ActionOrbit.App.Views.MiniTools;

public partial class SystemGlanceToolView : System.Windows.Controls.UserControl, IDisposable
{
    private const double Gibibyte = 1024d * 1024 * 1024;
    private readonly SystemMetricsReader _reader = new();
    private readonly DispatcherTimer _timer;

    public SystemGlanceToolView()
    {
        InitializeComponent();
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTick;
        _timer.Start();
        Refresh();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private void OnTick(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        var snapshot = _reader.Read();
        CpuProgress.Value = snapshot.CpuPercent;
        CpuText.Text = $"{snapshot.CpuPercent:0}%";
        MemoryProgress.Value = snapshot.MemoryPercent;
        MemoryText.Text = $"{snapshot.MemoryPercent:0}%";
        MemoryDetailText.Text = snapshot.TotalMemoryBytes == 0
            ? "Bellek bilgisi alınamadı"
            : $"{snapshot.UsedMemoryBytes / Gibibyte:0.0} / {snapshot.TotalMemoryBytes / Gibibyte:0.0} GB kullanım";
        BatteryText.Text = snapshot.HasBattery ? $"{snapshot.BatteryPercent}%" : "Masaüstü";
        PowerDetailText.Text = snapshot.HasBattery
            ? snapshot.IsCharging ? "Prize bağlı · şarj oluyor" : "Pil kullanılıyor"
            : "Pil algılanmadı";
    }
}
