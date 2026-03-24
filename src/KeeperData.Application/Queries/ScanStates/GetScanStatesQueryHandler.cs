using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.ScanStates;

public class GetScanStatesQueryHandler(IScanStateRepository scanStateRepository)
    : IQueryHandler<GetScanStatesQuery, GetScanStatesQuery.Result>
{
    private readonly IScanStateRepository _scanStateRepository = scanStateRepository;

    public async Task<GetScanStatesQuery.Result> Handle(
        GetScanStatesQuery request,
        CancellationToken cancellationToken)
    {
        var scanStates = await _scanStateRepository.GetAllAsync(0, 100, cancellationToken);
        return new GetScanStatesQuery.Result(scanStates);
    }
}