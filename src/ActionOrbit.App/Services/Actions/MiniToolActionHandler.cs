using ActionOrbit.App.Models;
using ActionOrbit.App.Services.MiniTools;

namespace ActionOrbit.App.Services.Actions;

public sealed class MiniToolActionHandler : IActionHandler
{
    private readonly IMiniToolLauncher _launcher;

    public MiniToolActionHandler(IMiniToolLauncher launcher) => _launcher = launcher;

    public bool CanHandle(OrbitAction action) =>
        string.Equals(action.Type, "mini_tool", StringComparison.OrdinalIgnoreCase);

    public Task<ActionExecutionResult> ExecuteAsync(OrbitAction action)
    {
        var toolId = action.Target?.Trim();
        if (!MiniToolCatalog.TryGet(toolId, out var tool))
        {
            return Task.FromResult(ActionExecutionResult.Failure("Bilinmeyen mini araç."));
        }

        _launcher.Show(tool.Id);
        return Task.FromResult(ActionExecutionResult.Success($"{tool.Title} açıldı."));
    }
}
