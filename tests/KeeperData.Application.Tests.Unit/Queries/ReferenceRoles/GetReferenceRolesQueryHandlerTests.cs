using FluentAssertions;
using KeeperData.Application.Queries.ReferenceRoles;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.ReferenceRoles;

public class GetReferenceRolesQueryHandlerTests
{
    private readonly Mock<IReferenceDataCache> _cacheMock;
    private readonly GetReferenceRolesQueryHandler _sut;

    public GetReferenceRolesQueryHandlerTests()
    {
        _cacheMock = new Mock<IReferenceDataCache>();
        _sut = new GetReferenceRolesQueryHandler(_cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ReturnsRolesOrderedBySortOrderThenName()
    {
        // Arrange
        var mockData = new List<RoleDocument>
        {
            CreateDoc("3", "OWNER", "Livestock Owner", 20),
            CreateDoc("1", "KEEPER", "Livestock Keeper", 10),
            CreateDoc("2", "AGENT", "Agent", 10)
        };

        _cacheMock.Setup(c => c.Roles).Returns(mockData);

        var query = new GetReferenceRolesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(3);
        result.Values.Should().HaveCount(3);

        result.Values[0].Name.Should().Be("Agent"); // SortOrder 10, A
        result.Values[1].Name.Should().Be("Livestock Keeper"); // SortOrder 10, L
        result.Values[2].Name.Should().Be("Livestock Owner");  // SortOrder 20
    }

    [Fact]
    public async Task Handle_WithLastUpdatedDateFilter_ReturnsOnlyModifiedRoles()
    {
        // Arrange
        var filterDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mockData = new List<RoleDocument>
        {
            CreateDoc("1", "OLD", "Old Role", 10, filterDate.AddDays(-1)),
            CreateDoc("2", "NEW", "New Role", 20, filterDate.AddDays(1)),
            CreateDoc("3", "EXACT", "Exact Role", 30, filterDate)
        };

        _cacheMock.Setup(c => c.Roles).Returns(mockData);

        var query = new GetReferenceRolesQuery { LastUpdatedDate = filterDate };

        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(2);
        result.Values.Should().Contain(s => s.Code == "NEW");
        result.Values.Should().Contain(s => s.Code == "EXACT");
        result.Values.Should().NotContain(s => s.Code == "OLD");
    }

    [Fact]
    public async Task Handle_WhenNoRolesExist_ReturnsEmptyList()
    {
        // Arrange
        _cacheMock.Setup(c => c.Roles).Returns([]);

        var query = new GetReferenceRolesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);
        result.Values.Should().BeEmpty();
    }

    private static RoleDocument CreateDoc(string id, string code, string name, int sortOrder, DateTime? lastModified = null)
    {
        return new RoleDocument
        {
            IdentifierId = id,
            Code = code,
            Name = name,
            SortOrder = sortOrder,
            LastModifiedDate = lastModified ?? DateTime.UtcNow
        };
    }
}