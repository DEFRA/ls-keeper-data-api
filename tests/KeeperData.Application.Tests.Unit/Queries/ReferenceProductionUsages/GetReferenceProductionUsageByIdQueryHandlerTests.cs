using FluentAssertions;
using KeeperData.Application.Queries.ReferenceProductionUsages;
using KeeperData.Core.Documents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Services;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.ReferenceProductionUsages;

public class GetReferenceProductionUsageByIdQueryHandlerTests
{
    private readonly Mock<IReferenceDataCache> _cacheMock;
    private readonly GetReferenceProductionUsageByIdQueryHandler _sut;

    public GetReferenceProductionUsageByIdQueryHandlerTests()
    {
        _cacheMock = new Mock<IReferenceDataCache>();
        _sut = new GetReferenceProductionUsageByIdQueryHandler(_cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenProductionUsageExists_ReturnsMappedDto()
    {
        // Arrange
        var usageId = "test-id";
        var mockDoc = new ProductionUsageDocument
        {
            IdentifierId = usageId,
            Code = "BEEF",
            Description = "Beef",
            LastModifiedDate = DateTime.UtcNow
        };

        _cacheMock.Setup(c => c.ProductionUsages).Returns([mockDoc]);

        var query = new GetReferenceProductionUsageByIdQuery(usageId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(usageId);
        result.Code.Should().Be("BEEF");
        result.Description.Should().Be("Beef");
    }
    [Fact]
    public async Task Handle_WhenProductionUsageDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var usageId = "non-existent-id";

        _cacheMock.Setup(c => c.ProductionUsages).Returns([]);

        var query = new GetReferenceProductionUsageByIdQuery(usageId);

        // Act
        var act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Document with Id {usageId} not found.");
    }
}