using ActionOrbit.App.Models;
using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.Services;

public sealed record ActionValidationResult(bool IsValid, string Message)
{
    public static ActionValidationResult Success { get; } = new(true, "");
    public static ActionValidationResult Failure(string message) => new(false, message);
}

public static class ActionValidationService
{
    public static ActionValidationResult Validate(OrbitAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (string.IsNullOrWhiteSpace(action.Title))
        {
            return ActionValidationResult.Failure("Aksiyon adı boş olamaz.");
        }

        if (!string.IsNullOrWhiteSpace(action.Shortcut) &&
            !HotkeyParser.TryParseDisplay(action.Shortcut, out _, out var shortcutError))
        {
            return ActionValidationResult.Failure($"Doğrudan kısayol geçersiz: {shortcutError}");
        }

        var target = action.Target?.Trim() ?? "";
        var expandedTarget = Environment.ExpandEnvironmentVariables(target);

        if (!action.IsFolder && action.Children.Count > 0)
        {
            return ActionValidationResult.Failure(
                "Alt aksiyon içeren bir öğenin türü klasör olmalı.");
        }

        return action.Type switch
        {
            "folder" => action.Children.Count == 0
                ? ActionValidationResult.Failure("Klasörün içinde en az bir alt aksiyon olmalı.")
                : ActionValidationResult.Success,
            "open_app" => ValidateApp(target, expandedTarget, action.Arguments),
            "open_file" => ValidateFile(target, expandedTarget),
            "open_folder" => ValidateFolder(target, expandedTarget),
            "open_url" => ValidateUrl(target),
            "send_hotkey" => ValidateHotkey(target),
            "type_text" => string.IsNullOrEmpty(action.Target)
                ? ActionValidationResult.Failure("Yazılacak metin boş olamaz.")
                : ActionValidationResult.Success,
            "run_command" => ValidateCommand(target, action.Arguments),
            _ => ActionValidationResult.Failure($"Bilinmeyen aksiyon türü: {action.Type}")
        };
    }

    private static ActionValidationResult ValidateApp(
        string target,
        string expandedTarget,
        string? arguments)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ActionValidationResult.Failure("Uygulama hedefi boş olamaz.");
        }

        if (!string.IsNullOrWhiteSpace(arguments) &&
            ActionSecurityService.IsShellInterpreter(expandedTarget))
        {
            return ActionValidationResult.Failure(
                "Komut yorumlayıcıları uygulama aksiyonuyla argümanlı çalıştırılamaz.");
        }

        return LooksLikePath(expandedTarget) && !File.Exists(expandedTarget)
            ? ActionValidationResult.Failure("Uygulama dosyası bulunamadı.")
            : ActionValidationResult.Success;
    }

    private static ActionValidationResult ValidateFile(string target, string expandedTarget)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ActionValidationResult.Failure("Dosya yolu boş olamaz.");
        }

        if (ActionSecurityService.IsExecutableFileTarget(expandedTarget))
        {
            return ActionValidationResult.Failure(
                "Çalıştırılabilir veya betik dosyaları dosya aksiyonuyla açılamaz.");
        }

        return File.Exists(expandedTarget)
            ? ActionValidationResult.Success
            : ActionValidationResult.Failure("Dosya bulunamadı.");
    }

    private static ActionValidationResult ValidateFolder(string target, string expandedTarget)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ActionValidationResult.Failure("Klasör yolu boş olamaz.");
        }

        return Directory.Exists(expandedTarget)
            ? ActionValidationResult.Success
            : ActionValidationResult.Failure("Klasör bulunamadı.");
    }

    private static ActionValidationResult ValidateUrl(string target) =>
        Uri.TryCreate(target, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
            ? ActionValidationResult.Success
            : ActionValidationResult.Failure("Web adresi http:// veya https:// ile başlamalı.");

    private static ActionValidationResult ValidateHotkey(string hotkey)
    {
        return HotkeyParser.TryParseDisplay(hotkey, out _, out var errorMessage)
            ? ActionValidationResult.Success
            : ActionValidationResult.Failure(errorMessage);
    }

    private static ActionValidationResult ValidateCommand(string target, string? arguments)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ActionValidationResult.Failure("Komut boş olamaz.");
        }

        var fullCommand = string.IsNullOrWhiteSpace(arguments) ? target : $"{target} {arguments}";
        return CommandSafetyService.IsBlocked(fullCommand)
            ? ActionValidationResult.Failure("Bu komut güvenlik filtresi tarafından engellendi.")
            : ActionValidationResult.Success;
    }

    private static bool LooksLikePath(string value) =>
        Path.IsPathFullyQualified(value)
        || value.Contains('\\', StringComparison.Ordinal)
        || value.Contains('/', StringComparison.Ordinal);
}
