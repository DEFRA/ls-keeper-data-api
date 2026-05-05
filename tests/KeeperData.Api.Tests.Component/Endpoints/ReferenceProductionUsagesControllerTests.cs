using FluentAssertions;
using KeeperData.Api.Controllers;
using KeeperData.Api.Controllers.RequestDtos.ReferenceProductionUsages;
using KeeperData.Application;
using KeeperData.Application.Queries.ReferenceProductionUsages;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class ReferenceProductionUsagesControllerTests
{
    private readonly Mock<IRequestExecutor> _executorMock;
    private readonly ReferenceProductionUsagesController _sut;

    public ReferenceProductionUsagesControllerTests()
    {
        _executorMock = new Mock<IRequestExecutor>();
        _sut = new ReferenceProductionUsagesController(_executorMock.Object);
    }

    [Fact]
    public async Task GetProductionUsages_ReturnsOkResult_WithData()
    {
        // Arrange
        var request = new GetReferenceProductionUsagesRequest { LastUpdatedDate = DateTime.UtcNow };
        var expectedResponse = new ReferenceProductionUsageListResponse
        {
            Count = 1,
            Values = [new ReferenceProductionUsageDto { Id = "1", Code = "BEEF", Description = "Beef" }]
        };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetReferenceProductionUsagesQuery>(q => q.LastUpdatedDate == request.LastUpdatedDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetProductionUsages(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetProductionUsageById_ReturnsOkResult_WithData()
    {
        // Arrange
        var usageId = Guid.NewGuid().ToString();
        var expectedResponse = new ReferenceProductionUsageDto { Id = usageId, Code = "BEEF", Description = "Beef" };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetReferenceProductionUsageByIdQuery>(q => q.Id == usageId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetProductionUsageById(usageId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}