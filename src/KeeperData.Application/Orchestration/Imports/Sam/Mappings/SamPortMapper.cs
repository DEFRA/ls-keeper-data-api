using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamPortMapper
{
    public static List<SamPortDocument> ToSilver(List<SamPort> rawPorts)
    {
        return rawPorts?
            .Where(x => !string.IsNullOrWhiteSpace(x.CPH))
            .Select(ToSilver)
            .ToList() ?? [];
    }

    public static SamPortDocument ToSilver(SamPort p)
    {
        return new SamPortDocument
        {
            LastUpdatedBatchId = p.BATCH_ID,
            CreatedDate = p.CreatedAtUtc ?? DateTime.UtcNow,
            LastUpdatedDate = p.UpdatedAtUtc ?? DateTime.UtcNow,
            Deleted = p.IsDeleted ?? false,
            ChangeType = p.CHANGE_TYPE,

            CountyParishHoldingNumber = p.CPH,
            PremisesName = p.PREMISES_NAME,
            AddressLine1 = p.ADDRESS_LINE_1,
            AddressLine2 = p.ADDRESS_LINE_2,
            AddressLine3 = p.ADDRESS_LINE_3,
            Postcode = p.POSTCODE,
            MapReference = p.MAP_REFERENCE,
            Easting = p.EASTING,
            Northing = p.NORTHING
        };
    }

    public static List<PortDocument> ToGold(List<SamPortDocument> silverPorts, string holdingIdentifier)
    {
        return silverPorts?
            .Select(p => ToGold(p, holdingIdentifier))
            .ToList() ?? [];
    }

    public static PortDocument ToGold(SamPortDocument silver, string holdingIdentifier)
    {
        return new PortDocument
        {
            CreatedDate = silver.CreatedDate,
            LastUpdatedDate = silver.LastUpdatedDate,
            Deleted = silver.Deleted,
            ChangeType = silver.ChangeType,
            HoldingIdentifier = holdingIdentifier,
            Name = silver.PremisesName,
            AddressLine1 = silver.AddressLine1,
            AddressLine2 = silver.AddressLine2,
            AddressLine3 = silver.AddressLine3,
            Postcode = silver.Postcode,
            MapReference = silver.MapReference,
            Easting = silver.Easting,
            Northing = silver.Northing,
            Source = "SAM"
        };
    }
}