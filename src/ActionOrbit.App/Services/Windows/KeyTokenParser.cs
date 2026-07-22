using System.Windows.Input;

namespace ActionOrbit.App.Services.Windows;

internal static class KeyTokenParser
{
    public static bool TryParseVirtualKey(string token, out uint virtualKey)
    {
        virtualKey = 0;
        var normalized = Normalize(token);

        if (normalized.Length == 1)
        {
            var c = normalized[0];
            if (c is >= 'A' and <= 'Z' || c is >= '0' and <= '9')
            {
                virtualKey = c;
                return true;
            }
        }

        if (normalized.StartsWith('F')
            && int.TryParse(normalized[1..], out var functionNumber)
            && functionNumber is >= 1 and <= 24)
        {
            virtualKey = (uint)(0x70 + functionNumber - 1);
            return true;
        }

        if (SpecialKeys.TryGetValue(normalized, out virtualKey))
        {
            return true;
        }

        if (Enum.TryParse<Key>(token, ignoreCase: true, out var wpfKey))
        {
            var key = KeyInterop.VirtualKeyFromKey(wpfKey);
            if (key > 0)
            {
                virtualKey = (uint)key;
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string token) =>
        token.Trim()
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("Control", "Ctrl", StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant();

    private static readonly Dictionary<string, uint> SpecialKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CTRL"] = 0x11,
        ["CONTROL"] = 0x11,
        ["SHIFT"] = 0x10,
        ["ALT"] = 0x12,
        ["WIN"] = 0x5B,
        ["WINDOWS"] = 0x5B,
        ["ENTER"] = 0x0D,
        ["RETURN"] = 0x0D,
        ["ESC"] = 0x1B,
        ["ESCAPE"] = 0x1B,
        ["TAB"] = 0x09,
        ["SPACE"] = 0x20,
        ["BACKSPACE"] = 0x08,
        ["DELETE"] = 0x2E,
        ["DEL"] = 0x2E,
        ["INSERT"] = 0x2D,
        ["INS"] = 0x2D,
        ["HOME"] = 0x24,
        ["END"] = 0x23,
        ["PAGEUP"] = 0x21,
        ["PAGEDOWN"] = 0x22,
        ["UP"] = 0x26,
        ["DOWN"] = 0x28,
        ["LEFT"] = 0x25,
        ["RIGHT"] = 0x27,
        ["`"] = 0xC0,
        ["OEM3"] = 0xC0,
        ["PLUS"] = 0xBB,
        ["+"] = 0xBB,
        ["MINUS"] = 0xBD,
        ["-"] = 0xBD,
        ["."] = 0xBE,
        ["PERIOD"] = 0xBE,
        [","] = 0xBC,
        ["COMMA"] = 0xBC,
        ["/"] = 0xBF,
        [";"] = 0xBA,
        ["["] = 0xDB,
        ["]"] = 0xDD,
        ["\\"] = 0xDC,
        ["'"] = 0xDE
    };
}
