using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public interface IHotkeyRegistrar
{
    bool IsRegistered { get; }
    void Register(HotkeyConfig hotkey);
}
