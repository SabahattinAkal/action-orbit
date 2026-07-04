using Microsoft.Win32;

namespace ActionOrbit.App.Services;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ActionOrbit";
    private readonly LogService _logService;

    public StartupService(LogService logService)
    {
        _logService = logService;
    }

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is string value &&
                string.Equals(Normalize(value), Normalize(GetStartupCommand()), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logService.Error("Startup registry read failed.", ex);
            return false;
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Windows başlangıç kaydı açılamadı.");

        if (enabled)
        {
            key.SetValue(ValueName, GetStartupCommand(), RegistryValueKind.String);
            _logService.Info("Startup registration enabled.");
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
        _logService.Info("Startup registration disabled.");
    }

    private static string GetStartupCommand()
    {
        var path = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Uygulama yolu bulunamadı.");
        }

        return $"\"{path}\"";
    }

    private static string Normalize(string value) =>
        value.Trim().Trim('"');
}
