namespace ActionOrbit.App.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = "Local\\ActionOrbitPro.SingleInstance";
    private const string ActivationEventName = "Local\\ActionOrbitPro.ActivateExisting";
    private const string ActivationAcknowledgedEventName = "Local\\ActionOrbitPro.ActivateExistingAck";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle? _activationEvent;
    private readonly EventWaitHandle? _activationAcknowledgedEvent;
    private readonly string _activationEventName;
    private readonly string _activationAcknowledgedEventName;
    private RegisteredWaitHandle? _activationRegistration;
    private bool _disposed;

    public SingleInstanceService()
        : this("")
    {
    }

    internal SingleInstanceService(string instanceSuffix)
    {
        var suffix = string.IsNullOrWhiteSpace(instanceSuffix) ? "" : $".{instanceSuffix}";
        _activationEventName = $"{ActivationEventName}{suffix}";
        _activationAcknowledgedEventName = $"{ActivationAcknowledgedEventName}{suffix}";
        _mutex = new Mutex(initiallyOwned: true, $"{MutexName}{suffix}", out var createdNew);
        IsPrimaryInstance = createdNew;

        if (IsPrimaryInstance)
        {
            _activationEvent = new EventWaitHandle(
                initialState: false,
                EventResetMode.AutoReset,
                _activationEventName);
            _activationAcknowledgedEvent = new EventWaitHandle(
                initialState: false,
                EventResetMode.AutoReset,
                _activationAcknowledgedEventName);
        }
    }

    public bool IsPrimaryInstance { get; }

    public void StartListening(Action activationRequested)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsPrimaryInstance || _activationEvent is null || _activationRegistration is not null)
        {
            return;
        }

        _activationRegistration = ThreadPool.RegisterWaitForSingleObject(
            _activationEvent,
            (_, timedOut) =>
            {
                if (!timedOut && !_disposed)
                {
                    try
                    {
                        activationRequested();
                    }
                    finally
                    {
                        _activationAcknowledgedEvent?.Set();
                    }
                }
            },
            state: null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    public bool SignalPrimaryInstance(TimeSpan? acknowledgementTimeout = null)
    {
        if (IsPrimaryInstance || _disposed)
        {
            return false;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                using var activationEvent = EventWaitHandle.OpenExisting(_activationEventName);
                using var acknowledgedEvent = EventWaitHandle.OpenExisting(_activationAcknowledgedEventName);
                acknowledgedEvent.Reset();
                if (!activationEvent.Set())
                {
                    return false;
                }
                return acknowledgedEvent.WaitOne(acknowledgementTimeout ?? TimeSpan.FromSeconds(2));
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                Thread.Sleep(50);
            }
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _activationRegistration?.Unregister(null);
        _activationEvent?.Dispose();
        _activationAcknowledgedEvent?.Dispose();

        if (IsPrimaryInstance)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // The process is already shutting down and no longer owns the mutex.
            }
        }

        _mutex.Dispose();
    }
}
