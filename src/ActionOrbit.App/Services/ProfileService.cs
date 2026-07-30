using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public sealed class ProfileService
{
    private readonly LogService _logService;
    private readonly object _resolutionLogGate = new();
    private string _lastMissKey = "";
    private bool _lastResolutionWasMatch = true;

    public ProfileService(LogService logService)
    {
        _logService = logService;
    }

    public ProfileConfig ResolveProfile(AppConfig config, string? processName)
    {
        var normalizedProcess = NormalizeProcessName(processName);

        if (!string.IsNullOrWhiteSpace(normalizedProcess))
        {
            var matched = config.Profiles.FirstOrDefault(profile =>
                profile.Matches.Any(match =>
                    ProcessMatches(normalizedProcess, match.ProcessName)));

            if (matched is not null)
            {
                RecordMatchedResolution();
                return matched;
            }
        }

        var defaultProfile = GetDefaultProfile(config);

        if (!string.IsNullOrWhiteSpace(normalizedProcess))
        {
            LogProfileMissOnce(normalizedProcess, defaultProfile);
        }

        return defaultProfile;
    }

    public ProfileConfig GetDefaultProfile(AppConfig config) =>
        config.Profiles.FirstOrDefault(profile =>
            string.Equals(profile.Id, config.DefaultProfileId, StringComparison.OrdinalIgnoreCase))
        ?? config.Profiles.First();

    private static bool ProcessMatches(string activeProcess, string configuredProcess)
    {
        var configured = NormalizeProcessName(configuredProcess);
        return string.Equals(activeProcess, configured, StringComparison.OrdinalIgnoreCase)
            || string.Equals(RemoveExe(activeProcess), RemoveExe(configured), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeProcessName(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return "";
        }

        var fileName = Path.GetFileName(processName.Trim());
        return fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.exe";
    }

    private static string RemoveExe(string processName) =>
        processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName[..^4]
            : processName;

    private void RecordMatchedResolution()
    {
        lock (_resolutionLogGate)
        {
            _lastResolutionWasMatch = true;
        }
    }

    private void LogProfileMissOnce(string normalizedProcess, ProfileConfig defaultProfile)
    {
        var missKey = $"{normalizedProcess}\u001f{defaultProfile.Id}\u001f{defaultProfile.Name}";
        lock (_resolutionLogGate)
        {
            if (!_lastResolutionWasMatch
                && string.Equals(_lastMissKey, missKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _lastResolutionWasMatch = false;
            _lastMissKey = missKey;
        }

        _logService.Info(
            $"No profile match for {LogService.SafeValue(normalizedProcess)}. " +
            $"Using {LogService.SafeValue(defaultProfile.Name)}.");
    }
}
