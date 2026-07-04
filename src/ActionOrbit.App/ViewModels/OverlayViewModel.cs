using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.Services.Windows;

namespace ActionOrbit.App.ViewModels;

public sealed class OverlayViewModel : ViewModelBase
{
    private readonly ProfileConfig _activeProfile;
    private readonly ProfileConfig _defaultProfile;
    private readonly ThemeConfig _theme;
    private readonly ActionExecutionService _actionExecutionService;
    private readonly LogService _logService;
    private readonly IntPtr _restoreWindow;
    private ProfileConfig _currentProfile;
    private OrbitAction? _expandedFolder;
    private double _expandedAnchorX;
    private double _expandedAnchorY;
    private double _expandedAngleDegrees;
    private string _selectedFolderTitle = "";
    private bool _hasSatellites;
    private int _folderOverflowCount;
    private double _satelliteGroupLeft;
    private double _satelliteGroupTop;
    private double _satelliteGroupWidth;
    private double _satelliteGroupHeight;
    private bool _isShowingDefaultProfile;

    public OverlayViewModel(
        ProfileConfig profile,
        ProfileConfig defaultProfile,
        ThemeConfig theme,
        ActionExecutionService actionExecutionService,
        LogService logService,
        IntPtr restoreWindow)
    {
        _activeProfile = profile;
        _defaultProfile = defaultProfile;
        _currentProfile = profile;
        _isShowingDefaultProfile = ActiveProfileIsDefault;
        _theme = theme;
        _actionExecutionService = actionExecutionService;
        _logService = logService;
        _restoreWindow = restoreWindow;
        ToggleDefaultProfileCommand = new RelayCommand(ToggleDefaultProfile);
        CollapseFolderCommand = new RelayCommand(CollapseFolder);

        ButtonSize = Math.Clamp(theme.ButtonSize > 0 ? theme.ButtonSize : 60, 54, 96);
        SatelliteButtonSize = Math.Clamp(ButtonSize - 10, 42, 78);
        RadiusX = Math.Clamp(theme.RadiusX > 0 ? theme.RadiusX : 116, 96, 190);
        RadiusY = Math.Clamp(theme.RadiusY > 0 ? theme.RadiusY : 98, 82, 168);
        SatelliteRadius = Math.Clamp(ButtonSize * 0.92, 56, 86);
        SatelliteAnchorOffset = Math.Clamp(ButtonSize * 0.82, 48, 78);

        var horizontalReach = RadiusX + SatelliteAnchorOffset + SatelliteRadius + SatelliteButtonSize;
        var verticalReach = RadiusY + SatelliteAnchorOffset + SatelliteRadius + SatelliteButtonSize;
        WindowWidth = horizontalReach * 2 + 44;
        WindowHeight = verticalReach * 2 + 44;
        CenterX = WindowWidth / 2;
        CenterY = WindowHeight / 2;

        AccentBrush = CreateBrush(theme.Accent, "#A51E39");
        OverlayInfoBackground = IsLightMode(theme) ? CreateBrush("#EFFFFFFF", "#EFFFFFFF") : CreateBrush("#E9111318", "#E9111318");
        OverlayInfoForeground = IsLightMode(theme) ? CreateBrush("#15171D", "#15171D") : CreateBrush("#FFFFFF", "#FFFFFF");
        OverlayInfoMutedForeground = IsLightMode(theme) ? CreateBrush("#5B6270", "#5B6270") : CreateBrush("#CBD5E1", "#CBD5E1");
        CenterHintBackground = IsLightMode(theme) ? CreateBrush("#D9111318", "#D9111318") : CreateBrush("#D9FFFFFF", "#D9FFFFFF");
        CenterHintForeground = IsLightMode(theme) ? CreateBrush("#FFFFFF", "#FFFFFF") : CreateBrush("#111318", "#111318");
        RebuildMainRing();
    }

    public event Action? CloseRequested;

