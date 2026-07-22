using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;

namespace ActionOrbit.App.ViewModels;

public sealed class ActionEditorViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly ActionExecutionService _actionExecutionService;
    private readonly LogService _logService;
    private readonly Func<ProfileConfig?> _getSelectedProfile;
    private readonly Action _markDirty;
    private readonly Action<string> _setStatus;
    private readonly Action<string, Action> _registerUndo;
    private readonly Action _showOverlay;
    private ActionEditorRowViewModel? _selectedAction;
    private ActionPresetOption? _selectedPreset;

    public ActionEditorViewModel(
        ConfigService configService,
        ActionExecutionService actionExecutionService,
        LogService logService,
        Func<ProfileConfig?> getSelectedProfile,
        Action markDirty,
        Action<string> setStatus,
        Action<string, Action> registerUndo,
        Action showOverlay)
    {
        _configService = configService;
        _actionExecutionService = actionExecutionService;
        _logService = logService;
        _getSelectedProfile = getSelectedProfile;
        _markDirty = markDirty;
        _setStatus = setStatus;
        _registerUndo = registerUndo;
        _showOverlay = showOverlay;

        AddActionCommand = new RelayCommand(AddAction);
        AddFolderCommand = new RelayCommand(AddFolder);
        AddChildActionCommand = new RelayCommand(AddChildAction);
        ApplyPresetCommand = new RelayCommand(ApplySelectedPreset);
        AddPresetToProfileCommand = new RelayCommand(AddSelectedPresetToProfile);
        ImportIconCommand = new RelayCommand(ImportIcon);
        DeleteActionCommand = new RelayCommand(DeleteAction);
        BrowseActionTargetCommand = new RelayCommand(BrowseActionTarget);
        TestActionCommand = new RelayCommand(TestSelectedAction);
        MoveActionUpCommand = new RelayCommand(() => MoveSelectedAction(-1));
        MoveActionDownCommand = new RelayCommand(() => MoveSelectedAction(1));
        MoveActionOutOfFolderCommand = new RelayCommand(MoveSelectedActionOutOfFolder);
        SelectRingPreviewSlotCommand = new RelayCommand(parameter =>
            SelectRingPreviewSlot(parameter as RingPreviewSlotViewModel));

        SelectedPreset = ActionPresets.FirstOrDefault();
    }

    public ObservableCollection<ActionEditorRowViewModel> ActionRows { get; } = [];
    public ObservableCollection<IconOption> AvailableIcons { get; } = [];
    public ObservableCollection<RingPreviewSlotViewModel> RingPreviewSlots { get; } = [];
    public IReadOnlyList<string> AvailableIconKeys => IconCatalog.AvailableKeys;
    public IReadOnlyList<ActionTypeOption> ActionTypeOptions { get; } = ActionDefinitionCatalog.TypeOptions;
    public IReadOnlyList<ActionPresetOption> ActionPresets { get; } = ActionDefinitionCatalog.Presets;

    public ActionEditorRowViewModel? SelectedAction
    {
        get => _selectedAction;
        set
        {
            if (SetProperty(ref _selectedAction, value))
            {
                RefreshSelectedActionState();
                RefreshRingPreviewSelection();
            }
        }
    }

    public ActionPresetOption? SelectedPreset
    {
        get => _selectedPreset;
        set => SetProperty(ref _selectedPreset, value);
    }

    public string RingPreviewSummary
    {
        get
        {
            var count = _getSelectedProfile()?.Actions.Count ?? 0;
            return count switch
            {
                0 => "Halka boş · aksiyon ekleyebilirsin",
                > 8 => "İlk sayfa · diğer aksiyonlar devam düğmesinde",
                _ => "Ana halka düzeni"
            };
        }
    }

    public bool HasSelectedAction => SelectedAction is not null;
    public bool HasNoSelectedAction => SelectedAction is null;
    public bool SelectedActionIsFolder => SelectedAction?.IsFolder == true;
    public bool SelectedActionIsChild => SelectedAction?.IsChild == true;
    public bool CanMoveSelectedActionOutOfFolder => SelectedAction?.Parent is not null;
    public string SelectedActionFolderSummary =>
        SelectedAction is { IsFolder: true } action
            ? $"{action.Title} klasöründe {action.ChildCount} alt aksiyon var. Yeni alt aksiyon ekleyebilir veya listedeki aksiyonları bu klasörün üstüne sürükleyebilirsin."
            : "";
    public string SelectedActionParentSummary =>
        SelectedAction is { Parent: not null } action
            ? $"{action.Title}, {action.ParentTitle} klasörünün içinde. Gerekirse aksiyonu ana seviyeye çıkarabilirsin."
            : "";
    public bool CanBrowseSelectedAction =>
        SelectedAction?.Type is "open_app" or "open_file" or "open_folder";
    public bool HasSelectedActionValidation =>
        !string.IsNullOrWhiteSpace(SelectedActionValidationMessage);
    public string SelectedActionValidationMessage =>
        SelectedAction is null
            ? ""
            : ActionValidationService.Validate(SelectedAction.Action).Message;

    public ICommand AddActionCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand AddChildActionCommand { get; }
    public ICommand ApplyPresetCommand { get; }
    public ICommand AddPresetToProfileCommand { get; }
    public ICommand ImportIconCommand { get; }
    public ICommand DeleteActionCommand { get; }
    public ICommand BrowseActionTargetCommand { get; }
    public ICommand TestActionCommand { get; }
    public ICommand MoveActionUpCommand { get; }
    public ICommand MoveActionDownCommand { get; }
    public ICommand MoveActionOutOfFolderCommand { get; }
    public ICommand SelectRingPreviewSlotCommand { get; }

    public void ReloadForSelectedProfile() => RebuildActionRows();

    public void RefreshAvailableIcons()
    {
        AvailableIcons.Clear();
        foreach (var icon in IconCatalog.GetAvailableIcons())
        {
            AvailableIcons.Add(icon);
        }

        OnPropertyChanged(nameof(AvailableIconKeys));
    }

    public bool CanMoveActionIntoFolder(ActionEditorRowViewModel? source, ActionEditorRowViewModel? target)
    {
        if (source is null ||
            target is null ||
            ReferenceEquals(source, target) ||
            !target.IsFolder ||
            ReferenceEquals(source.Parent?.Action, target.Action))
        {
            return false;
        }

        return !IsDescendantOf(target, source);
    }

    public void MoveActionIntoFolder(ActionEditorRowViewModel source, ActionEditorRowViewModel target)
    {
        if (!CanMoveActionIntoFolder(source, target))
        {
            _setStatus("Alt aksiyon yapmak için bir klasörün üstüne bırak.");
            return;
        }

        var movedAction = source.Action;
        var targetFolder = target.Action;
        var sourceOwner = source.Owner;
        var sourceIndex = sourceOwner.IndexOf(movedAction);
        targetFolder.Children ??= [];

        if (!sourceOwner.Remove(movedAction))
        {
            _setStatus("Aksiyon taşınamadı.");
            return;
        }

        targetFolder.Children.Add(movedAction);
        var targetOwner = targetFolder.Children;
        RebuildActionRows();
        SelectedAction = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, movedAction));
        _registerUndo($"{movedAction.Title} klasöre taşıma", () =>
        {
            targetOwner.Remove(movedAction);
            sourceOwner.Insert(Math.Clamp(sourceIndex, 0, sourceOwner.Count), movedAction);
            RebuildActionRows();
            SelectedAction = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, movedAction));
        });
        _markDirty();
        _setStatus($"{movedAction.Title}, {targetFolder.Title} klasörüne taşındı.");
    }

    public bool CanReorderRingPreviewSlot(
        RingPreviewSlotViewModel? source,
        RingPreviewSlotViewModel? target)
    {
        var sourceRow = source?.ActionRow;
        var targetRow = target?.ActionRow;

        return sourceRow is not null
               && targetRow is not null
               && !ReferenceEquals(sourceRow.Action, targetRow.Action)
               && sourceRow.Parent is null
               && targetRow.Parent is null
               && ReferenceEquals(sourceRow.Owner, targetRow.Owner);
    }

    public void ReorderRingPreviewSlot(
        RingPreviewSlotViewModel source,
        RingPreviewSlotViewModel target)
    {
        if (CanReorderRingPreviewSlot(source, target))
        {
            ReorderRingAction(source.ActionRow!, target.ActionRow!);
        }
    }

    public void MoveRingPreviewSlot(RingPreviewSlotViewModel? slot, int direction)
    {
        var sourceRow = slot?.ActionRow;
        if (sourceRow is null || direction == 0)
        {
            return;
        }

        var sourceIndex = sourceRow.Owner.IndexOf(sourceRow.Action);
        var targetIndex = sourceIndex + Math.Sign(direction);
        if (sourceIndex < 0 || targetIndex < 0 || targetIndex >= sourceRow.Owner.Count)
        {
            _setStatus("Aksiyon zaten halkanın bu yöndeki son konumunda.");
            return;
        }

        var targetAction = sourceRow.Owner[targetIndex];
        var targetRow = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, targetAction));
        if (targetRow is not null)
        {
            ReorderRingAction(sourceRow, targetRow);
        }
    }

    private void SelectRingPreviewSlot(RingPreviewSlotViewModel? slot)
    {
        if (slot?.ActionRow is not null)
        {
            SelectedAction = slot.ActionRow;
            _setStatus($"Halkada seçildi: {slot.Title}");
            return;
        }

        if (slot?.IsOverflow == true)
        {
            _showOverlay();
        }
    }

    private void ReorderRingAction(
        ActionEditorRowViewModel sourceRow,
        ActionEditorRowViewModel targetRow)
    {
        var owner = sourceRow.Owner;
        var movedAction = sourceRow.Action;

        if (!ActionOrderService.TryMoveToTarget(owner, movedAction, targetRow.Action, out var move))
        {
            return;
        }

        RebuildActionRows();
        SelectedAction = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, movedAction));
        _registerUndo($"{movedAction.Title} halka sıralama", () =>
        {
            owner.Remove(movedAction);
            owner.Insert(Math.Clamp(move.OriginalIndex, 0, owner.Count), movedAction);
            RebuildActionRows();
            SelectedAction = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, movedAction));
        });
        _markDirty();
        _setStatus($"{movedAction.Title}, halkada {move.NewIndex + 1}. konuma taşındı.");
    }

    private void RebuildActionRows()
    {
        var selectedId = SelectedAction?.Action.Id;
        ActionRows.Clear();
        var selectedProfile = _getSelectedProfile();

        if (selectedProfile is null)
        {
            SelectedAction = null;
            RebuildRingPreview();
            return;
        }

        AddRows(selectedProfile.Actions, selectedProfile.Actions, parent: null, depth: 0);
        SelectedAction = ActionRows.FirstOrDefault(row =>
            string.Equals(row.Action.Id, selectedId, StringComparison.OrdinalIgnoreCase))
            ?? ActionRows.FirstOrDefault();
        RebuildRingPreview();
    }

    private void RebuildRingPreview()
    {
        RingPreviewSlots.Clear();
        var rootRows = ActionRows.Where(row => row.Depth == 0).ToList();
        var visibleCount = rootRows.Count > 8 ? 7 : Math.Min(rootRows.Count, 8);
        var slotCount = visibleCount + (rootRows.Count > 8 ? 1 : 0);

        const double center = 116;
        const double radius = 78;
        const double halfSlot = 29;

        for (var index = 0; index < visibleCount; index++)
        {
            var row = rootRows[index];
            var angle = (-90 + (360d * index / Math.Max(slotCount, 1))) * Math.PI / 180d;
            RingPreviewSlots.Add(new RingPreviewSlotViewModel(
                row,
                row.Title,
                row.Icon,
                center + Math.Cos(angle) * radius - halfSlot,
                center + Math.Sin(angle) * radius - halfSlot)
            {
                IsSelected = ReferenceEquals(row, SelectedAction)
            });
        }

        if (rootRows.Count > 8)
        {
            var index = slotCount - 1;
            var angle = (-90 + (360d * index / slotCount)) * Math.PI / 180d;
            RingPreviewSlots.Add(new RingPreviewSlotViewModel(
                null,
                "Devam",
                "rotate",
                center + Math.Cos(angle) * radius - halfSlot,
                center + Math.Sin(angle) * radius - halfSlot,
                isOverflow: true));
        }

        OnPropertyChanged(nameof(RingPreviewSummary));
    }

    private void RefreshRingPreviewSelection()
    {
        foreach (var slot in RingPreviewSlots)
        {
            slot.IsSelected = slot.ActionRow is not null && ReferenceEquals(slot.ActionRow, SelectedAction);
        }
    }

    private void AddRows(
        List<OrbitAction> actions,
        List<OrbitAction> owner,
        ActionEditorRowViewModel? parent,
        int depth)
    {
        foreach (var action in actions)
        {
            action.Children ??= [];
            var row = new ActionEditorRowViewModel(action, owner, parent, depth);
            row.PropertyChanged += OnActionRowPropertyChanged;
            ActionRows.Add(row);

            if (action.Children.Count > 0)
            {
                AddRows(action.Children, action.Children, row, depth + 1);
            }
        }
    }

    private void MoveSelectedActionOutOfFolder()
    {
        if (SelectedAction?.Parent is null)
        {
            _setStatus("Bu aksiyon zaten ana halkada.");
            return;
        }

        var row = SelectedAction;
        var parent = row.Parent;
        var movedAction = row.Action;
        var sourceOwner = row.Owner;
        var destinationOwner = parent.Owner;
        var sourceIndex = sourceOwner.IndexOf(movedAction);

        if (!sourceOwner.Remove(movedAction))
        {
            _setStatus("Aksiyon klasörden çıkarılamadı.");
            return;
        }

        var parentIndex = destinationOwner.IndexOf(parent.Action);
        var insertIndex = parentIndex >= 0 ? parentIndex + 1 : destinationOwner.Count;
        destinationOwner.Insert(insertIndex, movedAction);

        RebuildActionRows();
        SelectedAction = ActionRows.FirstOrDefault(candidate => ReferenceEquals(candidate.Action, movedAction));
        _registerUndo($"{movedAction.Title} klasörden çıkarma", () =>
        {
            destinationOwner.Remove(movedAction);
            sourceOwner.Insert(Math.Clamp(sourceIndex, 0, sourceOwner.Count), movedAction);
            RebuildActionRows();
            SelectedAction = ActionRows.FirstOrDefault(candidate => ReferenceEquals(candidate.Action, movedAction));
        });
        _markDirty();
        _setStatus($"{movedAction.Title} klasörden çıkarıldı.");
    }

    private static bool IsDescendantOf(ActionEditorRowViewModel row, ActionEditorRowViewModel possibleAncestor)
    {
        var parent = row.Parent;
        while (parent is not null)
        {
            if (ReferenceEquals(parent.Action, possibleAncestor.Action))
            {
                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }

    private void OnActionRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ActionEditorRowViewModel.Icon) ||
            e.PropertyName is nameof(ActionEditorRowViewModel.Title) ||
            e.PropertyName is nameof(ActionEditorRowViewModel.Type) ||
            e.PropertyName is nameof(ActionEditorRowViewModel.Target) ||
            e.PropertyName is nameof(ActionEditorRowViewModel.Arguments) ||
            e.PropertyName is nameof(ActionEditorRowViewModel.Id))
        {
            _markDirty();
            if (e.PropertyName is nameof(ActionEditorRowViewModel.Icon) or
                nameof(ActionEditorRowViewModel.Title) or
                nameof(ActionEditorRowViewModel.Type))
            {
                RebuildRingPreview();
            }

            if (sender is ActionEditorRowViewModel row && ReferenceEquals(row, SelectedAction))
            {
                RefreshSelectedActionState();
            }
        }
    }

    private void AddAction() => AddActionTo(_getSelectedProfile()?.Actions, type: "open_app");

    private void AddFolder() => AddActionTo(_getSelectedProfile()?.Actions, type: "folder");

    private void AddChildAction()
    {
        if (SelectedAction is null)
        {
            _setStatus("Önce bir klasör aksiyonu seç.");
            return;
        }

        if (!SelectedAction.IsFolder)
        {
            SelectedAction.Type = "folder";
        }

        SelectedAction.Action.Children ??= [];
        AddActionTo(SelectedAction.Action.Children, type: "open_app");
    }

    private void ApplySelectedPreset()
    {
        if (SelectedPreset is null)
        {
            _setStatus("Önce hazır bir eylem seç.");
            return;
        }

        if (SelectedAction is null)
        {
            _setStatus("Hazır eylemi uygulamak için önce mevcut bir aksiyon seç. Yeni eklemek için 'Profile Ekle'yi kullan.");
            return;
        }

        var row = SelectedAction;
        row.Title = SelectedPreset.Title;
        row.Icon = SelectedPreset.Icon;
        row.Type = SelectedPreset.Type;
        row.Target = SelectedPreset.Target;
        row.Arguments = SelectedPreset.Arguments;

        if (string.IsNullOrWhiteSpace(row.Id) ||
            row.Id.StartsWith("action_", StringComparison.OrdinalIgnoreCase) ||
            row.Id.StartsWith("folder_", StringComparison.OrdinalIgnoreCase) ||
            row.Id.StartsWith("new_", StringComparison.OrdinalIgnoreCase))
        {
            row.Id = CreateUniqueActionId(row.Owner, NormalizeId(SelectedPreset.Id, "action"), row.Action);
        }

        SelectedAction = row;
        RefreshActionList();
        _markDirty();
        _setStatus($"Hazır eylem uygulandı: {SelectedPreset.Title}");
    }

    private void AddSelectedPresetToProfile()
    {
        if (SelectedPreset is null)
        {
            _setStatus("Önce hazır bir eylem seç.");
            return;
        }

        var selectedProfile = _getSelectedProfile();
        if (selectedProfile is null)
        {
            _setStatus("Önce hedef profili seç.");
            return;
        }

        var action = ActionDefinitionCatalog.CreateActionFromPreset(
            SelectedPreset,
            CreateUniqueActionId(selectedProfile.Actions, NormalizeId(SelectedPreset.Id, "action")));
        selectedProfile.Actions.Add(action);
        RebuildActionRows();
        SelectedAction = ActionRows.FirstOrDefault(candidate => ReferenceEquals(candidate.Action, action));
        _markDirty();
        _setStatus($"{SelectedPreset.Title}, {selectedProfile.Name} profiline eklendi.");
    }

    private void ImportIcon()
    {
        if (SelectedAction is null)
        {
            _setStatus("Önce ikon atanacak bir aksiyon seç.");
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "İkon seç",
            Filter = "İkon dosyaları (*.png;*.jpg;*.jpeg;*.svg)|*.png;*.jpg;*.jpeg;*.svg|Tüm dosyalar (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(_configService.IconDirectory);
            var targetPath = CreateUniqueIconPath(dialog.FileName);
            File.Copy(dialog.FileName, targetPath, overwrite: false);

            var key = $"custom:{Path.GetFileName(targetPath)}";
            if (!IconCatalog.HasIcon(key))
            {
                File.Delete(targetPath);
                _setStatus("Bu SVG desteklenmedi. Path tabanlı SVG veya PNG/JPG kullan.");
                return;
            }

            SelectedAction.Icon = key;
            RefreshAvailableIcons();
            RefreshActionList();
            _markDirty();
            _setStatus($"İkon içe aktarıldı: {Path.GetFileName(targetPath)}");
        }
        catch (Exception ex)
        {
            _setStatus($"İkon eklenemedi: {ex.Message}");
            _logService.Error("Icon import failed.", ex);
        }
    }

    private void BrowseActionTarget()
    {
        if (SelectedAction is null)
        {
            _setStatus("Önce bir aksiyon seç.");
            return;
        }

        try
        {
            switch (SelectedAction.Type)
            {
                case "open_app":
                    BrowseFileForSelectedAction(
                        "Uygulama seç",
                        "Uygulamalar (*.exe)|*.exe|Tüm dosyalar (*.*)|*.*");
                    break;
                case "open_file":
                    BrowseFileForSelectedAction("Dosya seç", "Tüm dosyalar (*.*)|*.*");
                    break;
                case "open_folder":
                    BrowseFolderForSelectedAction();
                    break;
                default:
                    _setStatus("Bu aksiyon türü için gözat seçeneği yok.");
                    break;
            }
        }
        catch (Exception ex)
        {
            _setStatus($"Hedef seçilemedi: {ex.Message}");
            _logService.Error("Action target browse failed.", ex);
        }
    }

    private async void TestSelectedAction()
    {
        if (SelectedAction is null)
        {
            _setStatus("Önce test edilecek aksiyonu seç.");
            return;
        }

        var validation = ActionValidationService.Validate(SelectedAction.Action);
        if (!validation.IsValid)
        {
            _setStatus($"Aksiyon test edilemedi: {validation.Message}");
            return;
        }

        if (SelectedAction.IsFolder)
        {
            _setStatus("Klasör aksiyonları önizleme halkasında açılır.");
            _showOverlay();
            return;
        }

        try
        {
            var title = SelectedAction.Title;
            var result = await _actionExecutionService.ExecuteAsync(SelectedAction.Action);
            _setStatus(result.Succeeded
                ? $"Aksiyon test edildi: {title}"
                : $"Aksiyon çalışmadı: {result.Message}");
        }
        catch (Exception ex)
        {
            _setStatus($"Aksiyon çalışmadı: {ex.Message}");
            _logService.Error("Action test failed.", ex);
        }
    }

    private void AddActionTo(List<OrbitAction>? actions, string type)
    {
        if (actions is null)
        {
            _setStatus("Önce bir profil seç.");
            return;
        }

        var action = new OrbitAction
        {
            Id = CreateUniqueActionId(actions, type == "folder" ? "folder" : "action"),
            Title = type == "folder" ? "Yeni Klasör" : "Yeni Aksiyon",
            Icon = type == "folder" ? "folder" : "app",
            Type = type,
            Target = "",
            Arguments = ""
        };

        actions.Add(action);
        RebuildActionRows();
        SelectedAction = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, action));
        _markDirty();
        _setStatus(type == "folder" ? "Klasör eklendi." : "Aksiyon eklendi.");
    }

    private void DeleteAction()
    {
        if (SelectedAction is null)
        {
            return;
        }

        var action = SelectedAction;
        var owner = action.Owner;
        var index = owner.IndexOf(action.Action);
        var confirmation = System.Windows.MessageBox.Show(
            $"{action.Title} aksiyonu silinsin mi?",
            "Aksiyon sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            _setStatus("Aksiyon silme iptal edildi.");
            return;
        }

        owner.Remove(action.Action);
        RebuildActionRows();
        _registerUndo($"{action.Title} silme", () =>
        {
            owner.Insert(Math.Clamp(index, 0, owner.Count), action.Action);
            RebuildActionRows();
            SelectedAction = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, action.Action));
        });
        _markDirty();
        _setStatus("Aksiyon silindi.");
    }

    private void MoveSelectedAction(int direction)
    {
        if (SelectedAction is null)
        {
            return;
        }

        var owner = SelectedAction.Owner;
        var index = owner.IndexOf(SelectedAction.Action);
        var targetIndex = index + direction;
        if (index < 0 || targetIndex < 0 || targetIndex >= owner.Count)
        {
            return;
        }

        owner.RemoveAt(index);
        owner.Insert(targetIndex, SelectedAction.Action);
        var moved = SelectedAction.Action;
        RebuildActionRows();
        SelectedAction = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, moved));
        _registerUndo($"{moved.Title} sıralama", () =>
        {
            owner.Remove(moved);
            owner.Insert(Math.Clamp(index, 0, owner.Count), moved);
            RebuildActionRows();
            SelectedAction = ActionRows.FirstOrDefault(row => ReferenceEquals(row.Action, moved));
        });
        _markDirty();
    }

    private void BrowseFileForSelectedAction(string title, string filter)
    {
        if (SelectedAction is null)
        {
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = filter,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        SelectedAction.Target = dialog.FileName;
        RefreshSelectedActionState();
        _markDirty();
        _setStatus("Aksiyon hedefi güncellendi.");
    }

    private void BrowseFolderForSelectedAction()
    {
        if (SelectedAction is null)
        {
            return;
        }

        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Klasör seç",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        SelectedAction.Target = dialog.SelectedPath;
        RefreshSelectedActionState();
        _markDirty();
        _setStatus("Klasör hedefi güncellendi.");
    }

    private void RefreshSelectedActionState()
    {
        OnPropertyChanged(nameof(HasSelectedAction));
        OnPropertyChanged(nameof(HasNoSelectedAction));
        OnPropertyChanged(nameof(SelectedActionIsFolder));
        OnPropertyChanged(nameof(SelectedActionIsChild));
        OnPropertyChanged(nameof(CanMoveSelectedActionOutOfFolder));
        OnPropertyChanged(nameof(SelectedActionFolderSummary));
        OnPropertyChanged(nameof(SelectedActionParentSummary));
        OnPropertyChanged(nameof(CanBrowseSelectedAction));
        OnPropertyChanged(nameof(SelectedActionValidationMessage));
        OnPropertyChanged(nameof(HasSelectedActionValidation));
    }

    private void RefreshActionList() =>
        System.Windows.Data.CollectionViewSource.GetDefaultView(ActionRows)?.Refresh();

    private string CreateUniqueIconPath(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var baseName = NormalizeId(Path.GetFileNameWithoutExtension(sourcePath), "icon");
        var fileName = $"{baseName}{extension}";
        var targetPath = Path.Combine(_configService.IconDirectory, fileName);
        var index = 2;

        while (File.Exists(targetPath))
        {
            fileName = $"{baseName}_{index}{extension}";
            targetPath = Path.Combine(_configService.IconDirectory, fileName);
            index++;
        }

        return targetPath;
    }

    private static string CreateUniqueActionId(
        List<OrbitAction> actions,
        string prefix,
        OrbitAction? ignoredAction = null)
    {
        var index = actions.Count + 1;
        var id = $"{prefix}_{index}";
        while (actions.Any(action =>
            !ReferenceEquals(action, ignoredAction) &&
            string.Equals(action.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            index++;
            id = $"{prefix}_{index}";
        }

        return id;
    }

    private static string NormalizeId(string value, string fallback)
    {
        var normalized = new string((value ?? "")
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray())
            .Trim('_');

        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
