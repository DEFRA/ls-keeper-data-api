using FluentAssertions;
using KeeperData.Application.Queries.Countries;
using KeeperData.Application.Queries.Countries.Adapters;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.Countries;

public class GetCountriesQueryHandlerTests
{
    [Fact]
    public async Task QueryHandlerShouldReturnRecordinCorrectMapping()
    {
        var repoMock = new Mock<ICountryRepository>();
        var lastUpdated = new DateTime(2001, 01, 01, 12, 13, 14);
        var request = new GetCountriesQuery() { Code = ["DE"] };
        var countryDoc = new CountryDocument { IdentifierId = "DE-123", Code = "DE", Name = "Germany", LongName = "longname", IsActive = true, DevolvedAuthority = false, EuTradeMember = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow, LastModifiedDate = lastUpdated };

        repoMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CountryDocument> { countryDoc });
        var sut = new GetCountriesQueryHandler(new CountriesQueryAdapter(repoMock.Object));
        var result = await sut.Handle(request, CancellationToken.None);

        var expected = new PaginatedResult<CountryDTO>
        {
            Page = 1,
            PageSize = 10,
            Count = 1,
            Values =
            [
                new CountryDTO
                {
                    Code = "DE",
                    IdentifierId = "DE-123",
                    DevolvedAuthorityFlag = false,
                    EuTradeMemberFlag = true,
                    LastUpdatedDate = lastUpdated,
                    LongName = "longname",
                    Name = "Germany"
                }
            ]
        };

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task WhenCountryDoesNotExistThenQueryHandlerShouldThrow()
    {
        var repoMock = new Mock<ICountryRepository>();
        repoMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CountryDocument>());
        var request = new GetCountriesQuery() { Code = ["DE"] };
        var sut = new GetCountriesQueryHandler(new CountriesQueryAdapter(repoMock.Object));

        var result = await sut.Handle(request, CancellationToken.None);

        Assert.Empty(result.Values);
    }
}