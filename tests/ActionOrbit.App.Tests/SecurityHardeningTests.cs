using System.Text.Json;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;

namespace ActionOrbit.App.Tests;

public sealed class SecurityHardeningTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"action-orbit-security-tests-{Guid.NewGuid():N}");

    [Theory]
    [InlineData("cmd.exe", true)]
    [InlineData(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", true)]
    [InlineData("notepad.exe", false)]
    public void ShellInterpreterDetection_UsesExecutableLeafName(string target, bool expected)
    {
        Assert.Equal(expected, ActionSecurityService.IsShellInterpreter(target));
    }

    [Theory]
    [InlineData("setup.exe", true)]
    [InlineData("shortcut.lnk", true)]
    [InlineData("script.ps1", true)]
    [InlineData("document.pdf", false)]
    public void ExecutableFileDetection_BlocksScriptAndLauncherExtensions(string target, bool expected)
    {
        Assert.Equal(expected, ActionSecurityService.IsExecutableFileTarget(target));
    }

    [Fact]
    public void ImportWarning_ListsExecutableAndInputActions()
    {
        var profile = new ProfileConfig
        {
            Id = "import",
            Name = "Downloaded profile",
            Actions =
            [
                Action("command", "run_command", "echo hello"),
                Action("typing", "type_text", "hello"),
                Action("website", "open_url", "https://example.com")
            ]
        };

        var warning = ActionSecurityService.BuildImportWarning(
            [profile],
            "download.profile.json",
            replacesConfiguration: false);

        Assert.Contains("2 aksiyon", warning, StringComparison.Ordinal);
        Assert.Contains("run_command", warning, StringComparison.Ordinal);
        Assert.DoesNotContain("[open_url]", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void IconCatalog_RejectsAbsoluteAndUncReferences()
    {
        Directory.CreateDirectory(_tempDirectory);
        IconCatalog.ConfigureCustomIconDirectory(_tempDirectory);

        Assert.False(IconCatalog.IsSafeIconReference(@"C:\temp\icon.png"));
        Assert.False(IconCatalog.IsSafeIconReference(@"\\example.invalid\share\icon.png"));
        Assert.Null(IconCatalog.GetImagePath(@"\\example.invalid\share\icon.png"));
    }

    [Fact]
    public void ReadConfigForImport_DisablesCommandActionsAndRejectsOversizedActionSets()
    {
        Directory.CreateDirectory(_tempDirectory);
        var log = new LogService(_tempDirectory);
        var service = new ConfigService(log, _tempDirectory);
        var config = DefaultConfigFactory.Create();
        config.Settings.AllowCommandActions = true;
        var safePath = Path.Combine(_tempDirectory, "safe.json");
        File.WriteAllText(safePath, JsonSerializer.Serialize(config));

        var imported = service.ReadConfigForImport(safePath);

        Assert.False(imported.Settings.AllowCommandActions);

        config.Profiles[0].Actions = Enumerable.Range(0, ConfigService.MaxActionsPerProfile + 1)
            .Select(index => Action($"action-{index}", "open_url", "https://example.com"))
            .ToList();
        var oversizedPath = Path.Combine(_tempDirectory, "too-many-actions.json");
        File.WriteAllText(oversizedPath, JsonSerializer.Serialize(config));

        Assert.Throws<InvalidOperationException>(() =>
            service.ReadConfigForImport(oversizedPath));
    }

    [Fact]
    public void ReadConfigForImport_RejectsOversizedFiles()
    {
        Directory.CreateDirectory(_tempDirectory);
        var path = Path.Combine(_tempDirectory, "oversized.json");
        File.WriteAllBytes(path, new byte[ConfigService.MaxConfigFileBytes + 1]);
        var service = new ConfigService(new LogService(_tempDirectory), _tempDirectory);

        Assert.Throws<InvalidOperationException>(() => service.ReadConfigForImport(path));
    }

    [Fact]
    public void ReadConfigForImport_EnforcesNestedActionDepthBoundary()
    {
        Directory.CreateDirectory(_tempDirectory);
        var service = new ConfigService(new LogService(_tempDirectory), _tempDirectory);
        var config = DefaultConfigFactory.Create();
        var allowedPath = Path.Combine(_tempDirectory, "allowed-depth.json");
        config.Profiles[0].Actions = [NestedAction(ConfigService.MaxActionDepth)];
        File.WriteAllText(allowedPath, JsonSerializer.Serialize(config));

        var imported = service.ReadConfigForImport(allowedPath);

        Assert.Single(imported.Profiles[0].Actions);

        var rejectedPath = Path.Combine(_tempDirectory, "rejected-depth.json");
        config.Profiles[0].Actions = [NestedAction(ConfigService.MaxActionDepth + 1)];
        File.WriteAllText(rejectedPath, JsonSerializer.Serialize(config));

        Assert.Throws<InvalidOperationException>(() =>
            service.ReadConfigForImport(rejectedPath));
    }

    [Fact]
    public async Task OpenApp_WithArguments_RequiresExplicitConfirmation()
    {
        Directory.CreateDirectory(_tempDirectory);
        var handler = new OpenAppActionHandler(
            new LogService(_tempDirectory),
            (_, _) => false);
        var action = Action("app", "open_app", "notepad.exe");
        action.Arguments = "document.txt";

        var result = await handler.ExecuteAsync(action);

        Assert.False(result.Succeeded);
        Assert.Contains("onaylanmad", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenApp_ShellInterpreterWithArguments_IsAlwaysRejected()
    {
        Directory.CreateDirectory(_tempDirectory);
        var confirmationRequested = false;
        var handler = new OpenAppActionHandler(
            new LogService(_tempDirectory),
            (_, _) =>
            {
                confirmationRequested = true;
                return true;
            });
        var action = Action("shell", "open_app", "cmd.exe");
        action.Arguments = "/c echo hello";

        var result = await handler.ExecuteAsync(action);

        Assert.False(result.Succeeded);
        Assert.False(confirmationRequested);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static OrbitAction Action(string id, string type, string target) =>
        new()
        {
            Id = id,
            Title = id,
            Type = type,
            Target = target
        };

    private static OrbitAction NestedAction(int maxDepth)
    {
        var root = Action("depth-0", "folder", "");
        var current = root;
        for (var depth = 1; depth <= maxDepth; depth++)
        {
            var child = Action(
                $"depth-{depth}",
                depth == maxDepth ? "open_url" : "folder",
                depth == maxDepth ? "https://example.com" : "");
            current.Children.Add(child);
            current = child;
        }

        return root;
    }
}
