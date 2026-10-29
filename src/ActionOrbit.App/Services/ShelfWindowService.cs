using ActionOrbit.App.ViewModels;
using ActionOrbit.App.Views.Shelf;

namespace ActionOrbit.App.Services;

public sealed class ShelfWindowService : IDisposable
{
    private readonly ShelfViewModel _viewModel;
    private ShelfWindow? _window;
    private PixelRect? _lastBounds;

    public ShelfWindowService(ShelfViewModel viewModel) => _viewModel = viewModel;

    public void Show()
    {
        if (_window is { IsVisible: true })
        {
            if (_window.WindowState == System.Windows.WindowState.Minimized)
            {
                _window.WindowState = System.Windows.WindowState.Normal;
            }

            _window.Activate();
            return;
        }

        _window = new ShelfWindow
        {
            DataContext = _viewModel,
            WindowStartupLocation = System.Windows.WindowStartupLocation.Manual
        };

        var window = _window;
        window.SourceInitialized += (_, _) => FloatingWindowPlacementService.Place(window, alignRight: true, _lastBounds);
        window.Loaded += (_, _) => FloatingWindowPlacementService.Place(window, alignRight: true, _lastBounds);

        window.Closing += (_, _) =>
        {
            _lastBounds = FloatingWindowPlacementService.GetBounds(window) ?? _lastBounds;
        };
        window.Closed += (_, _) => _window = null;
        _window.Show();
        _window.Activate();
    }

    public void Dispose()
    {
        _window?.Close();
        _window = null;
    }
}
