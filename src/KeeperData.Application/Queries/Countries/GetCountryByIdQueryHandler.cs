using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Countries;

public class GetCountryByIdQueryHandler(ICountryRepository repository) : IQueryHandler<GetCountryByIdQuery, CountryDTO>
{
    private readonly ICountryRepository _repository = repository;

    public async Task<CountryDTO> Handle(GetCountryByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Document with Id {request.Id} not found.");

        return new CountryDTO
        {
            Name = c.Name,
            Code = c.Code,
            IdentifierId = c.IdentifierId,
            LongName = c.LongName,
            DevolvedAuthorityFlag = c.DevolvedAuthority,
            EuTradeMemberFlag = c.EuTradeMember,
            LastUpdatedDate = c.LastModifiedDate
        };
    }
}