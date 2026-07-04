using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public sealed class ActionExecutionService
{
    private readonly LogService _logService;
    private readonly IReadOnlyList<IActionHandler> _handlers;

    public ActionExecutionService(LogService logService, IEnumerable<IActionHandler> handlers)
    {
        _logService = logService;
        _handlers = handlers.ToList();
    }

    public event EventHandler<ActionExecutionCompletedEventArgs>? ActionExecuted;

    public async Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        if (action.IsFolder)
        {
            var folderResult = ActionExecutionResult.Success("Folder navigation is handled by the overlay.");
            NotifyActionExecuted(action, folderResult);
            return folderResult;
        }

        var handler = _handlers.FirstOrDefault(candidate => candidate.CanHandle(action));
        if (handler is null)
        {
            var message = $"No handler found for action type '{action.Type}'.";
            _logService.Warn(message);
            var missingHandlerResult = ActionExecutionResult.Failure(message);
            NotifyActionExecuted(action, missingHandlerResult);
            return missingHandlerResult;
        }

        try
        {
            _logService.Info($"Executing action {action.Id} ({action.Type}).");
            var result = await handler.ExecuteAsync(action);
            if (!result.Succeeded)
            {
                _logService.Warn(result.Message);
            }

            NotifyActionExecuted(action, result);
            return result;
        }
        catch (Exception ex)
        {
            _logService.Error($"Action failed: {action.Id}", ex);
            var failedResult = ActionExecutionResult.Failure(ex.Message);
            NotifyActionExecuted(action, failedResult);
            return failedResult;
        }
    }

    private void NotifyActionExecuted(OrbitAction action, ActionExecutionResult result) =>
        ActionExecuted?.Invoke(this, new ActionExecutionCompletedEventArgs(action, result));
}

public sealed record ActionExecutionCompletedEventArgs(OrbitAction Action, ActionExecutionResult Result);
