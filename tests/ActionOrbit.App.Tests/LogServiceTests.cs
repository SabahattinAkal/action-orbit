using System.Text;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class LogServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"action-orbit-log-tests-{Guid.NewGuid():N}");

    [Fact]
    public void OversizedLog_RetainsBoundedTailAndContinuesInFreshFile()
    {
        var logService = new LogService(_tempDirectory);
        Directory.CreateDirectory(logService.LogDirectory);
        var oversizedLength = LogService.MaxLogFileBytes + 1024;
        var archiveStart = oversizedLength - LogService.RetainedArchiveBytes;
        var retainedMarker = Encoding.UTF8.GetBytes("\nretained-tail-marker\n");

        using (var stream = new FileStream(logService.LogPath, FileMode.Create, FileAccess.Write))
        {
            stream.SetLength(oversizedLength);
            stream.Seek(archiveStart + 16, SeekOrigin.Begin);
            stream.Write(retainedMarker);
        }

        logService.Info("fresh-entry");

        Assert.True(File.Exists(logService.PreviousLogPath));
        Assert.InRange(new FileInfo(logService.PreviousLogPath).Length, 1, LogService.RetainedArchiveBytes);
        Assert.Contains("retained-tail-marker", File.ReadAllText(logService.PreviousLogPath));
        Assert.Contains("fresh-entry", File.ReadAllText(logService.LogPath));
        Assert.True(new FileInfo(logService.LogPath).Length < LogService.MaxLogFileBytes);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
