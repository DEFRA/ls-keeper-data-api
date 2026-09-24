using KeeperData.Core.DTOs;

namespace KeeperData.Core.Repositories;

/// <summary>
/// Reads holding details from the locally cached SAM read model.
/// </summary>
public interface IHoldingDetailRepository
{
    /// <summary>
    /// Retrieves holding detail for the specified CPH, or null if not found.
    /// </summary>
    Task<HoldingDetail?> GetHoldingDetailByCphAsync(string cph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of holding details and the timestamp of the snapshot used.
    /// </summary>
    Task<(List<HoldingDetail> Items, int TotalCount, DateTime? DataTimestamp)> GetPagedHoldingsAsync(
        int page,
        int pageSize,
        string? sort,
        string? order,
        CancellationToken cancellationToken = default);
}