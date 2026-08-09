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
        Assert.True(viewModel.HasAccentIssue);
        Assert.False(viewModel.CanApplyVisualSettings);
        Assert.NotEmpty(viewModel.AccentIssueMessage);
    }

    [Fact]
    public void ActivationMode_UpdatesDependentSettingAvailability()
    {
        var viewModel = CreateViewModel(() => { }, _ => { });
        viewModel.RefreshFromConfig();
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        GetConfigService().CurrentConfig.Settings.Activation.Mode = "hold";
        viewModel.RefreshFromConfig();

        Assert.True(viewModel.IsHoldMode);
        Assert.False(viewModel.IsDoublePressMode);
        Assert.Contains(nameof(SettingsViewModel.IsHoldMode), changedProperties);
        Assert.Contains(nameof(SettingsViewModel.IsDoublePressMode), changedProperties);

        viewModel.ActivationMode = "double_press";

        Assert.False(viewModel.IsHoldMode);
        Assert.True(viewModel.IsDoublePressMode);
    }

    [Fact]
    public void AccentPreset_AppliesAndPersistsSelectedColor()
    {
        string? status = null;
        var viewModel = CreateViewModel(() => { }, message => status = message);
        viewModel.RefreshFromConfig();

        viewModel.UseAccentPresetCommand.Execute("#2563EB");

        Assert.Equal("#2563EB", viewModel.AccentInput);
        Assert.Equal("#2563EB", GetConfigService().CurrentConfig.Theme.Accent);
        Assert.Contains("kaydedildi", status, StringComparison.OrdinalIgnoreCase);
        Assert.False(viewModel.HasAccentIssue);
        Assert.True(viewModel.CanApplyVisualSettings);
    }

    [Fact]
    public void ResetVisualSettings_RestoresDefaultThemeAndOverlayValues()
    {
        var viewModel = CreateViewModel(() => { }, _ => { });
        viewModel.RefreshFromConfig();
        viewModel.ThemeMode = "light";
        viewModel.AccentInput = "#2563EB";
        viewModel.OverlayButtonSize = 95;

        viewModel.ResetVisualSettingsCommand.Execute(null);

        var defaults = DefaultConfigFactory.Create().Theme;
        Assert.Equal(defaults.Mode, viewModel.ThemeMode);
        Assert.Equal(defaults.Accent, viewModel.AccentInput);
        Assert.Equal(defaults.ButtonSize, viewModel.OverlayButtonSize);
        Assert.Equal(defaults.RadiusX, viewModel.OverlayRadiusX);
        Assert.Equal(defaults.RadiusY, viewModel.OverlayRadiusY);
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
