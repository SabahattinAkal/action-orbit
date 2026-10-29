using Microsoft.Win32;

namespace ActionOrbit.App.Services;

public sealed class StartupService : IStartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ActionOrbitPro";
    private readonly LogService _logService;
    private readonly string _installerPreferenceKeyPath;

    public StartupService(LogService logService, string installerPreferenceKeyPath = @"Software\ActionOrbitPro\Setup")
    {
        _logService = logService;
        _installerPreferenceKeyPath = installerPreferenceKeyPath;
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

    public void ApplyInstallerPreference(ConfigService configService)
    {
        using var key = Registry.CurrentUser.OpenSubKey(_installerPreferenceKeyPath, writable: true);
        if (key?.GetValue("RunAtStartup") is not int enabled)
        {
            return;
        }

        configService.CurrentConfig.Settings.RunAtStartup = enabled == 1;
        configService.Save(configService.CurrentConfig);
        key.DeleteValue("RunAtStartup", throwOnMissingValue: false);
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

        return $"\"{path}\" --background";
    }

    private static string Normalize(string value) =>
        value.Trim().Trim('"');
}
