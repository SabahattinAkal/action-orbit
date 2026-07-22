using System.Text.Json;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class ConfigServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"action-orbit-tests-{Guid.NewGuid():N}");

    [Fact]
    public void ReadConfigForImport_RepairsDuplicateProfileAndActionIds()
    {
        var service = CreateService();
        var path = WriteConfig(new AppConfig
        {
            ConfigVersion = DefaultConfigFactory.CurrentVersion,
            DefaultProfileId = "same",
            Hotkey = new HotkeyConfig { Display = "F13", Key = "F13", Modifiers = [] },
            Profiles =
            [
                new ProfileConfig
                {
                    Id = "same",
                    Name = "One",
                    Actions =
                    [
                        new OrbitAction { Id = "duplicate", Title = "One", Type = "open_url", Target = "https://example.com" },
                        new OrbitAction { Id = "duplicate", Title = "Two", Type = "open_url", Target = "https://example.org" }
                    ]
                },
                new ProfileConfig { Id = "same", Name = "Two" }
            ]
        });

        var config = service.ReadConfigForImport(path);

        Assert.Equal(2, config.Profiles.Select(profile => profile.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(2, config.Profiles[0].Actions.Select(action => action.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(config.Profiles, profile => profile.Id == config.DefaultProfileId);
    }

    [Fact]
    public void ReadConfigForImport_RejectsUnknownActionTypesWithoutChangingCurrentConfig()
    {
        var service = CreateService();
        var originalConfig = service.CurrentConfig;
        var path = WriteConfig(new AppConfig
        {
            ConfigVersion = DefaultConfigFactory.CurrentVersion,
            DefaultProfileId = "default",
            Hotkey = new HotkeyConfig { Display = "F13", Key = "F13", Modifiers = [] },
            Profiles =
            [
                new ProfileConfig
                {
                    Id = "default",
                    Name = "Default",
                    Actions = [new OrbitAction { Id = "bad", Title = "Bad", Type = "unknown" }]
                }
            ]
        });

        Assert.Throws<InvalidOperationException>(() => service.ReadConfigForImport(path));
        Assert.Same(originalConfig, service.CurrentConfig);
    }

    [Fact]
    public void Save_WritesValidConfigAndLastGoodSnapshot()
    {
        var service = CreateService();
        var config = DefaultConfigFactory.Create();
        config.Profiles[0].Name = "Saved Profile";

        service.Save(config);

        Assert.True(File.Exists(service.ConfigPath));
        var lastGoodPath = Path.Combine(service.AppDirectory, "config.lastgood.json");
        Assert.True(File.Exists(lastGoodPath));
        using var saved = JsonDocument.Parse(File.ReadAllText(service.ConfigPath));
        using var lastGood = JsonDocument.Parse(File.ReadAllText(lastGoodPath));
        Assert.Equal(DefaultConfigFactory.CurrentVersion, saved.RootElement.GetProperty("configVersion").GetInt32());
        Assert.Equal("Saved Profile", lastGood.RootElement.GetProperty("profiles")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void ImportProfile_RepairsDuplicateNestedActionIds()
    {
        var service = CreateService();
        var profilePath = Path.Combine(_tempDirectory, "profile.json");
        File.WriteAllText(profilePath, JsonSerializer.Serialize(new ProfileConfig
        {
            Id = "sample",
            Name = "Sample",
            Actions =
            [
                new OrbitAction
                {
                    Id = "folder",
                    Title = "Folder",
                    Type = "folder",
                    Children =
                    [
                        new OrbitAction { Id = "same", Title = "One", Type = "open_url", Target = "https://example.com" },
                        new OrbitAction { Id = "same", Title = "Two", Type = "open_url", Target = "https://example.org" }
                    ]
                }
            ]
        }));

        var imported = service.ImportProfile(profilePath);
        var ids = imported.Actions.SelectMany(Flatten).Select(action => action.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private ConfigService CreateService()
    {
        Directory.CreateDirectory(_tempDirectory);
        return new ConfigService(new LogService(_tempDirectory), _tempDirectory);
    }

    private string WriteConfig(AppConfig config)
    {
        Directory.CreateDirectory(_tempDirectory);
        var path = Path.Combine(_tempDirectory, $"config-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(config));
        return path;
    }

    private static IEnumerable<OrbitAction> Flatten(OrbitAction action)
    {
        yield return action;
        foreach (var child in action.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }
}
