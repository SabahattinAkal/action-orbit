using ActionOrbit.App.ViewModels;
using ActionOrbit.App.Views.Shelf;

namespace ActionOrbit.App.Services;

public sealed class ShelfWindowService : IDisposable
{
    private readonly ShelfViewModel _viewModel;
    private ShelfWindow? _window;
    private System.Windows.Rect? _lastBounds;

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

        var workArea = System.Windows.SystemParameters.WorkArea;
        if (_lastBounds is { } bounds)
        {
            _window.Width = Math.Clamp(bounds.Width, _window.MinWidth, workArea.Width);
            _window.Height = Math.Clamp(bounds.Height, _window.MinHeight, workArea.Height);
            _window.Left = Math.Clamp(bounds.Left, workArea.Left, workArea.Right - _window.Width);
            _window.Top = Math.Clamp(bounds.Top, workArea.Top, workArea.Bottom - _window.Height);
        }
        else
        {
            _window.Left = Math.Max(workArea.Left + 16, workArea.Right - _window.Width - 24);
            _window.Top = Math.Max(workArea.Top + 16, workArea.Top + (workArea.Height - _window.Height) / 2);
        }

        _window.Closed += (_, _) =>
        {
            if (_window is { } closingWindow)
            {
                var restoreBounds = closingWindow.RestoreBounds;
                _lastBounds = restoreBounds.Width > 0 && restoreBounds.Height > 0
                    ? restoreBounds
                    : new System.Windows.Rect(
                        closingWindow.Left,
                        closingWindow.Top,
                        closingWindow.Width,
                        closingWindow.Height);
            }

            _window = null;
        };
        _window.Show();
        _window.Activate();
    }

    public void Dispose()
    {
        _window?.Close();
        _window = null;
    }
}