    public ObservableCollection<ActionButtonViewModel> ActionItems { get; } = [];
    public ObservableCollection<ActionButtonViewModel> SatelliteItems { get; } = [];

    public string ProfileName => _currentProfile.Name;

    public ICommand ToggleDefaultProfileCommand { get; }
    public ICommand CollapseFolderCommand { get; }

    public string CenterButtonText =>
        _isShowingDefaultProfile ? "↩" : "↝";

    public string CenterButtonToolTip =>
        ActiveProfileIsDefault
            ? "Varsayılan aksiyonlar"
            : _isShowingDefaultProfile
                ? "Uygulama aksiyonlarına dön"
                : "Varsayılan aksiyonları göster";

    public string CenterButtonHint =>
        ActiveProfileIsDefault
            ? "Varsayılan"
            : _isShowingDefaultProfile
                ? "Varsayılan"
                : "Uygulama";

    private bool ActiveProfileIsDefault =>
        string.Equals(_activeProfile.Id, _defaultProfile.Id, StringComparison.OrdinalIgnoreCase);

    public string SelectedFolderTitle
    {
        get => _selectedFolderTitle;
        private set
        {
            if (SetProperty(ref _selectedFolderTitle, value))
            {
                OnPropertyChanged(nameof(FolderStatusText));
            }
        }
    }

    public bool HasSatellites
    {
        get => _hasSatellites;
        private set
        {
            if (SetProperty(ref _hasSatellites, value))
            {
                OnPropertyChanged(nameof(FolderStatusText));
                OnPropertyChanged(nameof(CanCollapseFolder));
            }
        }
    }

    public bool CanCollapseFolder => HasSatellites;

    public int FolderOverflowCount
    {
        get => _folderOverflowCount;
        private set
        {
            if (SetProperty(ref _folderOverflowCount, value))
            {
                OnPropertyChanged(nameof(HasFolderOverflow));
                OnPropertyChanged(nameof(FolderOverflowText));
            }
        }
    }

    public bool HasFolderOverflow => FolderOverflowCount > 0;
    public string FolderOverflowText => HasFolderOverflow
        ? $"+{FolderOverflowCount} aksiyon daha var"
        : "";

    public string FolderStatusText =>
        HasSatellites
            ? $"Klasör: {SelectedFolderTitle}"
            : "Ana halka";

    public double SatelliteGroupLeft
    {
        get => _satelliteGroupLeft;
        private set => SetProperty(ref _satelliteGroupLeft, value);
    }

    public double SatelliteGroupTop
    {
        get => _satelliteGroupTop;
        private set => SetProperty(ref _satelliteGroupTop, value);
    }

    public double SatelliteGroupWidth
    {
        get => _satelliteGroupWidth;
        private set => SetProperty(ref _satelliteGroupWidth, value);
    }

    public double SatelliteGroupHeight
    {
        get => _satelliteGroupHeight;
        private set => SetProperty(ref _satelliteGroupHeight, value);
    }

    public double ButtonSize { get; }
    public double SatelliteButtonSize { get; }
    public double RadiusX { get; }
    public double RadiusY { get; }
    public double SatelliteRadius { get; }
    public double SatelliteAnchorOffset { get; }
    public double WindowWidth { get; }
    public double WindowHeight { get; }
    public double CenterX { get; }
    public double CenterY { get; }
    public double OverlayInfoLeft => CenterX - 132;
    public double OverlayInfoTop => CenterY + RadiusY + ButtonSize / 2 + 12;
    public double CenterHintLeft => CenterX - 46;
    public double CenterHintTop => CenterY + 24;
    public System.Windows.Media.Brush AccentBrush { get; }
    public System.Windows.Media.Brush OverlayInfoBackground { get; }
    public System.Windows.Media.Brush OverlayInfoForeground { get; }
    public System.Windows.Media.Brush OverlayInfoMutedForeground { get; }
    public System.Windows.Media.Brush CenterHintBackground { get; }
    public System.Windows.Media.Brush CenterHintForeground { get; }

