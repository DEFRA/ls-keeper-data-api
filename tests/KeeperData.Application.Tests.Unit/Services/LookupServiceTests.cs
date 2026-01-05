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
}