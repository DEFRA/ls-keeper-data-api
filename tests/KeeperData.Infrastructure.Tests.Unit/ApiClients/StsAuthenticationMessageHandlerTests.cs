using System.Net;
using FluentAssertions;
using KeeperData.Infrastructure.ApiClients.Sts;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.ApiClients;

public class StsAuthenticationMessageHandlerTests
{
    [Fact]
    public async Task SendAsync_ShouldAttachAwsStsAuthorizationHeaderFromTokenProvider()
    {
        var tokenProviderMock = new Mock<IStsTokenProvider>();
        tokenProviderMock
            .Setup(p => p.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("encoded-sts-token-123");

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new StubInnerHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var handler = new StsAuthenticationMessageHandler(tokenProviderMock.Object)
        {
            InnerHandler = innerHandler
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://bridge.example.com")
        };

        var response = await client.GetAsync("/api/etl/staging/sqlite/latest");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("AwsSts");
        capturedRequest.Headers.Authorization.Parameter.Should().Be("encoded-sts-token-123");
    }

    private sealed class StubInnerHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}