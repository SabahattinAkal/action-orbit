using System.ComponentModel;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Tests;

public sealed class HotkeySettingsViewModelTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"action-orbit-hotkey-tests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveHotkey_WithValidInput_RegistersAndPersistsNewHotkey()
    {
        var registrar = new FakeHotkeyRegistrar();
        var (configService, viewModel, getStatus) = CreateViewModel(registrar);
        viewModel.HotkeyInput = "Ctrl+Space";

        viewModel.SaveHotkeyCommand.Execute(null);

        Assert.Equal("Ctrl+Space", configService.CurrentConfig.Hotkey.Display);
        Assert.Equal("Space", configService.CurrentConfig.Hotkey.Key);
        Assert.Equal(["Control"], configService.CurrentConfig.Hotkey.Modifiers);
        Assert.Equal("Ctrl+Space", registrar.LastRegistered?.Display);
        Assert.True(viewModel.IsHotkeyRegistered);
        Assert.Contains("Kısayol güncellendi", getStatus());

        var reloaded = new ConfigService(new LogService(_tempDirectory), _tempDirectory);
        reloaded.Load();
        Assert.Equal("Ctrl+Space", reloaded.CurrentConfig.Hotkey.Display);
    }

    [Fact]
    public void SaveHotkey_WhenRegistrationConflicts_PreservesOldConfigAndActiveState()
    {
        var registrar = new FakeHotkeyRegistrar
        {
            IsRegistered = true,
            RegistrationException = new Win32Exception(1409, "already registered")
        };
        var (configService, viewModel, _) = CreateViewModel(registrar);
        var originalDisplay = configService.CurrentConfig.Hotkey.Display;
        viewModel.HotkeyInput = "F14";

        viewModel.SaveHotkeyCommand.Execute(null);

        Assert.Equal(originalDisplay, configService.CurrentConfig.Hotkey.Display);
        Assert.Equal(originalDisplay, viewModel.HotkeyDisplay);
        Assert.True(viewModel.IsHotkeyRegistered);
        Assert.True(viewModel.HasHotkeyIssue);
        Assert.Contains("başka bir uygulama tarafından kullanılıyor", viewModel.HotkeyIssueMessage);
        Assert.Contains("Eski kısayol korundu", viewModel.HotkeyIssueMessage);
    }

    [Fact]
    public void SaveHotkey_WithInvalidInput_DoesNotTouchRegistrarOrConfig()
    {
        var registrar = new FakeHotkeyRegistrar();
        var (configService, viewModel, _) = CreateViewModel(registrar);
        var originalDisplay = configService.CurrentConfig.Hotkey.Display;
        viewModel.HotkeyInput = "Ctrl+Alt";

        viewModel.SaveHotkeyCommand.Execute(null);

        Assert.Equal(originalDisplay, configService.CurrentConfig.Hotkey.Display);
        Assert.Equal(0, registrar.RegisterCallCount);
        Assert.True(viewModel.HasHotkeyIssue);
        Assert.Contains("ana tus eksik", viewModel.HotkeyIssueMessage);
    }

    private (ConfigService ConfigService, HotkeySettingsViewModel ViewModel, Func<string> GetStatus)
        CreateViewModel(FakeHotkeyRegistrar registrar)
    {
        Directory.CreateDirectory(_tempDirectory);
        var logService = new LogService(_tempDirectory);
        var configService = new ConfigService(logService, _tempDirectory);
        configService.Load();
        var status = "";
        var viewModel = new HotkeySettingsViewModel(
            configService,
            registrar,
            logService,
            message => status = message,
            (_, _) => { });
        viewModel.RefreshFromConfig();
        return (configService, viewModel, () => status);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private sealed class FakeHotkeyRegistrar : IHotkeyRegistrar
    {
        public bool IsRegistered { get; set; }
        public Exception? RegistrationException { get; set; }
        public HotkeyConfig? LastRegistered { get; private set; }
        public int RegisterCallCount { get; private set; }

        public void Register(HotkeyConfig hotkey)
        {
            RegisterCallCount++;
            if (RegistrationException is not null)
            {
                throw RegistrationException;
            }

            LastRegistered = new HotkeyConfig
            {
                Display = hotkey.Display,
                Key = hotkey.Key,
                Modifiers = [.. hotkey.Modifiers]
            };
            IsRegistered = true;
        }
    }
}
