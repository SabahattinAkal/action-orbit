namespace ActionOrbit.App.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = "Local\\ActionOrbit.SingleInstance";
    private const string ActivationEventName = "Local\\ActionOrbit.ActivateExisting";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle? _activationEvent;
    private RegisteredWaitHandle? _activationRegistration;
    private bool _disposed;

    public SingleInstanceService()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        IsPrimaryInstance = createdNew;

        if (IsPrimaryInstance)
        {
            _activationEvent = new EventWaitHandle(
                initialState: false,
                EventResetMode.AutoReset,
                ActivationEventName);
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
                    activationRequested();
                }
            },
            state: null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    public bool SignalPrimaryInstance()
    {
        if (IsPrimaryInstance || _disposed)
        {
            return false;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                using var activationEvent = EventWaitHandle.OpenExisting(ActivationEventName);
                return activationEvent.Set();
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
