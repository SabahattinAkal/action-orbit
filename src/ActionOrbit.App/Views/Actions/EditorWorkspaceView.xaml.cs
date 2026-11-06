using System.Windows;
using System.Windows.Controls;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Views.Actions;

public partial class EditorWorkspaceView : System.Windows.Controls.UserControl
{
    private EditorLayoutMode? _layoutMode;

    internal bool RunPanelVisibilitySmoke()
    {
        var profiles = ShowProfiles.IsChecked;
        var details = ShowDetails.IsChecked;
        try
        {
            ShowProfiles.IsChecked = false;
            ShowDetails.IsChecked = false;
            UpdateLayout();
            var hidden = ProfilePanel.Visibility == Visibility.Collapsed && DetailPanel.Visibility == Visibility.Collapsed;
            ShowProfiles.IsChecked = true;
            ShowDetails.IsChecked = true;
            UpdateLayout();
            return hidden && ProfilePanel.Visibility == Visibility.Visible && DetailPanel.Visibility == Visibility.Visible;
        }
        finally { ShowProfiles.IsChecked = profiles; ShowDetails.IsChecked = details; }
    }

    private void PanelVisibility_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        _layoutMode = null;
        ApplyLayout(ActualWidth);
    }

    public EditorWorkspaceView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyLayout(ActualWidth);
        SizeChanged += (_, args) => ApplyLayout(args.NewSize.Width);
    }

    private void ApplyLayout(double availableWidth)
    {
        var mode = EditorLayoutPolicy.Resolve(availableWidth, ShowProfiles.IsChecked == true, ShowDetails.IsChecked == true);
        if (_layoutMode == mode)
        {
            return;
        }

        _layoutMode = mode;
        WorkspaceScroll.VerticalScrollBarVisibility =
            EditorLayoutPolicy.ShouldScrollWorkspace(mode)
                ? ScrollBarVisibility.Auto
                : ScrollBarVisibility.Disabled;

        if (mode == EditorLayoutMode.Compact)
        {
            ApplyCompactLayout();
            return;
        }

        ApplyWideLayout();
        ApplyPanelVisibility();
    }

    private void ApplyWideLayout()
    {
        LayoutGrid.MinHeight = 0;

        ProfileColumn.Width = new GridLength(0.95, GridUnitType.Star);
        ProfileColumn.MinWidth = 250;
        ActionColumn.Width = new GridLength(1.05, GridUnitType.Star);
        ActionColumn.MinWidth = 270;
        DetailColumn.Width = new GridLength(1.2, GridUnitType.Star);
        DetailColumn.MinWidth = 300;

        ProfileRow.Height = new GridLength(1, GridUnitType.Star);
        ActionRow.Height = new GridLength(0);
        DetailRow.Height = new GridLength(0);

        Place(ProfilePanel, row: 0, column: 0, new Thickness(0, 0, 14, 0));
        Place(ActionPanel, row: 0, column: 1, new Thickness(0, 0, 14, 0));
        Place(DetailPanel, row: 0, column: 2, new Thickness(0));
    }

    private void ApplyCompactLayout()
    {
        LayoutGrid.MinHeight = 0;

        ProfileColumn.Width = new GridLength(1, GridUnitType.Star);
        ProfileColumn.MinWidth = 0;
        ActionColumn.Width = new GridLength(0);
        ActionColumn.MinWidth = 0;
        DetailColumn.Width = new GridLength(0);
        DetailColumn.MinWidth = 0;

        ProfileRow.Height = new GridLength(650);
        ActionRow.Height = new GridLength(760);
        DetailRow.Height = new GridLength(840);

        Place(ProfilePanel, row: 0, column: 0, new Thickness(0, 0, 0, 14));
        Place(ActionPanel, row: 1, column: 0, new Thickness(0, 0, 0, 14));
        Place(DetailPanel, row: 2, column: 0, new Thickness(0));
        ApplyPanelVisibility();
    }

    private void ApplyPanelVisibility()
    {
        var profiles = ShowProfiles.IsChecked == true;
        var details = ShowDetails.IsChecked == true;
        ProfilePanel.Visibility = profiles ? Visibility.Visible : Visibility.Collapsed;
        DetailPanel.Visibility = details ? Visibility.Visible : Visibility.Collapsed;
        if (_layoutMode == EditorLayoutMode.Compact)
        {
            ProfileRow.Height = new GridLength(profiles ? 650 : 0);
            DetailRow.Height = new GridLength(details ? 840 : 0);
        }
        else
        {
            if (!profiles) { ProfileColumn.Width = new GridLength(0); ProfileColumn.MinWidth = 0; }
            if (!details) { DetailColumn.Width = new GridLength(0); DetailColumn.MinWidth = 0; }
            ActionPanel.Margin = details ? new Thickness(0, 0, 14, 0) : new Thickness(0);
        }
    }

    private static void Place(FrameworkElement element, int row, int column, Thickness margin)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        element.Margin = margin;
    }
}
