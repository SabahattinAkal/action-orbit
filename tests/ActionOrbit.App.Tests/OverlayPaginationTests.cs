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
        Assert.Equal("overflow", viewModel.ActionItems[^1].Type);

        viewModel.ActionItems[^1].Command.Execute(null);

        Assert.Contains(viewModel.ActionItems, item => item.Action.Id == "action_8");
        Assert.Contains(viewModel.ActionItems, item => item.Action.Id == "action_10");
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
        viewModel.SatelliteItems[^1].Command.Execute(null);
        Assert.Contains(viewModel.SatelliteItems, item => item.Action.Id == "action_9");
        Assert.Contains(viewModel.SatelliteItems, item => item.Action.Id == "action_10");
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
