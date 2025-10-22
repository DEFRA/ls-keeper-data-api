using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Mappings;

public static class CtsHoldingMapper
{
    public static List<CtsHoldingDocument> ToSilver(List<CtsCphHolding> rawHoldings)
    {
        var result = rawHoldings?
            .Where(x => x.LID_FULL_IDENTIFIER != null)
            .Select(h => new CtsHoldingDocument()
            {
                // Id - Leave to support upsert assigning Id

                LastUpdatedBatchId = h.BATCH_ID,
                Deleted = h.IsDeleted ?? false,

                CountyParishHoldingNumber = h.LID_FULL_IDENTIFIER,
                AlternativeHoldingIdentifier = null,

                CphTypeIdentifier = h.LTY_LOC_TYPE,
                LocationName = h.ADR_NAME,

                HoldingStartDate = h.LOC_EFFECTIVE_FROM,
                HoldingEndDate = h.LOC_EFFECTIVE_TO,
                HoldingStatus = h.LOC_EFFECTIVE_TO.HasValue
                                    && h.LOC_EFFECTIVE_TO != default
                                    ? HoldingStatusType.Inactive.ToString()
                                    : HoldingStatusType.Active.ToString(),

                PremiseActivityTypeId = null,
                PremiseActivityTypeCode = null,

                PremiseTypeIdentifier = null,
                PremiseTypeCode = null,

                Location = new LocationDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Easting = null,
                    Northing = null,
                    OsMapReference = null,
                    Address = new AddressDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        AddressLine = h.ADR_ADDRESS_2,
                        AddressLocality = h.ADR_ADDRESS_3,
                        AddressStreet = h.ADR_ADDRESS_4,
                        AddressTown = h.ADR_ADDRESS_5,
                        AddressPostCode = h.ADR_POST_CODE,

                        CountryIdentifier = null,
                        CountryCode = null,

                        UniquePropertyReferenceNumber = h.LOC_MAP_REFERENCE
                    }
                },

                Communication = new CommunicationDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Email = null,
                    Mobile = h.LOC_MOBILE_NUMBER,
                    Landline = h.LOC_TEL_NUMBER
                },

                GroupMarks = []
            });

        return result?.ToList() ?? [];
    }
}