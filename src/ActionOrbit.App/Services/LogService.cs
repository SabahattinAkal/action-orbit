using System.Globalization;

namespace ActionOrbit.App.Services;

public sealed class LogService
{
    internal const long MaxLogFileBytes = 5 * 1024 * 1024;
    internal const long RetainedArchiveBytes = 512 * 1024;
    private readonly object _gate = new();

    public LogService(string? appDirectory = null)
    {
        AppDirectory = string.IsNullOrWhiteSpace(appDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ActionOrbit")
            : appDirectory;
        Directory.CreateDirectory(LogDirectory);
    }

    public string AppDirectory { get; }

    public string LogDirectory =>
        Path.Combine(AppDirectory, "logs");

    public string LogPath =>
        Path.Combine(LogDirectory, "actionorbit.log");

    public string PreviousLogPath =>
        Path.Combine(LogDirectory, "actionorbit.previous.log");

    public void Info(string message) =>
        Write("INFO", message);

    public void Warn(string message) =>
        Write("WARN", message);

    public void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : $"{message}{Environment.NewLine}{exception}");

    private void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var line = string.Create(
                CultureInfo.InvariantCulture,
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}");

            lock (_gate)
            {
                RotateLogIfNeeded();
                File.AppendAllText(LogPath, line);
            }
        }
        catch
        {
            // Logging should never crash the app.
        }
    }

    private void RotateLogIfNeeded()
    {
        var logFile = new FileInfo(LogPath);
        if (!logFile.Exists || logFile.Length < MaxLogFileBytes)
        {
            return;
        }

        var temporaryArchivePath = $"{PreviousLogPath}.tmp";
        try
        {
            using (var source = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var archiveStart = Math.Max(0, source.Length - RetainedArchiveBytes);
                source.Seek(archiveStart, SeekOrigin.Begin);
                if (archiveStart > 0)
                {
                    SkipPartialLine(source);
                }

                using var archive = new FileStream(
                    temporaryArchivePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);
                source.CopyTo(archive);
            }

            File.Move(temporaryArchivePath, PreviousLogPath, overwrite: true);
            using var resetLog = new FileStream(LogPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        }
        finally
        {
            if (File.Exists(temporaryArchivePath))
            {
                File.Delete(temporaryArchivePath);
            }
        }
    }

    private static void SkipPartialLine(Stream stream)
    {
        int nextByte;
        do
        {
            nextByte = stream.ReadByte();
        }
        while (nextByte is not (-1 or '\n'));
    }
}
