using System.Windows;
using System.Windows.Threading;
using ActionOrbit.App.Services;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App;

public partial class App : System.Windows.Application
{
    private LogService? _logService;
    private ConfigService? _configService;
    private HotkeyService? _hotkeyService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _logService = new LogService();
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _configService = new ConfigService(_logService);
        _configService.Load();

        var activeWindowService = new ActiveWindowService(_logService);
        var profileService = new ProfileService(_logService);
        var startupService = new StartupService(_logService);
        SyncStartupRegistration(startupService);
        var inputService = new InputSimulationService(_logService);

        var actionExecutionService = new ActionExecutionService(
            _logService,
            new IActionHandler[]
            {
                new OpenUrlActionHandler(_logService),
                new OpenAppActionHandler(_logService),
                new OpenFileActionHandler(_logService),
                new OpenFolderActionHandler(_logService),
                new SendHotkeyActionHandler(inputService),
                new TypeTextActionHandler(inputService),
                new RunCommandActionHandler(_logService)
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
            _logService);

        var mainWindow = new MainWindow(viewModel, _hotkeyService);
        MainWindow = mainWindow;
        mainWindow.Show();

        _logService.Info("Action Orbit started.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        _hotkeyService?.Dispose();
        _logService?.Info("Action Orbit stopped.");
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logService?.Error("Unhandled UI exception.", e.Exception);
        e.Handled = true;
    }

    private void SyncStartupRegistration(StartupService startupService)
    {
        try
        {
            startupService.SetEnabled(_configService?.CurrentConfig.Settings.RunAtStartup == true);
        }
        catch (Exception ex)
        {
            _logService?.Error("Startup registration sync failed.", ex);
        }
    }
}
