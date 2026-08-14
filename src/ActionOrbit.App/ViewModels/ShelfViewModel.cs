using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using WpfClipboard = System.Windows.Clipboard;
using WpfDataFormats = System.Windows.DataFormats;

namespace ActionOrbit.App.ViewModels;

public sealed class ShelfViewModel : ViewModelBase, IDisposable
{
    private readonly ConfigService _configService;
    private readonly LogService _logService;
    private readonly Action<string> _setStatus;
    private readonly string _cacheDirectory;
    private readonly SafeRemoteImageService _remoteImages;
    private readonly ShelfDropService _dropService;
    private readonly ShelfPersistenceService _persistence;
    private readonly ImageProcessingService _imageProcessing;
    private readonly OrbitLinkService? _orbitLinkService;
    private ShelfBoardViewModel? _selectedShelf;
    private OrbitLinkPeerViewModel? _selectedPeer;
    private Action? _showFloatingShelf;
    private bool _isImporting;

    public ShelfViewModel(
        ConfigService configService,
        LogService logService,
        Action<string> setStatus,
        OrbitLinkService? orbitLinkService = null)
    {
        _configService = configService;
        _logService = logService;
        _setStatus = setStatus;
        _cacheDirectory = Path.Combine(configService.AppDirectory, "shelf-cache");
        _remoteImages = new SafeRemoteImageService(logService);
        _dropService = new ShelfDropService(_remoteImages, _cacheDirectory);
        _persistence = new ShelfPersistenceService(configService.AppDirectory, logService);
        _imageProcessing = new ImageProcessingService(_cacheDirectory);
        _orbitLinkService = orbitLinkService;

        NewShelfCommand = new RelayCommand(NewShelf);
        DeleteShelfCommand = new RelayCommand(DeleteShelf);
        TogglePinCommand = new RelayCommand(TogglePin);
        ClearShelfCommand = new RelayCommand(ClearShelf);
        CopyItemCommand = new RelayCommand(parameter => CopyItem(parameter as ShelfItemViewModel));
        SaveItemCommand = new RelayCommand(parameter => SaveItem(parameter as ShelfItemViewModel));
        RemoveItemCommand = new RelayCommand(parameter => RemoveItem(parameter as ShelfItemViewModel));
        ConvertToPngCommand = new RelayCommand(parameter => ProcessImage(parameter as ShelfItemViewModel, resize: false));
        ResizeImageCommand = new RelayCommand(parameter => ProcessImage(parameter as ShelfItemViewModel, resize: true));
        OpenFloatingShelfCommand = new RelayCommand(() => _showFloatingShelf?.Invoke());
        SendItemCommand = new RelayCommand(parameter => SendItem(parameter as ShelfItemViewModel));
        RetryTransferCommand = new RelayCommand(parameter => RetryTransfer(parameter as ShelfItemViewModel));
        CancelTransferCommand = new RelayCommand(parameter => CancelTransfer(parameter as ShelfItemViewModel));
        ToggleSharedShelfCommand = new RelayCommand(ToggleSharedShelf);

        if (_orbitLinkService is not null)
        {
            _orbitLinkService.StateChanged += OrbitLink_StateChanged;
            _orbitLinkService.ItemReceived += OrbitLink_ItemReceived;
            _orbitLinkService.TransferStatusChanged += OrbitLink_TransferStatusChanged;
            RefreshPeers();
        }

        Directory.CreateDirectory(_cacheDirectory);
        foreach (var board in _persistence.Load(Settings))
        {
            Shelves.Add(CreateShelf(board));
        }

        ReconcilePendingTransfers();

        CleanupExpiredCache();

        if (Shelves.Count == 0)
        {
            Shelves.Add(CreateShelf("Hızlı Raf"));
        }

        SelectedShelf = Shelves[0];
    }

