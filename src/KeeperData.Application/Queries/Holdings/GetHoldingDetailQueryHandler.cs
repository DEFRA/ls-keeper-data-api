using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Holdings;

public class GetHoldingDetailQueryHandler(IHoldingDetailRepository repository)
    : IQueryHandler<GetHoldingDetailQuery, HoldingDetail>
{
    private readonly IHoldingDetailRepository _repository = repository;

    public async Task<HoldingDetail> Handle(GetHoldingDetailQuery request, CancellationToken cancellationToken)
    {
        var result = await _repository.GetHoldingDetailByCphAsync(request.Cph, cancellationToken);
        if (result is null)
        {
            throw new NotFoundException($"Holding with CPH '{request.Cph}' was not found.");
        }

        return result;
    }
}
