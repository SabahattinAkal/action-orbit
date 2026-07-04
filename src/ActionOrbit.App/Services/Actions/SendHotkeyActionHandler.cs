using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services.Actions;

public sealed class SendHotkeyActionHandler : IActionHandler
{
    private readonly InputSimulationService _inputSimulationService;

    public SendHotkeyActionHandler(InputSimulationService inputSimulationService)
    {
        _inputSimulationService = inputSimulationService;
    }

    public bool CanHandle(OrbitAction action) =>
        string.Equals(action.Type, "send_hotkey", StringComparison.OrdinalIgnoreCase);

    public async Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Target))
        {
            return ActionExecutionResult.Failure("Kısayol boş.");
        }

        await _inputSimulationService.SendHotkeyAsync(action.Target);
        return ActionExecutionResult.Success();
    }
}
