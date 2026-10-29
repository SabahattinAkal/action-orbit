using System.Windows;
using System.Windows.Input;
using ActionOrbit.App.Services.MiniTools;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Views.MiniTools;

public partial class MiniToolWindow : Window
{
    private bool _isPinned;

    public MiniToolWindow(MiniToolDefinition definition, FrameworkElement content)
    {
        InitializeComponent();
        Title = $"Action Orbit · {definition.Title}";
        TitleText.Text = definition.Title;
        DescriptionText.Text = definition.Description;
        ToolContentHost.Content = content;
        SourceInitialized += (_, _) => FloatingWindowPlacementService.Place(this);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        FloatingWindowPlacementService.Place(this);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        _isPinned = !_isPinned;
        Topmost = _isPinned;
        PinButton.Content = _isPinned ? "◆" : "◇";
        PinButton.ToolTip = _isPinned ? "Üstte tutmayı bırak" : "Her zaman üstte tut";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
