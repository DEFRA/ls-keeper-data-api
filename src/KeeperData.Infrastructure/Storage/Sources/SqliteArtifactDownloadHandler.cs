using System.Net;

namespace KeeperData.Infrastructure.Storage.Sources;

/// <summary>
/// Downloads presigned artifact URLs through the platform proxy when one is configured, since the
/// bridge presigns against the public S3 endpoint.
/// </summary>
public class SqliteArtifactDownloadHandler : HttpClientHandler
{
    public SqliteArtifactDownloadHandler()
    {
        var proxyUri = Environment.GetEnvironmentVariable("CDP_HTTPS_PROXY");

        if (string.IsNullOrWhiteSpace(proxyUri))
        {
            UseProxy = false;
            return;
        }

        var uri = new UriBuilder(proxyUri);
        var proxy = new WebProxy { BypassProxyOnLocal = true };

        if (!string.IsNullOrWhiteSpace(uri.UserName) && !string.IsNullOrWhiteSpace(uri.Password))
        {
            proxy.Credentials = new NetworkCredential(uri.UserName, uri.Password);
        }

        // Strip the credentials so they cannot reach the logs.
        uri.UserName = string.Empty;
        uri.Password = string.Empty;
        proxy.Address = uri.Uri;

        Proxy = proxy;
        UseProxy = true;
    }
}
