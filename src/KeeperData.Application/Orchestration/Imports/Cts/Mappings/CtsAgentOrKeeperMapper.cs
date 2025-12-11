using KeeperData.Application.Extensions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Parties.Rules;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Application.Orchestration.Imports.Cts.Mappings;

public static class CtsAgentOrKeeperMapper
{
    public static async Task<List<CtsPartyDocument>> ToSilver(
        List<CtsAgentOrKeeper> rawParties,
        InferredRoleType inferredRoleType,
        Func<string?, CancellationToken, Task<(string? RoleTypeId, string? RoleTypeName)>> resolveRoleType,
        CancellationToken cancellationToken)
    {
        var result = new List<CtsPartyDocument>();

        var roleNameToLookup = inferredRoleType.GetDescription();
        var (roleTypeId, roleTypeName) = await resolveRoleType(roleNameToLookup, cancellationToken);

        foreach (var p in rawParties?.Where(x => x.LID_FULL_IDENTIFIER != null) ?? [])
        {
            var party = ToSilver(
                p,
                (roleNameToLookup, roleTypeId, roleTypeName));

            result.Add(party);
        }

        return result;
    }

    public static CtsPartyDocument ToSilver(
        CtsAgentOrKeeper p,
        (string? RoleNameToLookup, string? RoleTypeId, string? RoleTypeName) roleTypeInfo)
    {
        var partyTypeId = p.DeterminePartyType().ToString();

        var result = new CtsPartyDocument
        {
            // Id - Leave to support upsert assigning Id

            LastUpdatedBatchId = p.BATCH_ID,
            CreatedDate = p.CreatedAtUtc ?? DateTime.UtcNow,
            LastUpdatedDate = p.UpdatedAtUtc ?? DateTime.UtcNow,
            Deleted = p.IsDeleted ?? false,

            CountyParishHoldingNumber = p.LID_FULL_IDENTIFIER.LidIdentifierToCph(),

            PartyId = p.PAR_ID,
            PartyTypeId = partyTypeId,

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
                        RoleTypeId = roleTypeInfo.RoleTypeId,
                        RoleTypeName = roleTypeInfo.RoleTypeName,
                        SourceRoleName = roleTypeInfo.RoleNameToLookup,
                        EffectiveFromDate = p.LPR_EFFECTIVE_FROM_DATE,
                        EffectiveToDate = p.LPR_EFFECTIVE_TO_DATE
                    }
                ]
        };

        return result;
    }
}