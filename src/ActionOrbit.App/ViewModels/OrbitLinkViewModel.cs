using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ActionOrbit.App.Commands;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.ViewModels;

public sealed class OrbitLinkViewModel : ViewModelBase, IDisposable
{
    private readonly OrbitLinkService _service;
    private readonly Action<string> _setStatus;
    private string _deviceName;
    private string _pairAddress = "";
    private string _pairCodeInput = "";
    private string _pairingCode = "";
    private string _pairingAddress = "";
    private string _pairingExpiry = "";
    private string _connectionStatus = "Orbit Link kapalı";
    private bool _isBusy;
    private bool _isSyncing;
    private readonly DispatcherTimer _pairingTimer;
    private DateTime _pairingExpiresUtc;

    public OrbitLinkViewModel(OrbitLinkService service, Action<string> setStatus)
    {
        _service = service;
        _setStatus = setStatus;
        _deviceName = service.DeviceName;
        BeginPairingCommand = new RelayCommand(BeginPairing, () => !IsBusy);
        CopyPairingDetailsCommand = new RelayCommand(CopyPairingDetails, () => HasPairingOffer);
        PairDeviceCommand = new RelayCommand(PairDevice, () => !IsBusy);
        RemovePeerCommand = new RelayCommand(parameter => RemovePeer(parameter as OrbitLinkPeerViewModel));
        _service.StateChanged += Service_StateChanged;
        _pairingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _pairingTimer.Tick += PairingTimer_Tick;
        RefreshFromService();
    }

    public ObservableCollection<OrbitLinkPeerViewModel> Peers { get; } = [];
    public ICommand BeginPairingCommand { get; }
    public ICommand CopyPairingDetailsCommand { get; }
    public ICommand PairDeviceCommand { get; }
    public ICommand RemovePeerCommand { get; }

    public bool Enabled
    {
        get => _service.Enabled;
        set
        {
            if (_isSyncing || value == _service.Enabled) return;
            SetEnabled(value);
        }
    }

    public string DeviceName
    {
        get => _deviceName;
        set
        {
            if (!SetProperty(ref _deviceName, value ?? "") || _isSyncing) return;
            _service.UpdateDeviceName(_deviceName);
        }
    }

    public string PairAddress
    {
        get => _pairAddress;
        set => SetProperty(ref _pairAddress, value ?? "");
    }

    public string PairCodeInput
    {
        get => _pairCodeInput;
        set => SetProperty(ref _pairCodeInput, value ?? "");
    }

    public string PairingCode
    {
        get => _pairingCode;
        private set
        {
            if (SetProperty(ref _pairingCode, value)) OnPropertyChanged(nameof(HasPairingOffer));
        }
    }

    public string PairingAddress
    {
        get => _pairingAddress;
        private set => SetProperty(ref _pairingAddress, value);
    }

    public string PairingExpiry
    {
        get => _pairingExpiry;
        private set => SetProperty(ref _pairingExpiry, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetProperty(ref _connectionStatus, value);
    }

    public bool HasPairingOffer => PairingCode.Length > 0;
    public bool HasPeers => Peers.Count > 0;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            RaiseCommandStates();
        }
    }

    private async void SetEnabled(bool enabled)
    {
        if (IsBusy) return;
        IsBusy = true;
        var result = await _service.SetEnabledAsync(enabled);
        IsBusy = false;
        RefreshFromService();
        _setStatus(result.Message);
    }

    private void BeginPairing()
    {
        try
        {
            var offer = _service.BeginPairing();
            PairingCode = offer.Code;
            PairingAddress = offer.Address;
            _pairingExpiresUtc = offer.ExpiresUtc;
            UpdatePairingExpiry();
            _pairingTimer.Start();
            RaiseCommandStates();
            _setStatus("Tek kullanımlık Orbit Link eşleştirme kodu oluşturuldu.");
        }
        catch (Exception ex)
        {
            _setStatus(ex.Message);
        }
    }

    private void CopyPairingDetails()
    {
        if (!HasPairingOffer) return;
        try
        {
            System.Windows.Clipboard.SetText($"Orbit Link\nAdres: {PairingAddress}\nKod: {PairingCode}");
            _setStatus("Eşleştirme bilgileri panoya kopyalandı; RDP oturumuna yapıştırabilirsin.");
        }
        catch (Exception ex)
        {
            _setStatus($"Panoya kopyalanamadı: {ex.Message}");
        }
    }

    private async void PairDevice()
    {
        if (IsBusy) return;
        IsBusy = true;
        var result = await _service.PairAsync(PairAddress, PairCodeInput);
        IsBusy = false;
        if (result.Succeeded)
        {
            PairCodeInput = "";
            RefreshFromService();
        }
        _setStatus(result.Message);
    }

    private void RemovePeer(OrbitLinkPeerViewModel? peer)
    {
        if (peer is null) return;
        _service.RemovePeer(peer.Id);
        _setStatus($"{peer.Name} eşleşmesi kaldırıldı.");
    }

    private void Service_StateChanged(object? sender, EventArgs e)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(RefreshFromService);
            return;
        }
        RefreshFromService();
    }

    private void RefreshFromService()
    {
        _isSyncing = true;
        try
        {
            _deviceName = _service.DeviceName;
            OnPropertyChanged(nameof(DeviceName));
            OnPropertyChanged(nameof(Enabled));
            Peers.Clear();
            foreach (var peer in _service.Peers)
            {
                Peers.Add(new OrbitLinkPeerViewModel(peer, _service.HasReverseRoute(peer.Id)));
            }
            OnPropertyChanged(nameof(HasPeers));
            if (!_service.HasActivePairing && HasPairingOffer)
            {
                ClearPairingOffer();
            }
            ConnectionStatus = _service.Enabled
                ? _service.IsRunning
                    ? $"Yerel ağda hazır · {_service.LocalAddressSummary}"
                    : "Etkin, fakat dinleyici başlatılamadı"
                : "Orbit Link kapalı";
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void RaiseCommandStates()
    {
        (BeginPairingCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CopyPairingDetailsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (PairDeviceCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void PairingTimer_Tick(object? sender, EventArgs e)
    {
        if (_pairingExpiresUtc <= DateTime.UtcNow || !_service.HasActivePairing)
        {
            ClearPairingOffer();
            return;
        }
        UpdatePairingExpiry();
    }

    private void UpdatePairingExpiry()
    {
        var remaining = _pairingExpiresUtc - DateTime.UtcNow;
        PairingExpiry = $"{Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))} dakika geçerli";
    }

    private void ClearPairingOffer()
    {
        _pairingTimer.Stop();
        _pairingExpiresUtc = DateTime.MinValue;
        PairingCode = "";
        PairingAddress = "";
        PairingExpiry = "";
        RaiseCommandStates();
    }

    public void Dispose()
    {
        _pairingTimer.Stop();
        _pairingTimer.Tick -= PairingTimer_Tick;
        _service.StateChanged -= Service_StateChanged;
    }
}
