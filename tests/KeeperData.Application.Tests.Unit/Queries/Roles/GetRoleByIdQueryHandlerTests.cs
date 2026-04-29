using FluentAssertions;
using KeeperData.Application.Queries.Roles;
using KeeperData.Core.Documents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.Roles;

public class GetRoleByIdQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _repositoryMock;
    private readonly GetRoleByIdQueryHandler _sut;

    public GetRoleByIdQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRoleRepository>();
        _sut = new GetRoleByIdQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_ReturnsMappedDto()
    {
        // Arrange
        var roleId = "test-id";
        var expectedDate = DateTime.UtcNow;
        var mockDoc = new RoleDocument
        {
            IdentifierId = roleId,
            Code = "KEEPER",
            Name = "Livestock Keeper",
            LastModifiedDate = expectedDate
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDoc);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IdentifierId.Should().Be(roleId);
        result.Code.Should().Be("KEEPER");
        result.Name.Should().Be("Livestock Keeper");
        result.LastUpdatedDate.Should().Be(expectedDate);
    }

    [Fact]
    public async Task Handle_WhenRoleDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var roleId = "non-existent-id";

        _repositoryMock.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleDocument)null!);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        var act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Document with Id {roleId} not found.");
    }
}