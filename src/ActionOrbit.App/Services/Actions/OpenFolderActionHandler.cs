using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public sealed class OpenFolderActionHandler : ProcessActionHandlerBase
{
    public OpenFolderActionHandler(LogService logService)
        : base(logService)
    {
    }

    public override bool CanHandle(OrbitAction action) =>
        string.Equals(action.Type, "open_folder", StringComparison.OrdinalIgnoreCase);

    public override Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        var target = ExpandPath(action.Target);
        if (string.IsNullOrWhiteSpace(target))
        {
            return Task.FromResult(ActionExecutionResult.Failure("Klasör yolu boş."));
        }

        if (!Directory.Exists(target))
        {
            return Task.FromResult(ActionExecutionResult.Failure("Klasör bulunamadı."));
        }

        return StartShellAsync(action.Target);
    }
}
