using FluentAssertions;
using KeeperData.Application.Queries.IdentifierTypes;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.IdentifierTypes;

public class GetIdentifierTypesQueryHandlerTests
{
    private readonly Mock<ISiteIdentifierTypeRepository> _repositoryMock;
    private readonly GetIdentifierTypesQueryHandler _sut;

    public GetIdentifierTypesQueryHandlerTests()
    {
        _repositoryMock = new Mock<ISiteIdentifierTypeRepository>();
        _sut = new GetIdentifierTypesQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ReturnsIdentifierTypesOrderedByName()
    {
        // Arrange
        var mockData = new List<SiteIdentifierTypeDocument>
        {
            CreateDoc("3", "PRTN", "Port Number"),
            CreateDoc("1", "CPHN", "CPH Number"),
            CreateDoc("2", "FSAN", "FSA Number")
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        var query = new GetIdentifierTypesQuery();

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().ContainSingle();
        var response = result.First();

        response.Count.Should().Be(3);
        response.Values.Should().HaveCount(3);

        // Assert Ordering & Mapping
        response.Values[0].Name.Should().Be("CPH Number");
        response.Values[0].Description.Should().BeNull();
        response.Values[1].Name.Should().Be("FSA Number");
        response.Values[2].Name.Should().Be("Port Number");
    }

    [Fact]
    public async Task Handle_WithLastUpdatedDateFilter_ReturnsOnlyModifiedIdentifierTypes()
    {
        // Arrange
        var filterDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mockData = new List<SiteIdentifierTypeDocument>
        {
            CreateDoc("1", "OLD", "Old", filterDate.AddDays(-1)),
            CreateDoc("2", "NEW", "New", filterDate.AddDays(1)),
            CreateDoc("3", "EXACT", "Exact", filterDate)
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        var query = new GetIdentifierTypesQuery { LastUpdatedDate = filterDate };

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
    public async Task Handle_WhenNoIdentifierTypesExist_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SiteIdentifierTypeDocument>());

        var query = new GetIdentifierTypesQuery();

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        var response = result.First();
        response.Count.Should().Be(0);
        response.Values.Should().BeEmpty();
    }

    private static SiteIdentifierTypeDocument CreateDoc(string id, string code, string name, DateTime? lastModified = null)
    {
        return new SiteIdentifierTypeDocument
        {
            IdentifierId = id,
            Code = code,
            Name = name,
            LastModifiedDate = lastModified ?? DateTime.UtcNow
        };
    }
}