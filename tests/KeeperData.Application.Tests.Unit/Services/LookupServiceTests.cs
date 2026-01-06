using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class LookupServiceTests
{
    [Fact]
    public async Task SiteIdentifierTypeLookupService_GetByCodeAsync_ReturnsDocument()
    {
        var repo = new Mock<ISiteIdentifierTypeRepository>();
        var doc = new SiteIdentifierTypeDocument { IdentifierId = "1", Code = "CPHN", Name = "CPH", IsActive = true };

        repo.Setup(x => x.FindAsync("CPHN", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("1", "CPH"));

        repo.Setup(x => x.GetByIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var service = new SiteIdentifierTypeLookupService(repo.Object);
        var result = await service.GetByCodeAsync("CPHN", CancellationToken.None);

        result.Should().BeEquivalentTo(doc);
    }

    [Fact]
    public async Task SiteIdentifierTypeLookupService_GetByCodeAsync_ReturnsNull_WhenNotFound()
    {
        var repo = new Mock<ISiteIdentifierTypeRepository>();
        repo.Setup(x => x.FindAsync("UNKNOWN", It.IsAny<CancellationToken>()))
            .ReturnsAsync(((string?)null, (string?)null));

        var service = new SiteIdentifierTypeLookupService(repo.Object);
        var result = await service.GetByCodeAsync("UNKNOWN", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task PremiseActivityTypeLookupService_GetByCodeAsync_ReturnsDocument()
    {
        var repo = new Mock<IPremisesActivityTypeRepository>();
        var doc = new PremisesActivityTypeDocument { IdentifierId = "1", Code = "MKT", Name = "Market", IsActive = true };

        repo.Setup(x => x.FindAsync("MKT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("1", "Market"));

        repo.Setup(x => x.GetByIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var service = new PremiseActivityTypeLookupService(repo.Object);
        var result = await service.GetByCodeAsync("MKT", CancellationToken.None);

        result.Should().BeEquivalentTo(doc);
    }

    [Fact]
    public async Task PremiseActivityTypeLookupService_FindAsync_WhenCodeMatches_ReturnsIdAndName()
    {
        var repo = new Mock<IPremisesActivityTypeRepository>();
        repo.Setup(r => r.FindAsync("MKT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("1", "Market"));

        var service = new PremiseActivityTypeLookupService(repo.Object);
        var result = await service.FindAsync("MKT", CancellationToken.None);

        result.premiseActivityTypeId.Should().Be("1");
        result.premiseActivityTypeName.Should().Be("Market");
    }

    [Fact]
    public async Task PremiseActivityTypeLookupService_GetByCodeAsync_ReturnsNull_WhenNotFound()
    {
        var repo = new Mock<IPremisesActivityTypeRepository>();
        repo.Setup(r => r.FindAsync("UNKNOWN", It.IsAny<CancellationToken>()))
            .ReturnsAsync(((string?)null, (string?)null));

        var service = new PremiseActivityTypeLookupService(repo.Object);
        var result = await service.GetByCodeAsync("UNKNOWN", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ProductionUsageLookupService_FindAsync_NullInput_ReturnsNulls()
    {
        var repo = new Mock<IProductionUsageRepository>();
        var service = new ProductionUsageLookupService(repo.Object);
        var result = await service.FindAsync(null, CancellationToken.None);

        result.productionUsageId.Should().BeNull();
    }

    [Fact]
    public async Task ProductionUsageLookupService_FindAsync_HyphenInput_ReturnsNulls()
    {
        var repo = new Mock<IProductionUsageRepository>();
        var service = new ProductionUsageLookupService(repo.Object);
        var result = await service.FindAsync("-", CancellationToken.None);

        result.productionUsageId.Should().BeNull();
    }

    [Fact]
    public async Task ProductionUsageLookupService_FindAsync_DelegatesToRepository()
    {
        var repo = new Mock<IProductionUsageRepository>();
        repo.Setup(r => r.FindAsync("BEEF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("1", "Beef Production"));

        var service = new ProductionUsageLookupService(repo.Object);
        var result = await service.FindAsync("BEEF", CancellationToken.None);

        result.productionUsageId.Should().Be("1");
    }

    [Fact]
    public async Task ProductionUsageLookupService_GetByIdAsync_DelegatesToRepository()
    {
        var repo = new Mock<IProductionUsageRepository>();
        var doc = new ProductionUsageDocument { IdentifierId = "1", Code = "C", Description = "D" };
        repo.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var service = new ProductionUsageLookupService(repo.Object);
        var result = await service.GetByIdAsync("1", CancellationToken.None);

        result.Should().Be(doc);
    }

    [Fact]
    public async Task SpeciesTypeLookupService_FindAsync_NullInput_ReturnsNulls()
    {
        var repo = new Mock<ISpeciesRepository>();
        var service = new SpeciesTypeLookupService(repo.Object);
        var result = await service.FindAsync(null, CancellationToken.None);

        result.speciesTypeId.Should().BeNull();
    }

    [Fact]
    public async Task SpeciesTypeLookupService_FindAsync_HyphenInput_ReturnsNulls()
    {
        var repo = new Mock<ISpeciesRepository>();
        var service = new SpeciesTypeLookupService(repo.Object);
        var result = await service.FindAsync("-", CancellationToken.None);

        result.speciesTypeId.Should().BeNull();
    }

    [Fact]
    public async Task SpeciesTypeLookupService_FindAsync_DelegatesToRepository()
    {
        var repo = new Mock<ISpeciesRepository>();
        repo.Setup(r => r.FindAsync("BOV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("1", "Bovine"));

        var service = new SpeciesTypeLookupService(repo.Object);
        var result = await service.FindAsync("BOV", CancellationToken.None);

        result.speciesTypeId.Should().Be("1");
    }
}