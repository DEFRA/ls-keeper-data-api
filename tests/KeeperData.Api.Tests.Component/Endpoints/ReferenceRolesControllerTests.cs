using FluentAssertions;
using KeeperData.Api.Controllers;
using KeeperData.Api.Controllers.RequestDtos.ReferenceRoles;
using KeeperData.Application;
using KeeperData.Application.Queries.ReferenceRoles;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class ReferenceRolesControllerTests
{
    private readonly Mock<IRequestExecutor> _executorMock;
    private readonly ReferenceRolesController _sut;

    public ReferenceRolesControllerTests()
    {
        _executorMock = new Mock<IRequestExecutor>();
        _sut = new ReferenceRolesController(_executorMock.Object);
    }

    [Fact]
    public async Task GetRoles_ReturnsOkResult_WithData()
    {
        // Arrange
        var request = new GetReferenceRolesRequest { LastUpdatedDate = DateTime.UtcNow };
        var expectedResponse = new RoleListResponse
        {
            Count = 1,
            Values = [new RoleDto { IdentifierId = "1", Code = "KEEPER", Name = "Livestock Keeper" }]
        };

        _executorMock.Setup(x => x.ExecuteQuery(
                It.Is<GetReferenceRolesQuery>(q => q.LastUpdatedDate == request.LastUpdatedDate),
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
                It.Is<GetReferenceRoleByIdQuery>(q => q.Id == roleId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetRoleById(roleId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}
