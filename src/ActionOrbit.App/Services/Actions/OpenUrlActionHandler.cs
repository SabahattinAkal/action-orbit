using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public sealed class OpenUrlActionHandler : ProcessActionHandlerBase
{
    private readonly BrowserLaunchService _browserLaunchService;

    public OpenUrlActionHandler(LogService logService, BrowserLaunchService? browserLaunchService = null)
        : base(logService)
    {
        _browserLaunchService = browserLaunchService ?? new BrowserLaunchService();
    }

    public override bool CanHandle(OrbitAction action) =>
        string.Equals(action.Type, "open_url", StringComparison.OrdinalIgnoreCase);

    public override Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        var target = action.Target?.Trim() ?? "";
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return Task.FromResult(ActionExecutionResult.Failure("Web adresi http:// veya https:// ile başlamalı."));
        }

        return _browserLaunchService.OpenAsync(action.Browser, target);
    }
}
