using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public sealed record ImportedActionRisk(string Profile, string Action, string Type, string Target);

public static class ActionSecurityService
{
    private static readonly HashSet<string> ShellInterpreters = new(StringComparer.OrdinalIgnoreCase)
    {
        "cmd",
        "cmd.exe",
        "powershell",
        "powershell.exe",
        "pwsh",
        "pwsh.exe",
        "wscript",
        "wscript.exe",
        "cscript",
        "cscript.exe",
        "mshta",
        "mshta.exe",
        "rundll32",
        "rundll32.exe",
        "regsvr32",
        "regsvr32.exe"
    };

    private static readonly HashSet<string> ExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bat",
        ".cmd",
        ".com",
        ".cpl",
        ".exe",
        ".hta",
        ".js",
        ".jse",
        ".lnk",
        ".msi",
        ".msp",
        ".ps1",
        ".reg",
        ".scr",
        ".url",
        ".vbe",
        ".vbs",
        ".wsf",
        ".wsh"
    };

    public static bool IsShellInterpreter(string? target)
    {
        var expanded = Environment.ExpandEnvironmentVariables(target ?? "").Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(expanded))
        {
            return false;
        }

        return ShellInterpreters.Contains(Path.GetFileName(expanded));
    }

    public static bool IsExecutableFileTarget(string? target)
    {
        var expanded = Environment.ExpandEnvironmentVariables(target ?? "").Trim().Trim('"');
        return ExecutableExtensions.Contains(Path.GetExtension(expanded));
    }

    public static IReadOnlyList<ImportedActionRisk> FindImportedActionRisks(
        IEnumerable<ProfileConfig> profiles)
    {
        var risks = new List<ImportedActionRisk>();
        foreach (var profile in profiles)
        {
            AddRisks(profile.Actions, profile.Name, risks);
        }

        return risks;
    }

    public static string BuildImportWarning(
        IEnumerable<ProfileConfig> profiles,
        string sourceName,
        bool replacesConfiguration)
    {
        var profileList = profiles.ToList();
        var actionCount = profileList.Sum(profile => CountActions(profile.Actions));
        var risks = FindImportedActionRisks(profileList);
        var message =
            $"{sourceName}\n\n" +
            $"{profileList.Count} profil ve {actionCount} aksiyon içe aktarılacak.";

        if (replacesConfiguration)
        {
            message += "\nMevcut profiller ve ayarlar değiştirilecek.";
        }

        if (risks.Count > 0)
        {
            message +=
                $"\n\nDİKKAT: {risks.Count} aksiyon program/komut çalıştırabilir veya klavye girdisi gönderebilir:";

            foreach (var risk in risks.Take(8))
            {
                message +=
                    $"\n• {SafePreview(risk.Profile, 40)} / {SafePreview(risk.Action, 40)} " +
                    $"[{risk.Type}] → {SafePreview(risk.Target, 80)}";
            }

            if (risks.Count > 8)
            {
                message += $"\n• … ve {risks.Count - 8} aksiyon daha";
            }

            message += "\n\nDosyanın kaynağına tamamen güvenmiyorsan içe aktarma.";
        }

        return message + "\n\nDevam edilsin mi?";
    }

    private static void AddRisks(
        IEnumerable<OrbitAction> actions,
        string profileName,
        ICollection<ImportedActionRisk> risks)
    {
        foreach (var action in actions)
        {
            if (IsRiskyImportedAction(action))
            {
                risks.Add(new ImportedActionRisk(
                    profileName,
                    action.Title,
                    action.Type,
                    action.Target));
            }

            AddRisks(action.Children, profileName, risks);
        }
    }

    private static bool IsRiskyImportedAction(OrbitAction action)
    {
        var type = action.Type?.Trim().ToLowerInvariant();
        return type is "run_command" or "open_app" or "type_text" or "send_hotkey"
            || type == "open_file" && IsExecutableFileTarget(action.Target);
    }

    private static int CountActions(IEnumerable<OrbitAction> actions)
    {
        var count = 0;
        foreach (var action in actions)
        {
            count++;
            count += CountActions(action.Children);
        }

        return count;
    }

    private static string SafePreview(string? value, int maxLength)
    {
        var normalized = (value ?? "")
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal)
            .Trim();

        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..maxLength]}…";
    }
}
