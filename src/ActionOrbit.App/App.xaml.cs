using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App;

public partial class App : System.Windows.Application
{
    private LogService? _logService;
    private ConfigService? _configService;
    private HotkeyService? _hotkeyService;
    private SingleInstanceService? _singleInstanceService;
    private MainWindowViewModel? _mainWindowViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceService = new SingleInstanceService();
        if (!_singleInstanceService.IsPrimaryInstance)
        {
            _singleInstanceService.SignalPrimaryInstance();
            Shutdown();
            return;
        }

        _logService = new LogService();
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _configService = new ConfigService(_logService);
        _configService.Load();
        ThemeService.ApplyApplicationTheme(
            _configService.CurrentConfig.Theme.Mode,
            _configService.CurrentConfig.Theme.Accent);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        var activeWindowService = new ActiveWindowService(_logService);
        var profileService = new ProfileService(_logService);
        var startupService = new StartupService(_logService);
        var startupSyncIssue = SyncStartupRegistration(startupService);
        var inputService = new InputSimulationService(_logService);
        var confirmationService = new MessageBoxConfirmationService();

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
        mainWindow.Show();
        if (!string.IsNullOrWhiteSpace(startupSyncIssue))
        {
            viewModel.Status.ReportFailure(startupSyncIssue);
        }

        _singleInstanceService.StartListening(() =>
            Dispatcher.BeginInvoke(mainWindow.RestoreFromExternalRequest));

        _logService.Info("Action Orbit started.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _hotkeyService?.Dispose();
        _singleInstanceService?.Dispose();
        _logService?.Info("Action Orbit stopped.");
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logService?.Error("Unhandled UI exception.", e.Exception);
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
}
