namespace ActionOrbit.App.Services;

public static class CommandSafetyService
{
    public static bool IsBlocked(string? command)
    {
        var normalized = (command ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        return normalized.StartsWith("format ", StringComparison.Ordinal)
            || normalized.StartsWith("shutdown", StringComparison.Ordinal)
            || normalized.StartsWith("del ", StringComparison.Ordinal) && HasRecursiveOrQuietSwitch(normalized)
            || normalized.StartsWith("erase ", StringComparison.Ordinal) && HasRecursiveOrQuietSwitch(normalized)
            || normalized.StartsWith("rd ", StringComparison.Ordinal) && HasRecursiveOrQuietSwitch(normalized)
            || normalized.StartsWith("rmdir ", StringComparison.Ordinal) && HasRecursiveOrQuietSwitch(normalized)
            || normalized.Contains("remove-item", StringComparison.Ordinal) && normalized.Contains("-recurse", StringComparison.Ordinal)
            || normalized.Contains(" rm -rf", StringComparison.Ordinal)
            || normalized.StartsWith("rm -rf", StringComparison.Ordinal);
    }

    private static bool HasRecursiveOrQuietSwitch(string command) =>
        command.Contains("/s", StringComparison.Ordinal)
        || command.Contains("/q", StringComparison.Ordinal)
        || command.Contains("-recurse", StringComparison.Ordinal)
        || command.Contains("-force", StringComparison.Ordinal);
}
