namespace ActionOrbit.App.Services.Windows;

internal static class HotkeyChordParser
{
    public static bool TryParseTokens(string? value, out IReadOnlyList<string> tokens)
    {
        tokens = Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed == "+")
        {
            tokens = ["+"];
            return true;
        }

        var hasLiteralPlusKey = trimmed.EndsWith("++", StringComparison.Ordinal);
        var chordBody = hasLiteralPlusKey ? trimmed[..^2] : trimmed;
        var parts = chordBody.Split('+', StringSplitOptions.None);

        if (parts.Length == 0 || parts.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        var parsedTokens = parts.Select(part => part.Trim()).ToList();
        if (hasLiteralPlusKey)
        {
            parsedTokens.Add("+");
        }

        tokens = parsedTokens;
        return true;
    }
}
