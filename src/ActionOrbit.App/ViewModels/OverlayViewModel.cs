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
    private const int MainPageSize = 7;
    private const int FolderPageSize = 8;
    private const double CanvasEdgePadding = 22;
    private const double OverlayInfoGap = 12;
    internal const double OverlayInfoReservedHeight = 184;
    internal const double OverlayInfoBottomPadding = 16;

    private readonly ProfileConfig _activeProfile;
    private readonly ProfileConfig _defaultProfile;
    private readonly ThemeConfig _theme;
    private readonly ActionExecutionService _actionExecutionService;
    private readonly LogService _logService;
    private readonly IntPtr _restoreWindow;
    private readonly Action? _openShelf;
    private readonly Stack<FolderNavigationState> _folderHistory = [];
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
    private int _mainPageIndex;
    private int _folderPageIndex;
    private int _folderPageCount = 1;
    private int _keyboardSelectionIndex;
    private IReadOnlyList<RingRuntime> _currentRings = [];
    private int _ringIndex;

    public OverlayViewModel(
        ProfileConfig profile,
        ProfileConfig defaultProfile,
        ThemeConfig theme,
        ActionExecutionService actionExecutionService,
        LogService logService,
        IntPtr restoreWindow,
        Action? openShelf = null)
    {
        _activeProfile = profile;
        _defaultProfile = defaultProfile;
        _currentProfile = profile;
        LoadRingsForCurrentProfile();
        _isShowingDefaultProfile = ActiveProfileIsDefault;
        _theme = theme;
        _actionExecutionService = actionExecutionService;
        _logService = logService;
        _restoreWindow = restoreWindow;
        _openShelf = openShelf;
        ToggleDefaultProfileCommand = new RelayCommand(ToggleDefaultProfile);
        CollapseFolderCommand = new RelayCommand(CollapseFolder);
        OpenShelfCommand = new RelayCommand(OpenShelf, () => _openShelf is not null);

        ButtonSize = Math.Clamp(theme.ButtonSize > 0 ? theme.ButtonSize : 60, 54, 96);
        SatelliteButtonSize = Math.Clamp(ButtonSize - 10, 42, 78);
        RadiusX = Math.Clamp(theme.RadiusX > 0 ? theme.RadiusX : 116, 96, 190);
        RadiusY = Math.Clamp(theme.RadiusY > 0 ? theme.RadiusY : 98, 82, 168);
        SatelliteRadius = Math.Clamp(ButtonSize * 0.92, 56, 86);
        SatelliteAnchorOffset = Math.Clamp(ButtonSize * 0.82, 48, 78);

        var horizontalReach = RadiusX + SatelliteAnchorOffset + SatelliteRadius + SatelliteButtonSize;
        var verticalReach = RadiusY + SatelliteAnchorOffset + SatelliteRadius + SatelliteButtonSize;
        var orbitalHalfHeight = verticalReach + CanvasEdgePadding;
        var infoPanelHalfHeight = RadiusY
            + ButtonSize / 2
            + OverlayInfoGap
            + OverlayInfoReservedHeight
            + OverlayInfoBottomPadding;
        var windowHalfHeight = Math.Max(orbitalHalfHeight, infoPanelHalfHeight);
        WindowWidth = (horizontalReach + CanvasEdgePadding) * 2;
        WindowHeight = windowHalfHeight * 2;
        CenterX = WindowWidth / 2;
        CenterY = WindowHeight / 2;

        AccentBrush = CreateBrush(theme.Accent, "#A51E39");
        AccentForegroundBrush = CreateBrush(
            ThemeService.GetContrastingForeground(theme.Accent),
            "#FFFFFF");
        var isLightMode = ThemeService.IsLightMode(theme.Mode);
        OverlayInfoBackground = isLightMode ? CreateBrush("#EFFFFFFF", "#EFFFFFFF") : CreateBrush("#E9111318", "#E9111318");
        OverlayInfoForeground = isLightMode ? CreateBrush("#15171D", "#15171D") : CreateBrush("#FFFFFF", "#FFFFFF");
        OverlayInfoMutedForeground = isLightMode ? CreateBrush("#5B6270", "#5B6270") : CreateBrush("#CBD5E1", "#CBD5E1");
        CenterHintBackground = isLightMode ? CreateBrush("#D9111318", "#D9111318") : CreateBrush("#D9FFFFFF", "#D9FFFFFF");
        CenterHintForeground = isLightMode ? CreateBrush("#FFFFFF", "#FFFFFF") : CreateBrush("#111318", "#111318");
        RebuildMainRing();
    }

    public event Action? CloseRequested;

    public ObservableCollection<ActionButtonViewModel> ActionItems { get; } = [];
    public ObservableCollection<ActionButtonViewModel> SatelliteItems { get; } = [];

    public string ProfileName => _currentProfile.Name;
    public string CurrentRingName => _currentRings.Count == 0 ? "Ana Halka" : _currentRings[_ringIndex].Name;
    public bool HasMultipleRings => _currentRings.Count > 1;
    private List<OrbitAction> CurrentActions =>
        _currentRings.Count == 0 ? _currentProfile.Actions : _currentRings[_ringIndex].Actions;

    public ICommand ToggleDefaultProfileCommand { get; }
    public ICommand CollapseFolderCommand { get; }
    public ICommand OpenShelfCommand { get; }

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
    public string FolderOverflowText => FolderPageCount > 1
        ? $"Sayfa {FolderPageIndex + 1}/{FolderPageCount} • Sayfa düğmesi kalan aksiyonları gösterir"
        : "";

    public string FolderStatusText =>
        HasSatellites
            ? $"Klasör: {SelectedFolderTitle}"
            : MainPageCount > 1
                ? $"Ana halka • Sayfa {MainPageIndex + 1}/{MainPageCount}"
                : "Ana halka";

    public int MainPageIndex => _mainPageIndex;
    public int MainPageCount => GetMainPageCount();
    public int FolderPageIndex => _folderPageIndex;
    public int FolderPageCount
    {
        get => _folderPageCount;
        private set
        {
            if (SetProperty(ref _folderPageCount, value))
            {
                OnPropertyChanged(nameof(HasFolderOverflow));
                OnPropertyChanged(nameof(FolderOverflowText));
            }
        }
    }

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
    public double OverlayInfoTop => CenterY + RadiusY + ButtonSize / 2 + OverlayInfoGap;
    public double CenterHintLeft => CenterX - 46;
    public double CenterHintTop => CenterY + 24;
    public System.Windows.Media.Brush AccentBrush { get; }
    public System.Windows.Media.Brush AccentForegroundBrush { get; }
    public System.Windows.Media.Brush OverlayInfoBackground { get; }
    public System.Windows.Media.Brush OverlayInfoForeground { get; }
    public System.Windows.Media.Brush OverlayInfoMutedForeground { get; }
    public System.Windows.Media.Brush CenterHintBackground { get; }
    public System.Windows.Media.Brush CenterHintForeground { get; }
    public int KeyboardSelectionIndex => _keyboardSelectionIndex;
    public string KeyboardHint => "Oklarla seç · Enter aç · Esc geri";

    private void RebuildMainRing()
    {
        ActionItems.Clear();

        var totalCount = CurrentActions.Count;
        if (totalCount == 0)
        {
            return;
        }

        var pageCount = GetMainPageCount();
        _mainPageIndex = Math.Clamp(_mainPageIndex, 0, pageCount - 1);
        var visibleActions = pageCount > 1
            ? CurrentActions.Skip(_mainPageIndex * MainPageSize).Take(MainPageSize).ToList()
            : CurrentActions.ToList();
        var count = visibleActions.Count + (pageCount > 1 ? 1 : 0);

        var step = count == 1 ? 0 : 360.0 / count;
        const double startAngle = -90.0;

        for (var index = 0; index < visibleActions.Count; index++)
        {
            var action = visibleActions[index];
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
                isActiveFolder: ReferenceEquals(_expandedFolder, action),
                shortcutNumber: index + 1));
        }

        if (pageCount > 1)
        {
            var index = count - 1;
            var angleDegrees = startAngle + index * step;
            var angle = angleDegrees * Math.PI / 180.0;
            var centerX = CenterX + Math.Cos(angle) * RadiusX;
            var centerY = CenterY + Math.Sin(angle) * RadiusY;
            ActionItems.Add(CreatePaginationButton(
                title: "Sonraki sayfa",
                centerX,
                centerY,
                ButtonSize,
                iconFontSize: 22,
                angleDegrees,
                isSatellite: false,
                NextMainPage,
                shortcutNumber: count));
        }

        OnPropertyChanged(nameof(MainPageIndex));
        OnPropertyChanged(nameof(MainPageCount));
        OnPropertyChanged(nameof(FolderStatusText));
        ResetKeyboardSelection();
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

        SelectedFolderTitle = string.Join(
            " › ",
            _folderHistory
                .Reverse()
                .Select(state => state.Folder.Title)
                .Append(_expandedFolder.Title));

        var anchorAngle = _expandedAngleDegrees * Math.PI / 180.0;
        var outwardX = Math.Cos(anchorAngle);
        var outwardY = Math.Sin(anchorAngle);
        var groupCenterX = _expandedAnchorX + outwardX * SatelliteAnchorOffset;
        var groupCenterY = _expandedAnchorY + outwardY * SatelliteAnchorOffset;

        FolderPageCount = _expandedFolder.Children.Count > 9
            ? (int)Math.Ceiling(_expandedFolder.Children.Count / (double)FolderPageSize)
            : 1;
        _folderPageIndex = Math.Clamp(_folderPageIndex, 0, FolderPageCount - 1);
        var visibleChildren = FolderPageCount > 1
            ? _expandedFolder.Children.Skip(_folderPageIndex * FolderPageSize).Take(FolderPageSize).ToList()
            : _expandedFolder.Children.ToList();
        FolderOverflowCount = FolderPageCount > 1
            ? _expandedFolder.Children.Count - visibleChildren.Count
            : 0;
        var count = visibleChildren.Count + (FolderPageCount > 1 ? 1 : 0);
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
                    isActiveFolder: false,
                    shortcutNumber: index + 1));
                continue;
            }

            SatelliteItems.Add(CreatePaginationButton(
                title: "Sonraki sayfa",
                centerX,
                centerY,
                SatelliteButtonSize,
                iconFontSize: 18,
                angleDegrees,
                isSatellite: true,
                NextFolderPage,
                shortcutNumber: count));
        }

        var groupRadius = SatelliteRadius + SatelliteButtonSize + 18;
        SatelliteGroupLeft = groupCenterX - groupRadius;
        SatelliteGroupTop = groupCenterY - groupRadius;
        SatelliteGroupWidth = groupRadius * 2;
        SatelliteGroupHeight = groupRadius * 2;
        HasSatellites = true;
        OnPropertyChanged(nameof(FolderPageIndex));
        OnPropertyChanged(nameof(FolderOverflowText));
        ResetKeyboardSelection();
    }

    private ActionButtonViewModel CreateButton(
        OrbitAction action,
        double centerX,
        double centerY,
        double diameter,
        double iconFontSize,
        double angleDegrees,
        bool isSatellite,
        bool isActiveFolder,
        int shortcutNumber) =>
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
            ShortcutNumber = shortcutNumber,
            Command = new RelayCommand(parameter =>
            {
                if (parameter is ActionButtonViewModel item)
                {
                    _ = RunActionAsync(item);
                }
            })
        };

    private ActionButtonViewModel CreatePaginationButton(
        string title,
        double centerX,
        double centerY,
        double diameter,
        double iconFontSize,
        double angleDegrees,
        bool isSatellite,
        Action nextPage,
        int shortcutNumber) =>
        new()
        {
            Action = new OrbitAction
            {
                Id = "__overflow",
                Title = title,
                Icon = "arrow-right",
                Type = "overflow"
            },
            X = centerX - diameter / 2,
            Y = centerY - diameter / 2,
            CenterX = centerX,
            CenterY = centerY,
            Diameter = diameter,
            IconFontSize = iconFontSize,
            AngleDegrees = angleDegrees,
            IsSatellite = isSatellite,
            IsActiveFolder = false,
            ShortcutNumber = shortcutNumber,
            Command = new RelayCommand(nextPage)
        };

    public bool TryHandleKey(Key key)
    {
        var numberIndex = key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            Key.D4 or Key.NumPad4 => 3,
            Key.D5 or Key.NumPad5 => 4,
            Key.D6 or Key.NumPad6 => 5,
            Key.D7 or Key.NumPad7 => 6,
            Key.D8 or Key.NumPad8 => 7,
            Key.D9 or Key.NumPad9 => 8,
            _ => -1
        };

        if (numberIndex >= 0)
        {
            return SelectKeyboardItem(numberIndex, execute: true);
        }

        switch (key)
        {
            case Key.Left:
            case Key.Up:
                MoveKeyboardSelection(-1);
                return true;
            case Key.Right:
            case Key.Down:
            case Key.Tab:
                MoveKeyboardSelection(1);
                return true;
            case Key.Enter:
            case Key.Space:
                return SelectKeyboardItem(_keyboardSelectionIndex, execute: true);
            case Key.Back:
                return TryCollapseFolder();
            default:
                return false;
        }
    }

    private IReadOnlyList<ActionButtonViewModel> KeyboardItems =>
        HasSatellites ? SatelliteItems : ActionItems;

    private void MoveKeyboardSelection(int offset)
    {
        var items = KeyboardItems;
        if (items.Count == 0)
        {
            return;
        }

        _keyboardSelectionIndex = (_keyboardSelectionIndex + offset + items.Count) % items.Count;
        RefreshKeyboardSelection();
    }

    private bool SelectKeyboardItem(int index, bool execute)
    {
        var items = KeyboardItems;
        if (index < 0 || index >= items.Count)
        {
            return false;
        }

        _keyboardSelectionIndex = index;
        RefreshKeyboardSelection();
        if (execute)
        {
            items[index].Command.Execute(items[index]);
        }

        return true;
    }

    private void ResetKeyboardSelection()
    {
        _keyboardSelectionIndex = 0;
        RefreshKeyboardSelection();
    }

    private void RefreshKeyboardSelection()
    {
        for (var index = 0; index < ActionItems.Count; index++)
        {
            ActionItems[index].IsKeyboardSelected = !HasSatellites && index == _keyboardSelectionIndex;
        }

        for (var index = 0; index < SatelliteItems.Count; index++)
        {
            SatelliteItems[index].IsKeyboardSelected = HasSatellites && index == _keyboardSelectionIndex;
        }

        OnPropertyChanged(nameof(KeyboardSelectionIndex));
    }

    private int GetMainPageCount()
    {
        var count = CurrentActions.Count;
        return count > 8
            ? (int)Math.Ceiling(count / (double)MainPageSize)
            : 1;
    }

    private void NextMainPage()
    {
        var pageCount = GetMainPageCount();
        if (pageCount <= 1)
        {
            return;
        }

        ResetOpenFolder();
        _mainPageIndex = (_mainPageIndex + 1) % pageCount;
        RebuildMainRing();
    }

    private void NextFolderPage()
    {
        if (_expandedFolder is null || FolderPageCount <= 1)
        {
            return;
        }

        _folderPageIndex = (_folderPageIndex + 1) % FolderPageCount;
        RebuildSatellites();
    }

    private void ToggleDefaultProfile()
    {
        if (ActiveProfileIsDefault)
        {
            return;
        }

        _isShowingDefaultProfile = !_isShowingDefaultProfile;
        _currentProfile = _isShowingDefaultProfile ? _defaultProfile : _activeProfile;
        LoadRingsForCurrentProfile();
        _mainPageIndex = 0;
        ResetOpenFolder();
        RebuildMainRing();
        OnPropertyChanged(nameof(ProfileName));
        OnPropertyChanged(nameof(CenterButtonText));
        OnPropertyChanged(nameof(CenterButtonToolTip));
        OnPropertyChanged(nameof(CenterButtonHint));
    }

    public bool SwitchRing(int direction)
    {
        if (_currentRings.Count <= 1 || direction == 0)
        {
            return false;
        }

        _ringIndex = (_ringIndex + Math.Sign(direction) + _currentRings.Count) % _currentRings.Count;
        _mainPageIndex = 0;
        ResetOpenFolder();
        RebuildMainRing();
        OnPropertyChanged(nameof(CurrentRingName));
        OnPropertyChanged(nameof(FolderStatusText));
        return true;
    }

    private void LoadRingsForCurrentProfile()
    {
        _currentRings =
        [
            new RingRuntime(_currentProfile.MainRingName, _currentProfile.Actions),
            .. _currentProfile.RingSets.Select(ring => new RingRuntime(ring.Name, ring.Actions))
        ];
        _ringIndex = 0;
        OnPropertyChanged(nameof(CurrentRingName));
        OnPropertyChanged(nameof(HasMultipleRings));
    }

    private async Task RunActionAsync(ActionButtonViewModel item)
    {
        var action = item.Action;

        if (action.IsFolder)
        {
            if (action.Children.Count == 0)
            {
                _logService.Warn(
                    $"Folder action has no children: {LogService.SafeValue(action.Id)}");
                return;
            }

            if (!item.IsSatellite && _expandedFolder?.Id == action.Id)
            {
                ResetOpenFolder();
                RebuildMainRing();
                return;
            }

            if (item.IsSatellite && _expandedFolder is not null)
            {
                _folderHistory.Push(new FolderNavigationState(
                    _expandedFolder,
                    _expandedAnchorX,
                    _expandedAnchorY,
                    _expandedAngleDegrees,
                    _folderPageIndex));
            }
            else if (!item.IsSatellite)
            {
                _folderHistory.Clear();
            }

            if (!ReferenceEquals(_expandedFolder, action))
            {
                _folderPageIndex = 0;
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

    private void OpenShelf()
    {
        if (_openShelf is null)
        {
            return;
        }

        _logService.Info("Orbit Shelf requested from overlay.");
        CloseRequested?.Invoke();
        _openShelf();
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

    private void CollapseFolder()
    {
        if (_folderHistory.TryPop(out var previous))
        {
            _expandedFolder = previous.Folder;
            _expandedAnchorX = previous.AnchorX;
            _expandedAnchorY = previous.AnchorY;
            _expandedAngleDegrees = previous.AngleDegrees;
            _folderPageIndex = previous.PageIndex;
            RebuildMainRing();
            RebuildSatellites();
            return;
        }

        ResetOpenFolder();
        RebuildMainRing();
    }

    private void ResetOpenFolder()
    {
        _folderHistory.Clear();
        _expandedFolder = null;
        SatelliteItems.Clear();
        HasSatellites = false;
        FolderOverflowCount = 0;
        FolderPageCount = 1;
        _folderPageIndex = 0;
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

    private sealed record FolderNavigationState(
        OrbitAction Folder,
        double AnchorX,
        double AnchorY,
        double AngleDegrees,
        int PageIndex);

    private sealed record RingRuntime(string Name, List<OrbitAction> Actions);
}
