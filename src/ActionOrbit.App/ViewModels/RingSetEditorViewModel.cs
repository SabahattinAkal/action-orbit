using System.Collections.ObjectModel;
using System.Windows.Input;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.ViewModels;

public sealed class RingSetEditorViewModel : ViewModelBase
{
    private readonly Func<ProfileConfig?> _getSelectedProfile;
    private readonly Action _markDirty;
    private readonly Action<string> _setStatus;
    private readonly Action _selectionChanged;
    private RingSetOptionViewModel? _selectedRing;

    public RingSetEditorViewModel(
        Func<ProfileConfig?> getSelectedProfile,
        Action markDirty,
        Action<string> setStatus,
        Action selectionChanged)
    {
        _getSelectedProfile = getSelectedProfile;
        _markDirty = markDirty;
        _setStatus = setStatus;
        _selectionChanged = selectionChanged;
        AddRingCommand = new RelayCommand(AddRing);
        DuplicateRingCommand = new RelayCommand(DuplicateRing);
        DeleteRingCommand = new RelayCommand(DeleteRing);
    }

    public ObservableCollection<RingSetOptionViewModel> Rings { get; } = [];
    public ICommand AddRingCommand { get; }
    public ICommand DuplicateRingCommand { get; }
    public ICommand DeleteRingCommand { get; }

    public RingSetOptionViewModel? SelectedRing
    {
        get => _selectedRing;
        set
        {
            if (SetProperty(ref _selectedRing, value))
            {
                OnPropertyChanged(nameof(SelectedRingName));
                OnPropertyChanged(nameof(CanDeleteSelectedRing));
                OnPropertyChanged(nameof(SelectedActions));
                _selectionChanged();
            }
        }
    }

    public List<OrbitAction>? SelectedActions => SelectedRing?.Actions;
    public bool CanDeleteSelectedRing => SelectedRing is { IsMain: false };

    public string SelectedRingName
    {
        get => SelectedRing?.Name ?? "";
        set
        {
            if (SelectedRing is null || SelectedRing.Name == value)
            {
                return;
            }

            SelectedRing.Name = value;
            _markDirty();
            OnPropertyChanged();
        }
    }

    public void ReloadForSelectedProfile()
    {
        var selectedId = SelectedRing?.Id;
        Rings.Clear();
        var profile = _getSelectedProfile();
        if (profile is null)
        {
            SelectedRing = null;
            return;
        }

        Rings.Add(new RingSetOptionViewModel(profile, null));
        foreach (var ring in profile.RingSets)
        {
            Rings.Add(new RingSetOptionViewModel(profile, ring));
        }

        SelectedRing = Rings.FirstOrDefault(ring =>
            string.Equals(ring.Id, selectedId, StringComparison.OrdinalIgnoreCase)) ?? Rings[0];
    }

    public void RefreshCounts()
    {
        foreach (var ring in Rings)
        {
            ring.Refresh();
        }
    }

    private void AddRing()
    {
        var profile = _getSelectedProfile();
        if (profile is null)
        {
            _setStatus("Önce bir profil seç.");
            return;
        }

        if (profile.RingSets.Count >= ConfigService.MaxRingSetsPerProfile)
        {
            _setStatus($"Bir profilde en fazla {ConfigService.MaxRingSetsPerProfile + 1} halka olabilir.");
            return;
        }

        var id = CreateUniqueId(profile, "ring");
        var ring = new RingSetConfig
        {
            Id = id,
            Name = $"Yeni Halka {profile.RingSets.Count + 2}"
        };
        profile.RingSets.Add(ring);
        var option = new RingSetOptionViewModel(profile, ring);
        Rings.Add(option);
        SelectedRing = option;
        _markDirty();
        _setStatus("Yeni halka seti eklendi. Aksiyonları bu halkaya özel düzenleyebilirsin.");
    }

    private void DuplicateRing()
    {
        var profile = _getSelectedProfile();
        if (profile is null || SelectedRing is null)
        {
            return;
        }

        if (profile.RingSets.Count >= ConfigService.MaxRingSetsPerProfile)
        {
            _setStatus($"Bir profilde en fazla {ConfigService.MaxRingSetsPerProfile + 1} halka olabilir.");
            return;
        }

        var ring = new RingSetConfig
        {
            Id = CreateUniqueId(profile, SelectedRing.Id),
            Name = $"{SelectedRing.Name} Kopya",
            Actions = SelectedRing.Actions.Select(CopyAction).ToList()
        };
        profile.RingSets.Add(ring);
        var option = new RingSetOptionViewModel(profile, ring);
        Rings.Add(option);
        SelectedRing = option;
        _markDirty();
        _setStatus("Halka seti kopyalandı.");
    }

    private void DeleteRing()
    {
        var profile = _getSelectedProfile();
        if (profile is null || SelectedRing?.Ring is not RingSetConfig ring)
        {
            _setStatus("Ana halka silinemez.");
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"{ring.Name} ve içindeki {ring.Actions.Count} aksiyon silinsin mi?",
            "Halka setini sil",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        profile.RingSets.Remove(ring);
        Rings.Remove(SelectedRing);
        SelectedRing = Rings[0];
        _markDirty();
        _setStatus("Halka seti silindi.");
    }

    private static string CreateUniqueId(ProfileConfig profile, string prefix)
    {
        var normalized = new string(prefix
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray()).Trim('_');
        if (normalized.Length == 0 || string.Equals(normalized, "main", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "ring";
        }

        var used = profile.RingSets.Select(ring => ring.Id).Append("main").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidate = normalized;
        var index = 2;
        while (!used.Add(candidate))
        {
            candidate = $"{normalized}_{index++}";
        }

        return candidate;
    }

    private static OrbitAction CopyAction(OrbitAction source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Icon = source.Icon,
        Type = source.Type,
        Target = source.Target,
        Arguments = source.Arguments,
        Browser = source.Browser,
        Shortcut = source.Shortcut,
        Children = source.Children.Select(CopyAction).ToList()
    };
}
