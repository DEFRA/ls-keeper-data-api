using FluentAssertions;
using KeeperData.Infrastructure.Storage.Sources;
using KeeperData.Infrastructure.Tests.Unit.ApiClients.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Storage.Sources;

public class DataBridgeSqliteArtifactSourceTests
{
    private const string BridgeBaseUrl = "http://localhost:5560";
    private const string LatestRoute = "api/etl/staging/sqlite/latest";

    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly List<HttpRequestMessage> _metadataRequests = [];
    private readonly List<HttpRequestMessage> _downloadRequests = [];

    [Fact]
    public async Task GetLatestAsync_ReturnsTheArtifactDescribedByTheBridge()
    {
        var source = BuildSource(
            metadataResponse: () => JsonResponse("""
                {
                  "objectKey": "views/krds-db_20260821070003.sqlite",
                  "downloadUrl": "https://bridge-bucket.example/views/krds-db_20260821070003.sqlite?X-Amz-Signature=abc",
                  "size": 4096,
                  "lastModified": "2026-08-21T07:00:03Z",
                  "expiresAt": "2026-08-21T08:00:03Z"
                }
                """));

        var artifact = await source.GetLatestAsync(LatestRoute, CancellationToken.None);

        artifact.Should().NotBeNull();
        artifact!.ObjectKey.Should().Be("views/krds-db_20260821070003.sqlite");
        artifact.FileName.Should().Be("krds-db_20260821070003.sqlite");
        artifact.Size.Should().Be(4096);
        artifact.DownloadUrl.Should().StartWith("https://bridge-bucket.example/");

        _metadataRequests.Should().ContainSingle();
        _metadataRequests[0].RequestUri!.AbsolutePath.Should().Be($"/{LatestRoute}");
    }

    [Fact]
    public async Task GetLatestAsync_WhenBridgeHasNoArtifact_ReturnsNull()
    {
        var source = BuildSource(
            metadataResponse: () => new HttpResponseMessage(HttpStatusCode.NotFound));

        var artifact = await source.GetLatestAsync(LatestRoute, CancellationToken.None);

        artifact.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestAsync_WhenBridgeFails_Throws()
    {
        var source = BuildSource(
            metadataResponse: () => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var act = async () => await source.GetLatestAsync(LatestRoute, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetLatestAsync_WhenResponseIsIncomplete_ReturnsNull()
    {
        var source = BuildSource(
            metadataResponse: () => JsonResponse("""{ "objectKey": "views/krds-db_20260821070003.sqlite" }"""));

        var artifact = await source.GetLatestAsync(LatestRoute, CancellationToken.None);

        artifact.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestAsync_WhenServiceNameIsConfigured_PrefixesTheRoute()
    {
        var source = BuildSource(
            metadataResponse: () => JsonResponse("""
                {
                  "objectKey": "views/krds-db_20260821070003.sqlite",
                  "downloadUrl": "https://bridge-bucket.example/file?X-Amz-Signature=abc"
                }
                """),
            serviceName: "keeper-data-bridge");

        await source.GetLatestAsync(LatestRoute, CancellationToken.None);

        _metadataRequests[0].RequestUri!.AbsolutePath.Should().Be($"/keeper-data-bridge/{LatestRoute}");
    }

    [Fact]
    public async Task DownloadAsync_WritesTheFileWithoutSendingBridgeCredentials()
    {
        var source = BuildSource(
            metadataResponse: () => new HttpResponseMessage(HttpStatusCode.NotFound),
            downloadResponse: () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes("sqlite-bytes"))
            });

        var localPath = Path.Combine(Path.GetTempPath(), $"artifact-{Guid.NewGuid():N}.sqlite");

        try
        {
            await source.DownloadAsync(
                new Core.Storage.Sqlite.SqliteArtifact
                {
                    ObjectKey = "views/krds-db_20260821070003.sqlite",
                    DownloadUrl = "https://bridge-bucket.example/views/krds-db_20260821070003.sqlite?X-Amz-Signature=abc"
                },
                localPath,
                CancellationToken.None);

            (await File.ReadAllTextAsync(localPath)).Should().Be("sqlite-bytes");

            _downloadRequests.Should().ContainSingle();
            _downloadRequests[0].Headers.Authorization.Should().BeNull(
                "attaching bridge credentials to a presigned URL breaks its signature");
            _downloadRequests[0].Headers.Should().NotContain(h => h.Key == "Authorization" || h.Key == "x-api-key");
        }
        finally
        {
            try { File.Delete(localPath); } catch { }
        }
    }

    [Fact]
    public async Task DownloadAsync_WhenTheUrlHasExpired_Throws()
    {
        var source = BuildSource(
            metadataResponse: () => new HttpResponseMessage(HttpStatusCode.NotFound),
            downloadResponse: () => new HttpResponseMessage(HttpStatusCode.Forbidden));

        var act = async () => await source.DownloadAsync(
            new Core.Storage.Sqlite.SqliteArtifact
            {
                ObjectKey = "views/krds-db_20260821070003.sqlite",
                DownloadUrl = "https://bridge-bucket.example/expired?X-Amz-Signature=abc"
            },
            Path.Combine(Path.GetTempPath(), $"artifact-{Guid.NewGuid():N}.sqlite"),
            CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private DataBridgeSqliteArtifactSource BuildSource(
        Func<HttpResponseMessage> metadataResponse,
        Func<HttpResponseMessage>? downloadResponse = null,
        string serviceName = "")
    {
        var metadataClient = new HttpClient(new TestHttpMessageHandler((request, _) =>
        {
            _metadataRequests.Add(request);
            return Task.FromResult(metadataResponse());
        }))
        {
            BaseAddress = new Uri(BridgeBaseUrl)
        };

        // Mirrors the named client, which carries the bridge subscription key on every request.
        metadataClient.DefaultRequestHeaders.Add("Authorization", "ApiKey bridge-subscription-key");

        var downloadClient = new HttpClient(new TestHttpMessageHandler((request, _) =>
        {
            _downloadRequests.Add(request);
            return Task.FromResult(downloadResponse?.Invoke() ?? new HttpResponseMessage(HttpStatusCode.OK));
        }));

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(DataBridgeSqliteArtifactSource.MetadataClientName))
            .Returns(metadataClient);

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(DataBridgeSqliteArtifactSource.DownloadClientName))
            .Returns(downloadClient);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ApiClients:DataBridgeApi:ServiceName", serviceName }
            })
            .Build();

        return new DataBridgeSqliteArtifactSource(
            _httpClientFactoryMock.Object,
            configuration,
            Mock.Of<ILogger<DataBridgeSqliteArtifactSource>>());
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
