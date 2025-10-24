using KeeperData.Application.Extensions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Parties.Formatters;
using KeeperData.Core.Domain.Parties.Rules;
using KeeperData.Core.Domain.Sites.Extensions;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Mappings;

public static class SamHolderMapper
{
    private const string SaonLabel = "";

    public static async Task<List<SamPartyDocument>> ToSilver(
        List<SamCphHolder> rawHolders,
        string holdingIdentifier,
        InferredRoleType inferredRoleType,
        Func<string?, CancellationToken, Task<(string? RoleTypeId, string? RoleTypeName)>> resolveRoleType,
        Func<string?, CancellationToken, Task<(string? CountryId, string? CountryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var result = new List<SamPartyDocument>();

        foreach (var p in rawHolders?.Where(x => x.PARTY_ID != null) ?? [])
        {
            var roleNameToLookup = EnumExtensions.GetDescription(inferredRoleType);
            var (roleTypeId, roleTypeName) = await resolveRoleType(roleNameToLookup, cancellationToken);
            var (countryId, countryName) = await resolveCountry(p.COUNTRY_CODE, cancellationToken);
            var partyTypeId = p.DeterminePartyType().ToString();
            var addressLine = FormatAddressExtensions.FormatAddressRange(
                            p.SAON_START_NUMBER, p.SAON_START_NUMBER_SUFFIX,
                            p.SAON_END_NUMBER, p.SAON_END_NUMBER_SUFFIX,
                            p.PAON_START_NUMBER, p.PAON_START_NUMBER_SUFFIX,
                            p.PAON_END_NUMBER, p.PAON_END_NUMBER_SUFFIX,
                            saonLabel: SaonLabel);

            var party = new SamPartyDocument
            {
                // Id - Leave to support upsert assigning Id

                LastUpdatedBatchId = p.BATCH_ID,
                Deleted = p.IsDeleted ?? false,

                CountyParishHoldingNumber = holdingIdentifier,

                PartyId = p.PARTY_ID.ToString(),
                PartyTypeId = partyTypeId,

                PartyFullName = PartyNameFormatters.FormatPartyFullName(
                    p.ORGANISATION_NAME,
                    p.PERSON_TITLE,
                    p.PERSON_GIVEN_NAME,
                    p.PERSON_GIVEN_NAME2,
                    p.PERSON_FAMILY_NAME),

                PartyTitleTypeIdentifier = p.PERSON_TITLE,
                PartyFirstName = PartyNameFormatters.FormatPartyFirstName(
                    p.PERSON_GIVEN_NAME,
                    p.PERSON_GIVEN_NAME2),
                PartyLastName = p.PERSON_FAMILY_NAME,

                Address = new AddressDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    AddressLine = addressLine,
                    AddressLocality = p.LOCALITY,
                    AddressStreet = p.STREET,
                    AddressTown = p.TOWN,
                    AddressPostCode = p.POSTCODE,

                    CountryIdentifier = countryId,
                    CountryCode = p.COUNTRY_CODE,

                    UniquePropertyReferenceNumber = p.UDPRN
                },

                Communication = new CommunicationDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Email = p.INTERNET_EMAIL_ADDRESS,
                    Mobile = p.MOBILE_NUMBER,
                    Landline = p.TELEPHONE_NUMBER
                },

                Roles =
                [
                    new PartyRoleDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        RoleTypeId = roleTypeId,
                        RoleTypeName = roleTypeName,
                        SourceRoleName = roleNameToLookup,
                        EffectiveFromData = null,
                        EffectiveToData = null
                    }
                ]
            };

            result.Add(party);
        }

        return result;
    }
}