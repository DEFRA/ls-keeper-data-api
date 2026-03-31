using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Tests.Common.Utilities;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class ScanStatesEndpointTests : IDisposable
{
    private readonly AppWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;

    private const string BasicApiKey = "ApiKey";
    private const string BasicSecret = "integration-test-secret";

    public ScanStatesEndpointTests()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true"
        };

        _factory = new AppWebApplicationFactory(configurationOverrides);
        _httpClient = _factory.CreateClient();
        _httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);
    }

    public void Dispose()
    {
        _factory._scanStateRepositoryMock.Reset();
        _httpClient.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetScanStates_ReturnsAllScanStates()
    {
        // Arrange
        var scanStates = CreateScanStates();
        SetupRepository(scanStates);

        // Act
        var response = await _httpClient.GetAsync("/api/admin/scanstates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ScanStateDocument>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result.Should().Contain(s => s.Id == "CTS-Bulk");
        result.Should().Contain(s => s.Id == "CTS-Daily");
        result.Should().Contain(s => s.Id == "SAM-Bulk");
        result.Should().Contain(s => s.Id == "SAM-Daily");
    }

    [Fact]
    public async Task GetScanStates_WithEmptyCollection_ReturnsEmptyArray()
    {
        // Arrange
        SetupRepository([]);

        // Act
        var response = await _httpClient.GetAsync("/api/admin/scanstates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ScanStateDocument>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetScanStates_ReturnsScanStatesWithCorrectData()
    {
        // Arrange
        var scanCorrelationId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddHours(-2);
        var completedAt = DateTime.UtcNow.AddHours(-1);

        var scanStates = new List<ScanStateDocument>
        {
            new()
            {
                Id = "CTS-Bulk",
                LastSuccessfulScanStartedAt = startedAt,
                LastSuccessfulScanCompletedAt = completedAt,
                LastScanCorrelationId = scanCorrelationId,
                LastScanMode = "Bulk",
                LastScanItemCount = 5000
            }
        };

        SetupRepository(scanStates);

        // Act
        var response = await _httpClient.GetAsync("/api/admin/scanstates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ScanStateDocument>>();
        result.Should().NotBeNull();

        var scanState = result!.Single();
        scanState.Id.Should().Be("CTS-Bulk");
        scanState.LastSuccessfulScanStartedAt.Should().BeCloseTo(startedAt, TimeSpan.FromSeconds(1));
        scanState.LastSuccessfulScanCompletedAt.Should().BeCloseTo(completedAt, TimeSpan.FromSeconds(1));
        scanState.LastScanCorrelationId.Should().Be(scanCorrelationId);
        scanState.LastScanMode.Should().Be("Bulk");
        scanState.LastScanItemCount.Should().Be(5000);
    }

    [Fact]
    public async Task GetScanStates_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/admin/scanstates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetScanStates_WithRepositoryError_ReturnsInternalServerError()
    {
        // Arrange
        _factory._scanStateRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var response = await _httpClient.GetAsync("/api/admin/scanstates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Failed to retrieve scan states");
    }

    [Fact]
    public async Task GetScanStates_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        SetupRepository([]);

        // Act
        var response = await _httpClient.GetAsync("/api/admin/scanstates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory._scanStateRepositoryMock.Verify(
            r => r.GetAllAsync(0, 100, It.IsAny<CancellationToken>()),
            Times.Once,
            "Should fetch all scan states with skip=0 and limit=100");
    }

    [Fact]
    public async Task GetScanStates_ReturnsContentTypeJson()
    {
        // Arrange
        SetupRepository(CreateScanStates());

        // Act
        var response = await _httpClient.GetAsync("/api/admin/scanstates");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetScanStates_WithMultipleScanStates_ReturnsInCorrectOrder()
    {
        // Arrange
        var scanStates = CreateScanStates();
        SetupRepository(scanStates);

        // Act
        var response = await _httpClient.GetAsync("/api/admin/scanstates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ScanStateDocument>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result!.Select(s => s.Id).Should().ContainInOrder("CTS-Bulk", "CTS-Daily", "SAM-Bulk", "SAM-Daily");
    }

    private void SetupRepository(List<ScanStateDocument> scanStates)
    {
        _factory._scanStateRepositoryMock
            .Setup(r => r.GetAllAsync(0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanStates);
    }

    private static List<ScanStateDocument> CreateScanStates()
    {
        return
        [
            new()
            {
                Id = "CTS-Bulk",
                LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-2),
                LastSuccessfulScanCompletedAt = DateTime.UtcNow.AddHours(-1),
                LastScanCorrelationId = Guid.NewGuid(),
                LastScanMode = "Bulk",
                LastScanItemCount = 5000
            },
            new()
            {
                Id = "CTS-Daily",
                LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-4),
                LastSuccessfulScanCompletedAt = DateTime.UtcNow.AddHours(-3),
                LastScanCorrelationId = Guid.NewGuid(),
                LastScanMode = "Daily",
                LastScanItemCount = 250
            },
            new()
            {
                Id = "SAM-Bulk",
                LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-6),
                LastSuccessfulScanCompletedAt = DateTime.UtcNow.AddHours(-5),
                LastScanCorrelationId = Guid.NewGuid(),
                LastScanMode = "Bulk",
                LastScanItemCount = 3000
            },
            new()
            {
                Id = "SAM-Daily",
                LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-8),
                LastSuccessfulScanCompletedAt = DateTime.UtcNow.AddHours(-7),
                LastScanCorrelationId = Guid.NewGuid(),
                LastScanMode = "Daily",
                LastScanItemCount = 150
            }
        ];
    }
}