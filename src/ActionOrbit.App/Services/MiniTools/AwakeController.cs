using System.Runtime.InteropServices;

namespace ActionOrbit.App.Services.MiniTools;

internal sealed class AwakeController : IDisposable
{
    private bool _isActive;

    public bool IsActive => _isActive;

    public bool Activate()
    {
        var result = SetThreadExecutionState(
            ExecutionState.Continuous |
            ExecutionState.SystemRequired |
            ExecutionState.DisplayRequired);
        _isActive = result != 0;
        return _isActive;
    }

    public void Deactivate()
    {
        if (!_isActive)
        {
            return;
        }

        SetThreadExecutionState(ExecutionState.Continuous);
        _isActive = false;
    }

    public void Dispose() => Deactivate();

    [Flags]
    private enum ExecutionState : uint
    {
        SystemRequired = 0x00000001,
        DisplayRequired = 0x00000002,
        Continuous = 0x80000000
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState flags);
}
