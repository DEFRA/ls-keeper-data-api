using System.Text;
using Amazon.Runtime;
using FluentAssertions;
using KeeperData.Infrastructure.ApiClients.Configuration;
using KeeperData.Infrastructure.ApiClients.Sts;
using Microsoft.AspNetCore.WebUtilities;

namespace KeeperData.Infrastructure.Tests.Unit.ApiClients;

public class AwsStsTokenProviderTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private static string DecodePresignedUrl(string token)
        => Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

    [Fact]
    public async Task GetTokenAsync_WithStaticCredentials_ShouldProducePresignedStsGetCallerIdentityUrl()
    {
        var credentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
        var clock = new FakeTimeProvider(FixedNow);
        var config = new ApiClientConfiguration
        {
            StsEndpoint = "https://sts.eu-west-2.amazonaws.com",
            StsRegion = "eu-west-2",
            StsExpiresSeconds = 60
        };

        var provider = new AwsStsTokenProvider(config, credentials, clock);

        var token = await provider.GetTokenAsync();
        var url = DecodePresignedUrl(token);

        var uri = new Uri(url);
        uri.Scheme.Should().Be("https");
        uri.Host.Should().Be("sts.eu-west-2.amazonaws.com");
        uri.AbsolutePath.Should().Be("/");

        var query = QueryHelpers.ParseQuery(uri.Query);
        query["Action"].ToString().Should().Be("GetCallerIdentity");
        query["Version"].ToString().Should().Be("2011-06-15");
        query["X-Amz-Algorithm"].ToString().Should().Be("AWS4-HMAC-SHA256");
        query["X-Amz-Credential"].ToString().Should().Be("AKIAIOSFODNN7EXAMPLE/20261014/eu-west-2/sts/aws4_request");
        query["X-Amz-Date"].ToString().Should().Be("20261014T120000Z");
        query["X-Amz-Expires"].ToString().Should().Be("60");
        query["X-Amz-SignedHeaders"].ToString().Should().Be("host");
        query.ContainsKey("X-Amz-Security-Token").Should().BeFalse();
        query["X-Amz-Signature"].ToString().Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public async Task GetTokenAsync_WithSessionCredentials_ShouldIncludeSecurityTokenInQuery()
    {
        var credentials = new SessionAWSCredentials("ASIAEXAMPLEKEY", "secret-key", "session-token-abc+/=");
        var clock = new FakeTimeProvider(FixedNow);

        var provider = new AwsStsTokenProvider(
            stsEndpoint: "https://sts.eu-west-2.amazonaws.com",
            stsRegion: "eu-west-2",
            expiresSeconds: 60,
            credentialsFactory: () => credentials,
            timeProvider: clock);

        var token = await provider.GetTokenAsync();
        var url = DecodePresignedUrl(token);

        var query = QueryHelpers.ParseQuery(new Uri(url).Query);
        query["X-Amz-Security-Token"].ToString().Should().Be("session-token-abc+/=");
    }

    [Fact]
    public async Task GetTokenAsync_WithinCacheWindow_ShouldReuseTokenAndRefreshAfterWindow()
    {
        var credentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "secret");
        var clock = new FakeTimeProvider(FixedNow);
        var callCount = 0;

        var provider = new AwsStsTokenProvider(
            stsEndpoint: "https://sts.eu-west-2.amazonaws.com",
            stsRegion: "eu-west-2",
            expiresSeconds: 60,
            credentialsFactory: () =>
            {
                callCount++;
                return credentials;
            },
            timeProvider: clock);

        var first = await provider.GetTokenAsync();
        clock.Advance(TimeSpan.FromSeconds(30));
        var second = await provider.GetTokenAsync();

        second.Should().Be(first);
        callCount.Should().Be(1);

        clock.Advance(TimeSpan.FromSeconds(20));
        var third = await provider.GetTokenAsync();

        third.Should().NotBe(first);
        callCount.Should().Be(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-uri")]
    [InlineData("ftp://sts.eu-west-2.amazonaws.com")]
    public void Constructor_WithInvalidEndpoint_ShouldThrowArgumentException(string endpoint)
    {
        var act = () => new AwsStsTokenProvider(endpoint, "eu-west-2", 60);

        act.Should().Throw<ArgumentException>().WithParameterName("stsEndpoint");
    }

    [Fact]
    public void Constructor_WithBlankRegion_ShouldThrowArgumentException()
    {
        var act = () => new AwsStsTokenProvider("https://sts.eu-west-2.amazonaws.com", " ", 60);

        act.Should().Throw<ArgumentException>().WithParameterName("stsRegion");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(301)]
    public void Constructor_WithOutOfRangeExpiry_ShouldThrowArgumentOutOfRangeException(int expiresSeconds)
    {
        var act = () => new AwsStsTokenProvider("https://sts.eu-west-2.amazonaws.com", "eu-west-2", expiresSeconds);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("expiresSeconds");
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }
}