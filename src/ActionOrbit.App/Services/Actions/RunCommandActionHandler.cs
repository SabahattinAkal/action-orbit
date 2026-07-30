using System.Diagnostics;
using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public sealed class RunCommandActionHandler : IActionHandler
{
    private readonly LogService _logService;
    private readonly Func<bool> _isCommandExecutionEnabled;
    private readonly Func<string, bool> _confirmCommand;

    public RunCommandActionHandler(
        LogService logService,
        Func<bool>? isCommandExecutionEnabled = null,
        Func<string, bool>? confirmCommand = null)
    {
        _logService = logService;
        _isCommandExecutionEnabled = isCommandExecutionEnabled ?? (() => false);
        _confirmCommand = confirmCommand ?? (_ => false);
    }

    public bool CanHandle(OrbitAction action) =>
        string.Equals(action.Type, "run_command", StringComparison.OrdinalIgnoreCase);

    public async Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Target))
        {
            return ActionExecutionResult.Failure("Komut boş.");
        }

        if (!_isCommandExecutionEnabled())
        {
            return ActionExecutionResult.Failure(
                "Komut aksiyonları güvenlik nedeniyle kapalı. Ayarlar bölümünden açıkça etkinleştir.");
        }

        var command = Environment.ExpandEnvironmentVariables(action.Target);
        var arguments = Environment.ExpandEnvironmentVariables(action.Arguments ?? "");
        var fullCommand = string.IsNullOrWhiteSpace(arguments)
            ? command
            : $"{command} {arguments}";

        if (CommandSafetyService.IsBlocked(fullCommand))
        {
            return ActionExecutionResult.Failure(
                "Bu komut public beta güvenlik filtresine takıldı. Silme, formatlama veya kapatma komutlarını elle çalıştır.");
        }

        if (!_confirmCommand(fullCommand))
        {
            return ActionExecutionResult.Failure("Komut çalıştırma kullanıcı tarafından iptal edildi.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("/d");
        startInfo.ArgumentList.Add("/s");
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(fullCommand);

        try
        {
            _logService.Info($"Running command action {LogService.SafeValue(action.Id)}.");
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return ActionExecutionResult.Failure("Komut başlatılamadı.");
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                return ActionExecutionResult.Success("Komut başlatıldı ve arka planda çalışıyor.");
            }

            return process.ExitCode == 0
                ? ActionExecutionResult.Success()
                : ActionExecutionResult.Failure(
                    $"Komut {process.ExitCode} hata koduyla sonlandı.");
        }
        catch (Exception ex)
        {
            _logService.Error("Command execution failed.", ex);
            return ActionExecutionResult.Failure($"Komut çalıştırılamadı: {ex.Message}");
        }
    }

}
