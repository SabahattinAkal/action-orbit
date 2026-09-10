using System.ComponentModel;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class InputSimulationServiceTests
{
    [Fact]
    public async Task PrepareForTextInput_RestoresTargetFocusBeforeTyping()
    {
        using var directory = new TemporaryDirectory();
        var target = new IntPtr(42);
        var foreground = new IntPtr(7);
        var focusAttempts = 0;
        var service = new InputSimulationService(
            new LogService(directory.Path),
            () => target,
            () => foreground,
            window =>
            {
                focusAttempts++;
                foreground = window;
                return true;
            },
            _ => 0,
            _ => Task.CompletedTask);

        await service.PrepareForTextInputAsync();

        Assert.Equal(1, focusAttempts);
        Assert.Equal(target, foreground);
    }

    [Fact]
    public async Task PrepareForTextInput_WaitsUntilShortcutModifiersAreReleased()
    {
        using var directory = new TemporaryDirectory();
        var checks = 0;
        var delays = 0;
        var service = new InputSimulationService(
            new LogService(directory.Path),
            getInputTargetWindow: null,
            () => IntPtr.Zero,
            _ => true,
            _ => ++checks <= 2 ? unchecked((short)0x8000) : (short)0,
            _ =>
            {
                delays++;
                return Task.CompletedTask;
            });

        await service.PrepareForTextInputAsync();

        Assert.Equal(2, delays);
        Assert.True(checks > 2);
    }

    [Fact]
    public async Task PrepareForTextInput_ReportsWhenTargetCannotReceiveFocus()
    {
        using var directory = new TemporaryDirectory();
        var service = new InputSimulationService(
            new LogService(directory.Path),
            () => new IntPtr(42),
            () => new IntPtr(7),
            _ => false,
            _ => 0,
            _ => Task.CompletedTask);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            service.PrepareForTextInputAsync);

        Assert.Contains("odaklanılamadı", error.Message);
    }

    [Fact]
    public void EnsureAllInputsWereSent_WhenCountMatches_DoesNotThrow()
    {
        InputSimulationService.EnsureAllInputsWereSent(4, 4, 0);
    }

    [Fact]
    public void EnsureAllInputsWereSent_WhenCountIsPartial_ThrowsWithCountsAndError()
    {
        var error = Assert.Throws<Win32Exception>(() =>
            InputSimulationService.EnsureAllInputsWereSent(6, 2, 5));

        Assert.Equal(5, error.NativeErrorCode);
        Assert.Contains("2/6", error.Message);
    }
}
