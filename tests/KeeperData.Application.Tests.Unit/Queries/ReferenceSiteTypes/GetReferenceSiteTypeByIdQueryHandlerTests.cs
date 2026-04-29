using FluentAssertions;
using KeeperData.Application.Queries.ReferenceSiteTypes;
using KeeperData.Core.Documents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Services;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.ReferenceSiteTypes;

public class GetReferenceSiteTypeByIdQueryHandlerTests
{
    private readonly Mock<IReferenceDataCache> _cacheMock;
    private readonly GetReferenceSiteTypeByIdQueryHandler _sut;

    public GetReferenceSiteTypeByIdQueryHandlerTests()
    {
        _cacheMock = new Mock<IReferenceDataCache>();
        _sut = new GetReferenceSiteTypeByIdQueryHandler(_cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSiteTypeExists_ReturnsMappedDto()
    {
        // Arrange
        var typeId = "test-id";
        var mockDoc = new SiteTypeDocument
        {
            IdentifierId = typeId,
            Code = "AH",
            Name = "Agricultural Holding",
            LastModifiedDate = DateTime.UtcNow
        };

        _cacheMock.Setup(c => c.SiteTypes).Returns([mockDoc]);

        var query = new GetReferenceSiteTypeByIdQuery(typeId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(typeId);
        result.Code.Should().Be("AH");
        result.Name.Should().Be("Agricultural Holding");
    }

    [Fact]
    public async Task Handle_WhenSiteTypeDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var typeId = "non-existent-id";

        _cacheMock.Setup(c => c.SiteTypes).Returns([]);

        var query = new GetReferenceSiteTypeByIdQuery(typeId);

        // Act
        var act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Document with Id {typeId} not found.");
    }
}