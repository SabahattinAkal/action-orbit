using System.Diagnostics;
using System.Text;
using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.Services;

public sealed class ActiveWindowService
{
    private const string DesktopProcessName = "desktop.exe";
    private const string TaskbarProcessName = "taskbar.exe";
    private readonly LogService _logService;
    private readonly object _cacheGate = new();
    private string _lastObservedProcessName = "";
    private IntPtr _lastObservedWindowHandle;
    private string _lastExternalProcessName = "";
    private IntPtr _lastExternalWindowHandle;

    public ActiveWindowService(LogService logService)
    {
        _logService = logService;
    }

    public string GetActiveProcessName(string? ignoredProcessName = null)
    {
        try
        {
            var handle = NativeMethods.GetForegroundWindow();
            if (handle == IntPtr.Zero)
            {
                return "";
            }

            return GetProcessNameForWindow(handle, ignoredProcessName);
        }
        catch (Exception ex)
        {
            _logService.Error("Could not read active window process.", ex);
            return "";
        }
    }

    public string GetProcessNameForWindow(IntPtr handle, string? ignoredProcessName = null)
    {
        try
        {
            if (handle == IntPtr.Zero)
            {
                return "";
            }

            string processName;
            lock (_cacheGate)
            {
                processName = handle == _lastObservedWindowHandle
                    ? _lastObservedProcessName
                    : "";
            }

            if (string.IsNullOrWhiteSpace(processName))
            {
                NativeMethods.GetWindowThreadProcessId(handle, out var processId);
                if (processId == 0)
                {
                    return "";
                }

                using var process = Process.GetProcessById((int)processId);
                processName = $"{process.ProcessName}.exe";

                if (string.Equals(processName, "explorer.exe", StringComparison.OrdinalIgnoreCase))
                {
                    processName = ClassifyExplorerWindow(handle);
                }
            }

            lock (_cacheGate)
            {
                _lastObservedProcessName = processName;
                _lastObservedWindowHandle = handle;

                if (!string.IsNullOrWhiteSpace(ignoredProcessName) &&
                    string.Equals(processName, ignoredProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return _lastExternalProcessName;
                }

                _lastExternalProcessName = processName;
                _lastExternalWindowHandle = handle;
                return processName;
            }
        }
        catch (Exception ex)
        {
            _logService.Error("Could not read window process.", ex);
            return "";
        }
    }

    public IntPtr GetLastExternalWindowHandle()
    {
        IntPtr handle;
        lock (_cacheGate)
        {
            handle = _lastExternalWindowHandle;
        }

        return handle != IntPtr.Zero && NativeMethods.IsWindow(handle)
            ? handle
            : IntPtr.Zero;
    }

    private static string ClassifyExplorerWindow(IntPtr handle)
    {
        var className = GetWindowClassName(handle);
        return className switch
        {
            "CabinetWClass" or "ExploreWClass" => "explorer.exe",
            "Progman" or "WorkerW" or "SHELLDLL_DefView" or "SysListView32" => DesktopProcessName,
            "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Button" => TaskbarProcessName,
            _ => "explorer.exe"
        };
    }

    private static string GetWindowClassName(IntPtr handle)
    {
        var builder = new StringBuilder(256);
        var length = NativeMethods.GetClassName(handle, builder, builder.Capacity);
        return length > 0 ? builder.ToString() : "";
    }
}
