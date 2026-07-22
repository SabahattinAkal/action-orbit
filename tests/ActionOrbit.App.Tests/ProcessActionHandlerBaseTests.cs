using System.Diagnostics;
using ActionOrbit.App.Services.Actions;

namespace ActionOrbit.App.Tests;

public sealed class ProcessActionHandlerBaseTests
{
    [Fact]
    public void StartShell_WhenShellUsesExistingProcess_ReturnsSuccess()
    {
        var result = ProcessActionHandlerBase.StartShell(
            new ProcessStartInfo(),
            _ => null);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void StartShell_WhenShellThrows_ReturnsFailureWithReason()
    {
        var result = ProcessActionHandlerBase.StartShell(
            new ProcessStartInfo(),
            _ => throw new InvalidOperationException("shell error"));

        Assert.False(result.Succeeded);
        Assert.Contains("shell error", result.Message);
    }
}
