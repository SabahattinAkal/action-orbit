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

    public Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Target))
        {
            return Task.FromResult(ActionExecutionResult.Failure("Komut boş."));
        }

        var command = Environment.ExpandEnvironmentVariables(action.Target);
        var arguments = Environment.ExpandEnvironmentVariables(action.Arguments ?? "");
        var fullCommand = string.IsNullOrWhiteSpace(arguments)
            ? command
            : $"{command} {arguments}";

        if (CommandSafetyService.IsBlocked(fullCommand))
        {
            return Task.FromResult(ActionExecutionResult.Failure(
                "Bu komut public beta güvenlik filtresine takıldı. Silme, formatlama veya kapatma komutlarını elle çalıştır."));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {fullCommand}",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            _logService.Info($"Running command: {fullCommand}");
            var process = Process.Start(startInfo);
            return Task.FromResult(process is null
                ? ActionExecutionResult.Failure("Komut başlatılamadı.")
                : ActionExecutionResult.Success());
        }
        catch (Exception ex)
        {
            _logService.Error("Command execution failed.", ex);
            return Task.FromResult(ActionExecutionResult.Failure($"Komut çalıştırılamadı: {ex.Message}"));
        }
    }

}
