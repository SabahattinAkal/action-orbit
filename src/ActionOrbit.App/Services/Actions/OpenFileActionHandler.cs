using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public sealed class OpenFileActionHandler : ProcessActionHandlerBase
{
    public OpenFileActionHandler(LogService logService)
        : base(logService)
    {
    }

    public override bool CanHandle(OrbitAction action) =>
        string.Equals(action.Type, "open_file", StringComparison.OrdinalIgnoreCase);

    public override Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        var target = ExpandPath(action.Target);
        if (string.IsNullOrWhiteSpace(target))
        {
            return Task.FromResult(ActionExecutionResult.Failure("Dosya yolu boş."));
        }

        if (!File.Exists(target))
        {
            return Task.FromResult(ActionExecutionResult.Failure("Dosya bulunamadı."));
        }

        return StartShellAsync(action.Target);
    }
}
