using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Parties.Formatters;
using KeeperData.Core.Domain.Parties.Rules;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Extensions;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamHolderMapper
{
    public static async Task<List<SamPartyDocument>> ToSilver(
        List<SamCphHolder> rawHolders,
        InferredRoleType inferredRoleType,
        Func<string?, CancellationToken, Task<(string? RoleTypeId, string? RoleTypeCode, string? RoleTypeName)>> resolveRoleType,
        Func<string?, string?, CancellationToken, Task<(string? countryId, string? countryCode, string? countryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var result = new List<SamPartyDocument>();

        var roleNameToLookup = EnumExtensions.GetDescription(inferredRoleType);
        var (roleTypeId, roleTypeCode, roleTypeName) = await resolveRoleType(roleNameToLookup, cancellationToken);

        foreach (var p in rawHolders?.Where(x => x.PARTY_ID != null) ?? [])
        {
            var party = await ToSilver(
                p,
                (roleNameToLookup, roleTypeId, roleTypeCode, roleTypeName),
                resolveCountry,
                cancellationToken);

            result.Add(party);
        }

        return result;
    }

    public static async Task<SamPartyDocument> ToSilver(
        SamCphHolder p,
        (string? RoleNameToLookup, string? RoleTypeId, string? RoleTypeCode, string? RoleTypeName) roleTypeInfo,
        Func<string?, string?, CancellationToken, Task<(string? countryId, string? countryCode, string? countryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var (countryId, countryCode, _) = await resolveCountry(p.COUNTRY_CODE, p.UK_INTERNAL_CODE, cancellationToken);
        var partyTypeId = p.DeterminePartyType().ToString();
        var addressLine = AddressFormatters.FormatAddressRange(
                        p.SAON_START_NUMBER, p.SAON_START_NUMBER_SUFFIX,
                        p.SAON_END_NUMBER, p.SAON_END_NUMBER_SUFFIX,
                        p.PAON_START_NUMBER, p.PAON_START_NUMBER_SUFFIX,
                        p.PAON_END_NUMBER, p.PAON_END_NUMBER_SUFFIX,
                        p.SAON_DESCRIPTION, p.PAON_DESCRIPTION);

        var result = new SamPartyDocument
        {
            // Id - Leave to support upsert assigning Id

            LastUpdatedBatchId = p.BATCH_ID,
            CreatedDate = p.CreatedAtUtc ?? DateTime.UtcNow,
            LastUpdatedDate = p.UpdatedAtUtc ?? DateTime.UtcNow,
            Deleted = p.IsDeleted ?? false,

            PartyId = p.PARTY_ID.ToString(),
            PartyTypeId = partyTypeId,

            PartyFullName = PartyNameFormatters.FormatPartyFullName(
                p.ORGANISATION_NAME,
                p.PERSON_TITLE,
                p.PERSON_GIVEN_NAME,
                p.PERSON_GIVEN_NAME2,
                p.PERSON_INITIALS,
                p.PERSON_FAMILY_NAME),

            PartyTitleTypeIdentifier = p.PERSON_TITLE,
            PartyFirstName = PartyNameFormatters.FormatPartyFirstName(
                p.PERSON_GIVEN_NAME,
                p.PERSON_GIVEN_NAME2),
            PartyInitials = p.PERSON_INITIALS,
            PartyLastName = p.PERSON_FAMILY_NAME,

            CphList = p.CphList ?? [],

            Address = new AddressDocument
            {
                IdentifierId = Guid.NewGuid().ToString(),
                AddressLine = addressLine,
                AddressLocality = p.LOCALITY,
                AddressStreet = p.STREET,
                AddressTown = p.TOWN,
                AddressPostCode = p.POSTCODE,
                CountrySubDivision = p.UK_INTERNAL_CODE,

                CountryIdentifier = countryId,
                CountryCode = countryCode,

                UniquePropertyReferenceNumber = p.UDPRN
            },

            Communication = new CommunicationDocument
            {
                IdentifierId = Guid.NewGuid().ToString(),
                Email = p.INTERNET_EMAIL_ADDRESS,
                Mobile = p.MOBILE_NUMBER,
                Landline = p.TELEPHONE_NUMBER
            },

            Roles = roleTypeInfo.RoleTypeId != null
                ? [
                    new PartyRoleDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        RoleTypeId = roleTypeInfo.RoleTypeId,
                        RoleTypeCode = roleTypeInfo.RoleTypeCode,
                        RoleTypeName = roleTypeInfo.RoleTypeName,
                        SourceRoleName = roleTypeInfo.RoleNameToLookup,
                        EffectiveFromDate = null,
                        EffectiveToDate = null
                    }
                  ]
                : []
        };

        return result;
    }
}