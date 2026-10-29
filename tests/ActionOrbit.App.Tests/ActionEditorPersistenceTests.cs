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
    public void ReorderAction_MovesDraggedRowWithinItsOwnerAndKeepsSelection()
    {
        var (configService, viewModel) = CreateViewModel();
        var profile = configService.CurrentConfig.Profiles[0];
        profile.Actions =
        [
            CreateAction("alpha", "Alpha"),
            CreateAction("beta", "Beta"),
            CreateAction("gamma", "Gamma")
        ];
        viewModel.ReloadForSelectedProfile();
        var source = viewModel.ActionRows.Single(row => row.Action.Id == "alpha");
        var target = viewModel.ActionRows.Single(row => row.Action.Id == "gamma");

        Assert.True(ActionEditorViewModel.CanReorderAction(source, target));
        viewModel.ReorderAction(source, target, placeAfterTarget: true);

        Assert.Equal(["beta", "gamma", "alpha"], profile.Actions.Select(action => action.Id));
        Assert.Equal("alpha", viewModel.SelectedAction?.Action.Id);
    }

    [Fact]
    public void ReorderButtons_UseTheClickedRowEvenWhenAnotherRowIsSelected()
    {
        var (configService, viewModel) = CreateViewModel();
        var profile = configService.CurrentConfig.Profiles[0];
        profile.Actions =
        [
            CreateAction("alpha", "Alpha"),
            CreateAction("beta", "Beta"),
            CreateAction("gamma", "Gamma")
        ];
        viewModel.ReloadForSelectedProfile();

        var clickedRow = viewModel.ActionRows.Single(row => row.Action.Id == "alpha");
        viewModel.SelectedAction = viewModel.ActionRows.Single(row => row.Action.Id == "gamma");

        Assert.True(viewModel.MoveActionDownCommand.CanExecute(clickedRow));
        viewModel.MoveActionDownCommand.Execute(clickedRow);

        Assert.Equal(["beta", "alpha", "gamma"], profile.Actions.Select(action => action.Id));
        Assert.Equal("alpha", viewModel.SelectedAction?.Action.Id);
        Assert.False(viewModel.MoveActionUpCommand.CanExecute(viewModel.ActionRows.Single(row => row.Action.Id == "beta")));
        Assert.False(viewModel.MoveActionDownCommand.CanExecute(viewModel.ActionRows.Single(row => row.Action.Id == "gamma")));
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReorderAndUndo_AfterAutosave_StillChangeThePersistedActions(bool nested)
    {
        var undo = new UndoManager();
        var (configService, viewModel) = CreateViewModel(undo.Register);
        var actions = new List<OrbitAction>
        {
            CreateAction("alpha", "Alpha"), CreateAction("beta", "Beta"), CreateAction("gamma", "Gamma")
        };
        configService.CurrentConfig.Profiles[0].Actions = nested
            ? [new OrbitAction { Id = "folder", Title = "Folder", Type = "folder", Children = actions }]
            : actions;
        viewModel.ReloadForSelectedProfile();
        configService.Save(configService.CurrentConfig);

        viewModel.MoveActionDownCommand.Execute(viewModel.ActionRows.Single(row => row.Id == "alpha"));
        configService.Save(configService.CurrentConfig);
        Assert.Equal(["beta", "alpha", "gamma"], actions.Select(action => action.Id));

        undo.Undo();
        configService.Save(configService.CurrentConfig);
        var reloaded = CreateConfigService();
        reloaded.Load();
        var persisted = reloaded.CurrentConfig.Profiles[0].Actions;
        if (nested) persisted = Assert.Single(persisted).Children;
        Assert.Equal(["alpha", "beta", "gamma"], persisted.Select(action => action.Id));
    }

    [Fact]
    public void ActionSearch_FindsNestedActionsWithFolderContext_WithoutChangingSavedDataOrPreview()
    {
        var dirtyCount = 0;
        var (config, editor) = CreateViewModel(markDirty: () => dirtyCount++);
        var child = CreateAction("needle", "Needle tool");
        config.CurrentConfig.Profiles[0].Actions =
        [new OrbitAction { Id = "folder", Title = "Tools", Type = "folder", Children = [child, CreateAction("other", "Other")] },
            CreateAction("root", "Root")];
        editor.ReloadForSelectedProfile();
        editor.ActionSearchText = "needle";

        Assert.Equal(["folder", "needle"], editor.VisibleActionRows.Select(row => row.Id));
        Assert.Equal(4, editor.ActionRows.Count);
        Assert.Equal(2, editor.RingPreviewSlots.Count);
        Assert.Equal(0, dirtyCount);
        config.Save(config.CurrentConfig);
        Assert.DoesNotContain("actionSearchText", File.ReadAllText(config.ConfigPath), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CollapsedFolder_SearchTemporarilyRevealsChildren_AndClearRestoresCollapse()
    {
        var dirtyCount = 0;
        var (config, editor) = CreateViewModel(markDirty: () => dirtyCount++);
        config.CurrentConfig.Profiles[0].Actions =
        [new OrbitAction { Id = "folder", Title = "Tools", Type = "folder", Children = [CreateAction("child", "Child")] }];
        editor.ReloadForSelectedProfile();
        var folder = editor.ActionRows[0];
        editor.SelectedAction = editor.ActionRows[1];
        editor.ToggleActionFolderCommand.Execute(folder);
        Assert.Same(folder, Assert.Single(editor.VisibleActionRows));
        Assert.Same(folder, editor.SelectedAction);
        editor.ActionSearchText = "Child";
        Assert.Equal(2, editor.VisibleActionRows.Count);
        Assert.True(folder.IsExpanded);
        Assert.False(editor.CanToggleActionFolders);
        editor.ClearActionSearchCommand.Execute(null);
        Assert.Single(editor.VisibleActionRows);
        Assert.False(folder.IsExpanded);
        Assert.Equal(0, dirtyCount);
    }

    [Fact]
    public void FilteredReorder_UsesFullOwnerOrder_AndPersistsAfterSave()
    {
        var (config, editor) = CreateViewModel();
        config.CurrentConfig.Profiles[0].Actions =
        [CreateAction("alpha", "Alpha"), CreateAction("beta", "Beta"), CreateAction("gamma", "Gamma")];
        editor.ReloadForSelectedProfile();
        editor.ActionSearchText = "Beta";
        editor.MoveActionUpCommand.Execute(Assert.Single(editor.VisibleActionRows));
        config.Save(config.CurrentConfig);
        Assert.Equal(["beta", "alpha", "gamma"], config.Reload().Profiles[0].Actions.Select(action => action.Id));
        Assert.Single(editor.VisibleActionRows);
        Assert.Equal("beta", editor.SelectedAction?.Id);
    }

    [Fact]
    public void SearchWithoutResults_CanBeCleared_AndDoesNotHideNewActions()
    {
        var (config, editor) = CreateViewModel();
        config.CurrentConfig.Profiles[0].Actions = [CreateAction("one", "One")];
        editor.ReloadForSelectedProfile();
        editor.ActionSearchText = "not-present";
        Assert.True(editor.HasNoVisibleActions);
        editor.AddActionCommand.Execute(null);
        Assert.False(editor.HasActionSearch);
        Assert.Contains(editor.SelectedAction, editor.VisibleActionRows);
        Assert.Equal(2, editor.VisibleActionRows.Count);
    }

    [Fact]
    public void AddChild_IntoCollapsedFolder_RevealsTheNewAction()
    {
        var (config, editor) = CreateViewModel();
        config.CurrentConfig.Profiles[0].Actions =
        [new OrbitAction { Id = "folder", Title = "Tools", Type = "folder", Children = [CreateAction("child", "Child")] }];
        editor.ReloadForSelectedProfile();
        editor.ToggleActionFolderCommand.Execute(editor.SelectedAction);
        Assert.Single(editor.VisibleActionRows);
        editor.AddChildActionCommand.Execute(null);
        Assert.Equal(3, editor.VisibleActionRows.Count);
        Assert.Contains(editor.SelectedAction, editor.VisibleActionRows);
        Assert.True(editor.SelectedAction!.IsChild);
    }

    private (ConfigService ConfigService, ActionEditorViewModel ViewModel) CreateViewModel(Action<string, Action>? registerUndo = null, Action? markDirty = null)
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
            markDirty ?? (() => { }),
            _ => { },
            registerUndo ?? ((_, _) => { }),
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
