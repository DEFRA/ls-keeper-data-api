using FluentAssertions;
using KeeperData.Api.Controllers;
using KeeperData.Api.Controllers.RequestDtos.ReferenceSiteTypes;
using KeeperData.Application;
using KeeperData.Application.Queries.ReferenceSiteTypes;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class ReferenceSiteTypesControllerTests
{
    private readonly Mock<IRequestExecutor> _executorMock;
    private readonly ReferenceSiteTypesController _sut;

    public ReferenceSiteTypesControllerTests()
    {
        _executorMock = new Mock<IRequestExecutor>();
        _sut = new ReferenceSiteTypesController(_executorMock.Object);
    }

    [Fact]
    public async Task GetSiteTypes_ReturnsOkResult_WithData()
    {
        // Arrange
        var request = new GetReferenceSiteTypesRequest { LastUpdatedDate = DateTime.UtcNow };
        var expectedResponse = new ReferenceSiteTypeListResponse
        {
            Count = 1,
            Values = [new ReferenceSiteTypeDto { Id = "1", Code = "AH", Name = "Agricultural Holding" }]
        };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetReferenceSiteTypesQuery>(q => q.LastUpdatedDate == request.LastUpdatedDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetSiteTypes(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetSiteTypeById_ReturnsOkResult_WithData()
    {
        // Arrange
        var typeId = Guid.NewGuid().ToString();
        var expectedResponse = new ReferenceSiteTypeDto { Id = typeId, Code = "AH", Name = "Agricultural Holding" };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetReferenceSiteTypeByIdQuery>(q => q.Id == typeId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetSiteTypeById(typeId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}