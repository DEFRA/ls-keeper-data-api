using FluentAssertions;
using KeeperData.Api.Controllers;
using KeeperData.Api.Controllers.RequestDtos.Roles;
using KeeperData.Application;
using KeeperData.Application.Queries.Roles;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class RolesControllerTests
{
    private readonly Mock<IRequestExecutor> _executorMock;
    private readonly RolesController _sut;

    public RolesControllerTests()
    {
        _executorMock = new Mock<IRequestExecutor>();
        _sut = new RolesController(_executorMock.Object);
    }

    [Fact]
    public async Task GetRoles_ReturnsOkResult_WithData()
    {
        // Arrange
        var request = new GetRolesRequest { LastUpdatedDate = DateTime.UtcNow };
        var expectedResponse = new List<RoleListResponse>
        {
            new() { Count = 1, Values = [new RoleDto { IdentifierId = "1", Code = "KEEPER", Name = "Livestock Keeper" }] }
        };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetRolesQuery>(q => q.LastUpdatedDate == request.LastUpdatedDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetRoles(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetRoleById_ReturnsOkResult_WithData()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var expectedResponse = new RoleDto { IdentifierId = roleId, Code = "KEEPER", Name = "Livestock Keeper" };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetRoleByIdQuery>(q => q.Id == roleId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetRoleById(roleId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}