    public ObservableCollection<ShelfBoardViewModel> Shelves { get; } = [];
    public ObservableCollection<OrbitLinkPeerViewModel> OrbitLinkPeers { get; } = [];
    public ICommand NewShelfCommand { get; }
    public ICommand DeleteShelfCommand { get; }
    public ICommand TogglePinCommand { get; }
    public ICommand ClearShelfCommand { get; }
    public ICommand CopyItemCommand { get; }
    public ICommand SaveItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand ConvertToPngCommand { get; }
    public ICommand ResizeImageCommand { get; }
    public ICommand OpenFloatingShelfCommand { get; }
    public ICommand SendItemCommand { get; }
    public ICommand RetryTransferCommand { get; }
    public ICommand CancelTransferCommand { get; }
    public ICommand ToggleSharedShelfCommand { get; }
    public ShelfSettings Settings => _configService.CurrentConfig.Settings.Shelf;

    public ShelfBoardViewModel? SelectedShelf
    {
        get => _selectedShelf;
        set
        {
            if (SetProperty(ref _selectedShelf, value))
            {
                value?.Touch();
                OnPropertyChanged(nameof(HasSelectedShelf));
                OnPropertyChanged(nameof(SelectedShelfSummary));
                OnPropertyChanged(nameof(SelectedShelfPinButtonText));
                OnPropertyChanged(nameof(SelectedShelfSharedButtonText));
            }
        }
    }

    public OrbitLinkPeerViewModel? SelectedPeer
    {
        get => _selectedPeer;
        set => SetProperty(ref _selectedPeer, value);
    }

    public bool HasSelectedShelf => SelectedShelf is not null;
    public bool IsImporting
    {
        get => _isImporting;
        private set => SetProperty(ref _isImporting, value);
    }
    public string SelectedShelfSummary => SelectedShelf is null
        ? "Raf seçilmedi"
        : $"{SelectedShelf.ItemCount}/{Settings.MaxItemsPerShelf} öğe · {FormatBytes(SelectedShelf.TotalBytes)}";
    public string SelectedShelfPinButtonText => SelectedShelf?.IsPinned == true
        ? "Sabitlemeyi Kaldır"
        : "Sabitle";
    public string SelectedShelfSharedButtonText => SelectedShelf?.IsShared == true
        ? "Ortak Raf Açık"
        : "Ortak Raf";
    public bool HasOrbitLinkPeers => _orbitLinkService?.Enabled == true && OrbitLinkPeers.Count > 0;
    public string OrbitLinkSummary => HasOrbitLinkPeers
        ? $"{OrbitLinkPeers.Count} eşleşen cihaz"
        : "Cihaz eşleştirmek için Ayarlar › Orbit Link";

    public void SetFloatingShelfOpener(Action showFloatingShelf) => _showFloatingShelf = showFloatingShelf;

