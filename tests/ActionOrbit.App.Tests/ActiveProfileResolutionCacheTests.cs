using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class ActiveProfileResolutionCacheTests
{
    [Fact]
    public void RecordedProcess_DoesNotRequireRepeatedResolution()
    {
        var cache = new ActiveProfileResolutionCache();

        Assert.True(cache.RequiresResolution("chrome.exe"));
        cache.RecordResolution("chrome.exe");

        Assert.False(cache.RequiresResolution("CHROME.EXE"));
    }

    [Fact]
    public void ProcessChange_RequiresResolution()
    {
        var cache = new ActiveProfileResolutionCache();
        cache.RecordResolution("chrome.exe");

        Assert.True(cache.RequiresResolution("Code.exe"));
    }

    [Fact]
    public void Invalidation_RequiresResolutionForSameProcess()
    {
        var cache = new ActiveProfileResolutionCache();
        cache.RecordResolution("chrome.exe");

        cache.Invalidate();

        Assert.True(cache.RequiresResolution("chrome.exe"));
    }
}
