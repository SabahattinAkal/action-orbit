using System.Net;
using System.Net.Http.Headers;
using System.Windows;
using ActionOrbit.App.Models;
using ActionOrbit.App.Services;
using ActionOrbit.App.ViewModels;

namespace ActionOrbit.App.Tests;

public sealed class ShelfSecurityTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.20.1.1")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.10.20")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:10.0.0.1")]
    public void IsPublicAddress_RejectsPrivateAndLocalAddresses(string value) =>
        Assert.False(SafeRemoteImageService.IsPublicAddress(IPAddress.Parse(value)));

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("2606:4700:4700::1111")]
    public void IsPublicAddress_AllowsPublicAddresses(string value) =>
        Assert.True(SafeRemoteImageService.IsPublicAddress(IPAddress.Parse(value)));

    [Fact]
    public async Task ValidateRemoteUri_RejectsLocalhostBeforeDownload()
    {
        var result = await SafeRemoteImageService.ValidateRemoteUriAsync(
            new Uri("http://localhost/private.png"),
            TestContext.Current.CancellationToken);
        Assert.False(result.IsSafe);
    }

    [Fact]
    public async Task ShelfDrop_ImportsFileDropWithoutExecutingFile()
    {
        using var temp = new TemporaryDirectory();
        var file = Path.Combine(temp.Path, "sample.exe");
        await File.WriteAllBytesAsync(file, [0x4D, 0x5A, 0, 0], TestContext.Current.CancellationToken);
        using var remote = new SafeRemoteImageService(new LogService(temp.Path));
        var service = new ShelfDropService(remote, Path.Combine(temp.Path, "cache"));
        var data = new System.Windows.DataObject();
        data.SetData(System.Windows.DataFormats.FileDrop, new[] { file });

        var result = await service.ImportAsync(
            data,
            new ShelfSettings(),
            20,
            100_000_000,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var item = Assert.Single(result.Items);
        Assert.Equal("file", item.Kind);
        Assert.Equal(file, item.LocalPath);
        Assert.False(item.IsTemporary);
    }

    [Fact]
    public async Task ShelfDrop_ImportsPlainTextAndUrlAsData()
    {
        using var temp = new TemporaryDirectory();
        using var remote = new SafeRemoteImageService(new LogService(temp.Path));
        var service = new ShelfDropService(remote, Path.Combine(temp.Path, "cache"));
        var data = new System.Windows.DataObject(System.Windows.DataFormats.UnicodeText, "https://example.com/page?token=secret");

        var result = await service.ImportAsync(
            data,
            new ShelfSettings(),
            20,
            100_000_000,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var item = Assert.Single(result.Items);
        Assert.Equal("url", item.Kind);
        Assert.Equal("https://example.com/page?token=secret", item.TextContent);
    }

    [Fact]
    public async Task ShelfDrop_FallsBackWhenBrowserFileDropPathIsVirtual()
    {
        using var temp = new TemporaryDirectory();
        using var remote = new SafeRemoteImageService(new LogService(temp.Path));
        var service = new ShelfDropService(remote, Path.Combine(temp.Path, "cache"));
        var data = new System.Windows.DataObject();
        data.SetData(System.Windows.DataFormats.FileDrop, new[] { Path.Combine(temp.Path, "missing-browser-cache.png") });
        data.SetData(System.Windows.DataFormats.UnicodeText, "Tarayıcıdan gelen içerik");

        var result = await service.ImportAsync(
            data,
            new ShelfSettings(),
            20,
            100_000_000,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("text", Assert.Single(result.Items).Kind);
    }

    [Fact]
    public async Task RemoteImageDownload_ClosesFileBeforeSignatureAndDimensionValidation()
    {
        using var temp = new TemporaryDirectory();
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        using var service = new SafeRemoteImageService(
            new LogService(temp.Path),
            new StubHttpMessageHandler(() =>
            {
                var content = new ByteArrayContent(png);
                content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            }),
            (_, _) => Task.FromResult(RemoteUriValidation.Success));

        var result = await service.DownloadAsync(
            new Uri("https://images.example.test/pixel.png"),
            Path.Combine(temp.Path, "cache"),
            1024 * 1024,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.True(File.Exists(result.Path));
        Assert.Equal(png.Length, result.SizeBytes);
    }

    [Fact]
    public void ShelfRename_IsPersistedImmediatelyWhenRecentShelvesAreEnabled()
    {
        using var temp = new TemporaryDirectory();
        var log = new LogService(temp.Path);
        var config = new ConfigService(log, temp.Path);
        config.CurrentConfig.Settings.Shelf.RememberRecentShelves = true;

        using var viewModel = new ShelfViewModel(config, log, _ => { });
        viewModel.SelectedShelf!.Name = "Tasarım Aktarımı";

        var loaded = new ShelfPersistenceService(temp.Path, log)
            .Load(config.CurrentConfig.Settings.Shelf);
        Assert.Equal("Tasarım Aktarımı", Assert.Single(loaded).Name);
    }

    private sealed class StubHttpMessageHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responseFactory());
    }
}
