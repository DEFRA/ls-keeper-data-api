using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Holdings;

public class GetHoldingDetailQuery : IQuery<HoldingDetail>
{
    public required string County { get; init; }
    public required string Parish { get; init; }
    public required string Holding { get; init; }

    public string Cph => $"{County}/{Parish}/{Holding}";
}
