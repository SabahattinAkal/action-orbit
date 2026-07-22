using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class ProfileServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"action-orbit-tests-{Guid.NewGuid():N}");

    [Fact]
    public void ResolveProfile_MatchesProcessNameWithOrWithoutExeSuffix()
    {
        var service = new ProfileService(new LogService(_tempDirectory));
        var config = CreateConfig();

        var profile = service.ResolveProfile(config, "CHROME");

        Assert.Equal("browser", profile.Id);
    }

    [Fact]
    public void ResolveProfile_ReturnsConfiguredDefaultWhenNoMatchExists()
    {
        var service = new ProfileService(new LogService(_tempDirectory));
        var config = CreateConfig();

        var profile = service.ResolveProfile(config, "unknown.exe");

        Assert.Equal("default", profile.Id);
    }

    [Fact]
    public void ResolveProfile_RepeatedMissWritesSingleDiagnostic()
    {
        var logService = new LogService(_tempDirectory);
        var service = new ProfileService(logService);
        var config = CreateConfig();

        service.ResolveProfile(config, "unknown.exe");
        service.ResolveProfile(config, "UNKNOWN");
        service.ResolveProfile(config, "unknown.exe");

        var missLines = File.ReadAllLines(logService.LogPath)
            .Where(line => line.Contains("No profile match for unknown.exe", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Single(missLines);
    }

    [Fact]
    public void ResolveProfile_MatchBetweenMissesAllowsNewDiagnostic()
    {
        var logService = new LogService(_tempDirectory);
        var service = new ProfileService(logService);
        var config = CreateConfig();

        service.ResolveProfile(config, "unknown.exe");
        service.ResolveProfile(config, "chrome.exe");
        service.ResolveProfile(config, "unknown.exe");

        var missLines = File.ReadAllLines(logService.LogPath)
            .Where(line => line.Contains("No profile match for unknown.exe", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Equal(2, missLines.Count);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static AppConfig CreateConfig() =>
        new()
        {
            DefaultProfileId = "default",
            Profiles =
            [
                new ProfileConfig { Id = "default", Name = "Default" },
                new ProfileConfig
                {
                    Id = "browser",
                    Name = "Browser",
                    Matches = [new ProfileMatch { ProcessName = "chrome.exe" }]
                }
            ]
        };
}
