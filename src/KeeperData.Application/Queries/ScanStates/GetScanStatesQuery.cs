using KeeperData.Core.Documents;

namespace KeeperData.Application.Queries.ScanStates;

public record GetScanStatesQuery : IQuery<GetScanStatesQuery.Result>
{
    public record Result(IEnumerable<ScanStateDocument> ScanStates);
}