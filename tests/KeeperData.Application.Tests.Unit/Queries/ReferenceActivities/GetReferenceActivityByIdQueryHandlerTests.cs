using FluentAssertions;
using KeeperData.Application.Queries.ReferenceActivities;
using KeeperData.Core.Documents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Services;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.ReferenceActivities;

public class GetReferenceActivityByIdQueryHandlerTests
{
    private readonly Mock<IReferenceDataCache> _cacheMock;
    private readonly GetReferenceActivityByIdQueryHandler _sut;

    public GetReferenceActivityByIdQueryHandlerTests()
    {
        _cacheMock = new Mock<IReferenceDataCache>();
        _sut = new GetReferenceActivityByIdQueryHandler(_cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenActivityExists_ReturnsMappedDto()
    {
        // Arrange
        var activityId = "test-id";
        var mockDoc = new SiteActivityTypeDocument
        {
            IdentifierId = activityId,
            Code = "MARP",
            Name = "Market on Paved Ground",
            LastModifiedDate = DateTime.UtcNow
        };

        _cacheMock.Setup(c => c.SiteActivityTypes).Returns([mockDoc]);

        var query = new GetReferenceActivityByIdQuery(activityId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(activityId);
        result.Code.Should().Be("MARP");
        result.Name.Should().Be("Market on Paved Ground");
    }

    [Fact]
    public async Task Handle_WhenActivityDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var activityId = "non-existent-id";

        _cacheMock.Setup(c => c.SiteActivityTypes).Returns([]);

        var query = new GetReferenceActivityByIdQuery(activityId);

        // Act
        var act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Document with Id {activityId} not found.");
    }
}