using FluentAssertions;
using KeeperData.Infrastructure.Storage.Sources;
using System.Net;

namespace KeeperData.Infrastructure.Tests.Unit.Storage.Sources;

public class SqliteArtifactDownloadHandlerTests
{
    private const string ProxyEnvironmentVariable = "CDP_HTTPS_PROXY";

    [Fact]
    public void GivenNoProxyEnvironmentVariable_WhenConstructed_ThenProxyIsDisabled()
    {
        using var scope = new EnvironmentVariableScope(ProxyEnvironmentVariable, null);

        var sut = new SqliteArtifactDownloadHandler();

        sut.UseProxy.Should().BeFalse();
    }

    [Fact]
    public void GivenAnEmptyProxyEnvironmentVariable_WhenConstructed_ThenProxyIsDisabled()
    {
        using var scope = new EnvironmentVariableScope(ProxyEnvironmentVariable, "");

        var sut = new SqliteArtifactDownloadHandler();

        sut.UseProxy.Should().BeFalse();
    }

    [Fact]
    public void GivenAProxyWithoutCredentials_WhenConstructed_ThenProxyIsEnabledWithoutCredentials()
    {
        using var scope = new EnvironmentVariableScope(ProxyEnvironmentVariable, "http://proxy.example.com:8080");

        var sut = new SqliteArtifactDownloadHandler();

        sut.UseProxy.Should().BeTrue();
        var proxy = sut.Proxy.Should().BeOfType<WebProxy>().Subject;
        proxy.BypassProxyOnLocal.Should().BeTrue();
        proxy.Credentials.Should().BeNull();
        proxy.Address!.Host.Should().Be("proxy.example.com");
        proxy.Address!.Port.Should().Be(8080);
    }

    [Fact]
    public void GivenAProxyWithCredentials_WhenConstructed_ThenCredentialsAreExtractedAndStrippedFromAddress()
    {
        using var scope = new EnvironmentVariableScope(ProxyEnvironmentVariable, "http://proxyuser:proxypass@proxy.example.com:8080");

        var sut = new SqliteArtifactDownloadHandler();

        sut.UseProxy.Should().BeTrue();
        var proxy = sut.Proxy.Should().BeOfType<WebProxy>().Subject;
        var credentials = proxy.Credentials.Should().BeOfType<NetworkCredential>().Subject;
        credentials.UserName.Should().Be("proxyuser");
        credentials.Password.Should().Be("proxypass");
        proxy.Address!.UserInfo.Should().BeEmpty();
        proxy.Address!.Host.Should().Be("proxy.example.com");
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _originalValue;

        public EnvironmentVariableScope(string name, string? value)
        {
            _name = name;
            _originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_name, _originalValue);
        }
    }
}
