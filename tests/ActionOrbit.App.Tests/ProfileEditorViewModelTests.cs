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

    [Fact]
    public void EditingDefaultProfileId_UpdatesDefaultReference()
    {
        var viewModel = CreateViewModel();
        viewModel.ReloadFromConfig();
        var defaultProfile = viewModel.Profiles.Single(profile => profile.Id == "default");
        viewModel.SelectedProfile = defaultProfile;

        viewModel.SelectedProfileId = "renamed_default";

        Assert.Equal("renamed_default", defaultProfile.Id);
        Assert.Equal("renamed_default", viewModel.DefaultProfileId);
        Assert.True(viewModel.SelectedProfileIsDefault);
    }

    [Fact]
    public void SetDefaultProfile_UpdatesDefaultProfileIdForListBadges()
    {
        var viewModel = CreateViewModel();
        viewModel.ReloadFromConfig();
        var target = viewModel.Profiles.First(profile => profile.Id != viewModel.DefaultProfileId);
        viewModel.SelectedProfile = target;

        viewModel.SetDefaultProfileCommand.Execute(null);

        Assert.Equal(target.Id, viewModel.DefaultProfileId);
        Assert.True(viewModel.SelectedProfileIsDefault);
    }

    [Fact]
    public void EditingProfileId_ToDuplicateValue_IsRejected()
    {
        string? status = null;
        var viewModel = CreateViewModel(setStatus: message => status = message);
        viewModel.ReloadFromConfig();
        var selected = viewModel.Profiles[0];
        var duplicateId = viewModel.Profiles[1].Id;
        viewModel.SelectedProfile = selected;

        viewModel.SelectedProfileId = duplicateId;

        Assert.Equal("default", selected.Id);
        Assert.Equal("default", viewModel.SelectedProfileId);
        Assert.Contains("zaten", status);
    }

    private ProfileEditorViewModel CreateViewModel(
        Action? markDirty = null,
        Action? selectedProfileChanged = null,
        Action<string>? setStatus = null)
    {
        Directory.CreateDirectory(_tempDirectory);
        var logService = new LogService(_tempDirectory);
        var configService = new ConfigService(logService, _tempDirectory);
        return new ProfileEditorViewModel(
            configService,
            new ActiveWindowService(logService),
            new ProfileService(logService),
            markDirty ?? (() => { }),
            setStatus ?? (_ => { }),
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
