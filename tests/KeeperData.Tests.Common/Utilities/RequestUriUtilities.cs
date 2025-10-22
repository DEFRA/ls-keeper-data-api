using KeeperData.Infrastructure.ApiClients.Extensions;

namespace KeeperData.Tests.Common.Utilities;

public static class RequestUriUtilities
{
    public static string GetQueryUri(string endpoint, object routeParams, Dictionary<string, string> query)
    {
        return UriTemplate.Resolve(endpoint, routeParams, query);
    }
}