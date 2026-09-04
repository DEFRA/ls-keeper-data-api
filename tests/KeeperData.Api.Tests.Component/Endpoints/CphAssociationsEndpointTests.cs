using FluentAssertions;
using KeeperData.Api.Controllers.ResponseDtos.CphAssociations;
using KeeperData.Application.Queries.CphAssociations;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class CphAssociationsEndpointTests : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture;
    private readonly Mock<IReadModelSqliteCacheService> _mockCache;
    private readonly Mock<KeeperData.Application.IRequestExecutor> _mockExecutor;
    private readonly HttpClient _client;

    public CphAssociationsEndpointTests(AppTestFixture appTestFixture)
    {
        _appTestFixture = appTestFixture;

        _mockCache = new Mock<IReadModelSqliteCacheService>();
        _mockExecutor = new Mock<KeeperData.Application.IRequestExecutor>();

        _client = _appTestFixture.AppWebApplicationFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_mockCache.Object);
                services.AddSingleton(_mockExecutor.Object);
            });
        }).CreateClient();
        
        _client.AddBasicApiKey("ApiKey", "integration-test-secret");
    }

    [Fact]
    public async Task GetCphAssociations_WhenCacheNotLoaded_Returns503ProblemDetails()
    {
        // Arrange
        _mockCache.Setup(c => c.IsLoaded).Returns(false);

        // Act
        var response = await _client.GetAsync("/cph-associations?email=test@test.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(503);
        problemDetails.Detail.Should().Be("The SAM read model is not cached locally, so CPH associations cannot be resolved.");
    }

    [Fact]
    public async Task GetCphAssociations_WhenEmailInvalid_Returns400ProblemDetails()
    {
        _mockCache.Setup(c => c.IsLoaded).Returns(true);

        // Act
        var response = await _client.GetAsync("/cph-associations?email=not-an-email");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
        problemDetails.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task GetCphAssociations_WhenEmailMissing_Returns400ProblemDetails()
    {
        _mockCache.Setup(c => c.IsLoaded).Returns(true);

        // Act
        var response = await _client.GetAsync("/cph-associations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
        problemDetails.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task GetCphAssociations_WhenAssociationsExist_Returns200WithList()
    {
        // Arrange
        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        _mockExecutor
            .Setup(x => x.ExecuteQuery(It.IsAny<GetCphAssociationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CphAssociationResult> 
            { 
                new CphAssociationResult { Cph = "12/345/6789", Role = "owner" } 
            });

        // Act
        var response = await _client.GetAsync("/cph-associations?email=test@test.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var associations = await response.Content.ReadFromJsonAsync<List<CphAssociationResponse>>();
        associations.Should().NotBeNull();
        associations.Should().HaveCount(1);
        associations![0].Cph.Should().Be("12/345/6789");
        associations[0].Role.Should().Be("owner");
    }

    [Fact]
    public async Task GetCphAssociations_WhenNoAssociations_Returns200WithEmptyList()
    {
        // Arrange
        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        _mockExecutor
            .Setup(x => x.ExecuteQuery(It.IsAny<GetCphAssociationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CphAssociationResult>());

        // Act
        var response = await _client.GetAsync("/cph-associations?email=test@test.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var associations = await response.Content.ReadFromJsonAsync<List<CphAssociationResponse>>();
        associations.Should().NotBeNull();
        associations.Should().BeEmpty();
    }
}
