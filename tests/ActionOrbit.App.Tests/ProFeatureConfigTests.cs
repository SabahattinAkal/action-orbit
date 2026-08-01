using System.Text.Json;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class ProFeatureConfigTests
{
    [Fact]
    public void Save_NormalizesProSettingsRingBrowserAndShortcut()
    {
        using var temp = new TemporaryDirectory();
        var service = new ConfigService(new LogService(temp.Path), temp.Path);
        var config = DefaultConfigFactory.Create();
        config.Settings.Activation = new ActivationSettings
        {
            Mode = "HOLD",
            HoldDelayMilliseconds = 20,
            DoublePressWindowMilliseconds = 5000,
            SuppressedProcesses = ["PowerPnt", "powerpnt.exe", "mstsc"]
        };
        config.Settings.Shelf = new ShelfSettings
        {
            MaxItemsPerShelf = 500,
            MaxItemBytes = 10,
            MaxTotalBytes = 20,
            RetentionHours = 0
        };
        config.Profiles[0].RingSets =
        [
            new RingSetConfig
            {
                Id = "design",
                Name = " Tasarım ",
                Actions =
                [
                    new OrbitAction
                    {
                        Id = "figma",
                        Title = "Figma",
                        Type = "open_url",
                        Target = "https://figma.com",
                        Browser = "CHROME",
                        Shortcut = "ctrl+alt+f"
                    }
                ]
            }
        ];

        service.Save(config);

        Assert.Equal("hold", config.Settings.Activation.Mode);
        Assert.Equal(100, config.Settings.Activation.HoldDelayMilliseconds);
        Assert.Equal(900, config.Settings.Activation.DoublePressWindowMilliseconds);
        Assert.Equal(["PowerPnt.exe", "mstsc.exe"], config.Settings.Activation.SuppressedProcesses);
        Assert.Equal(100, config.Settings.Shelf.MaxItemsPerShelf);
        Assert.Equal(1024 * 1024, config.Settings.Shelf.MaxItemBytes);
        var ring = Assert.Single(config.Profiles[0].RingSets);
        Assert.Equal("Tasarım", ring.Name);
        Assert.Equal("chrome", ring.Actions[0].Browser);
        Assert.Equal("Ctrl+Alt+F", ring.Actions[0].Shortcut);
    }

    [Fact]
    public void Load_UpgradesVersionSevenWithoutOverwritingThemeValues()
    {
        using var temp = new TemporaryDirectory();
        var service = new ConfigService(new LogService(temp.Path), temp.Path);
        var config = DefaultConfigFactory.Create();
        config.ConfigVersion = 7;
        config.Theme.ButtonSize = 96;
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(service.ConfigPath, json);

        var loaded = service.Load();

        Assert.Equal(DefaultConfigFactory.CurrentVersion, loaded.ConfigVersion);
        Assert.Equal(96, loaded.Theme.ButtonSize);
        Assert.NotNull(loaded.Settings.Activation);
        Assert.NotNull(loaded.Settings.Shelf);
    }

    [Fact]
    public void ImportedRiskSummary_IncludesAdditionalRingActions()
    {
        var profile = new ProfileConfig
        {
            Id = "default",
            Name = "Default",
            Actions = [],
            RingSets =
            [
                new RingSetConfig
                {
                    Id = "tools",
                    Name = "Tools",
                    Actions = [new OrbitAction { Id = "cmd", Title = "Cmd", Type = "run_command", Target = "whoami" }]
                }
            ]
        };

        var risks = ActionSecurityService.FindImportedActionRisks([profile]);

        var risk = Assert.Single(risks);
        Assert.Contains("Tools", risk.Profile);
    }

    [Fact]
    public void ProfileCopy_CopiesAdditionalRingsDeeply()
    {
        var source = new ProfileConfig
        {
            Id = "source",
            Name = "Source",
            MainRingName = "Daily",
            RingSets =
            [
                new RingSetConfig
                {
                    Id = "design",
                    Name = "Design",
                    Actions = [new OrbitAction { Id = "site", Title = "Site", Type = "open_url", Target = "https://example.com" }]
                }
            ]
        };

        var copy = ProfileCopyService.Copy(source, "copy", "Copy");
        copy.RingSets[0].Actions[0].Title = "Changed";

        Assert.Equal("Daily", copy.MainRingName);
        Assert.Equal("Site", source.RingSets[0].Actions[0].Title);
    }
}
