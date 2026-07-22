using System.Diagnostics;
using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public abstract class ProcessActionHandlerBase : IActionHandler
{
    protected ProcessActionHandlerBase(LogService logService)
    {
        LogService = logService;
    }

    protected LogService LogService { get; }

    public abstract bool CanHandle(OrbitAction action);

    public abstract Task<ActionExecutionResult> ExecuteAsync(OrbitAction action);

    protected static string ExpandPath(string value) =>
        Environment.ExpandEnvironmentVariables(value ?? "");

    protected static Task<ActionExecutionResult> StartShellAsync(string target, string arguments = "")
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return Task.FromResult(ActionExecutionResult.Failure("Aksiyon hedefi boş."));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = ExpandPath(target),
            Arguments = Environment.ExpandEnvironmentVariables(arguments ?? ""),
            UseShellExecute = true
        };

        return Task.FromResult(StartShell(startInfo, Process.Start));
    }

    internal static ActionExecutionResult StartShell(
        ProcessStartInfo startInfo,
        Func<ProcessStartInfo, Process?> startProcess)
    {
        try
        {
            // ShellExecute may hand the request to an existing Explorer/browser process and
            // legitimately return null. A completed call without an exception means Windows
            // accepted the request, so it must not be reported as an action failure.
            _ = startProcess(startInfo);
            return ActionExecutionResult.Success();
        }
        catch (Exception ex)
        {
            return ActionExecutionResult.Failure($"Aksiyon başlatılamadı: {ex.Message}");
        }
    }

    protected static bool LooksLikePath(string value) =>
        Path.IsPathFullyQualified(value) ||
        value.Contains('\\', StringComparison.Ordinal) ||
        value.Contains('/', StringComparison.Ordinal);
}
