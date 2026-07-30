using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public sealed class OpenAppActionHandler : ProcessActionHandlerBase
{
    private readonly Func<string, string, bool> _confirmArguments;

    public OpenAppActionHandler(
        LogService logService,
        Func<string, string, bool>? confirmArguments = null)
        : base(logService)
    {
        _confirmArguments = confirmArguments ?? ((_, _) => false);
    }

    public override bool CanHandle(OrbitAction action) =>
        string.Equals(action.Type, "open_app", StringComparison.OrdinalIgnoreCase);

    public override Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        var target = ExpandPath(action.Target);
        if (!string.IsNullOrWhiteSpace(action.Arguments) &&
            ActionSecurityService.IsShellInterpreter(target))
        {
            return Task.FromResult(ActionExecutionResult.Failure(
                "Komut yorumlayıcıları uygulama aksiyonuyla argümanlı çalıştırılamaz. Komut aksiyonunu kullan."));
        }

        if (!string.IsNullOrWhiteSpace(action.Arguments) &&
            !_confirmArguments(target, action.Arguments))
        {
            return Task.FromResult(ActionExecutionResult.Failure(
                "Uygulama argümanları kullanıcı tarafından onaylanmadı."));
        }

        if (LooksLikePath(target) && !File.Exists(target))
        {
            return Task.FromResult(ActionExecutionResult.Failure("Uygulama dosyası bulunamadı."));
        }

        return StartShellAsync(action.Target, action.Arguments);
    }
}