    public void RefreshSettings()
    {
        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(SelectedShelfSummary));
        CleanupExpiredCache();
    }

    public async Task HandleDropAsync(System.Windows.IDataObject data)
    {
        if (!Settings.Enabled)
        {
            _setStatus("Orbit Shelf ayarlardan devre dışı bırakılmış.");
            return;
        }

        if (SelectedShelf is null || IsImporting)
        {
            return;
        }

        try
        {
            IsImporting = true;
            var formatNames = data.GetFormats(autoConvert: false)
                .Where(format => !string.IsNullOrWhiteSpace(format))
                .Take(20)
                .Select(format => LogService.SafeValue(format));
            _logService.Info($"Shelf drop formats: {string.Join(", ", formatNames)}.");
            var result = await _dropService.ImportAsync(
                data,
                Settings,
                Settings.MaxItemsPerShelf - SelectedShelf.ItemCount,
                Settings.MaxTotalBytes - SelectedShelf.TotalBytes);
            if (!result.Succeeded)
            {
                _setStatus($"Rafa eklenemedi: {result.Message}");
                return;
            }

            foreach (var item in result.Items)
            {
                SelectedShelf.Add(item);
            }

            Persist();
            OnPropertyChanged(nameof(SelectedShelfSummary));
            if (SelectedShelf.IsShared && _orbitLinkService is not null && OrbitLinkPeers.Count > 0)
            {
                await ShareItemsAsync(result.Items);
            }
            var skipped = result.SkippedCount > 0 ? $" {result.SkippedCount} öğe sınırlar nedeniyle atlandı." : "";
            _setStatus($"Orbit Shelf'e {result.Items.Count} öğe eklendi.{skipped}");
        }
        catch (Exception ex)
        {
            _logService.Error("Shelf drop failed.", ex);
            _setStatus($"Rafa eklenemedi: {ex.Message}");
        }
        finally
        {
            IsImporting = false;
        }
    }

    public System.Windows.DataObject BuildDragData(ShelfItemViewModel item)
    {
        var data = new System.Windows.DataObject();
        var model = item.Item;
        if (item.HasLocalPath)
        {
            data.SetData(WpfDataFormats.FileDrop, new[] { model.LocalPath });
        }

        if (item.IsImage)
        {
            try
            {
                data.SetImage(ImageProcessingService.LoadFrame(model.LocalPath));
            }
            catch (Exception ex)
            {
                _logService.Error("Shelf bitmap drag preparation failed.", ex);
            }
        }

        var text = model.Kind is "text" or "url" ? model.TextContent : model.Source;
        if (!string.IsNullOrWhiteSpace(text))
        {
            data.SetData(WpfDataFormats.UnicodeText, text);
            data.SetData(WpfDataFormats.Text, text);
        }

        return data;
    }

    private void NewShelf()
    {
        var shelf = CreateShelf($"Raf {Shelves.Count + 1}");
        Shelves.Add(shelf);
        SelectedShelf = shelf;
        Persist();
        _setStatus("Yeni Orbit Shelf oluşturuldu.");
    }

    private void DeleteShelf()
    {
        if (SelectedShelf is null)
        {
            return;
        }

        var removing = SelectedShelf;
        if (Shelves.Count == 1)
        {
            ClearShelf();
            removing.Name = "Hızlı Raf";
            removing.IsPinned = false;
            return;
        }

        foreach (var item in removing.Items.ToList())
        {
            TryDeleteTemporaryItem(item.Item, removing);
        }

        var index = Shelves.IndexOf(removing);
        removing.PropertyChanged -= Shelf_PropertyChanged;
        Shelves.Remove(removing);
        SelectedShelf = Shelves[Math.Clamp(index, 0, Shelves.Count - 1)];
        Persist();
        _setStatus("Raf silindi.");
    }

    private void TogglePin()
    {
        if (SelectedShelf is null)
        {
            return;
        }

        SelectedShelf.IsPinned = !SelectedShelf.IsPinned;
        _setStatus(SelectedShelf.IsPinned ? "Raf sabitlendi ve yerel olarak korunacak." : "Raf sabitlemesi kaldırıldı.");
    }

    private void ToggleSharedShelf()
    {
        if (SelectedShelf is null) return;
        SelectedShelf.IsShared = !SelectedShelf.IsShared;
        if (SelectedShelf.IsShared) SelectedShelf.IsPinned = true;
        Persist();
        OnPropertyChanged(nameof(SelectedShelfSharedButtonText));
        OnPropertyChanged(nameof(SelectedShelfPinButtonText));
        _setStatus(SelectedShelf.IsShared
            ? "Bu raf Ortak Raf oldu; yeni bırakılan öğeler eşleşen cihazlara gönderilecek."
            : "Ortak Raf paylaşımı kapatıldı.");
    }

    private async void SendItem(ShelfItemViewModel? item)
    {
        if (item is null || _orbitLinkService is null)
        {
            return;
        }
        if (SelectedPeer is null)
        {
            _setStatus("Önce Orbit Link hedef cihazını seç.");
            return;
        }
        _setStatus($"{item.DisplayName} gönderiliyor…");
        var result = await _orbitLinkService.SendItemAsync(SelectedPeer.Id, item.Item);
        _setStatus(result.Message);
    }

    private async void RetryTransfer(ShelfItemViewModel? item)
    {
        if (item is null || _orbitLinkService is null || string.IsNullOrWhiteSpace(item.Item.TransferId)) return;
        _setStatus($"{item.DisplayName} yeniden deneniyor…");
        OrbitLinkOperationResult result;
        if (_orbitLinkService.PendingTransfers.Any(status => string.Equals(
                status.TransferId,
                item.Item.TransferId,
                StringComparison.OrdinalIgnoreCase)))
        {
            result = await _orbitLinkService.RetryTransferAsync(item.Item.TransferId);
        }
        else
        {
            var peerId = item.Item.LastTransferPeerId;
            if (!_orbitLinkService.Peers.Any(peer => string.Equals(peer.Id, peerId, StringComparison.OrdinalIgnoreCase)))
            {
                _setStatus("Yeniden denemek için hedef cihaz artık eşleşmiş değil.");
                return;
            }
            result = await _orbitLinkService.SendItemAsync(peerId, item.Item);
        }
        _setStatus(result.Message);
    }

    private void CancelTransfer(ShelfItemViewModel? item)
    {
        if (item is null || _orbitLinkService is null || string.IsNullOrWhiteSpace(item.Item.TransferId)) return;
        var result = _orbitLinkService.CancelTransfer(item.Item.TransferId);
        _setStatus(result.Message);
    }

    private async Task ShareItemsAsync(IReadOnlyList<ShelfItem> items)
    {
        if (_orbitLinkService is null) return;
        var peers = OrbitLinkPeers.ToList();
        var failures = 0;
        foreach (var item in items)
        {
            foreach (var peer in peers)
            {
                var result = await _orbitLinkService.SendItemAsync(peer.Id, item);
                if (!result.Succeeded) failures++;
            }
        }
        if (failures > 0)
        {
            _setStatus($"Ortak Raf aktarımında {failures} gönderim tamamlanamadı.");
        }
    }

    private void OrbitLink_StateChanged(object? sender, EventArgs e) => RunOnUiThread(RefreshPeers);

    private void RefreshPeers()
    {
        var selectedId = SelectedPeer?.Id;
        OrbitLinkPeers.Clear();
        if (_orbitLinkService is not null)
        {
            foreach (var peer in _orbitLinkService.Peers)
            {
                OrbitLinkPeers.Add(new OrbitLinkPeerViewModel(
                    peer,
                    _orbitLinkService.HasReverseRoute(peer.Id)));
            }
        }
        SelectedPeer = OrbitLinkPeers.FirstOrDefault(peer => peer.Id == selectedId) ?? OrbitLinkPeers.FirstOrDefault();
        OnPropertyChanged(nameof(HasOrbitLinkPeers));
        OnPropertyChanged(nameof(OrbitLinkSummary));
    }

    private void OrbitLink_ItemReceived(object? sender, OrbitLinkItemReceivedEventArgs e) =>
        RunOnUiThreadSync(() => AddReceivedItem(e));

    private void OrbitLink_TransferStatusChanged(object? sender, OrbitLinkTransferStatusChangedEventArgs e) =>
        RunOnUiThread(() => ApplyTransferStatus(e.Status));

    private void ReconcilePendingTransfers()
    {
        if (_orbitLinkService is null) return;
        foreach (var status in _orbitLinkService.PendingTransfers)
        {
            ApplyTransferStatus(status, persist: false);
        }
    }

    private void ApplyTransferStatus(OrbitLinkTransferStatus status, bool persist = true)
    {
        var item = Shelves
            .SelectMany(shelf => shelf.Items)
            .FirstOrDefault(candidate => string.Equals(candidate.Id, status.ShelfItemId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate.Item.TransferId, status.TransferId, StringComparison.OrdinalIgnoreCase));
        if (item is null) return;
        item.ApplyTransferStatus(status);
        if (persist) Persist();
    }

    private void AddReceivedItem(OrbitLinkItemReceivedEventArgs args)
    {
        if (!Settings.Enabled
            || Shelves.SelectMany(shelf => shelf.Items).Any(item =>
                string.Equals(item.Item.TransferId, args.Item.TransferId, StringComparison.OrdinalIgnoreCase)))
        {
            args.Reject(
                Settings.Enabled ? "Bu aktarım daha önce alınmış." : "Orbit Shelf alıcı bilgisayarda kapalı.",
                isDuplicate: Settings.Enabled);
            return;
        }

        var sharedShelf = Shelves.FirstOrDefault(shelf => shelf.IsShared)
            ?? Shelves.FirstOrDefault(shelf => string.Equals(shelf.Name, "Ortak Raf", StringComparison.OrdinalIgnoreCase));
        if (sharedShelf is null)
        {
            sharedShelf = CreateShelf(new ShelfBoard { Name = "Ortak Raf", IsPinned = true, IsShared = true });
            Shelves.Add(sharedShelf);
        }
        if (sharedShelf.ItemCount >= Settings.MaxItemsPerShelf
            || sharedShelf.TotalBytes + args.Item.SizeBytes > Settings.MaxTotalBytes
            || args.Item.SizeBytes > Settings.MaxItemBytes)
        {
            args.Reject("Alıcı Ortak Raf sınırına ulaştı.");
            _setStatus($"{args.Peer.Name} cihazından gelen öğe raf sınırını aştı.");
            return;
        }

        sharedShelf.Add(args.Item);
        SelectedShelf = sharedShelf;
        Persist();
        OnPropertyChanged(nameof(SelectedShelfSummary));
        _setStatus($"{args.Peer.Name} cihazından Ortak Raf'a eklendi: {args.Item.DisplayName}");
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(action);
            return;
        }
        action();
    }

    private static void RunOnUiThreadSync(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(action);
            return;
        }
        action();
    }

    private void ClearShelf()
    {
        if (SelectedShelf is null)
        {
            return;
        }

        foreach (var item in SelectedShelf.Items.ToList())
        {
            TryDeleteTemporaryItem(item.Item, SelectedShelf);
            SelectedShelf.Remove(item);
        }

        Persist();
        OnPropertyChanged(nameof(SelectedShelfSummary));
        _setStatus("Raf temizlendi.");
    }

    private void CopyItem(ShelfItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            WpfClipboard.SetDataObject(BuildDragData(item), true);
            _setStatus($"Panoya kopyalandı: {item.DisplayName}");
        }
        catch (Exception ex)
        {
            _logService.Error("Shelf clipboard copy failed.", ex);
            _setStatus($"Panoya kopyalanamadı: {ex.Message}");
        }
    }

    private void SaveItem(ShelfItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        if (item.HasLocalPath && File.Exists(item.Item.LocalPath))
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Raf öğesini kaydet",
                FileName = item.DisplayName,
                Filter = "Tüm dosyalar (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                File.Copy(item.Item.LocalPath, dialog.FileName, overwrite: true);
                _setStatus($"Kaydedildi: {dialog.FileName}");
            }
            return;
        }

        if (item.Kind is "text" or "url")
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Metni kaydet",
                FileName = "orbit-shelf.txt",
                Filter = "Metin dosyası (*.txt)|*.txt"
            };
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, item.Item.TextContent);
                _setStatus($"Kaydedildi: {dialog.FileName}");
            }
        }
    }

    private void RemoveItem(ShelfItemViewModel? item)
    {
        if (SelectedShelf is null || item is null)
        {
            return;
        }

        TryDeleteTemporaryItem(item.Item, SelectedShelf);
        SelectedShelf.Remove(item);
        Persist();
        OnPropertyChanged(nameof(SelectedShelfSummary));
    }

    private void ProcessImage(ShelfItemViewModel? source, bool resize)
    {
        if (SelectedShelf is null || source is not { IsImage: true })
        {
            _setStatus("Bu işlem için rafta yerel bir görsel seç.");
            return;
        }

        var result = resize
            ? _imageProcessing.ResizeToFit(source.Item.LocalPath, 1600)
            : _imageProcessing.ConvertToPng(source.Item.LocalPath);
        if (!result.Succeeded)
        {
            _setStatus(result.Message);
            return;
        }

        if (SelectedShelf.ItemCount >= Settings.MaxItemsPerShelf ||
            SelectedShelf.TotalBytes + result.SizeBytes > Settings.MaxTotalBytes)
        {
            try { File.Delete(result.Path); } catch { }
            _setStatus("İşlenen görsel raf sınırını aşıyor.");
            return;
        }

        var suffix = resize ? "-1600px" : "-png";
        SelectedShelf.Add(new ShelfItem
        {
            Kind = "image",
            DisplayName = $"{Path.GetFileNameWithoutExtension(source.DisplayName)}{suffix}.png",
            Source = source.Item.Source,
            LocalPath = result.Path,
            SizeBytes = result.SizeBytes,
            IsTemporary = true
        });
        Persist();
        OnPropertyChanged(nameof(SelectedShelfSummary));
        _setStatus(resize
            ? $"Görsel {result.PixelWidth}×{result.PixelHeight} boyutuna getirildi."
            : "Görsel PNG olarak dönüştürüldü.");
    }

    private ShelfBoardViewModel CreateShelf(string name) => CreateShelf(new ShelfBoard { Name = name });

    private ShelfBoardViewModel CreateShelf(ShelfBoard board)
    {
        var shelf = new ShelfBoardViewModel(board);
        shelf.PropertyChanged += Shelf_PropertyChanged;
        return shelf;
    }

    private void Shelf_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(ShelfBoardViewModel.Name)
            or nameof(ShelfBoardViewModel.IsPinned)
            or nameof(ShelfBoardViewModel.IsShared)))
        {
            return;
        }

        Persist();
        OnPropertyChanged(nameof(SelectedShelfSummary));
        OnPropertyChanged(nameof(SelectedShelfPinButtonText));
        OnPropertyChanged(nameof(SelectedShelfSharedButtonText));
    }

    private void Persist()
    {
        foreach (var shelf in Shelves)
        {
            shelf.Board.Items = shelf.Items.Select(item => item.Item).ToList();
        }
        _persistence.Save(Shelves.Select(shelf => shelf.Board), Settings);
    }

    private void TryDeleteTemporaryItem(ShelfItem item, ShelfBoardViewModel owner)
    {
        if (!item.IsTemporary || string.IsNullOrWhiteSpace(item.LocalPath))
        {
            return;
        }

        var referencedElsewhere = Shelves
            .Where(shelf => !ReferenceEquals(shelf, owner))
            .SelectMany(shelf => shelf.Items)
            .Any(candidate => string.Equals(
                candidate.Item.LocalPath,
                item.LocalPath,
                StringComparison.OrdinalIgnoreCase));
        if (!referencedElsewhere)
        {
            try { File.Delete(item.LocalPath); } catch { }
        }
    }

    private void CleanupExpiredCache()
    {
        try
        {
            var expiry = DateTime.UtcNow.AddHours(-Settings.RetentionHours);
            var retainedPaths = Shelves
                .SelectMany(shelf => shelf.Items)
                .Where(item => item.Item.IsTemporary)
                .Select(item => item.Item.LocalPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.EnumerateFiles(_cacheDirectory))
            {
                if (!retainedPaths.Contains(file) && File.GetLastWriteTimeUtc(file) < expiry)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            _logService.Error("Shelf cache cleanup failed.", ex);
        }
    }

    private static string FormatBytes(long bytes) => bytes >= 1024 * 1024
        ? $"{bytes / (1024d * 1024):0.##} MB"
        : $"{bytes / 1024d:0.##} KB";

    public void Dispose()
    {
        Persist();
        foreach (var shelf in Shelves)
        {
            shelf.PropertyChanged -= Shelf_PropertyChanged;
        }
        _remoteImages.Dispose();
        if (_orbitLinkService is not null)
        {
            _orbitLinkService.StateChanged -= OrbitLink_StateChanged;
            _orbitLinkService.ItemReceived -= OrbitLink_ItemReceived;
            _orbitLinkService.TransferStatusChanged -= OrbitLink_TransferStatusChanged;
        }
    }
}
