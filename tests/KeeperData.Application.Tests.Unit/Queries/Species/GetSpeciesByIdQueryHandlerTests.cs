using FluentAssertions;
using KeeperData.Application.Queries.Species;
using KeeperData.Core.Documents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.Species;

public class GetSpeciesByIdQueryHandlerTests
{
    private readonly Mock<ISpeciesRepository> _repositoryMock;
    private readonly GetSpeciesByIdQueryHandler _sut;

    public GetSpeciesByIdQueryHandlerTests()
    {
        _repositoryMock = new Mock<ISpeciesRepository>();
        _sut = new GetSpeciesByIdQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSpeciesExists_ReturnsMappedDto()
    {
        // Arrange
        var speciesId = "test-id";
        var expectedDate = DateTime.UtcNow;
        var mockDoc = new SpeciesDocument
        {
            IdentifierId = speciesId,
            Code = "CTT",
            Name = "Cattle",
            LastModifiedDate = expectedDate
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(speciesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDoc);

        var query = new GetSpeciesByIdQuery(speciesId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IdentifierId.Should().Be(speciesId);
        result.Code.Should().Be("CTT");
        result.Name.Should().Be("Cattle");
        result.LastUpdatedDate.Should().Be(expectedDate);
    }

    [Fact]
    public async Task Handle_WhenSpeciesDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var speciesId = "non-existent-id";

        _repositoryMock.Setup(r => r.GetByIdAsync(speciesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SpeciesDocument)null!);

        var query = new GetSpeciesByIdQuery(speciesId);

        // Act
        var act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Document with Id {speciesId} not found.");
    }
}