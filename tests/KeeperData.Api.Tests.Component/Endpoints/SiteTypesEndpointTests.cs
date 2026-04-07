using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Tests.Common.Utilities;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class SiteTypesEndpointTests : IDisposable
{
    private readonly AppWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;

    private const string BasicApiKey = "ApiKey";
    private const string BasicSecret = "integration-test-secret";

    public SiteTypesEndpointTests()
    {
        _factory = new AppWebApplicationFactory();
        _httpClient = _factory.CreateClient();
        _httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);
    }

    public void Dispose()
    {
        _factory._referenceDataCacheMock.Reset();
        _httpClient.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetSiteTypes_WithSiteTypesInCache_ReturnsOkResult()
    {
        // Arrange
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "market-id",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "MA",
                    Name = "Market"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "TRAD",
                        Name = "Trading"
                    },
                    new SiteTypeMapActivityInfo
                    {
                        Code = "SALE",
                        Name = "Sales"
                    }
                ]
            },
            new()
            {
                IdentifierId = "farm-id",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "AH",
                    Name = "Agricultural Holding"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "FARM",
                        Name = "Farming"
                    }
                ]
            }
        };

        SetupReferenceDataCache(siteTypeMaps);

        // Act
        var response = await _httpClient.GetAsync("/api/sitetypes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<SiteTypeDTO>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var marketDto = result!.First(r => r.Id == "market-id");
        marketDto.Type.Code.Should().Be("MA");
        marketDto.Type.Name.Should().Be("Market");
        marketDto.Activities.Should().HaveCount(2);
        marketDto.Activities.Should().Contain(a => a.Code == "TRAD" && a.Name == "Trading");
        marketDto.Activities.Should().Contain(a => a.Code == "SALE" && a.Name == "Sales");

        var farmDto = result.First(r => r.Id == "farm-id");
        farmDto.Type.Code.Should().Be("AH");
        farmDto.Type.Name.Should().Be("Agricultural Holding");
        farmDto.Activities.Should().HaveCount(1);
        farmDto.Activities[0].Code.Should().Be("FARM");
    }

    [Fact]
    public async Task GetSiteTypes_WithEmptyCache_ReturnsEmptyList()
    {
        // Arrange
        SetupReferenceDataCache([]);

        // Act
        var response = await _httpClient.GetAsync("/api/sitetypes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<SiteTypeDTO>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSiteTypes_WithSiteTypeWithNoActivities_ReturnsCorrectly()
    {
        // Arrange
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "airport-id",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "AP",
                    Name = "Airport"
                },
                Activities = []
            }
        };

        SetupReferenceDataCache(siteTypeMaps);

        // Act
        var response = await _httpClient.GetAsync("/api/sitetypes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<SiteTypeDTO>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].Type.Code.Should().Be("AP");
        result[0].Type.Name.Should().Be("Airport");
        result[0].Activities.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSiteTypes_WithMultipleSiteTypes_ReturnsAllCorrectly()
    {
        // Arrange
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "AC",
                    Name = "Assembly Centre"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "ASM",
                        Name = "Assembly"
                    }
                ]
            },
            new()
            {
                IdentifierId = "id2",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "CA",
                    Name = "Calf Collection Centre"
                },
                Activities = []
            },
            new()
            {
                IdentifierId = "id3",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "SR",
                    Name = "Slaughter House Red Meat"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "SLTR",
                        Name = "Slaughter"
                    },
                    new SiteTypeMapActivityInfo
                    {
                        Code = "PROC",
                        Name = "Processing"
                    }
                ]
            }
        };

        SetupReferenceDataCache(siteTypeMaps);

        // Act
        var response = await _httpClient.GetAsync("/api/sitetypes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<SiteTypeDTO>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(r => r.Type.Code == "AC");
        result.Should().Contain(r => r.Type.Code == "CA");
        result.Should().Contain(r => r.Type.Code == "SR");

        var slaughterhouseDto = result!.First(r => r.Type.Code == "SR");
        slaughterhouseDto.Activities.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSiteTypes_ReturnsCorrectContentType()
    {
        // Arrange
        SetupReferenceDataCache([]);

        // Act
        var response = await _httpClient.GetAsync("/api/sitetypes");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetSiteTypes_VerifiesResponseStructure()
    {
        // Arrange
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "zoo-id",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "ZO",
                    Name = "Zoo"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "EXHIBIT",
                        Name = "Exhibition"
                    }
                ]
            }
        };

        SetupReferenceDataCache(siteTypeMaps);

        // Act
        var response = await _httpClient.GetAsync("/api/sitetypes");

        // Assert
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("\"id\"");
        responseBody.Should().Contain("\"type\"");
        responseBody.Should().Contain("\"code\"");
        responseBody.Should().Contain("\"name\"");
        responseBody.Should().Contain("\"activities\"");
        responseBody.Should().Contain("zoo-id");
        responseBody.Should().Contain("ZO");
        responseBody.Should().Contain("Zoo");
        responseBody.Should().Contain("EXHIBIT");
        responseBody.Should().Contain("Exhibition");
    }

    [Fact]
    public async Task GetSiteTypes_WithAllKnownSiteTypeCodes_ReturnsCorrectly()
    {
        // Arrange
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            CreateSiteTypeMap("ac-id", "AC", "Assembly Centre"),
            CreateSiteTypeMap("ah-id", "AH", "Agricultural Holding"),
            CreateSiteTypeMap("ap-id", "AP", "Airport"),
            CreateSiteTypeMap("ma-id", "MA", "Market"),
            CreateSiteTypeMap("sr-id", "SR", "Slaughter House Red Meat"),
            CreateSiteTypeMap("sw-id", "SW", "Slaughter House White Meat"),
            CreateSiteTypeMap("zo-id", "ZO", "Zoo")
        };

        SetupReferenceDataCache(siteTypeMaps);

        // Act
        var response = await _httpClient.GetAsync("/api/sitetypes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<SiteTypeDTO>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(7);
        result.Should().Contain(r => r.Type.Code == "AC");
        result.Should().Contain(r => r.Type.Code == "AH");
        result.Should().Contain(r => r.Type.Code == "AP");
        result.Should().Contain(r => r.Type.Code == "MA");
        result.Should().Contain(r => r.Type.Code == "SR");
        result.Should().Contain(r => r.Type.Code == "SW");
        result.Should().Contain(r => r.Type.Code == "ZO");
    }

    [Fact]
    public async Task GetSiteTypes_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/sitetypes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSiteTypes_CallsCacheCorrectly()
    {
        // Arrange
        SetupReferenceDataCache([]);

        // Act
        var response = await _httpClient.GetAsync("/api/sitetypes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory._referenceDataCacheMock.Verify(
            c => c.SiteTypeMaps,
            Times.Once,
            "Should access SiteTypeMaps from cache once");
    }

    private void SetupReferenceDataCache(List<SiteTypeMapDocument> siteTypeMaps)
    {
        _factory._referenceDataCacheMock
            .Setup(c => c.SiteTypeMaps)
            .Returns(siteTypeMaps);
    }

    private static SiteTypeMapDocument CreateSiteTypeMap(string id, string code, string name)
    {
        return new SiteTypeMapDocument
        {
            IdentifierId = id,
            Type = new SiteTypeMapTypeInfo
            {
                Code = code,
                Name = name
            },
            Activities = []
        };
    }
}