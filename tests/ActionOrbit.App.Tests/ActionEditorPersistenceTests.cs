using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Tests;

public sealed class ActionEditorPersistenceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"action-orbit-action-editor-tests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndReload_PreservesActionOrderAndFolderMove()
    {
        var (configService, viewModel) = CreateViewModel();
        var profile = configService.CurrentConfig.Profiles[0];
        profile.Actions =
        [
            CreateAction("alpha", "Alpha"),
            CreateAction("beta", "Beta")
        ];
        viewModel.ReloadForSelectedProfile();

        viewModel.SelectedAction = viewModel.ActionRows.Single(row => row.Action.Id == "alpha");
        viewModel.MoveActionDownCommand.Execute(null);
        viewModel.AddFolderCommand.Execute(null);
        var folder = viewModel.SelectedAction!;
        viewModel.AddActionCommand.Execute(null);
        var nestedAction = viewModel.SelectedAction!;
        nestedAction.Title = "Kalıcı Alt Aksiyon";
        nestedAction.Type = "open_url";
        nestedAction.Target = "https://example.com/persisted";
        viewModel.MoveActionIntoFolder(nestedAction, folder);

        configService.Save(configService.CurrentConfig);
        var reloaded = CreateConfigService();
        reloaded.Load();

        var savedProfile = reloaded.CurrentConfig.Profiles[0];
        Assert.Equal(["beta", "alpha", folder.Action.Id], savedProfile.Actions.Select(action => action.Id));
        var savedFolder = savedProfile.Actions[2];
        var savedChild = Assert.Single(savedFolder.Children);
        Assert.Equal("Kalıcı Alt Aksiyon", savedChild.Title);
        Assert.Equal("open_url", savedChild.Type);
        Assert.Equal("https://example.com/persisted", savedChild.Target);
    }

    [Fact]
    public void SaveAndReload_PreservesPresetAppliedByActionEditor()
    {
        var (configService, viewModel) = CreateViewModel();
        var profile = configService.CurrentConfig.Profiles[0];
        profile.Actions = [CreateAction("editable", "Düzenlenecek")];
        viewModel.ReloadForSelectedProfile();
        viewModel.SelectedAction = Assert.Single(viewModel.ActionRows);
        viewModel.SelectedPreset = viewModel.ActionPresets.First(preset => preset.Type != "folder");

        viewModel.ApplyPresetCommand.Execute(null);
        configService.Save(configService.CurrentConfig);
        var reloaded = CreateConfigService();
        reloaded.Load();

        var savedAction = Assert.Single(reloaded.CurrentConfig.Profiles[0].Actions);
        Assert.Equal(viewModel.SelectedPreset.Title, savedAction.Title);
        Assert.Equal(viewModel.SelectedPreset.Type, savedAction.Type);
        Assert.Equal(viewModel.SelectedPreset.Target, savedAction.Target);
        Assert.Equal(viewModel.SelectedPreset.Arguments, savedAction.Arguments);
    }

    [Fact]
    public void AddChildAction_WhenRegularActionIsSelected_DoesNotConvertItToFolder()
    {
        var (configService, viewModel) = CreateViewModel();
        var action = CreateAction("regular", "Normal Aksiyon");
        configService.CurrentConfig.Profiles[0].Actions = [action];
        viewModel.ReloadForSelectedProfile();
        viewModel.SelectedAction = Assert.Single(viewModel.ActionRows);

        viewModel.AddChildActionCommand.Execute(null);

        Assert.Equal("open_url", action.Type);
        Assert.Empty(action.Children);
    }

    [Fact]
    public void ApplyPreset_WhenFolderHasChildren_PreservesFolderAndChildren()
    {
        var (configService, viewModel) = CreateViewModel();
        var child = CreateAction("child", "Alt Aksiyon");
        var folder = new OrbitAction
        {
            Id = "folder",
            Title = "Klasör",
            Type = "folder",
            Children = [child]
        };
        configService.CurrentConfig.Profiles[0].Actions = [folder];
        viewModel.ReloadForSelectedProfile();
        viewModel.SelectedAction = viewModel.ActionRows[0];
        viewModel.SelectedPreset = viewModel.ActionPresets.First(preset => preset.Type != "folder");

        Assert.False(viewModel.ApplyPresetCommand.CanExecute(null));
        viewModel.ApplyPresetCommand.Execute(null);

        Assert.Equal("folder", folder.Type);
        Assert.Equal("Klasör", folder.Title);
        Assert.Same(child, Assert.Single(folder.Children));
    }

    [Fact]
    public void RingPreview_PaginatesEveryRootActionAndFollowsSelection()
    {
        var (configService, viewModel) = CreateViewModel();
        configService.CurrentConfig.Profiles[0].Actions = Enumerable.Range(1, 18)
            .Select(index => CreateAction($"action_{index}", $"Aksiyon {index}"))
            .ToList();

        viewModel.ReloadForSelectedProfile();

        Assert.Equal(3, viewModel.RingPreviewPageCount);
        Assert.Equal(8, viewModel.RingPreviewSlots.Count);
        Assert.Equal("1 / 3", viewModel.RingPreviewPageText);

        viewModel.NextRingPreviewPageCommand.Execute(null);

        Assert.Equal(1, viewModel.RingPreviewPageIndex);
        Assert.Equal("Aksiyon 9", viewModel.RingPreviewSlots[0].Title);

        viewModel.SelectedAction = viewModel.ActionRows.Single(row => row.Action.Id == "action_18");

        Assert.Equal(2, viewModel.RingPreviewPageIndex);
        Assert.Equal(2, viewModel.RingPreviewSlots.Count);
        Assert.Contains(viewModel.RingPreviewSlots, slot => slot.Title == "Aksiyon 18" && slot.IsSelected);
    }

    private (ConfigService ConfigService, ActionEditorViewModel ViewModel) CreateViewModel()
    {
        var configService = CreateConfigService();
        configService.Load();
        var logService = new LogService(_tempDirectory);
        var executionService = new ActionExecutionService(logService, Array.Empty<IActionHandler>());
        var selectedProfile = configService.CurrentConfig.Profiles[0];
        var viewModel = new ActionEditorViewModel(
            configService,
            executionService,
            logService,
            () => selectedProfile,
            () => { },
            _ => { },
            (_, _) => { },
            () => { });
        return (configService, viewModel);
    }

    private ConfigService CreateConfigService()
    {
        Directory.CreateDirectory(_tempDirectory);
        return new ConfigService(new LogService(_tempDirectory), _tempDirectory);
    }

    private static OrbitAction CreateAction(string id, string title) => new()
    {
        Id = id,
        Title = title,
        Icon = "app",
        Type = "open_url",
        Target = $"https://example.com/{id}"
    };

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
