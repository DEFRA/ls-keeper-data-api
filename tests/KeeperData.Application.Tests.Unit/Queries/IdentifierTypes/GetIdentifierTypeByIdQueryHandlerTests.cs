using FluentAssertions;
using KeeperData.Application.Queries.IdentifierTypes;
using KeeperData.Core.Documents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.IdentifierTypes;

public class GetIdentifierTypeByIdQueryHandlerTests
{
    private readonly Mock<ISiteIdentifierTypeRepository> _repositoryMock;
    private readonly GetIdentifierTypeByIdQueryHandler _sut;

    public GetIdentifierTypeByIdQueryHandlerTests()
    {
        _repositoryMock = new Mock<ISiteIdentifierTypeRepository>();
        _sut = new GetIdentifierTypeByIdQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenIdentifierTypeExists_ReturnsMappedDto()
    {
        // Arrange
        var typeId = "test-id";
        var expectedDate = DateTime.UtcNow;
        var mockDoc = new SiteIdentifierTypeDocument
        {
            IdentifierId = typeId,
            Code = "CPHN",
            Name = "CPH Number",
            LastModifiedDate = expectedDate
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDoc);

        var query = new GetIdentifierTypeByIdQuery(typeId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IdentifierId.Should().Be(typeId);
        result.Code.Should().Be("CPHN");
        result.Name.Should().Be("CPH Number");
        result.Description.Should().BeNull();
        result.LastUpdatedDate.Should().Be(expectedDate);
    }

    [Fact]
    public async Task Handle_WhenIdentifierTypeDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var typeId = "non-existent-id";

        _repositoryMock.Setup(r => r.GetByIdAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteIdentifierTypeDocument)null!);

        var query = new GetIdentifierTypeByIdQuery(typeId);

        // Act
        var act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Document with Id {typeId} not found.");
    }
}