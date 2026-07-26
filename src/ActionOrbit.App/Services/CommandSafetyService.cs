namespace ActionOrbit.App.Services;

public static class CommandSafetyService
{
    private static readonly string[] BlockedCommandTokens =
    [
        "format",
        "shutdown",
        "diskpart",
        "remove-item",
        "clear-content",
        "erase",
        "ri",
        "rmdir",
        "bcdedit",
        "cipher"
    ];

    public static bool IsBlocked(string? command)
    {
        var normalized = (command ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        var tokens = Tokenize(normalized);
        if (tokens.Any(token => BlockedCommandTokens.Contains(token, StringComparer.Ordinal)))
        {
            return true;
        }

        if (tokens.Contains("reg", StringComparer.Ordinal)
            && tokens.Contains("delete", StringComparer.Ordinal))
        {
            return true;
        }

        if (tokens.Contains("del", StringComparer.Ordinal)
            || tokens.Contains("rd", StringComparer.Ordinal))
        {
            return true;
        }

        for (var index = 0; index < tokens.Count; index++)
        {
            if (!string.Equals(tokens[index], "rm", StringComparison.Ordinal))
            {
                continue;
            }

            var isCachedGitRemoval = index > 0
                && string.Equals(tokens[index - 1], "git", StringComparison.Ordinal)
                && tokens.Skip(index + 1).Contains("--cached", StringComparer.Ordinal);
            if (!isCachedGitRemoval)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> Tokenize(string command) =>
        command
            .Replace("&&", " ", StringComparison.Ordinal)
            .Replace("||", " ", StringComparison.Ordinal)
            .Split(
                [' ', '\t', '\r', '\n', '&', '|', ';', '(', ')', '"', '\''],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeCommandToken)
            .ToArray();

    private static string NormalizeCommandToken(string token)
    {
        var separatorIndex = token.LastIndexOfAny(['\\', '/']);
        var leaf = separatorIndex >= 0 ? token[(separatorIndex + 1)..] : token;
        foreach (var extension in new[] { ".exe", ".com", ".cmd", ".bat" })
        {
            if (leaf.EndsWith(extension, StringComparison.Ordinal))
            {
                return leaf[..^extension.Length];
            }
        }

        return leaf;
    }
}