    private void RebuildMainRing()
    {
        ActionItems.Clear();

        var count = _currentProfile.Actions.Count;
        if (count == 0)
        {
            return;
        }

        var step = count == 1 ? 0 : 360.0 / count;
        const double startAngle = -90.0;

        for (var index = 0; index < count; index++)
        {
            var action = _currentProfile.Actions[index];
            var angleDegrees = startAngle + index * step;
            var angle = angleDegrees * Math.PI / 180.0;
            var centerX = CenterX + Math.Cos(angle) * RadiusX;
            var centerY = CenterY + Math.Sin(angle) * RadiusY;

            ActionItems.Add(CreateButton(
                action,
                centerX,
                centerY,
                ButtonSize,
                iconFontSize: 25,
                angleDegrees,
                isSatellite: false,
                isActiveFolder: _expandedFolder?.Id == action.Id));
        }
    }

    private void RebuildSatellites()
    {
        SatelliteItems.Clear();

        if (_expandedFolder is null || _expandedFolder.Children.Count == 0)
        {
            HasSatellites = false;
            FolderOverflowCount = 0;
            SelectedFolderTitle = "";
            return;
        }

        SelectedFolderTitle = _expandedFolder.Title;

        var anchorAngle = _expandedAngleDegrees * Math.PI / 180.0;
        var outwardX = Math.Cos(anchorAngle);
        var outwardY = Math.Sin(anchorAngle);
        var groupCenterX = _expandedAnchorX + outwardX * SatelliteAnchorOffset;
        var groupCenterY = _expandedAnchorY + outwardY * SatelliteAnchorOffset;

        var visibleChildren = _expandedFolder.Children.Count > 9
            ? _expandedFolder.Children.Take(8).ToList()
            : _expandedFolder.Children.ToList();
        var overflowCount = _expandedFolder.Children.Count - visibleChildren.Count;
        FolderOverflowCount = overflowCount;
        var count = visibleChildren.Count + (overflowCount > 0 ? 1 : 0);
        var spread = count <= 2 ? 108.0 : Math.Min(238.0, 40.0 * (count - 1));
        var firstAngle = _expandedAngleDegrees - spread / 2;
        var step = count <= 1 ? 0 : spread / (count - 1);

        for (var index = 0; index < count; index++)
        {
            var angleDegrees = firstAngle + index * step;
            var angle = angleDegrees * Math.PI / 180.0;
            var centerX = groupCenterX + Math.Cos(angle) * SatelliteRadius;
            var centerY = groupCenterY + Math.Sin(angle) * SatelliteRadius;

            if (index < visibleChildren.Count)
            {
                SatelliteItems.Add(CreateButton(
                    visibleChildren[index],
                    centerX,
                    centerY,
                    SatelliteButtonSize,
                    iconFontSize: 21,
                    angleDegrees,
                    isSatellite: true,
                    isActiveFolder: false));
                continue;
            }

            SatelliteItems.Add(CreateOverflowButton(
                overflowCount,
                centerX,
                centerY,
                SatelliteButtonSize,
                iconFontSize: 18,
                angleDegrees));
        }

        var groupRadius = SatelliteRadius + SatelliteButtonSize + 18;
        SatelliteGroupLeft = groupCenterX - groupRadius;
        SatelliteGroupTop = groupCenterY - groupRadius;
        SatelliteGroupWidth = groupRadius * 2;
        SatelliteGroupHeight = groupRadius * 2;
        HasSatellites = true;
    }

