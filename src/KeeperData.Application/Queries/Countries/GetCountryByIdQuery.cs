using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Countries
{
    public record GetCountryByIdQuery(string Id) : IQuery<CountryDTO>;
}