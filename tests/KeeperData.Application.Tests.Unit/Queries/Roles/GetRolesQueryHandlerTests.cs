using FluentAssertions;
using KeeperData.Application.Queries.Roles;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.Roles;

public class GetRolesQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _repositoryMock;
    private readonly GetRolesQueryHandler _sut;

    public GetRolesQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRoleRepository>();
        _sut = new GetRolesQueryHandler(_repositoryMock.Object);
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

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        var query = new GetRolesQuery();

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().ContainSingle();
        var response = result.First();

        response.Count.Should().Be(3);
        response.Values.Should().HaveCount(3);

        // Assert Ordering (AC7)
        response.Values[0].Name.Should().Be("Agent"); // SortOrder 10, A
        response.Values[1].Name.Should().Be("Livestock Keeper"); // SortOrder 10, L
        response.Values[2].Name.Should().Be("Livestock Owner");  // SortOrder 20
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

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        var query = new GetRolesQuery { LastUpdatedDate = filterDate };

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
    public async Task Handle_WhenNoRolesExist_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleDocument>());

        var query = new GetRolesQuery();

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        var response = result.First();
        response.Count.Should().Be(0);
        response.Values.Should().BeEmpty();
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