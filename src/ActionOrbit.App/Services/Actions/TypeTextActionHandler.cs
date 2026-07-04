using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public sealed class TypeTextActionHandler : IActionHandler
{
    private readonly InputSimulationService _inputSimulationService;

    public TypeTextActionHandler(InputSimulationService inputSimulationService)
    {
        _inputSimulationService = inputSimulationService;
    }

    public bool CanHandle(OrbitAction action) =>
        string.Equals(action.Type, "type_text", StringComparison.OrdinalIgnoreCase);

    public async Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        if (string.IsNullOrEmpty(action.Target))
        {
            return ActionExecutionResult.Failure("Yazılacak metin boş.");
        }

        await _inputSimulationService.TypeTextAsync(action.Target);
        return ActionExecutionResult.Success();
    }
}
