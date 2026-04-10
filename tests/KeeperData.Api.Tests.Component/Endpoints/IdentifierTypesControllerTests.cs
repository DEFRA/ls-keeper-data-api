using FluentAssertions;
using KeeperData.Api.Controllers;
using KeeperData.Api.Controllers.RequestDtos.IdentifierTypes;
using KeeperData.Application;
using KeeperData.Application.Queries.IdentifierTypes;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class IdentifierTypesControllerTests
{
    private readonly Mock<IRequestExecutor> _executorMock;
    private readonly IdentifierTypesController _sut;

    public IdentifierTypesControllerTests()
    {
        _executorMock = new Mock<IRequestExecutor>();
        _sut = new IdentifierTypesController(_executorMock.Object);
    }

    [Fact]
    public async Task GetIdentifierTypes_ReturnsOkResult_WithData()
    {
        // Arrange
        var request = new GetIdentifierTypesRequest { LastUpdatedDate = DateTime.UtcNow };
        var expectedResponse = new List<IdentifierTypeListResponse>
        {
            new() { Count = 1, Values = [new IdentifierTypeDTO { IdentifierId = "1", Code = "CPHN", Name = "CPH Number" }] }
        };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetIdentifierTypesQuery>(q => q.LastUpdatedDate == request.LastUpdatedDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetIdentifierTypes(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetIdentifierTypeById_ReturnsOkResult_WithData()
    {
        // Arrange
        var typeId = Guid.NewGuid().ToString();
        var expectedResponse = new IdentifierTypeDTO { IdentifierId = typeId, Code = "CPHN", Name = "CPH Number" };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetIdentifierTypeByIdQuery>(q => q.Id == typeId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetIdentifierTypeById(typeId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}