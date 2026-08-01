using ActionOrbit.App.ViewModels;
using ActionOrbit.App.Views.Shelf;

namespace ActionOrbit.App.Services;

public sealed class ShelfWindowService : IDisposable
{
    private readonly ShelfViewModel _viewModel;
    private ShelfWindow? _window;

    public ShelfWindowService(ShelfViewModel viewModel) => _viewModel = viewModel;

    public void Show()
    {
        if (_window is { IsVisible: true })
        {
            _window.Activate();
            return;
        }

        _window = new ShelfWindow
        {
            DataContext = _viewModel,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
        };
        _window.Closed += (_, _) => _window = null;
        _window.Show();
        _window.Activate();
    }

    public void Dispose()
    {
        _window?.Close();
        _window = null;
    }
}
