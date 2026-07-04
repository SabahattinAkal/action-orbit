using System.Globalization;

namespace ActionOrbit.App.Services;

public sealed class LogService
{
    private readonly object _gate = new();

    public LogService()
    {
        Directory.CreateDirectory(LogDirectory);
    }

    public string AppDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ActionOrbit");

    public string LogDirectory =>
        Path.Combine(AppDirectory, "logs");

    public string LogPath =>
        Path.Combine(LogDirectory, "actionorbit.log");

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
                File.AppendAllText(LogPath, line);
            }
        }
        catch
        {
            // Logging should never crash the app.
        }
    }
}
