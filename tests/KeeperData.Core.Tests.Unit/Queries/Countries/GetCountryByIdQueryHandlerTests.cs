using FluentAssertions;
using KeeperData.Application.Queries.Countries;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Core.Tests.Unit.Queries.Countries;

public class GetCountryByIdQueryHandlerTests
{
    [Fact]
    public async Task QueryHandlerShouldReturnRecordinCorrectMapping()
    {
        var repoMock = new Mock<ICountryRepository>();
        var id = "abc-123";
        var lastUpdated = new DateTime(2001, 01, 01, 12, 13, 14);
        var request = new GetCountryByIdQuery(id);
        var countryDoc = new CountryDocument { IdentifierId = "DE-123", Code = "DE", Name = "Germany", LongName = "longname", IsActive = true, DevolvedAuthority = false, EuTradeMember = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow, LastModifiedDate = lastUpdated };

        repoMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(countryDoc);
        var sut = new GetCountryByIdQueryHandler(repoMock.Object);
        var result = await sut.Handle(request, CancellationToken.None);

        var expected = new CountryDTO { Code = "DE", IdentifierId = "DE-123", DevolvedAuthorityFlag = false, EuTradeMemberFlag = true, LastUpdatedDate = lastUpdated, LongName = "longname", Name = "Germany" };
        result.Should().BeEquivalentTo(expected);
    }
}