using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ActionOrbit.App.Services.MiniTools;

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
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var cursor = System.Windows.Forms.Cursor.Position;
        var screen = System.Windows.Forms.Screen.FromPoint(cursor);
        var scale = VisualTreeHelper.GetDpi(this);
        var workArea = screen.WorkingArea;
        var left = workArea.Left / scale.DpiScaleX;
        var top = workArea.Top / scale.DpiScaleY;
        var right = workArea.Right / scale.DpiScaleX;
        var bottom = workArea.Bottom / scale.DpiScaleY;
        Left = Math.Clamp(cursor.X / scale.DpiScaleX + 18, left + 10, right - ActualWidth - 10);
        Top = Math.Clamp(cursor.Y / scale.DpiScaleY + 18, top + 10, bottom - ActualHeight - 10);
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
