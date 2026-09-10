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
}