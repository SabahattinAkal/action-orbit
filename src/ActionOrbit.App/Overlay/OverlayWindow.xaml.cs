using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using System.Windows.Media.Animation;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Overlay;

public partial class OverlayWindow : Window
{
    private bool _isClosing;
    private readonly bool _cancelWhenPointerLeaves;

    public OverlayWindow(
        ProfileConfig profile,
        ProfileConfig defaultProfile,
        ThemeConfig theme,
        ActivationSettings activationSettings,
        ActionExecutionService actionExecutionService,
        LogService logService,
        IntPtr restoreWindow)
    {
        InitializeComponent();
        _cancelWhenPointerLeaves = activationSettings.CancelWhenPointerLeaves;

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
            if (theme.Animation)
            {
                BeginAnimation(
                    OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    });
            }
        };
        Closing += (_, _) => _isClosing = true;
    }

    public double OrbitCenterX { get; }
    public double OrbitCenterY { get; }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (DataContext is OverlayViewModel viewModel && viewModel.TryHandleKey(e.Key))
        {
            e.Handled = true;
            return;
        }

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

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is OverlayViewModel viewModel && viewModel.SwitchRing(e.Delta > 0 ? -1 : 1))
        {
            e.Handled = true;
        }
    }

    private void Window_PreviewMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (!_cancelWhenPointerLeaves || _isClosing || DataContext is not OverlayViewModel viewModel)
        {
            return;
        }

        var position = e.GetPosition(RootCanvas);
        var normalizedX = (position.X - OrbitCenterX) / (viewModel.RadiusX + viewModel.ButtonSize);
        var normalizedY = (position.Y - OrbitCenterY) / (viewModel.RadiusY + viewModel.ButtonSize);
        if (normalizedX * normalizedX + normalizedY * normalizedY > 2.1)
        {
            CloseOverlay();
        }
    }

    public void ExecuteHoveredActionOrClose()
    {
        var hoveredButton = FindHoveredButton(RootCanvas);
        if (hoveredButton?.Command?.CanExecute(hoveredButton.CommandParameter) == true)
        {
            hoveredButton.Command.Execute(hoveredButton.CommandParameter);
            return;
        }

        CloseOverlay();
    }

    private static WpfButton? FindHoveredButton(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is WpfButton { IsMouseOver: true } button)
            {
                return button;
            }

            var nested = FindHoveredButton(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
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
