using ActionOrbit.App.Models;
using ActionOrbit.App.Services.Actions;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Tests;

public sealed class StatusCenterViewModelTests
{
    [Fact]
    public void ReportActionResult_WhenSuccessful_ShowsSuccessWithoutTrayWarning()
    {
        var viewModel = new StatusCenterViewModel();
        var notificationCount = 0;
        viewModel.UserNotificationRequested += (_, _) => notificationCount++;

        viewModel.ReportActionResult(CreateAction(), ActionExecutionResult.Success());

        Assert.Equal("Aksiyon çalıştı: İndirilenler", viewModel.Message);
        Assert.Equal(StatusTone.Success, viewModel.Tone);
        Assert.Equal(0, notificationCount);
    }

    [Fact]
    public void ReportActionResult_WhenFailed_ShowsErrorAndRequestsTrayWarning()
    {
        var viewModel = new StatusCenterViewModel();
        string? notification = null;
        viewModel.UserNotificationRequested += (message, isError) =>
            notification = $"{isError}:{message}";

        viewModel.ReportActionResult(
            CreateAction(),
            ActionExecutionResult.Failure("Klasör bulunamadı."));

        Assert.Equal(StatusTone.Error, viewModel.Tone);
        Assert.Contains("Klasör bulunamadı", viewModel.Message);
        Assert.Equal($"True:{viewModel.Message}", notification);
    }

    private static OrbitAction CreateAction() => new()
    {
        Id = "downloads",
        Title = "İndirilenler",
        Type = "open_folder",
        Target = "%USERPROFILE%\\Downloads"
    };
}
