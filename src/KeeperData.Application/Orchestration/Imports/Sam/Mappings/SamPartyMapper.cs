using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Parties;
using KeeperData.Core.Domain.Parties.Formatters;
using KeeperData.Core.Domain.Parties.Rules;
using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Repositories;
using MongoDB.Driver;

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
                    EffectiveFromDate = p.PARTY_ROLE_FROM_DATE,
                    EffectiveToDate = p.PARTY_ROLE_TO_DATE
                });
            }
        }

        return result;
    }

    public static async Task<List<PartyDocument>> ToGold(
        DateTime currentDateTime,
        List<SamPartyDocument> silverParties,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        IGenericRepository<PartyDocument> goldPartyRepository,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<SpeciesDocument?>> getSpeciesTypeById,
        CancellationToken cancellationToken)
    {
        if (silverParties?.Count == 0)
            return [];

        var result = new List<PartyDocument>();

        foreach (var silverParty in silverParties ?? [])
        {
            var existingPartyFilter = Builders<PartyDocument>.Filter.Eq(x => x.CustomerNumber, silverParty.PartyId);

            var existingParty = await goldPartyRepository.FindOneByFilterAsync(existingPartyFilter, cancellationToken);

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
            PartyStatusFormatters.FormatPartyStatus(incoming.Deleted),
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

                var matchingMarks = goldSiteGroupMarks
                    .Where(m =>
                        m.PartyId == incoming.PartyId
                        && m.RoleTypeId == r.RoleTypeId
                        && !string.IsNullOrWhiteSpace(m.SpeciesTypeId))
                    .ToList();

                var speciesManaged = new List<ManagedSpecies>();

                foreach (var mark in matchingMarks)
                {
                    var speciesDoc = await getSpeciesTypeById(mark.SpeciesTypeId, cancellationToken);
                    if (speciesDoc is null)
                        continue;

                    var managedSpecies = ManagedSpecies.Create(
                        code: speciesDoc.Code,
                        name: speciesDoc.Name,
                        startDate: mark.GroupMarkStartDate,
                        endDate: mark.GroupMarkEndDate);

                    speciesManaged.Add(managedSpecies);
                }

                var partyRole = PartyRole.Create(role, speciesManaged);
                partyRoles.Add(partyRole);
            }

            party.SetRoles(partyRoles);
        }

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
            PartyStatusFormatters.FormatPartyStatus(incoming.Deleted),
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

                var matchingMarks = goldSiteGroupMarks
                    .Where(m =>
                        m.PartyId == incoming.PartyId
                        && m.RoleTypeId == r.RoleTypeId
                        && !string.IsNullOrWhiteSpace(m.SpeciesTypeId))
                    .ToList();

                var speciesManaged = new List<ManagedSpecies>();

                foreach (var mark in matchingMarks)
                {
                    var speciesDoc = await getSpeciesTypeById(mark.SpeciesTypeId, cancellationToken);
                    if (speciesDoc is null)
                        continue;

                    var managedSpecies = ManagedSpecies.Create(
                        code: speciesDoc.Code,
                        name: speciesDoc.Name,
                        startDate: mark.GroupMarkStartDate,
                        endDate: mark.GroupMarkEndDate);

                    speciesManaged.Add(managedSpecies);
                }

                var partyRole = PartyRole.Create(role, speciesManaged);

                party.AddOrUpdateRole(currentDateTime, partyRole);
            }
        }
        else if (party.Roles.Count != 0)
        {
            party.SetRoles([]);
        }

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
}