    private ActionButtonViewModel CreateButton(
        OrbitAction action,
        double centerX,
        double centerY,
        double diameter,
        double iconFontSize,
        double angleDegrees,
        bool isSatellite,
        bool isActiveFolder) =>
        new()
        {
            Action = action,
            X = centerX - diameter / 2,
            Y = centerY - diameter / 2,
            CenterX = centerX,
            CenterY = centerY,
            Diameter = diameter,
            IconFontSize = iconFontSize,
            AngleDegrees = angleDegrees,
            IsSatellite = isSatellite,
            IsActiveFolder = isActiveFolder,
            Command = new RelayCommand(parameter =>
            {
                if (parameter is ActionButtonViewModel item)
                {
                    _ = RunActionAsync(item);
                }
            })
        };

    private ActionButtonViewModel CreateOverflowButton(
        int overflowCount,
        double centerX,
        double centerY,
        double diameter,
        double iconFontSize,
        double angleDegrees) =>
        new()
        {
            Action = new OrbitAction
            {
                Id = "__overflow",
                Title = $"+{overflowCount} daha",
                Icon = $"+{overflowCount}",
                Type = "overflow"
            },
            X = centerX - diameter / 2,
            Y = centerY - diameter / 2,
            CenterX = centerX,
            CenterY = centerY,
            Diameter = diameter,
            IconFontSize = iconFontSize,
            AngleDegrees = angleDegrees,
            IsSatellite = true,
            IsActiveFolder = false,
            Command = new RelayCommand(() =>
                _logService.Info($"Folder has {overflowCount} more hidden actions."))
        };

    private void ToggleDefaultProfile()
    {
        if (ActiveProfileIsDefault)
        {
            return;
        }

        _isShowingDefaultProfile = !_isShowingDefaultProfile;
        _currentProfile = _isShowingDefaultProfile ? _defaultProfile : _activeProfile;
        ResetOpenFolder();
        RebuildMainRing();
        OnPropertyChanged(nameof(ProfileName));
        OnPropertyChanged(nameof(CenterButtonText));
        OnPropertyChanged(nameof(CenterButtonToolTip));
        OnPropertyChanged(nameof(CenterButtonHint));
    }

    private async Task RunActionAsync(ActionButtonViewModel item)
    {
        var action = item.Action;

        if (action.IsFolder)
        {
            if (action.Children.Count == 0)
            {
                _logService.Warn($"Folder action has no children: {action.Id}");
                return;
            }

            if (!item.IsSatellite && _expandedFolder?.Id == action.Id)
            {
                CollapseSatellites();
                return;
            }

            _expandedFolder = action;
            _expandedAnchorX = item.CenterX;
            _expandedAnchorY = item.CenterY;
            _expandedAngleDegrees = item.AngleDegrees;
            RebuildMainRing();
            RebuildSatellites();
            return;
        }

        CloseRequested?.Invoke();

        // Give Windows a brief moment to close the overlay, then return focus to the app that opened it.
        await Task.Delay(90);
        if (_restoreWindow != IntPtr.Zero)
        {
            NativeMethods.SetForegroundWindow(_restoreWindow);
            await Task.Delay(90);
        }

        await _actionExecutionService.ExecuteAsync(action);
    }

    public bool TryCollapseFolder()
    {
        if (!HasSatellites)
        {
            return false;
        }

        CollapseFolder();
        return true;
    }

    private void CollapseSatellites()
    {
        CollapseFolder();
    }

    private void CollapseFolder()
    {
        ResetOpenFolder();
        RebuildMainRing();
    }

    private void ResetOpenFolder()
    {
        _expandedFolder = null;
        SatelliteItems.Clear();
        HasSatellites = false;
        FolderOverflowCount = 0;
        SelectedFolderTitle = "";
    }

    private static System.Windows.Media.Brush CreateBrush(string color, string fallback)
    {
        try
        {
            return (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(color)!;
        }
        catch
        {
            return (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(fallback)!;
        }
    }

    private static bool IsLightMode(ThemeConfig theme) =>
        string.Equals(theme.Mode, "light", StringComparison.OrdinalIgnoreCase);
}
