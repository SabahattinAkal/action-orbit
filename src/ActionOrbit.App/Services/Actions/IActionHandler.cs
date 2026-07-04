using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public interface IActionHandler
{
    bool CanHandle(OrbitAction action);
    Task<ActionExecutionResult> ExecuteAsync(OrbitAction action);
}
