using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Parties;
using KeeperData.Core.Domain.Parties.Formatters;
using KeeperData.Core.Domain.Parties.Rules;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Mappings;

public static class SamPartyMapper
{
    private const string SaonLabel = "";

    public static async Task<List<SamPartyDocument>> ToSilver(
        DateTime currentDateTime,
        List<SamParty> rawParties,
        string holdingIdentifier,
        Func<string?, CancellationToken, Task<(string? RoleTypeId, string? RoleTypeName)>> resolveRoleType,
        Func<string?, CancellationToken, Task<(string? CountryId, string? CountryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var result = new List<SamPartyDocument>();

        foreach (var p in rawParties?.Where(x => x.PARTY_ID != null) ?? [])
        {
            var (countryId, countryName) = await resolveCountry(p.COUNTRY_CODE, cancellationToken);
            var partyTypeId = p.DeterminePartyType().ToString();
            var addressLine = AddressFormatters.FormatAddressRange(
                            p.SAON_START_NUMBER, p.SAON_START_NUMBER_SUFFIX,
                            p.SAON_END_NUMBER, p.SAON_END_NUMBER_SUFFIX,
                            p.PAON_START_NUMBER, p.PAON_START_NUMBER_SUFFIX,
                            p.PAON_END_NUMBER, p.PAON_END_NUMBER_SUFFIX,
                            saonLabel: SaonLabel);

            var party = new SamPartyDocument
            {
                // Id - Leave to support upsert assigning Id

                LastUpdatedBatchId = p.BATCH_ID,
                LastUpdatedDate = currentDateTime,
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

                Address = new Core.Documents.Silver.AddressDocument
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

                Communication = new Core.Documents.Silver.CommunicationDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Email = p.INTERNET_EMAIL_ADDRESS,
                    Mobile = p.MOBILE_NUMBER,
                    Landline = p.TELEPHONE_NUMBER
                },

                Roles = []
            };

            var roleList = p.ROLES?.Split(",")
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .ToArray() ?? [];

            foreach (var roleNameToLookup in roleList)
            {
                var (roleTypeId, roleTypeName) = await resolveRoleType(roleNameToLookup, cancellationToken);

                party.Roles.Add(new Core.Documents.Silver.PartyRoleDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    RoleTypeId = roleTypeId,
                    RoleTypeName = roleTypeName,
                    SourceRoleName = roleNameToLookup,
                    EffectiveFromData = null,
                    EffectiveToData = null
                });
            }

            result.Add(party);
        }

        return result;
    }

    public static async Task<List<PartyDocument>> ToGold(
        DateTime currentDateTime,
        List<SamPartyDocument> silverParties,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<SpeciesTypeDocument?>> getSpeciesTypeById,
        CancellationToken cancellationToken)
    {
        if (silverParties?.Count == 0)
            return [];
        
        var result = new List<PartyDocument>();

        foreach (var silverParty in silverParties ?? [])
        {
            PartyDocument? existingParty = null; // TODO - Inject and lookup via Repository

            var party = existingParty is not null
                ? await UpdatePartyAsync(
                    currentDateTime,
                    silverParty,
                    existingParty,
                    getCountryById,
                    getSpeciesTypeById,
                    cancellationToken)
                : await CreatePartyAsync(
                    currentDateTime,
                    silverParty,
                    getCountryById,
                    getSpeciesTypeById,
                    cancellationToken);

            result.Add(PartyDocument.FromDomain(party));
        }

        return result;
    }

    private static async Task<Party> CreatePartyAsync(
        DateTime currentDateTime,
        SamPartyDocument incoming,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<SpeciesTypeDocument?>> getSpeciesTypeById,
        CancellationToken cancellationToken)
    {
        var country = await GetCountryAsync(
            incoming.Address?.CountryIdentifier,
            getCountryById,
            cancellationToken);

        var party = Party.Create(
            incoming.LastUpdatedBatchId,
            incoming.PartyTitleTypeIdentifier,
            incoming.PartyFirstName,
            incoming.PartyLastName,
            incoming.PartyFullName,
            incoming.PartyId,
            incoming.PartyTypeId,
            string.Empty, // TODO - Check State
            incoming.Deleted);

        // TODO - Add remaining fields

        return await Task.FromResult(party);
    }

    private static async Task<Party> UpdatePartyAsync(
        DateTime currentDateTime,
        SamPartyDocument incoming,
        PartyDocument existing,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<SpeciesTypeDocument?>> getSpeciesTypeById,
        CancellationToken cancellationToken)
    {
        var party = existing.ToDomain();

        var country = await GetCountryAsync(
            incoming.Address?.CountryIdentifier,
            getCountryById,
            cancellationToken);

        party.Update(
            currentDateTime,
            incoming.LastUpdatedBatchId,
            incoming.PartyTitleTypeIdentifier,
            incoming.PartyFirstName,
            incoming.PartyLastName,
            incoming.PartyFullName,
            incoming.PartyId,
            incoming.PartyTypeId,
            string.Empty, // TODO - Check State
            incoming.Deleted);

        // TODO - Add remaining fields

        return party;
    }

    private static async Task<Country?> GetCountryAsync(
        string? countryIdentifier,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        CancellationToken cancellationToken)
    {
        if (countryIdentifier == null) return null;

        var countryDocument = await getCountryById(countryIdentifier, cancellationToken);

        if (countryDocument == null)
            return null;

        return countryDocument.ToDomain();
    }

    /*
    {
  "count": 1,
  "values": [
    {
I      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
I      "title": "Mr",
I      "firstName": "John",
I      "lastName": "Doe",
I      "name": "John Doe",
I      "partyType": "Person",
      "communication": [
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "email": "john.doe@somecompany.co.uk",
          "mobile": "07123456789",
          "landline": "0114 1231234",
          "lastUpdatedDate": "2025-10-30T15:07:00.047Z"
        }
      ],
      "correspondanceAddress": {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "uprn": 671544009,
        "addressLine1": "Hansel & Gretel Farm, Pigs Street",
        "addressLine2": "Cloverfield",
        "postTown": "Clover town",
        "county": "Sussex",
        "postCode": "S36 2BS",
        "country": {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "code": "GB-ENG",
          "name": "England",
          "longName": "England - United Kingdom",
          "euTradeMemberFlag": true,
          "devolvedAuthorityFlag": false,
          "lastUpdatedDate": "2025-10-30T15:07:00.047Z"
        },
        "lastUpdatedDate": "2025-10-30T15:07:00.047Z"
      },
      "partyRoles": [
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "role": "Keeper",
          "startDate": "2025-10-30",
          "endDate": "2025-10-30",
          "speciesManagedByRole": [
            {
              "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
              "code": "BV",
              "name": "Bovine",
              "lastUpdatedDate": "2025-10-30T15:07:00.047Z"
            }
          ],
          "lastUpdatedDate": "2025-10-30T15:07:00.047Z"
        }
      ],
I      "state": "active",
I      "lastUpdatedDate": "2025-10-30T15:07:00.047Z"
    }
  ]
}
    */
}