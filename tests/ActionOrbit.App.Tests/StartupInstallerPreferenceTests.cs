using ActionOrbit.App.Services;
using Microsoft.Win32;

namespace ActionOrbit.App.Tests;

public sealed class StartupInstallerPreferenceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InstallerChoice_PersistsOnceAndDoesNotOverrideLaterUserChanges(bool enabled)
    {
        var id = Guid.NewGuid().ToString("N");
        var directory = Path.Combine(Path.GetTempPath(), "action-orbit-startup-test-" + id);
        var keyPath = @"Software\ActionOrbitTests\" + id;
        try
        {
            var log = new LogService(directory);
            var config = new ConfigService(log, directory);
            config.Load();
            using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
                key.SetValue("RunAtStartup", enabled ? 1 : 0, RegistryValueKind.DWord);

            var startup = new StartupService(log, keyPath);
            startup.ApplyInstallerPreference(config);
            Assert.Equal(enabled, config.Reload().Settings.RunAtStartup);
            using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
                Assert.Null(key!.GetValue("RunAtStartup"));

            config.CurrentConfig.Settings.RunAtStartup = !enabled;
            config.Save(config.CurrentConfig);
            startup.ApplyInstallerPreference(config);
            Assert.Equal(!enabled, config.Reload().Settings.RunAtStartup);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
