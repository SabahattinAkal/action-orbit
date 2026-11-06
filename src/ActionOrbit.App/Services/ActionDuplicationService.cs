using ActionOrbit.App.Models;

namespace ActionOrbit.App.Services;

public static class ActionDuplicationService
{
    public static bool TryDuplicate(OrbitAction source, IEnumerable<OrbitAction> profileRoots, out OrbitAction? copy)
    {
        copy = null;
        var all = Flatten(profileRoots).ToList();
        if (all.Count + Flatten([source]).Count() > ConfigService.MaxActionsPerProfile) return false;
        var ids = all.Select(action => action.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        OrbitAction Clone(OrbitAction action)
        {
            string id;
            do { id = "action_" + Guid.NewGuid().ToString("N"); } while (!ids.Add(id));
            return new OrbitAction
            {
                Id = id, Title = action.Title, Icon = action.Icon, Type = action.Type,
                Target = action.Target, Arguments = action.Arguments, Browser = action.Browser,
                Shortcut = "", Children = action.Children.Select(Clone).ToList()
            };
        }
        copy = Clone(source);
        const string suffix = " (kopya)";
        copy.Title = source.Title[..Math.Min(source.Title.Length, ConfigService.MaxTitleLength - suffix.Length)] + suffix;
        return true;
    }

    private static IEnumerable<OrbitAction> Flatten(IEnumerable<OrbitAction> roots)
    {
        foreach (var action in roots)
        {
            yield return action;
            foreach (var child in Flatten(action.Children)) yield return child;
        }
    }
}
