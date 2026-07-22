using ActionOrbit.App.Services;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Tests;

public sealed class ProfileEditorViewModelTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"action-orbit-profile-editor-tests-{Guid.NewGuid():N}");

    [Fact]
    public void ReloadFromConfig_PopulatesProfilesAndSelectsOne()
    {
        var selectionChanges = 0;
        var viewModel = CreateViewModel(selectedProfileChanged: () => selectionChanges++);

        viewModel.ReloadFromConfig();

        Assert.NotEmpty(viewModel.Profiles);
        Assert.NotNull(viewModel.SelectedProfile);
        Assert.Equal(viewModel.Profiles.Count, viewModel.ProfileCount);
        Assert.True(selectionChanges > 0);
    }

    [Fact]
    public void EditingProfileFields_UpdatesSelectedProfileAndMatchChips()
    {
        var dirtyCount = 0;
        var viewModel = CreateViewModel(markDirty: () => dirtyCount++);
        viewModel.ReloadFromConfig();

        viewModel.SelectedProfileName = "Odak";
        viewModel.SelectedProfileMatchesText = "Code.exe, chrome.exe, code.exe";

        Assert.Equal("Odak", viewModel.SelectedProfile!.Name);
        Assert.Equal(2, viewModel.SelectedProfile.Matches.Count);
        Assert.Equal(["Code.exe", "chrome.exe"], viewModel.SelectedProfileMatchChips);
        Assert.True(dirtyCount >= 2);
    }

    [Fact]
    public void DuplicateProfile_CreatesIndependentCopy()
    {
        var viewModel = CreateViewModel();
        viewModel.ReloadFromConfig();
        var source = viewModel.SelectedProfile!;
        var originalCount = viewModel.ProfileCount;

        viewModel.DuplicateProfileCommand.Execute(null);

        Assert.Equal(originalCount + 1, viewModel.ProfileCount);
        Assert.NotSame(source, viewModel.SelectedProfile);
        Assert.NotSame(source.Actions, viewModel.SelectedProfile!.Actions);
    }

    private ProfileEditorViewModel CreateViewModel(
        Action? markDirty = null,
        Action? selectedProfileChanged = null)
    {
        Directory.CreateDirectory(_tempDirectory);
        var logService = new LogService(_tempDirectory);
        var configService = new ConfigService(logService, _tempDirectory);
        return new ProfileEditorViewModel(
            configService,
            new ActiveWindowService(logService),
            new ProfileService(logService),
            markDirty ?? (() => { }),
            _ => { },
            (_, _) => { },
            selectedProfileChanged ?? (() => { }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
