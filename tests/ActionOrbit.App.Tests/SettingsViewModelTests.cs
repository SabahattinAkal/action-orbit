using ActionOrbit.App.Services;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Tests;

public sealed class SettingsViewModelTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"action-orbit-settings-tests-{Guid.NewGuid():N}");

    [Fact]
    public void RefreshAndEdit_KeepConfigAndClampedValuesInSync()
    {
        var dirtyCount = 0;
        var viewModel = CreateViewModel(() => dirtyCount++, _ => { });

        viewModel.RefreshFromConfig();
        viewModel.CloseToTray = !viewModel.CloseToTray;
        viewModel.OverlayButtonSize = 500;
        viewModel.OverlayRadiusX = 1;
        viewModel.OverlayRadiusY = 500;
        viewModel.ShelfEnabled = false;
        viewModel.RememberRecentShelves = true;
        viewModel.ShelfMaxItems = 500;
        viewModel.ShelfRetentionHours = 0;

        Assert.Equal(viewModel.CloseToTray, GetConfigService().CurrentConfig.Settings.CloseToTray);
        Assert.Equal(96, viewModel.OverlayButtonSize);
        Assert.Equal(96, viewModel.OverlayRadiusX);
        Assert.Equal(168, viewModel.OverlayRadiusY);
        Assert.False(GetConfigService().CurrentConfig.Settings.Shelf.Enabled);
        Assert.True(GetConfigService().CurrentConfig.Settings.Shelf.RememberRecentShelves);
        Assert.Equal(100, viewModel.ShelfMaxItems);
        Assert.Equal(1, viewModel.ShelfRetentionHours);
        Assert.True(dirtyCount >= 8);
    }

    [Fact]
    public void ApplyTheme_RejectsInvalidAccentWithoutSaving()
    {
        string? status = null;
        var viewModel = CreateViewModel(() => { }, message => status = message);
        viewModel.RefreshFromConfig();
        viewModel.AccentInput = "invalid";

        viewModel.ApplyThemeSettingsCommand.Execute(null);

        Assert.Contains("#RRGGBB", status);
    }

    private ConfigService? _configService;

    private ConfigService GetConfigService() =>
        _configService ?? throw new InvalidOperationException("Config service was not initialized.");

    private SettingsViewModel CreateViewModel(Action markDirty, Action<string> setStatus)
    {
        Directory.CreateDirectory(_tempDirectory);
        var logService = new LogService(_tempDirectory);
        _configService = new ConfigService(logService, _tempDirectory);
        return new SettingsViewModel(
            _configService,
            new StartupService(logService),
            logService,
            markDirty,
            setStatus,
            (_, _) => { });
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
