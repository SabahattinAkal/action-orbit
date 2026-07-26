using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class ActionValidationServiceTests
{
    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://localhost:5000", true)]
    [InlineData("ftp://example.com", false)]
    [InlineData("example.com", false)]
    public void OpenUrl_RequiresHttpOrHttps(string target, bool expected)
    {
        var result = ActionValidationService.Validate(Create("open_url", target));
        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData("Ctrl+Shift+T", true)]
    [InlineData("Win+.", true)]
    [InlineData("Ctrl++", true)]
    [InlineData("Ctrl+Alt", false)]
    [InlineData("Ctrl+", false)]
    [InlineData("Ctrl+NotAKey", false)]
    public void SendHotkey_ValidatesEveryToken(string target, bool expected)
    {
        var result = ActionValidationService.Validate(Create("send_hotkey", target));
        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData("echo hello", false)]
    [InlineData("shutdown /s", true)]
    [InlineData("del C:\\temp /s /q", true)]
    [InlineData("powershell Remove-Item C:\\temp -Recurse -Force", true)]
    [InlineData("rm -rf /", true)]
    [InlineData("rm notes.txt", true)]
    [InlineData("rm -r ./cache", true)]
    [InlineData("powershell -Command ri notes.txt", true)]
    [InlineData("echo ok && shutdown /s", true)]
    [InlineData("shutdown.exe /s", true)]
    [InlineData("C:\\Windows\\System32\\shutdown.exe /s", true)]
    [InlineData("format.com D:", true)]
    [InlineData("cmd /c del C:\\temp\\file.txt", true)]
    [InlineData("powershell -Command Clear-Content C:\\temp\\file.txt", true)]
    [InlineData("reg delete HKCU\\Software\\Sample /f", true)]
    [InlineData("diskpart /s cleanup.txt", true)]
    [InlineData("echo harmless", false)]
    [InlineData("git rm --cached file.txt", false)]
    public void CommandSafety_BlocksDestructivePatterns(string command, bool blocked)
    {
        Assert.Equal(blocked, CommandSafetyService.IsBlocked(command));
        var result = ActionValidationService.Validate(Create("run_command", command));
        Assert.Equal(!blocked, result.IsValid);
    }

    [Fact]
    public void Folder_RequiresAtLeastOneChild()
    {
        var folder = Create("folder", "");

        Assert.False(ActionValidationService.Validate(folder).IsValid);
        folder.Children.Add(Create("open_url", "https://example.com"));
        Assert.True(ActionValidationService.Validate(folder).IsValid);
    }

    [Fact]
    public void NonFolder_WithChildren_IsRejected()
    {
        var action = Create("open_url", "https://example.com");
        action.Children.Add(Create("open_url", "https://example.org"));

        var result = ActionValidationService.Validate(action);

        Assert.False(result.IsValid);
        Assert.Contains("klasör", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OrbitAction Create(string type, string target) =>
        new()
        {
            Id = "action",
            Title = "Action",
            Type = type,
            Target = target,
            Children = []
        };
}
