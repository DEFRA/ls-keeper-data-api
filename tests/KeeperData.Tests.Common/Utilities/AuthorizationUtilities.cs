using System.Net.Http.Headers;
using System.Text;

namespace KeeperData.Tests.Common.Utilities;

public static class AuthorizationUtilities
{
    public static void AddBasicApiKey(this HttpClient client, string clientId, string secret)
    {
        var raw = $"{clientId}:{secret}";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", base64);
    }

    public static void AddJwt(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "fake-jwt-token");
    }
}