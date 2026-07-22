using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public static class ProfileCopyService
{
    public static ProfileConfig Copy(ProfileConfig source, string id, string name)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new ProfileConfig
        {
            Id = id,
            Name = name,
            Matches = source.Matches
                .Select(match => new ProfileMatch { ProcessName = match.ProcessName })
                .ToList(),
            Actions = source.Actions.Select(CopyAction).ToList()
        };
    }

    private static OrbitAction CopyAction(OrbitAction action) =>
        new()
        {
            Id = action.Id,
            Title = action.Title,
            Icon = action.Icon,
            Type = action.Type,
            Target = action.Target,
            Arguments = action.Arguments,
            Children = action.Children.Select(CopyAction).ToList()
        };
}
