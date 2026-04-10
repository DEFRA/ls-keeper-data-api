using FluentAssertions;
using KeeperData.Application.Queries.Species;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.Species;

public class GetSpeciesQueryHandlerTests
{
    private readonly Mock<ISpeciesRepository> _repositoryMock;
    private readonly GetSpeciesQueryHandler _sut;

    public GetSpeciesQueryHandlerTests()
    {
        _repositoryMock = new Mock<ISpeciesRepository>();
        _sut = new GetSpeciesQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ReturnsSpeciesOrderedBySortOrderThenName()
    {
        // Arrange
        var mockData = new List<SpeciesDocument>
        {
            CreateSpeciesDoc("3", "PIG", "Pig", 30),
            CreateSpeciesDoc("1", "CTT", "Cattle", 10),
            CreateSpeciesDoc("4", "SHP", "Sheep", 20),
            CreateSpeciesDoc("2", "ALP", "Alpaca", 10) // Same SortOrder as Cattle, should be sorted alphabetically by Name
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        var query = new GetSpeciesQuery();

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().ContainSingle();
        var response = result.First();

        response.Count.Should().Be(4);
        response.Values.Should().HaveCount(4);

        // Assert Ordering
        response.Values[0].Name.Should().Be("Alpaca"); // SortOrder 10, A
        response.Values[1].Name.Should().Be("Cattle"); // SortOrder 10, C
        response.Values[2].Name.Should().Be("Sheep");  // SortOrder 20
        response.Values[3].Name.Should().Be("Pig");    // SortOrder 30
    }

    [Fact]
    public async Task Handle_WithLastUpdatedDateFilter_ReturnsOnlyModifiedSpecies()
    {
        // Arrange
        var filterDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mockData = new List<SpeciesDocument>
        {
            CreateSpeciesDoc("1", "OLD", "Old Species", 10, filterDate.AddDays(-1)),
            CreateSpeciesDoc("2", "NEW", "New Species", 20, filterDate.AddDays(1)),
            CreateSpeciesDoc("3", "EXACT", "Exact Species", 30, filterDate)
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        var query = new GetSpeciesQuery { LastUpdatedDate = filterDate };

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        var response = result.First();
        response.Count.Should().Be(2);
        response.Values.Should().Contain(s => s.Code == "NEW");
        response.Values.Should().Contain(s => s.Code == "EXACT");
        response.Values.Should().NotContain(s => s.Code == "OLD");
    }

    [Fact]
    public async Task Handle_WhenNoSpeciesExist_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpeciesDocument>());

        var query = new GetSpeciesQuery();

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        var response = result.First();
        response.Count.Should().Be(0);
        response.Values.Should().BeEmpty();
    }

    private static SpeciesDocument CreateSpeciesDoc(string id, string code, string name, int sortOrder, DateTime? lastModified = null)
    {
        return new SpeciesDocument
        {
            IdentifierId = id,
            Code = code,
            Name = name,
            SortOrder = sortOrder,
            LastModifiedDate = lastModified ?? DateTime.UtcNow
        };
    }
}