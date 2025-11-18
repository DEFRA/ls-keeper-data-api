using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Parties;
using KeeperData.Core.Domain.Parties.Formatters;
using KeeperData.Core.Domain.Parties.Rules;
using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamPartyMapper
{
    public static async Task<List<SamPartyDocument>> ToSilver(
        DateTime currentDateTime,
        List<SamParty> rawParties,
        Func<string?, CancellationToken, Task<(string? RoleTypeId, string? RoleTypeName)>> resolveRoleType,
        Func<string?, CancellationToken, Task<(string? CountryId, string? CountryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var result = new List<SamPartyDocument>();

        foreach (var p in rawParties?.Where(x => x.PARTY_ID != null) ?? [])
        {
            var party = await ToSilver(
                currentDateTime,
                p,
                resolveRoleType,
                resolveCountry,
                cancellationToken);

            result.Add(party);
        }

        return result;
    }

    public static async Task<SamPartyDocument> ToSilver(
        DateTime currentDateTime,
        SamParty p,
        Func<string?, CancellationToken, Task<(string? RoleTypeId, string? RoleTypeName)>> resolveRoleType,
        Func<string?, CancellationToken, Task<(string? CountryId, string? CountryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var (countryId, countryName) = await resolveCountry(p.COUNTRY_CODE, cancellationToken);
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
            LastUpdatedDate = currentDateTime,
            Deleted = p.IsDeleted ?? false,
            IsHolder = false,

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

            Address = new Core.Documents.Silver.AddressDocument
            {
                IdentifierId = Guid.NewGuid().ToString(),
                AddressLine = addressLine,
                AddressLocality = p.LOCALITY,
                AddressStreet = p.STREET,
                AddressTown = p.TOWN,
                AddressPostCode = p.POSTCODE,
                CountrySubDivision = p.UK_INTERNAL_CODE,

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

            if (roleTypeId != null)
            {
                result.Roles.Add(new Core.Documents.Silver.PartyRoleDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    RoleTypeId = roleTypeId,
                    RoleTypeName = roleTypeName,
                    SourceRoleName = roleNameToLookup,
                    EffectiveFromDate = null,
                    EffectiveToDate = null
                });
            }
        }

        return result;
    }

    public static async Task<List<PartyDocument>> ToGold(
        DateTime currentDateTime,
        List<SamPartyDocument> silverParties,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<SpeciesDocument?>> getSpeciesTypeById,
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
                    goldSiteGroupMarks,
                    getCountryById,
                    getSpeciesTypeById,
                    cancellationToken)
                : await CreatePartyAsync(
                    currentDateTime,
                    silverParty,
                    goldSiteGroupMarks,
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
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<SpeciesDocument?>> getSpeciesTypeById,
        CancellationToken cancellationToken)
    {
        int? uprn = int.TryParse(incoming.Address?.UniquePropertyReferenceNumber, out var value) ? value : null;

        var country = await GetCountryAsync(
            incoming.Address?.CountryIdentifier,
            getCountryById,
            cancellationToken);

        var address = Address.Create(
            uprn,
            incoming.Address?.AddressLine ?? string.Empty,
            incoming.Address?.AddressStreet,
            incoming.Address?.AddressTown,
            incoming.Address?.AddressLocality,
            incoming.Address?.AddressPostCode ?? string.Empty,
            country);

        var communication = Communication.Create(
            incoming.Communication?.Email,
            incoming.Communication?.Mobile,
            incoming.Communication?.Landline,
            false);

        var party = Party.Create(
            incoming.PartyTitleTypeIdentifier,
            incoming.PartyFirstName,
            incoming.PartyLastName,
            incoming.PartyFullName,
            incoming.PartyId,
            incoming.PartyTypeId,
            string.Empty, // TODO - Check State
            incoming.Deleted,
            address);

        party.AddOrUpdatePrimaryCommunication(
            currentDateTime,
            communication);

        var roleList = incoming.Roles?
            .Where(x => !string.IsNullOrWhiteSpace(x.RoleTypeId))
            .ToList();

        if (roleList?.Count > 0)
        {
            var partyRoles = new List<PartyRole>();

            foreach (var r in roleList)
            {
                var role = Role.Create(
                    r.RoleTypeId ?? string.Empty,
                    r.RoleTypeName ?? string.Empty,
                    r.EffectiveFromDate,
                    r.EffectiveToDate
                );

                var speciesManaged = new List<ManagedSpecies>();

                // TODO: Populate speciesManaged from goldSiteGroupMarks

                var partyRole = PartyRole.Create(role, speciesManaged);
                partyRoles.Add(partyRole);
            }

            party.SetRoles(partyRoles);
        }

        // TODO - Add remaining fields

        return await Task.FromResult(party);
    }

    private static async Task<Party> UpdatePartyAsync(
        DateTime currentDateTime,
        SamPartyDocument incoming,
        PartyDocument existing,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<SpeciesDocument?>> getSpeciesTypeById,
        CancellationToken cancellationToken)
    {
        var party = existing.ToDomain();

        int? uprn = int.TryParse(incoming.Address?.UniquePropertyReferenceNumber, out var value) ? value : null;

        var country = await GetCountryAsync(
            incoming.Address?.CountryIdentifier,
            getCountryById,
            cancellationToken);

        var updatedAddress = Address.Create(
            uprn,
            incoming.Address?.AddressLine ?? string.Empty,
            incoming.Address?.AddressStreet,
            incoming.Address?.AddressTown,
            incoming.Address?.AddressLocality,
            incoming.Address?.AddressPostCode ?? string.Empty,
            country);

        var updatedCommunication = Communication.Create(
            incoming.Communication?.Email,
            incoming.Communication?.Mobile,
            incoming.Communication?.Landline,
            false);

        party.Update(
            currentDateTime,
            incoming.PartyTitleTypeIdentifier,
            incoming.PartyFirstName,
            incoming.PartyLastName,
            incoming.PartyFullName,
            incoming.PartyId,
            incoming.PartyTypeId,
            string.Empty, // TODO - Check State
            incoming.Deleted);

        party.SetAddress(
            currentDateTime,
            updatedAddress);

        party.AddOrUpdatePrimaryCommunication(
            currentDateTime,
            updatedCommunication);

        var roleList = incoming.Roles?
            .Where(r => !string.IsNullOrWhiteSpace(r.RoleTypeId))
            .ToList();

        if (roleList?.Count > 0)
        {
            foreach (var r in roleList)
            {
                var role = Role.Create(
                    r.RoleTypeId ?? string.Empty,
                    r.RoleTypeName ?? string.Empty,
                    r.EffectiveFromDate,
                    r.EffectiveToDate
                );

                var speciesManaged = new List<ManagedSpecies>();

                // TODO: Populate speciesManaged from goldSiteGroupMarks

                var partyRole = PartyRole.Create(role, speciesManaged);

                party.AddOrUpdateRole(currentDateTime, partyRole);
            }
        }
        else if (party.Roles.Count != 0)
        {
            party.SetRoles([]);
        }

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
IU      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
IU      "title": "Mr",
IU      "firstName": "John",
IU      "lastName": "Doe",
IU      "name": "John Doe",
IU      "partyType": "Person",
IU      "communication": [
        {
IU          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
IU          "email": "john.doe@somecompany.co.uk",
IU          "mobile": "07123456789",
IU          "landline": "0114 1231234",
          "lastUpdatedDate": "2025-10-30T15:07:00.047Z"
        }
      ],
IU      "correspondanceAddress": {
IU       "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
IU        "uprn": 671544009,
IU        "addressLine1": "Hansel & Gretel Farm, Pigs Street",
IU        "addressLine2": "Cloverfield",
IU        "postTown": "Clover town",
IU        "county": "Sussex",
IU        "postCode": "S36 2BS",
IU        "country": {
IU          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
IU          "code": "GB-ENG",
IU          "name": "England",
IU          "longName": "England - United Kingdom",
IU          "euTradeMemberFlag": true,
IU          "devolvedAuthorityFlag": false,
          "lastUpdatedDate": "2025-10-30T15:07:00.047Z"
        },
        "lastUpdatedDate": "2025-10-30T15:07:00.047Z"
      },
      "partyRoles": [
        {
IU          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
IU          "role": "Keeper",
IU          "startDate": "2025-10-30",
IU          "endDate": "2025-10-30",
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
      "state": "active",
IU     "lastUpdatedDate": "2025-10-30T15:07:00.047Z"
    }
  ]
}
    */
}