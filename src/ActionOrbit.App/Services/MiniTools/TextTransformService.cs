using System.Globalization;
using System.Text.RegularExpressions;

namespace ActionOrbit.App.Services.MiniTools;

internal static partial class TextTransformService
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static int CountWords(string? value) => WordRegex().Matches(value ?? "").Count;

    public static string ToUpper(string? value) => (value ?? "").ToUpper(TurkishCulture);

    public static string ToLower(string? value) => (value ?? "").ToLower(TurkishCulture);

    public static string ToTitleCase(string? value) =>
        TurkishCulture.TextInfo.ToTitleCase(ToLower(value));

    public static string NormalizeWhitespace(string? value)
    {
        var normalized = (value ?? "").Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalized.Split('\n');
        var output = new List<string>(lines.Length);
        var previousWasBlank = false;

        foreach (var line in lines)
        {
            var cleaned = HorizontalWhitespaceRegex().Replace(line, " ").Trim();
            var isBlank = cleaned.Length == 0;
            if (isBlank && previousWasBlank)
            {
                continue;
            }

            output.Add(cleaned);
            previousWasBlank = isBlank;
        }

        return string.Join(Environment.NewLine, output).Trim();
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['’][\p{L}\p{N}]+)?", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"[^\S\r\n]+", RegexOptions.CultureInvariant)]
    private static partial Regex HorizontalWhitespaceRegex();
}
