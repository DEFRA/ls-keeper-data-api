using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;

namespace KeeperData.Core.Repositories;

public interface IFacilityBusinessActivityMapRepository : IReferenceDataRepository<FacilityBusinessActivityMapListDocument, FacilityBusinessActivityMapDocument>
{
    Task<FacilityBusinessActivityMapDocument?> FindByActivityCodeAsync(string? lookupValue, CancellationToken cancellationToken = default);
}