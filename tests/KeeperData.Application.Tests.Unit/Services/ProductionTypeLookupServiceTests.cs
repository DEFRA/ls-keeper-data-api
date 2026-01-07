using FluentAssertions;
using KeeperData.Application.Services;

namespace KeeperData.Application.Tests.Unit.Services;

public class ProductionTypeLookupServiceTests
{
    private readonly ProductionTypeLookupService _sut = new();
    //Test verify that stub behavior 
    [Fact]
    public async Task GetByIdAsync_ReturnsStubResult()
    {
        var result = await _sut.GetByIdAsync("123", CancellationToken.None);
        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be("123");
    }

    [Fact]
    public async Task GetByIdAsync_NullId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(null, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_ReturnsNulls()
    {
        var result = await _sut.FindAsync("test", CancellationToken.None);
        result.productionTypeId.Should().BeNull();
        result.productionTypeName.Should().BeNull();
    }
}