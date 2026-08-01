using System.Diagnostics;
using Microsoft.Win32;
using ActionOrbit.App.Services.Actions;

namespace ActionOrbit.App.Services;

public sealed class BrowserLaunchService
{
    private static readonly IReadOnlyDictionary<string, string> ExecutableNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome"] = "chrome.exe",
            ["edge"] = "msedge.exe",
            ["firefox"] = "firefox.exe",
            ["brave"] = "brave.exe"
        };

    public Task<ActionExecutionResult> OpenAsync(string browser, string url)
    {
        if (string.IsNullOrWhiteSpace(browser) ||
            string.Equals(browser, "system", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(ProcessActionHandlerBase.StartShell(
                new ProcessStartInfo(url) { UseShellExecute = true },
                Process.Start));
        }

        if (!ExecutableNames.TryGetValue(browser, out var executableName))
        {
            return Task.FromResult(ActionExecutionResult.Failure("Seçilen tarayıcı desteklenmiyor."));
        }

        var executablePath = ResolveExecutable(executableName);
        if (executablePath is null)
        {
            return Task.FromResult(ActionExecutionResult.Failure(
                $"{GetDisplayName(browser)} bilgisayarda bulunamadı. Aksiyonda Sistem varsayılanı seçilebilir."));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = true
        };
        startInfo.ArgumentList.Add(url);
        return Task.FromResult(ProcessActionHandlerBase.StartShell(startInfo, Process.Start));
    }

    internal static string? ResolveExecutable(string executableName)
    {
        var registryLocations = new[]
        {
            $@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{executableName}",
            $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{executableName}",
            $@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\{executableName}"
        };

        foreach (var location in registryLocations)
        {
            if (Registry.GetValue(location, "", null) is string path && File.Exists(path))
            {
                return path;
            }
        }

        return FindKnownInstall(executableName);
    }

    private static string? FindKnownInstall(string executableName)
    {
        var relativePaths = executableName.ToLowerInvariant() switch
        {
            "chrome.exe" => new[] { @"Google\Chrome\Application\chrome.exe" },
            "msedge.exe" => new[] { @"Microsoft\Edge\Application\msedge.exe" },
            "firefox.exe" => new[] { @"Mozilla Firefox\firefox.exe" },
            "brave.exe" => new[] { @"BraveSoftware\Brave-Browser\Application\brave.exe" },
            _ => []
        };
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };

        return roots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .SelectMany(root => relativePaths.Select(relative => Path.Combine(root, relative)))
            .FirstOrDefault(File.Exists);
    }

    private static string GetDisplayName(string browser) => browser.ToLowerInvariant() switch
    {
        "chrome" => "Google Chrome",
        "edge" => "Microsoft Edge",
        "firefox" => "Mozilla Firefox",
        "brave" => "Brave",
        _ => browser
    };
}
