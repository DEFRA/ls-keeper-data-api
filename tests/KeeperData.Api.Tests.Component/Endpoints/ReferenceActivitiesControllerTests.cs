using FluentAssertions;
using KeeperData.Api.Controllers;
using KeeperData.Api.Controllers.RequestDtos.ReferenceActivities;
using KeeperData.Application;
using KeeperData.Application.Queries.ReferenceActivities;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class ReferenceActivitiesControllerTests
{
    private readonly Mock<IRequestExecutor> _executorMock;
    private readonly ReferenceActivitiesController _sut;

    public ReferenceActivitiesControllerTests()
    {
        _executorMock = new Mock<IRequestExecutor>();
        _sut = new ReferenceActivitiesController(_executorMock.Object);
    }

    [Fact]
    public async Task GetActivities_ReturnsOkResult_WithData()
    {
        // Arrange
        var request = new GetReferenceActivitiesRequest { LastUpdatedDate = DateTime.UtcNow };
        var expectedResponse = new ReferenceActivityListResponse
        {
            Count = 1,
            Values = [new ReferenceActivityDto { Id = "1", Code = "MARP", Name = "Market on Paved Ground" }]
        };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetReferenceActivitiesQuery>(q => q.LastUpdatedDate == request.LastUpdatedDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetActivities(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetActivityById_ReturnsOkResult_WithData()
    {
        // Arrange
        var activityId = Guid.NewGuid().ToString();
        var expectedResponse = new ReferenceActivityDto { Id = activityId, Code = "MARP", Name = "Market on Paved Ground" };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetReferenceActivityByIdQuery>(q => q.Id == activityId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetActivityById(activityId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}