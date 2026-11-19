using System.Net.Http.Headers;

namespace KeeperData.Infrastructure.ApiClients.Sts;

public sealed class StsAuthenticationMessageHandler(IStsTokenProvider tokenProvider) : DelegatingHandler
{
    public const string SchemeName = "AwsSts";

    private readonly IStsTokenProvider _tokenProvider = tokenProvider;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue(SchemeName, token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}