using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Tests;

public sealed class AutosaveViewModelTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"action-orbit-autosave-tests-{Guid.NewGuid():N}");

    [Fact]
    public void MarkDirtyAndFlush_SavesConfigAndClearsPendingState()
    {
        var persistence = new FakeConfigPersistence();
        var afterSaveCount = 0;
        var status = "";
        var tone = StatusTone.Info;
        using var viewModel = CreateViewModel(
            persistence,
            (message, statusTone) => (status, tone) = (message, statusTone),
            () => afterSaveCount++);

        viewModel.MarkDirty();

        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal("Değişiklikler bekliyor", viewModel.StateText);
        Assert.Equal(StatusTone.Warning, tone);
        Assert.True(viewModel.FlushPendingChanges());
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.LastSaveFailed);
        Assert.Equal(1, persistence.SaveCallCount);
        Assert.Equal(1, afterSaveCount);
        Assert.Equal("Otomatik kaydedildi: 12:34:56", status);
        Assert.Equal(StatusTone.Success, tone);
    }

    [Fact]
    public void Flush_WhenPersistenceFails_KeepsPendingStateAndShowsError()
    {
        var persistence = new FakeConfigPersistence
        {
            SaveException = new IOException("disk unavailable")
        };
        var status = "";
        var tone = StatusTone.Info;
        using var viewModel = CreateViewModel(
            persistence,
            (message, statusTone) => (status, tone) = (message, statusTone),
            () => { });
        viewModel.MarkDirty();

        Assert.False(viewModel.FlushPendingChanges());

        Assert.True(viewModel.HasUnsavedChanges);
        Assert.True(viewModel.LastSaveFailed);
        Assert.Equal("Kaydetme hatası", viewModel.StateText);
        Assert.Contains("disk unavailable", status);
        Assert.Equal(StatusTone.Error, tone);
    }

    [Fact]
    public void SaveNow_WhenNothingIsDirty_StillPerformsExplicitSave()
    {
        var persistence = new FakeConfigPersistence();
        var status = "";
        using var viewModel = CreateViewModel(
            persistence,
            (message, _) => status = message,
            () => { });

        Assert.True(viewModel.SaveNow());

        Assert.Equal(1, persistence.SaveCallCount);
        Assert.Equal("Config kaydedildi.", status);
    }

    private AutosaveViewModel CreateViewModel(
        IConfigPersistence persistence,
        Action<string, StatusTone> setStatus,
        Action afterSave)
    {
        Directory.CreateDirectory(_tempDirectory);
        return new AutosaveViewModel(
            persistence,
            new LogService(_tempDirectory),
            setStatus,
            afterSave,
            now: () => new DateTime(2026, 7, 22, 12, 34, 56),
            startTimer: false);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private sealed class FakeConfigPersistence : IConfigPersistence
    {
        public AppConfig CurrentConfig { get; } = DefaultConfigFactory.Create();
        public Exception? SaveException { get; set; }
        public int SaveCallCount { get; private set; }

        public void Save(AppConfig config)
        {
            SaveCallCount++;
            if (SaveException is not null)
            {
                throw SaveException;
            }
        }
    }
}
