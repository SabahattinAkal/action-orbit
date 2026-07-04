using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public sealed class OpenAppActionHandler : ProcessActionHandlerBase
{
    public OpenAppActionHandler(LogService logService)
        : base(logService)
    {
    }

    public override bool CanHandle(OrbitAction action) =>
        string.Equals(action.Type, "open_app", StringComparison.OrdinalIgnoreCase);

    public override Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        var target = ExpandPath(action.Target);
        if (LooksLikePath(target) && !File.Exists(target))
        {
            return Task.FromResult(ActionExecutionResult.Failure("Uygulama dosyası bulunamadı."));
        }

        return StartShellAsync(action.Target, action.Arguments);
    }
}
