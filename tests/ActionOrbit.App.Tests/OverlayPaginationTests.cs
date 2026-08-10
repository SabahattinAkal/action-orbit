using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.ViewModels;
using System.Windows.Input;

namespace ActionOrbit.App.Tests;

public sealed class OverlayPaginationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"action-orbit-tests-{Guid.NewGuid():N}");

    [Fact]
    public void MainRing_PaginatesMoreThanEightActions()
    {
        var profile = new ProfileConfig
        {
            Id = "default",
            Name = "Default",
            Actions = Enumerable.Range(1, 10).Select(CreateAction).ToList()
        };
        var viewModel = CreateViewModel(profile);

        Assert.Equal(8, viewModel.ActionItems.Count);
        Assert.Equal(2, viewModel.MainPageCount);
        Assert.DoesNotContain(viewModel.ActionItems, item => item.Type == "overflow");
        Assert.True(viewModel.HasPageNavigation);
        Assert.False(viewModel.PreviousPageCommand.CanExecute(null));
        Assert.True(viewModel.NextPageCommand.CanExecute(null));

        viewModel.NextPageCommand.Execute(null);

        Assert.Contains(viewModel.ActionItems, item => item.Action.Id == "action_9");
        Assert.Contains(viewModel.ActionItems, item => item.Action.Id == "action_10");
        Assert.True(viewModel.PreviousPageCommand.CanExecute(null));
        Assert.False(viewModel.NextPageCommand.CanExecute(null));

        viewModel.PreviousPageCommand.Execute(null);

        Assert.Contains(viewModel.ActionItems, item => item.Action.Id == "action_1");
        Assert.Equal(0, viewModel.MainPageIndex);
    }

    [Fact]
    public void FolderRing_PaginatesAllChildrenInsteadOfHidingThem()
    {
        var folder = new OrbitAction
        {
            Id = "folder",
            Title = "Folder",
            Type = "folder",
            Children = Enumerable.Range(1, 10).Select(CreateAction).ToList()
        };
        var profile = new ProfileConfig { Id = "default", Name = "Default", Actions = [folder] };
        var viewModel = CreateViewModel(profile);
        var folderButton = viewModel.ActionItems[0];

        folderButton.Command.Execute(folderButton);

        Assert.Equal(9, viewModel.SatelliteItems.Count);
        Assert.Equal(2, viewModel.FolderPageCount);
        Assert.DoesNotContain(viewModel.SatelliteItems, item => item.Type == "overflow");
        Assert.True(viewModel.HasPageNavigation);

        viewModel.NextPageCommand.Execute(null);

        Assert.Contains(viewModel.SatelliteItems, item => item.Action.Id == "action_10");
        Assert.Single(viewModel.SatelliteItems);
    }

    [Fact]
    public void KeyboardArrows_MoveVisibleSelection()
    {
        var profile = new ProfileConfig
        {
            Id = "default",
            Name = "Default",
            Actions = Enumerable.Range(1, 3).Select(CreateAction).ToList()
        };
        var viewModel = CreateViewModel(profile);

        Assert.True(viewModel.ActionItems[0].IsKeyboardSelected);
        Assert.True(viewModel.TryHandleKey(Key.Right));
        Assert.False(viewModel.ActionItems[0].IsKeyboardSelected);
        Assert.True(viewModel.ActionItems[1].IsKeyboardSelected);
    }

    [Fact]
    public void FolderPagination_NavigatesThroughEveryChildAndBackToFirstPage()
    {
        var folder = new OrbitAction
        {
            Id = "folder",
            Title = "Folder",
            Type = "folder",
            Children = Enumerable.Range(1, 25).Select(CreateAction).ToList()
        };
        var profile = new ProfileConfig { Id = "default", Name = "Default", Actions = [folder] };
        var viewModel = CreateViewModel(profile);
        var folderButton = viewModel.ActionItems[0];
        folderButton.Command.Execute(folderButton);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        for (var page = 0; page < viewModel.FolderPageCount; page++)
        {
            foreach (var item in viewModel.SatelliteItems)
            {
                visited.Add(item.Action.Id);
            }

            if (viewModel.NextPageCommand.CanExecute(null))
            {
                viewModel.NextPageCommand.Execute(null);
            }
        }

        Assert.Equal(25, visited.Count);
        Assert.Equal(viewModel.FolderPageCount - 1, viewModel.FolderPageIndex);

        while (viewModel.PreviousPageCommand.CanExecute(null))
        {
            viewModel.PreviousPageCommand.Execute(null);
        }

        Assert.Equal(0, viewModel.FolderPageIndex);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(1)]
    public void MainRing_DoesNotShowPageControlsWhenEverythingFits(int actionCount)
    {
        var profile = new ProfileConfig
        {
            Id = "default",
            Name = "Default",
            Actions = Enumerable.Range(1, actionCount).Select(CreateAction).ToList()
        };
        var viewModel = CreateViewModel(profile);

        Assert.Equal(actionCount, viewModel.ActionItems.Count);
        Assert.False(viewModel.HasPageNavigation);
        Assert.False(viewModel.PreviousPageCommand.CanExecute(null));
        Assert.False(viewModel.NextPageCommand.CanExecute(null));
    }

    [Fact]
    public void FolderRing_DoesNotShowPageControlsForNineChildren()
    {
        var folder = new OrbitAction
        {
            Id = "folder",
            Title = "Folder",
            Type = "folder",
            Children = Enumerable.Range(1, 9).Select(CreateAction).ToList()
        };
        var profile = new ProfileConfig { Id = "default", Name = "Default", Actions = [folder] };
        var viewModel = CreateViewModel(profile);

        viewModel.ActionItems[0].Command.Execute(viewModel.ActionItems[0]);

        Assert.Equal(9, viewModel.SatelliteItems.Count);
        Assert.False(viewModel.HasPageNavigation);
        Assert.False(viewModel.PreviousPageCommand.CanExecute(null));
        Assert.False(viewModel.NextPageCommand.CanExecute(null));
    }

    [Fact]
    public void LightAccent_UsesDarkOverlayForeground()
    {
        var profile = new ProfileConfig
        {
            Id = "default",
            Name = "Default",
            Actions = [CreateAction(1)]
        };
        var log = new LogService(_tempDirectory);
        var executor = new ActionExecutionService(log, []);
        var viewModel = new OverlayViewModel(
            profile,
            profile,
            new ThemeConfig { Accent = "#FFFFFF", Mode = "light" },
            executor,
            log,
            IntPtr.Zero);

        var foreground = Assert.IsType<System.Windows.Media.SolidColorBrush>(
            viewModel.AccentForegroundBrush);
        Assert.Equal(System.Windows.Media.Color.FromRgb(0x11, 0x13, 0x18), foreground.Color);
    }

    [Fact]
    public void OpenShelfCommand_ClosesOverlayBeforeOpeningShelf()
    {
        var profile = new ProfileConfig
        {
            Id = "default",
            Name = "Default",
            Actions = [CreateAction(1)]
        };
        var log = new LogService(_tempDirectory);
        var executor = new ActionExecutionService(log, []);
        var calls = new List<string>();
        var viewModel = new OverlayViewModel(
            profile,
            profile,
            new ThemeConfig(),
            executor,
            log,
            IntPtr.Zero,
            () => calls.Add("shelf"));
        viewModel.CloseRequested += () => calls.Add("close");

        Assert.True(viewModel.OpenShelfCommand.CanExecute(null));
        viewModel.OpenShelfCommand.Execute(null);

        Assert.Equal(["close", "shelf"], calls);
    }

    [Fact]
    public void OverlayWindow_ReservesEnoughBottomSpaceForExpandedFolderInfoPanel()
    {
        var profile = new ProfileConfig
        {
            Id = "default",
            Name = "Default",
            Actions = [CreateAction(1)]
        };
        var viewModel = CreateViewModel(profile);

        var remainingBottomSpace = viewModel.WindowHeight
            - viewModel.OverlayInfoTop
            - OverlayViewModel.OverlayInfoReservedHeight;

        Assert.True(
            remainingBottomSpace >= OverlayViewModel.OverlayInfoBottomPadding,
            $"Bilgi panelinin altında yalnızca {remainingBottomSpace:0.##} px kaldı.");
    }

    [Fact]
    public void NestedFolder_BackReturnsToParentBeforeMainRing()
    {
        var nested = new OrbitAction
        {
            Id = "nested",
            Title = "Alt Klasör",
            Type = "folder",
            Children = [CreateAction(1)]
        };
        var root = new OrbitAction
        {
            Id = "root",
            Title = "Üst Klasör",
            Type = "folder",
            Children = [nested]
        };
        var profile = new ProfileConfig { Id = "default", Name = "Default", Actions = [root] };
        var viewModel = CreateViewModel(profile);

        viewModel.ActionItems[0].Command.Execute(viewModel.ActionItems[0]);
        viewModel.SatelliteItems[0].Command.Execute(viewModel.SatelliteItems[0]);

        Assert.Equal("Üst Klasör › Alt Klasör", viewModel.SelectedFolderTitle);
        Assert.True(viewModel.TryCollapseFolder());
        Assert.Equal("Üst Klasör", viewModel.SelectedFolderTitle);
        Assert.Same(nested, viewModel.SatelliteItems[0].Action);

        Assert.True(viewModel.TryCollapseFolder());
        Assert.False(viewModel.HasSatellites);
        Assert.False(viewModel.TryCollapseFolder());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private OverlayViewModel CreateViewModel(ProfileConfig profile)
    {
        var log = new LogService(_tempDirectory);
        var executor = new ActionExecutionService(log, []);
        return new OverlayViewModel(profile, profile, new ThemeConfig(), executor, log, IntPtr.Zero);
    }

    private static OrbitAction CreateAction(int index) =>
        new()
        {
            Id = $"action_{index}",
            Title = $"Action {index}",
            Type = "open_url",
            Target = "https://example.com"
        };
}
