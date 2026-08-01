using ActionOrbit.App.Models;

namespace ActionOrbit.App.ViewModels;

public sealed class RingSetOptionViewModel : ViewModelBase
{
    private readonly ProfileConfig _profile;

    public RingSetOptionViewModel(ProfileConfig profile, RingSetConfig? ring)
    {
        _profile = profile;
        Ring = ring;
    }

    public RingSetConfig? Ring { get; }
    public bool IsMain => Ring is null;
    public string Id => Ring?.Id ?? "main";
    public List<OrbitAction> Actions => Ring?.Actions ?? _profile.Actions;

    public string Name
    {
        get => Ring?.Name ?? _profile.MainRingName;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "Yeni Halka" : value.Trim();
            if (Ring is null)
            {
                if (_profile.MainRingName == normalized)
                {
                    return;
                }

                _profile.MainRingName = normalized;
            }
            else
            {
                if (Ring.Name == normalized)
                {
                    return;
                }

                Ring.Name = normalized;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string DisplayName => IsMain ? $"{Name} · ana" : Name;
    public int ActionCount => Actions.Count;

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(ActionCount));
    }
}
