using ActionOrbit.App.Views.MiniTools;

namespace ActionOrbit.App.Services.MiniTools;

public sealed class MiniToolWindowService : IMiniToolLauncher, IDisposable
{
    private readonly Dictionary<string, MiniToolWindow> _windows =
        new(StringComparer.OrdinalIgnoreCase);

    public void Show(string toolId)
    {
        if (!MiniToolCatalog.TryGet(toolId, out var definition))
        {
            throw new ArgumentOutOfRangeException(nameof(toolId), "Bilinmeyen mini araç.");
        }

        if (_windows.TryGetValue(definition.Id, out var existing))
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
            {
                existing.WindowState = System.Windows.WindowState.Normal;
            }

            existing.Activate();
            return;
        }

        var content = CreateContent(definition.Id);
        var window = new MiniToolWindow(definition, content);
        _windows[definition.Id] = window;
        window.Closed += (_, _) =>
        {
            _windows.Remove(definition.Id);
            if (content is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
        window.Show();
        window.Activate();
    }

    public void Dispose()
    {
        foreach (var window in _windows.Values.ToList())
        {
            window.Close();
        }

        _windows.Clear();
    }

    private static System.Windows.Controls.UserControl CreateContent(string toolId) => toolId switch
    {
        "timer" => new TimerToolView(),
        "caffeine" => new CaffeineToolView(),
        "system_glance" => new SystemGlanceToolView(),
        "calculator" => new CalculatorToolView(),
        "color_picker" => new ColorPickerToolView(),
        _ => throw new ArgumentOutOfRangeException(nameof(toolId))
    };
}
