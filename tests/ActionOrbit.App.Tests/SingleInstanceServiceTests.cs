using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class SingleInstanceServiceTests
{
    [Fact]
    public void SignalPrimaryInstance_ReturnsTrueWhenPrimaryAcknowledgesActivation()
    {
        var suffix = Guid.NewGuid().ToString("N");
        using var activated = new ManualResetEventSlim();
        using var primary = new SingleInstanceService(suffix);
        using var secondary = new SingleInstanceService(suffix);
        primary.StartListening(activated.Set);

        var acknowledged = secondary.SignalPrimaryInstance(TimeSpan.FromSeconds(2));

        Assert.True(primary.IsPrimaryInstance);
        Assert.False(secondary.IsPrimaryInstance);
        Assert.True(acknowledged);
        Assert.True(activated.IsSet);
    }

    [Fact]
    public void SignalPrimaryInstance_ReturnsFalseWhenPrimaryDoesNotListen()
    {
        var suffix = Guid.NewGuid().ToString("N");
        using var primary = new SingleInstanceService(suffix);
        using var secondary = new SingleInstanceService(suffix);

        var acknowledged = secondary.SignalPrimaryInstance(TimeSpan.FromMilliseconds(100));

        Assert.False(acknowledged);
    }
}
