namespace ActionOrbit.App.Services;

public interface IStartupRegistration
{
    bool IsEnabled();
    void SetEnabled(bool enabled);
}
