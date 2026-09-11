using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace KeeperData.Api.Tests.Component.Endpoints;

public sealed class OpenApiEndpointTests : IDisposable
{
    private readonly AppWebApplicationFactory _factory = new();
    private readonly HttpClient _httpClient;

    public OpenApiEndpointTests()
    {
        _httpClient = _factory.CreateClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetV2_ReturnsOnlyExpectedV2Contract()
    {
        // Act
        using var response = await _httpClient.GetAsync("/openapi/v2.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        root.GetProperty("openapi").GetString().Should().StartWith("3.1.");

        var paths = root.GetProperty("paths");
        paths.EnumerateObject().Select(path => path.Name).Should().BeEquivalentTo(
        [
            "/api/v2/cph-associations",
            "/api/v2/holdings/{county}/{parish}/{holding}",
            "/api/v2/user-accounts",
            "/api/v2/user-accounts/{subject}"
        ]);
        paths.GetProperty("/api/v2/cph-associations").EnumerateObject().Select(operation => operation.Name).Should().Equal("get");
        paths.GetProperty("/api/v2/holdings/{county}/{parish}/{holding}").EnumerateObject().Select(operation => operation.Name).Should().Equal("get");
        paths.GetProperty("/api/v2/user-accounts").EnumerateObject().Select(operation => operation.Name).Should().Equal("post");
        paths.GetProperty("/api/v2/user-accounts/{subject}").EnumerateObject().Select(operation => operation.Name).Should().Equal("get");

        var securitySchemes = root.GetProperty("components").GetProperty("securitySchemes");
        securitySchemes.GetProperty("Bearer").GetProperty("scheme").GetString().Should().Be("bearer");
        securitySchemes.GetProperty("Basic").GetProperty("scheme").GetString().Should().Be("basic");
        root.GetProperty("security").GetArrayLength().Should().Be(2);
    }
}