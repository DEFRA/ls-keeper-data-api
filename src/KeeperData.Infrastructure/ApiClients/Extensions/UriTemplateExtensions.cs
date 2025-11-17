namespace KeeperData.Infrastructure.ApiClients.Extensions;

public static class UriTemplate
{
    public static string Resolve(
        string template,
        object routeParams,
        IDictionary<string, string>? odataParams = null)
    {
        var uri = template;
        var props = routeParams.GetType().GetProperties();

        foreach (var prop in props)
        {
            var value = prop.GetValue(routeParams)?.ToString();
            uri = uri.Replace($"{{{prop.Name}}}", Uri.EscapeDataString(value ?? string.Empty));
        }

        if (odataParams is { Count: > 0 })
        {
            var query = string.Join("&", odataParams
                .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

            uri += uri.Contains('?') ? $"&{query}" : $"?{query}";
        }

        return uri;
    }
}