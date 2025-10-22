using KeeperData.Infrastructure;
using System.Text;
using System.Text.Json;

namespace KeeperData.Tests.Common.Utilities;

public static class HttpContentUtility
{
    public static StringContent CreateResponseContent<T>(T response)
    {
        var resultContent = new StringContent(
            content: JsonSerializer.Serialize(response, JsonDefaults.DefaultOptionsWithDataBridgeApiSupport),
            encoding: Encoding.UTF8,
            mediaType: "application/json");

        return resultContent;
    }
}