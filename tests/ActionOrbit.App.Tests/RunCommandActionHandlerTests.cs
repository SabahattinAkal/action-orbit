using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;

namespace ActionOrbit.App.Tests;

public sealed class RunCommandActionHandlerTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"action-orbit-command-handler-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_RunsCommandAndArgumentsWithoutConfirmation()
    {
        var handler = CreateHandler();

        var action = CreateAction("exit");
        action.Arguments = "/b 0";
        var result = await handler.ExecuteAsync(action);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandReturnsFailure_ReportsExitCode()
    {
        var handler = CreateHandler();

        var action = CreateAction("exit");
        action.Arguments = "/b 7";
        var result = await handler.ExecuteAsync(action);

        Assert.False(result.Succeeded);
        Assert.Contains("7", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandIsBlocked_DoesNotStartIt()
    {
        var handler = CreateHandler();

        var result = await handler.ExecuteAsync(CreateAction("shutdown.exe /s"));

        Assert.False(result.Succeeded);
        Assert.Contains("güvenlik filtresine", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandActionsAreDisabled_ReturnsFailure()
    {
        var handler = CreateHandler(enabled: false);

        var result = await handler.ExecuteAsync(CreateAction("ver > nul"));

        Assert.False(result.Succeeded);
        Assert.Contains("kapalı", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutExplicitEnablement_ReturnsFailure()
    {
        var handler = new RunCommandActionHandler(new LogService(_tempDirectory));

        var result = await handler.ExecuteAsync(CreateAction("ver > nul"));

        Assert.False(result.Succeeded);
        Assert.Contains("kapalı", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotWriteCommandContentsToLog()
    {
        const string secretMarker = "action-orbit-secret-marker";
        var handler = CreateHandler();

        var result = await handler.ExecuteAsync(CreateAction($"echo {secretMarker} > nul"));
        var log = File.ReadAllText(Path.Combine(_tempDirectory, "logs", "actionorbit.log"));

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(secretMarker, log, StringComparison.Ordinal);
    }

    private RunCommandActionHandler CreateHandler(bool enabled = true)
    {
        Directory.CreateDirectory(_tempDirectory);
        return new RunCommandActionHandler(
            new LogService(_tempDirectory),
            () => enabled);
    }

    private static OrbitAction CreateAction(string command) => new()
    {
        Id = "command",
        Title = "Komut",
        Type = "run_command",
        Target = command
    };

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
