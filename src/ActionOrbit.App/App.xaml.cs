using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.Services.MiniTools;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App;

public partial class App : System.Windows.Application
{
    private LogService? _logService;
    private ConfigService? _configService;
    private HotkeyService? _hotkeyService;
    private SingleInstanceService? _singleInstanceService;
    private MainWindowViewModel? _mainWindowViewModel;
    private MiniToolWindowService? _miniToolWindowService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ReleaseSmokeOptions? releaseSmoke;
        try
        {
            releaseSmoke = ReleaseSmokeOptions.Parse(e.Args);
        }
        catch
        {
            Shutdown(2);
            return;
        }

        if (releaseSmoke is null)
        {
            _singleInstanceService = new SingleInstanceService();
            if (!_singleInstanceService.IsPrimaryInstance)
            {
                if (!_singleInstanceService.SignalPrimaryInstance())
                {
                    System.Windows.MessageBox.Show(
                        "Action Orbit zaten çalışıyor ancak mevcut pencere yanıt vermedi.\n\n" +
                        "Bildirim alanındaki Action Orbit simgesinden Çıkış'ı seç veya Görev Yöneticisi'nde " +
                        "ActionOrbit.App.exe işlemini sonlandır; ardından bu sürümü yeniden aç.",
                        "Action Orbit zaten çalışıyor",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                Shutdown();
                return;
            }
        }

        _logService = new LogService(releaseSmoke?.AppDirectory);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _configService = new ConfigService(_logService, releaseSmoke?.AppDirectory);
        _configService.Load();
        ThemeService.ApplyApplicationTheme(
            _configService.CurrentConfig.Theme.Mode,
            _configService.CurrentConfig.Theme.Accent);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        var activeWindowService = new ActiveWindowService(_logService);
        var profileService = new ProfileService(_logService);
        var startupService = new StartupService(_logService);
        var startupSyncIssue = releaseSmoke is null ? SyncStartupRegistration(startupService) : null;
        var inputService = new InputSimulationService(_logService);
        var confirmationService = new MessageBoxConfirmationService();
        _miniToolWindowService = new MiniToolWindowService();

        var actionExecutionService = new ActionExecutionService(
            _logService,
            new IActionHandler[]
            {
                new OpenUrlActionHandler(_logService),
                new OpenAppActionHandler(
                    _logService,
                    (target, arguments) => confirmationService.Confirm(
                        "Uygulama argümanları onayı",
                        $"Aşağıdaki uygulama argümanlarla başlatılacak:\n\n{target}\n{arguments}\n\nDevam edilsin mi?")),
                new OpenFileActionHandler(_logService),
                new OpenFolderActionHandler(_logService),
                new MiniToolActionHandler(_miniToolWindowService),
                new SendHotkeyActionHandler(inputService),
                new TypeTextActionHandler(inputService),
                new RunCommandActionHandler(
                    _logService,
                    () => _configService!.CurrentConfig.Settings.AllowCommandActions,
                    command => confirmationService.Confirm(
                        "Komut çalıştırma onayı",
                        $"Aşağıdaki komut mevcut kullanıcı yetkileriyle çalıştırılacak:\n\n{command}\n\nDevam edilsin mi?"))
            });

        var overlayService = new OverlayService(
            _configService,
            activeWindowService,
            profileService,
            actionExecutionService,
            _logService);

        _hotkeyService = new HotkeyService(_logService);
        var viewModel = new MainWindowViewModel(
            _configService,
            _hotkeyService,
            activeWindowService,
            profileService,
            overlayService,
            actionExecutionService,
            startupService,
            _logService,
            confirmationService);
        _mainWindowViewModel = viewModel;

        var mainWindow = new MainWindow(viewModel, _hotkeyService);
        MainWindow = mainWindow;
        if (releaseSmoke is not null)
        {
            mainWindow.ShowActivated = false;
            mainWindow.ShowInTaskbar = false;
            mainWindow.Opacity = 0;
            mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            mainWindow.Left = -32000;
            mainWindow.Top = -32000;
            mainWindow.ContentRendered += (_, _) => RunReleaseSmoke(mainWindow, releaseSmoke);
        }
        mainWindow.Show();
        if (!string.IsNullOrWhiteSpace(startupSyncIssue))
        {
            viewModel.Status.ReportFailure(startupSyncIssue);
        }

        _singleInstanceService?.StartListening(() =>
            Dispatcher.BeginInvoke(mainWindow.RestoreFromExternalRequest));

        _logService.Info("Action Orbit started.");
    }

    private async void RunReleaseSmoke(MainWindow mainWindow, ReleaseSmokeOptions options)
    {
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        var checks = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        string? error = null;
        try
        {
            foreach (var check in mainWindow.RunReleaseSmokeChecks())
            {
                checks[check.Key] = check.Value;
            }
        }
        catch (Exception ex)
        {
            error = ex.GetBaseException().GetType().Name;
            _logService?.Error("Release smoke check failed.", ex);
        }

        var succeeded = error is null && checks.Count >= 4 && checks.Values.All(value => value);
        try
        {
            var report = new ReleaseSmokeReport
            {
                Succeeded = succeeded,
                ProductVersion = GetProductVersion(),
                Checks = checks,
                Error = error ?? ""
            };
            var reportDirectory = Path.GetDirectoryName(options.ReportPath)!;
            Directory.CreateDirectory(reportDirectory);
            var temporaryPath = $"{options.ReportPath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, options.ReportPath, overwrite: true);
        }
        catch (Exception ex)
        {
            succeeded = false;
            _logService?.Error("Release smoke report could not be written.", ex);
        }

        Environment.ExitCode = succeeded ? 0 : 1;
        mainWindow.ExitFromTrayForReleaseSmoke();
    }

    private static string GetProductVersion()
    {
        var processPath = Environment.ProcessPath;
        return string.IsNullOrWhiteSpace(processPath)
            ? "unknown"
            : FileVersionInfo.GetVersionInfo(processPath).ProductVersion ?? "unknown";
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _hotkeyService?.Dispose();
        _miniToolWindowService?.Dispose();
        _singleInstanceService?.Dispose();
        _logService?.Info("Action Orbit stopped.");
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logService?.Error("Unhandled UI exception.", e.Exception);
        if (MainWindow is null || !MainWindow.IsLoaded)
        {
            var logPath = _logService?.LogPath ?? "%AppData%\\ActionOrbitPro\\logs\\actionorbit.log";
            System.Windows.MessageBox.Show(
                "Action Orbit başlatılırken bir arayüz hatası oluştu ve uygulama güvenli biçimde kapatılacak.\n\n" +
                $"Hata: {e.Exception.GetBaseException().Message}\n\nLog: {logPath}",
                "Action Orbit başlatılamadı",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
            Shutdown(1);
            return;
        }

        _mainWindowViewModel?.Status.ReportUnexpectedError();
        e.Handled = true;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_configService is null ||
            !string.Equals(_configService.CurrentConfig.Theme.Mode, "system", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Dispatcher.BeginInvoke(() => ThemeService.ApplyApplicationTheme(
            _configService.CurrentConfig.Theme.Mode,
            _configService.CurrentConfig.Theme.Accent));
    }

    private string? SyncStartupRegistration(StartupService startupService)
    {
        try
        {
            startupService.SetEnabled(_configService?.CurrentConfig.Settings.RunAtStartup == true);
            return null;
        }
        catch (Exception ex)
        {
            _logService?.Error("Startup registration sync failed.", ex);
            return $"Windows başlangıç ayarı uygulanamadı: {ex.Message}";
        }
    }

    private sealed class ReleaseSmokeOptions
    {
        public required string ReportPath { get; init; }
        public required string AppDirectory { get; init; }

        public static ReleaseSmokeOptions? Parse(IReadOnlyList<string> arguments)
        {
            var reportIndex = arguments
                .Select((value, index) => (value, index))
                .FirstOrDefault(item => string.Equals(
                    item.value,
                    "--release-smoke-report",
                    StringComparison.OrdinalIgnoreCase));
            if (reportIndex.value is null) return null;
            if (reportIndex.index + 1 >= arguments.Count)
            {
                throw new ArgumentException("Release smoke report path is missing.");
            }

            var appDirectoryIndex = arguments
                .Select((value, index) => (value, index))
                .FirstOrDefault(item => string.Equals(
                    item.value,
                    "--release-smoke-app-directory",
                    StringComparison.OrdinalIgnoreCase));
            if (appDirectoryIndex.value is null || appDirectoryIndex.index + 1 >= arguments.Count)
            {
                throw new ArgumentException("Release smoke app directory is missing.");
            }

            var requestedReportPath = arguments[reportIndex.index + 1];
            var requestedAppDirectory = arguments[appDirectoryIndex.index + 1];
            if (!Path.IsPathFullyQualified(requestedReportPath)
                || !Path.IsPathFullyQualified(requestedAppDirectory))
            {
                throw new ArgumentException("Release smoke paths must be absolute.");
            }
            var reportPath = Path.GetFullPath(requestedReportPath);
            var appDirectory = Path.GetFullPath(requestedAppDirectory);
            return new ReleaseSmokeOptions
            {
                ReportPath = reportPath,
                AppDirectory = appDirectory
            };
        }
    }

    private sealed class ReleaseSmokeReport
    {
        public bool Succeeded { get; set; }
        public string ProductVersion { get; set; } = "";
        public Dictionary<string, bool> Checks { get; set; } = [];
        public string Error { get; set; } = "";
    }
}
