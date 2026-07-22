using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public interface IConfigPersistence
{
    AppConfig CurrentConfig { get; }
    void Save(AppConfig config);
}
