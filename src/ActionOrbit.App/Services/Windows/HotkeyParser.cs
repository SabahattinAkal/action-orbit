using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Windows;

internal static class HotkeyParser
{
    public static bool TryParseDisplay(string? value, out HotkeyConfig hotkey, out string errorMessage)
    {
        hotkey = new HotkeyConfig();
        errorMessage = "";

        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Kisayol bos olamaz.";
            return false;
        }

        if (!HotkeyChordParser.TryParseTokens(value, out var tokens))
        {
            errorMessage = "Kisayol formati okunamadi. Ornek: Ctrl+Shift+R";
            return false;
        }

        var modifiers = new List<string>();
        string? key = null;

        foreach (var token in tokens)
        {
            if (TryNormalizeModifier(token, out var modifier))
            {
                if (!modifiers.Contains(modifier, StringComparer.OrdinalIgnoreCase))
                {
                    modifiers.Add(modifier);
                }

                continue;
            }

            if (key is not null)
            {
                errorMessage = "Kisayolda tek ana tus olmali. Ornek: Ctrl+Alt+Shift+R";
                return false;
            }

            key = NormalizeKeyDisplay(token);
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            errorMessage = "Kisayolda ana tus eksik. Ornek: F13 veya Ctrl+Space";
            return false;
        }

        if (!KeyTokenParser.TryParseVirtualKey(key, out _))
        {
            errorMessage = $"Tus okunamadi: {key}";
            return false;
        }

        hotkey = new HotkeyConfig
        {
            Display = BuildDisplay(modifiers, key),
            Modifiers = modifiers,
            Key = key
        };
        return true;
    }

    public static bool TryParse(HotkeyConfig hotkey, out uint modifiers, out uint virtualKey)
    {
        modifiers = NativeMethods.ModNoRepeat;

        foreach (var modifier in hotkey.Modifiers)
        {
            switch (modifier.Trim().ToLowerInvariant())
            {
                case "control":
                case "ctrl":
                    modifiers |= NativeMethods.ModControl;
                    break;
                case "alt":
                    modifiers |= NativeMethods.ModAlt;
                    break;
                case "shift":
                    modifiers |= NativeMethods.ModShift;
                    break;
                case "win":
                case "windows":
                    modifiers |= NativeMethods.ModWin;
                    break;
            }
        }

        virtualKey = KeyTokenParser.TryParseVirtualKey(hotkey.Key, out var key)
            ? key
            : 0;

        return virtualKey != 0;
    }

    private static bool TryNormalizeModifier(string token, out string modifier)
    {
        modifier = token.Trim().ToLowerInvariant() switch
        {
            "control" or "ctrl" => "Control",
            "alt" => "Alt",
            "shift" => "Shift",
            "win" or "windows" => "Win",
            _ => ""
        };

        return modifier.Length > 0;
    }

    private static string NormalizeKeyDisplay(string token)
    {
        var trimmed = token.Trim();
        return trimmed.Equals("Control", StringComparison.OrdinalIgnoreCase)
            ? "Ctrl"
            : trimmed.Equals("Escape", StringComparison.OrdinalIgnoreCase)
                ? "Esc"
                : trimmed;
    }

    private static string BuildDisplay(IReadOnlyList<string> modifiers, string key)
    {
        var orderedModifiers = new[] { "Control", "Alt", "Shift", "Win" }
            .Where(modifier => modifiers.Contains(modifier, StringComparer.OrdinalIgnoreCase))
            .Select(modifier => modifier == "Control" ? "Ctrl" : modifier);

        return string.Join("+", orderedModifiers.Append(key));
    }
}
