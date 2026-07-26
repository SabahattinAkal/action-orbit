using System.ComponentModel;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class InputSimulationServiceTests
{
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
