using System.Windows;
using System.Windows.Input;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Overlay;

public partial class OverlayWindow : Window
{
    private bool _isClosing;

    public OverlayWindow(
        ProfileConfig profile,
        ProfileConfig defaultProfile,
        ThemeConfig theme,
        ActionExecutionService actionExecutionService,
        LogService logService,
        IntPtr restoreWindow)
    {
        InitializeComponent();

        var viewModel = new OverlayViewModel(profile, defaultProfile, theme, actionExecutionService, logService, restoreWindow);
        viewModel.CloseRequested += CloseOverlay;
        DataContext = viewModel;

        Width = viewModel.WindowWidth;
        Height = viewModel.WindowHeight;
        RootCanvas.Width = viewModel.WindowWidth;
        RootCanvas.Height = viewModel.WindowHeight;
        OrbitCenterX = viewModel.CenterX;
        OrbitCenterY = viewModel.CenterY;

        Loaded += (_, _) =>
        {
            Activate();
            Focus();
        };
        Closing += (_, _) => _isClosing = true;
    }

    public double OrbitCenterX { get; }
    public double OrbitCenterY { get; }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            if (TryCollapseOpenFolder())
            {
                return;
            }

            CloseOverlay();
        }
    }

    private void RootCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, RootCanvas))
        {
            if (TryCollapseOpenFolder())
            {
                return;
            }

            CloseOverlay();
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        CloseOverlay();
    }

    private void CloseOverlay()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        try
        {
            Close();
        }
        catch (InvalidOperationException)
        {
            // WPF can raise Deactivated while the window is already in its closing path.
        }
    }

    private bool TryCollapseOpenFolder() =>
        DataContext is OverlayViewModel viewModel && viewModel.TryCollapseFolder();
}
