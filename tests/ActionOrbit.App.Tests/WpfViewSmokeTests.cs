using ActionOrbit.App.Views.Settings;
using ActionOrbit.App.Views.Shelf;

namespace ActionOrbit.App.Tests;

public sealed class WpfViewSmokeTests
{
    [Fact]
    public void SettingsAndShelfViews_LoadTheirXamlWithoutResourceErrors()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var application = new App();
                application.InitializeComponent();
                _ = new SettingsView();
                _ = new ShelfWorkspaceView();
                var shelfWindow = new ShelfWindow();
                shelfWindow.Close();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "WPF görünüm testi zaman aşımına uğradı.");

        Assert.Null(failure);
    }
}
