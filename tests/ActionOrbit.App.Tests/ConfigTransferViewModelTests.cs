using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Tests;

public sealed class ConfigTransferViewModelTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"action-orbit-transfer-tests-{Guid.NewGuid():N}");

    [Fact]
    public void ApplyImportedConfig_SynchronizesHotkeyStartupAndThemeState()
    {
        var fixture = CreateFixture();
        var imported = DefaultConfigFactory.Create();
        imported.Hotkey = new HotkeyConfig { Display = "F14", Key = "F14", Modifiers = [] };
        imported.Settings.RunAtStartup = true;
        imported.Theme.Mode = "light";

        var applied = fixture.Transfer.ApplyImportedConfig(imported, "import.json");

        Assert.True(applied);
        Assert.Equal("F14", fixture.Config.CurrentConfig.Hotkey.Display);
        Assert.Equal("F14", fixture.HotkeyRegistrar.LastRegistered?.Display);
        Assert.True(fixture.Startup.IsEnabled());
        Assert.Equal("light", fixture.Settings.ThemeMode);
        Assert.Contains("içe aktarıldı", fixture.GetStatus());
    }

    [Fact]
    public void ApplyImportedConfig_WhenStartupUpdateFails_RestoresPreviousHotkey()
    {
        var fixture = CreateFixture();
        fixture.Hotkey.RegisterConfiguredHotkey();
        var originalHotkey = fixture.Config.CurrentConfig.Hotkey.Display;
        fixture.Startup.SetException = new InvalidOperationException("registry denied");
        var imported = DefaultConfigFactory.Create();
        imported.Hotkey = new HotkeyConfig { Display = "F14", Key = "F14", Modifiers = [] };
        imported.Settings.RunAtStartup = true;

        var applied = fixture.Transfer.ApplyImportedConfig(imported, "import.json");

        Assert.False(applied);
        Assert.Equal(originalHotkey, fixture.Config.CurrentConfig.Hotkey.Display);
        Assert.Equal(originalHotkey, fixture.HotkeyRegistrar.LastRegistered?.Display);
        Assert.Contains("uygulanmadı", fixture.GetStatus());
    }

    [Fact]
    public void ApplyImportedConfig_WhenSaveFails_RestoresStartupAndHotkey()
    {
        var fixture = CreateFixture();
        fixture.Hotkey.RegisterConfiguredHotkey();
        fixture.Startup.SetEnabled(false);
        var originalHotkey = fixture.Config.CurrentConfig.Hotkey.Display;
        var imported = DefaultConfigFactory.Create();
        imported.Hotkey = new HotkeyConfig { Display = "F14", Key = "F14", Modifiers = [] };
        imported.Settings.RunAtStartup = true;
        imported.Profiles[0].Actions[0].Type = "unknown";

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Transfer.ApplyImportedConfig(imported, "import.json"));

        Assert.False(fixture.Startup.IsEnabled());
        Assert.Equal(originalHotkey, fixture.HotkeyRegistrar.LastRegistered?.Display);
        Assert.Equal(originalHotkey, fixture.Config.CurrentConfig.Hotkey.Display);
    }

    private Fixture CreateFixture()
    {
        Directory.CreateDirectory(_tempDirectory);
        var log = new LogService(_tempDirectory);
        var config = new ConfigService(log, _tempDirectory);
        config.Load();
        var status = "";
        var hotkeyRegistrar = new FakeHotkeyRegistrar();
        var startup = new FakeStartupRegistration();
        var settings = new SettingsViewModel(
            config,
            startup,
            log,
            () => { },
            message => status = message,
            (_, _) => { });
        var hotkey = new HotkeySettingsViewModel(
            config,
            hotkeyRegistrar,
            log,
            message => status = message,
            (_, _) => { });
        hotkey.RefreshFromConfig();
        settings.RefreshFromConfig();

        ConfigTransferViewModel? transfer = null;
        transfer = new ConfigTransferViewModel(
            config,
            log,
            hotkey,
            settings,
            () => config.CurrentConfig.Profiles[0],
            () => settings.CompleteExternalConfigChange(),
            _ => { },
            () => { },
            (_, _) => { },
            message => status = message);

        return new Fixture(
            config,
            settings,
            hotkey,
            transfer,
            hotkeyRegistrar,
            startup,
            () => status);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private sealed record Fixture(
        ConfigService Config,
        SettingsViewModel Settings,
        HotkeySettingsViewModel Hotkey,
        ConfigTransferViewModel Transfer,
        FakeHotkeyRegistrar HotkeyRegistrar,
        FakeStartupRegistration Startup,
        Func<string> GetStatus);

    private sealed class FakeHotkeyRegistrar : IHotkeyRegistrar
    {
        public bool IsRegistered { get; private set; }
        public HotkeyConfig? LastRegistered { get; private set; }

        public void Register(HotkeyConfig hotkey)
        {
            LastRegistered = new HotkeyConfig
            {
                Display = hotkey.Display,
                Key = hotkey.Key,
                Modifiers = [.. hotkey.Modifiers]
            };
            IsRegistered = true;
        }
    }

    private sealed class FakeStartupRegistration : IStartupRegistration
    {
        private bool _enabled;

        public Exception? SetException { get; set; }

        public bool IsEnabled() => _enabled;

        public void SetEnabled(bool enabled)
        {
            if (SetException is not null)
            {
                throw SetException;
            }

            _enabled = enabled;
        }
    }
}
