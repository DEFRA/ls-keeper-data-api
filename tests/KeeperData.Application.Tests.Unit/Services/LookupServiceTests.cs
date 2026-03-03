using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class LookupServiceTests
{
    [Fact]
    public async Task SiteIdentifierTypeLookupService_GetByCodeAsync_ReturnsDocument()
    {
        var cache = new Mock<IReferenceDataCache>();
        var doc = new SiteIdentifierTypeDocument { IdentifierId = "1", Code = "CPHN", Name = "CPH", IsActive = true };
        cache.Setup(c => c.SiteIdentifierTypes).Returns(new[] { doc });

        var service = new SiteIdentifierTypeLookupService(cache.Object);
        var result = await service.GetByCodeAsync("CPHN", CancellationToken.None);

        result.Should().BeEquivalentTo(doc);
    }

    [Fact]
    public async Task SiteIdentifierTypeLookupService_GetByCodeAsync_ReturnsNull_WhenNotFound()
    {
        var cache = new Mock<IReferenceDataCache>();
        cache.Setup(c => c.SiteIdentifierTypes).Returns(Array.Empty<SiteIdentifierTypeDocument>());

        var service = new SiteIdentifierTypeLookupService(cache.Object);
        var result = await service.GetByCodeAsync("UNKNOWN", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task PremiseActivityTypeLookupService_GetByCodeAsync_ReturnsDocument()
    {
        var cache = new Mock<IReferenceDataCache>();
        var doc = new PremisesActivityTypeDocument { IdentifierId = "1", Code = "MKT", Name = "Market", IsActive = true };
        cache.Setup(c => c.PremisesActivityTypes).Returns(new[] { doc });

        var service = new PremiseActivityTypeLookupService(cache.Object);
        var result = await service.GetByCodeAsync("MKT", CancellationToken.None);

        result.Should().BeEquivalentTo(doc);
    }

    [Fact]
    public async Task PremiseActivityTypeLookupService_FindAsync_WhenCodeMatches_ReturnsIdAndName()
    {
        var cache = new Mock<IReferenceDataCache>();
        var doc = new PremisesActivityTypeDocument { IdentifierId = "1", Code = "MKT", Name = "Market", IsActive = true };
        cache.Setup(c => c.PremisesActivityTypes).Returns(new[] { doc });

        var service = new PremiseActivityTypeLookupService(cache.Object);
        var result = await service.FindAsync("MKT", CancellationToken.None);

        result.premiseActivityTypeId.Should().Be("1");
        result.premiseActivityTypeName.Should().Be("Market");
    }

    [Fact]
    public async Task PremiseActivityTypeLookupService_GetByCodeAsync_ReturnsNull_WhenNotFound()
    {
        var cache = new Mock<IReferenceDataCache>();
        cache.Setup(c => c.PremisesActivityTypes).Returns(Array.Empty<PremisesActivityTypeDocument>());

        var service = new PremiseActivityTypeLookupService(cache.Object);
        var result = await service.GetByCodeAsync("UNKNOWN", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ProductionUsageLookupService_FindAsync_NullInput_ReturnsNulls()
    {
        var cache = new Mock<IReferenceDataCache>();
        cache.Setup(c => c.ProductionUsages).Returns(Array.Empty<ProductionUsageDocument>());
        var service = new ProductionUsageLookupService(cache.Object);
        var result = await service.FindAsync(null, CancellationToken.None);

        result.productionUsageId.Should().BeNull();
    }

    [Fact]
    public async Task ProductionUsageLookupService_FindAsync_HyphenInput_ReturnsNulls()
    {
        var cache = new Mock<IReferenceDataCache>();
        cache.Setup(c => c.ProductionUsages).Returns(Array.Empty<ProductionUsageDocument>());
        var service = new ProductionUsageLookupService(cache.Object);
        var result = await service.FindAsync("-", CancellationToken.None);

        result.productionUsageId.Should().BeNull();
    }

    [Fact]
    public async Task ProductionUsageLookupService_FindAsync_DelegatesToCache()
    {
        var cache = new Mock<IReferenceDataCache>();
        var doc = new ProductionUsageDocument { IdentifierId = "1", Code = "BEEF", Description = "Beef Production" };
        cache.Setup(c => c.ProductionUsages).Returns(new[] { doc });

        var service = new ProductionUsageLookupService(cache.Object);
        var result = await service.FindAsync("BEEF", CancellationToken.None);

        result.productionUsageId.Should().Be("1");
    }

    [Fact]
    public async Task ProductionUsageLookupService_GetByIdAsync_DelegatesToCache()
    {
        var cache = new Mock<IReferenceDataCache>();
        var doc = new ProductionUsageDocument { IdentifierId = "1", Code = "C", Description = "D" };
        cache.Setup(c => c.ProductionUsages).Returns(new[] { doc });

        var service = new ProductionUsageLookupService(cache.Object);
        var result = await service.GetByIdAsync("1", CancellationToken.None);

        result.Should().Be(doc);
    }

    [Fact]
    public async Task SpeciesTypeLookupService_FindAsync_NullInput_ReturnsNulls()
    {
        var cache = new Mock<IReferenceDataCache>();
        cache.Setup(c => c.Species).Returns(Array.Empty<SpeciesDocument>());
        var service = new SpeciesTypeLookupService(cache.Object);
        var result = await service.FindAsync(null, CancellationToken.None);

        result.speciesTypeId.Should().BeNull();
    }

    [Fact]
    public async Task SpeciesTypeLookupService_FindAsync_HyphenInput_ReturnsNulls()
    {
        var cache = new Mock<IReferenceDataCache>();
        cache.Setup(c => c.Species).Returns(Array.Empty<SpeciesDocument>());
        var service = new SpeciesTypeLookupService(cache.Object);
        var result = await service.FindAsync("-", CancellationToken.None);

        result.speciesTypeId.Should().BeNull();
    }

    [Fact]
    public async Task SpeciesTypeLookupService_FindAsync_DelegatesToCache()
    {
        var cache = new Mock<IReferenceDataCache>();
        var doc = new SpeciesDocument
        {
            IdentifierId = "1",
            Code = "BOV",
            Name = "Bovine",
            IsActive = true,
            SortOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };
        cache.Setup(c => c.Species).Returns(new[] { doc });

        var service = new SpeciesTypeLookupService(cache.Object);
        var result = await service.FindAsync("BOV", CancellationToken.None);

        result.speciesTypeId.Should().Be("1");
    }
}