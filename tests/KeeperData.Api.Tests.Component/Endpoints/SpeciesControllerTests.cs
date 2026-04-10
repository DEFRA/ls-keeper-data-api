using FluentAssertions;
using KeeperData.Api.Controllers;
using KeeperData.Api.Controllers.RequestDtos.Species;
using KeeperData.Application;
using KeeperData.Application.Queries.Species;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class SpeciesControllerTests
{
    private readonly Mock<IRequestExecutor> _executorMock;
    private readonly SpeciesController _sut;

    public SpeciesControllerTests()
    {
        _executorMock = new Mock<IRequestExecutor>();
        _sut = new SpeciesController(_executorMock.Object);
    }

    [Fact]
    public async Task GetSpeciesTypes_ReturnsOkResult_WithData()
    {
        // Arrange
        var request = new GetSpeciesRequest { LastUpdatedDate = DateTime.UtcNow };
        var expectedResponse = new List<SpeciesListResponse>
        {
            new() { Count = 1, Values = [new SpeciesDTO { IdentifierId = "1", Code = "A", Name = "A" }] }
        };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetSpeciesQuery>(q => q.LastUpdatedDate == request.LastUpdatedDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetSpeciesTypes(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetSpeciesTypeById_ReturnsOkResult_WithData()
    {
        // Arrange
        var speciesId = Guid.NewGuid().ToString();
        var expectedResponse = new SpeciesDTO { IdentifierId = speciesId, Code = "CTT", Name = "Cattle" };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetSpeciesByIdQuery>(q => q.Id == speciesId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetSpeciesTypeById(speciesId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}