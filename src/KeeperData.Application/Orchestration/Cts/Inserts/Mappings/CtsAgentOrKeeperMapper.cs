using KeeperData.Application.Extensions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Parties.Rules;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Mappings;

public static class CtsAgentOrKeeperMapper
{
    public static async Task<List<CtsPartyDocument>> ToSilver(
        List<CtsAgentOrKeeper> rawParties,
        InferredRoleType inferredRoleType,
        Func<string, CancellationToken, Task<(string? RoleTypeId, string? RoleTypeName)>> resolveRoleType,
        CancellationToken cancellationToken)
    {
        var result = new List<CtsPartyDocument>();

        foreach (var p in rawParties?.Where(x => x.LID_FULL_IDENTIFIER != null) ?? [])
        {
            var roleNameToLookup = EnumExtensions.GetDescription(inferredRoleType);
            var (roleTypeId, roleTypeName) = !string.IsNullOrWhiteSpace(roleNameToLookup)
                ? await resolveRoleType(roleNameToLookup, cancellationToken)
                : (null, null);

            var party = new CtsPartyDocument
            {
                Id = Guid.NewGuid().ToString(),
                LastUpdatedBatchId = p.BATCH_ID,
                Deleted = false,

                CountyParishHoldingNumber = p.LID_FULL_IDENTIFIER,

                PartyId = p.PAR_ID.ToString(),
                PartyTypeId = p.DeterminePartyType().ToString(),

                PartyFullName = null,

                PartyTitleTypeIdentifier = p.PAR_TITLE,
                PartyFirstName = p.PAR_INITIALS,
                PartyLastName = p.PAR_SURNAME,

                Address = new AddressDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    AddressLine = p.ADR_ADDRESS_2,
                    AddressLocality = p.ADR_ADDRESS_3,
                    AddressStreet = p.ADR_ADDRESS_4,
                    AddressTown = p.ADR_ADDRESS_5,
                    AddressPostCode = p.ADR_POST_CODE,

                    CountryIdentifier = null,
                    CountryCode = null,

                    UniquePropertyReferenceNumber = null
                },

                Communication = new CommunicationDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Email = p.PAR_EMAIL_ADDRESS,
                    Mobile = p.PAR_MOBILE_NUMBER,
                    Landline = p.PAR_TEL_NUMBER
                },

                Roles =
                [
                    new PartyRoleDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        RoleTypeId = roleTypeId,
                        RoleTypeName = roleTypeName,
                        SourceRoleName = roleNameToLookup,
                        EffectiveFromData = p.LPR_EFFECTIVE_FROM_DATE,
                        EffectiveToData = p.LPR_EFFECTIVE_TO_DATE
                    }
                ]
            };

            result.Add(party);
        }

        return result;
    }
}