using System.Diagnostics;
using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public sealed class RunCommandActionHandler : IActionHandler
{
    private readonly LogService _logService;

    public RunCommandActionHandler(LogService logService)
    {
        _logService = logService;
    }

    public bool CanHandle(OrbitAction action) =>
        string.Equals(action.Type, "run_command", StringComparison.OrdinalIgnoreCase);

    public async Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Target))
        {
            return ActionExecutionResult.Failure("Komut boş.");
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
            _logService.Info($"Running command: {fullCommand}");
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
