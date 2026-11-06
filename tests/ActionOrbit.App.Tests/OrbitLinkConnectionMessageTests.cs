using System.Net.Sockets;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class OrbitLinkConnectionMessageTests
{
    [Theory]
    [InlineData(SocketError.ConnectionRefused, "kabul etmedi")]
    [InlineData(SocketError.TimedOut, "zamanında")]
    [InlineData(SocketError.NetworkUnreachable, "ağa")]
    [InlineData(SocketError.AccessDenied, "engellendi")]
    [InlineData(SocketError.AddressAlreadyInUse, "kullanımda")]
    public void NetworkFailures_ExplainWhatToCheck(SocketError error, string expected)
        => Assert.Contains(expected, OrbitLinkConnectionMessage.FromException(new SocketException((int)error)));

    [Fact]
    public void ExceptionDetails_AreNotExposedToTheUser()
    {
        var message = OrbitLinkConnectionMessage.FromException(new IOException("private-fixture-path-and-code"));
        Assert.DoesNotContain("private-fixture", message);
        Assert.Contains("tekrar dene", message);
        Assert.Contains("zamanında", OrbitLinkConnectionMessage.FromException(new OperationCanceledException()));
    }
}
