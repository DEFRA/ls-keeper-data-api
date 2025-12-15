using KeeperData.Application.Extensions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Documents.Working;
using KeeperData.Core.Domain.Enums;
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
        string holdingIdentifier,
        List<SamParty> rawParties,
        Func<string?, CancellationToken, Task<(string? RoleTypeId, string? RoleTypeName)>> resolveRoleType,
        Func<string?, string?, CancellationToken, Task<(string? CountryId, string? CountryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var result = new List<SamPartyDocument>();

        foreach (var p in rawParties?.Where(x => x.PARTY_ID != null) ?? [])
        {
            var party = await ToSilver(
                holdingIdentifier,
                p,
                resolveRoleType,
                resolveCountry,
                cancellationToken);

            result.Add(party);
        }

        return result;
    }

    public static async Task<SamPartyDocument> ToSilver(
        string holdingIdentifier,
        SamParty p,
        Func<string?, CancellationToken, Task<(string? RoleTypeId, string? RoleTypeName)>> resolveRoleType,
        Func<string?, string?, CancellationToken, Task<(string? CountryId, string? CountryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var (countryId, countryName) = await resolveCountry(p.COUNTRY_CODE, p.UK_INTERNAL_CODE, cancellationToken);
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

        if ((p.CphList ?? []).Count == 0)
        {
            result.CountyParishHoldingNumber = holdingIdentifier;
        }
        else
        {
            result.CphList = p.CphList ?? [];
        }

        return result;
    }

    public static async Task<List<PartyDocument>> ToGold(
        string goldSiteId,
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
                    goldSiteId,
                    silverParty,
                    existingParty,
                    goldSiteGroupMarks,
                    getCountryById,
                    getSpeciesTypeById,
                    cancellationToken)
                : await CreatePartyAsync(
                    goldSiteId,
                    silverParty,
                    goldSiteGroupMarks,
                    getCountryById,
                    getSpeciesTypeById,
                    cancellationToken);

            result.Add(PartyDocument.FromDomain(party));
        }

        return result;
    }

    // TODO - Add tests for RemoveSitePartyOrphans
    public static async Task<List<PartyDocument>> RemoveSitePartyOrphans(
        string goldSiteId,
        List<SitePartyRoleRelationship> orphansToClean,
        IGenericRepository<PartyDocument> goldPartyRepository,
        CancellationToken cancellationToken)
    {
        var result = new List<PartyDocument>();

        var groupedOrphans = orphansToClean
            .GroupBy(o => o.PartyId);

        foreach (var orphanGroup in groupedOrphans)
        {
            var existingPartyFilter = Builders<PartyDocument>.Filter.Eq(x => x.CustomerNumber, orphanGroup.Key);
            var existingParty = await goldPartyRepository.FindOneByFilterAsync(existingPartyFilter, cancellationToken);

            if (existingParty is null) continue;

            var roleIds = orphanGroup
                .Where(o => !string.IsNullOrEmpty(o.RoleTypeId))
                .Select(o => o.RoleTypeId!);

            var party = existingParty.ToDomain();

            foreach (var roleId in roleIds)
            {
                party.DeleteRole(roleId, goldSiteId);
            }

            result.Add(PartyDocument.FromDomain(party));
        }

        return result;
    }

    public static List<SamParty> AggregatePartyAndHolder(List<SamParty> rawParties, List<SamCphHolder> rawHolders)
    {
        var holderRole = InferredRoleType.CphHolder.GetDescription();
        var partyMap = rawParties.ToDictionary(p => p.PARTY_ID, p => p);

        foreach (var holder in rawHolders)
        {
            if (holder.PARTY_ID != null && partyMap.TryGetValue(holder.PARTY_ID, out var party))
            {
                party.ROLES ??= string.Empty;

                var roles = party.RoleList;
                if (!roles.Contains(holderRole!))
                {
                    roles.Add(holderRole!);
                    party.ROLES = string.Join(",", roles);
                }

                MergeSamPartyFromHolder(party, holder);
            }
            else
            {
                var newSamParty = CreateSamPartyFromHolder(holder, holderRole!);
                partyMap[newSamParty.PARTY_ID] = newSamParty;
            }
        }

        return [.. partyMap.Values];
    }

    private static void MergeSamPartyFromHolder(SamParty party, SamCphHolder holder)
    {
        if (string.IsNullOrEmpty(party.PERSON_TITLE) && !string.IsNullOrEmpty(holder.PERSON_TITLE))
            party.PERSON_TITLE = holder.PERSON_TITLE;

        if (string.IsNullOrEmpty(party.PERSON_GIVEN_NAME) && !string.IsNullOrEmpty(holder.PERSON_GIVEN_NAME))
            party.PERSON_GIVEN_NAME = holder.PERSON_GIVEN_NAME;

        if (string.IsNullOrEmpty(party.PERSON_GIVEN_NAME2) && !string.IsNullOrEmpty(holder.PERSON_GIVEN_NAME2))
            party.PERSON_GIVEN_NAME2 = holder.PERSON_GIVEN_NAME2;

        if (string.IsNullOrEmpty(party.PERSON_INITIALS) && !string.IsNullOrEmpty(holder.PERSON_INITIALS))
            party.PERSON_INITIALS = holder.PERSON_INITIALS;

        if (string.IsNullOrEmpty(party.PERSON_FAMILY_NAME) && !string.IsNullOrEmpty(holder.PERSON_FAMILY_NAME))
            party.PERSON_FAMILY_NAME = holder.PERSON_FAMILY_NAME;

        if (string.IsNullOrEmpty(party.ORGANISATION_NAME) && !string.IsNullOrEmpty(holder.ORGANISATION_NAME))
            party.ORGANISATION_NAME = holder.ORGANISATION_NAME;

        if (string.IsNullOrEmpty(party.TELEPHONE_NUMBER) && !string.IsNullOrEmpty(holder.TELEPHONE_NUMBER))
            party.TELEPHONE_NUMBER = holder.TELEPHONE_NUMBER;

        if (string.IsNullOrEmpty(party.MOBILE_NUMBER) && !string.IsNullOrEmpty(holder.MOBILE_NUMBER))
            party.MOBILE_NUMBER = holder.MOBILE_NUMBER;

        if (string.IsNullOrEmpty(party.INTERNET_EMAIL_ADDRESS) && !string.IsNullOrEmpty(holder.INTERNET_EMAIL_ADDRESS))
            party.INTERNET_EMAIL_ADDRESS = holder.INTERNET_EMAIL_ADDRESS;

        if (party.SAON_START_NUMBER == null && holder.SAON_START_NUMBER != null)
            party.SAON_START_NUMBER = holder.SAON_START_NUMBER;

        if (party.SAON_START_NUMBER_SUFFIX == null && holder.SAON_START_NUMBER_SUFFIX != null)
            party.SAON_START_NUMBER_SUFFIX = holder.SAON_START_NUMBER_SUFFIX;

        if (party.SAON_END_NUMBER == null && holder.SAON_END_NUMBER != null)
            party.SAON_END_NUMBER = holder.SAON_END_NUMBER;

        if (party.SAON_END_NUMBER_SUFFIX == null && holder.SAON_END_NUMBER_SUFFIX != null)
            party.SAON_END_NUMBER_SUFFIX = holder.SAON_END_NUMBER_SUFFIX;

        if (string.IsNullOrEmpty(party.SAON_DESCRIPTION) && !string.IsNullOrEmpty(holder.SAON_DESCRIPTION))
            party.SAON_DESCRIPTION = holder.SAON_DESCRIPTION;

        if (party.PAON_START_NUMBER == null && holder.PAON_START_NUMBER != null)
            party.PAON_START_NUMBER = holder.PAON_START_NUMBER;

        if (party.PAON_START_NUMBER_SUFFIX == null && holder.PAON_START_NUMBER_SUFFIX != null)
            party.PAON_START_NUMBER_SUFFIX = holder.PAON_START_NUMBER_SUFFIX;

        if (party.PAON_END_NUMBER == null && holder.PAON_END_NUMBER != null)
            party.PAON_END_NUMBER = holder.PAON_END_NUMBER;

        if (party.PAON_END_NUMBER_SUFFIX == null && holder.PAON_END_NUMBER_SUFFIX != null)
            party.PAON_END_NUMBER_SUFFIX = holder.PAON_END_NUMBER_SUFFIX;

        if (string.IsNullOrEmpty(party.PAON_DESCRIPTION) && !string.IsNullOrEmpty(holder.PAON_DESCRIPTION))
            party.PAON_DESCRIPTION = holder.PAON_DESCRIPTION;

        if (string.IsNullOrEmpty(party.STREET) && !string.IsNullOrEmpty(holder.STREET))
            party.STREET = holder.STREET;

        if (string.IsNullOrEmpty(party.TOWN) && !string.IsNullOrEmpty(holder.TOWN))
            party.TOWN = holder.TOWN;

        if (string.IsNullOrEmpty(party.LOCALITY) && !string.IsNullOrEmpty(holder.LOCALITY))
            party.LOCALITY = holder.LOCALITY;

        if (string.IsNullOrEmpty(party.UK_INTERNAL_CODE) && !string.IsNullOrEmpty(holder.UK_INTERNAL_CODE))
            party.UK_INTERNAL_CODE = holder.UK_INTERNAL_CODE;

        if (string.IsNullOrEmpty(party.POSTCODE) && !string.IsNullOrEmpty(holder.POSTCODE))
            party.POSTCODE = holder.POSTCODE;

        if (string.IsNullOrEmpty(party.COUNTRY_CODE) && !string.IsNullOrEmpty(holder.COUNTRY_CODE))
            party.COUNTRY_CODE = holder.COUNTRY_CODE;

        if (string.IsNullOrEmpty(party.UDPRN) && !string.IsNullOrEmpty(holder.UDPRN))
            party.UDPRN = holder.UDPRN;

        if (party.PREFERRED_CONTACT_METHOD_IND == null && holder.PREFERRED_CONTACT_METHOD_IND != null)
            party.PREFERRED_CONTACT_METHOD_IND = holder.PREFERRED_CONTACT_METHOD_IND;

        party.CPHS = holder.CPHS;
    }

    private static SamParty CreateSamPartyFromHolder(SamCphHolder holder, string role)
    {
        return new SamParty
        {
            BATCH_ID = holder.BATCH_ID,
            CHANGE_TYPE = holder.CHANGE_TYPE,
            IsDeleted = holder.IsDeleted,
            CreatedAtUtc = holder.CreatedAtUtc,
            UpdatedAtUtc = holder.UpdatedAtUtc,

            PARTY_ID = holder.PARTY_ID!,

            PERSON_TITLE = holder.PERSON_TITLE,
            PERSON_GIVEN_NAME = holder.PERSON_GIVEN_NAME,
            PERSON_GIVEN_NAME2 = holder.PERSON_GIVEN_NAME2,
            PERSON_INITIALS = holder.PERSON_INITIALS,
            PERSON_FAMILY_NAME = holder.PERSON_FAMILY_NAME,
            ORGANISATION_NAME = holder.ORGANISATION_NAME,

            TELEPHONE_NUMBER = holder.TELEPHONE_NUMBER,
            MOBILE_NUMBER = holder.MOBILE_NUMBER,
            INTERNET_EMAIL_ADDRESS = holder.INTERNET_EMAIL_ADDRESS,

            SAON_START_NUMBER = holder.SAON_START_NUMBER,
            SAON_START_NUMBER_SUFFIX = holder.SAON_START_NUMBER_SUFFIX,
            SAON_END_NUMBER = holder.SAON_END_NUMBER,
            SAON_END_NUMBER_SUFFIX = holder.SAON_END_NUMBER_SUFFIX,
            SAON_DESCRIPTION = holder.SAON_DESCRIPTION,

            PAON_START_NUMBER = holder.PAON_START_NUMBER,
            PAON_START_NUMBER_SUFFIX = holder.PAON_START_NUMBER_SUFFIX,
            PAON_END_NUMBER = holder.PAON_END_NUMBER,
            PAON_END_NUMBER_SUFFIX = holder.PAON_END_NUMBER_SUFFIX,
            PAON_DESCRIPTION = holder.PAON_DESCRIPTION,

            STREET = holder.STREET,
            TOWN = holder.TOWN,
            LOCALITY = holder.LOCALITY,
            UK_INTERNAL_CODE = holder.UK_INTERNAL_CODE,
            POSTCODE = holder.POSTCODE,
            COUNTRY_CODE = holder.COUNTRY_CODE,
            UDPRN = holder.UDPRN,

            PREFERRED_CONTACT_METHOD_IND = holder.PREFERRED_CONTACT_METHOD_IND,

            ROLES = role,

            CPHS = holder.CPHS
        };
    }

    private static async Task<Party> CreatePartyAsync(
        string goldSiteId,
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
            incoming.CreatedDate,
            incoming.LastUpdatedDate,
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
            party.LastUpdatedDate,
            communication);

        var roleList = incoming.Roles?
            .Where(x => !string.IsNullOrWhiteSpace(x.RoleTypeId))
            .ToList();

        if (roleList?.Count > 0)
        {
            var partyRoles = new List<PartyRole>();

            foreach (var r in roleList)
            {
                var partyRoleSite = PartyRoleSite.Create(goldSiteId);

                var partyRoleRole = PartyRoleRole.Create(
                    r.RoleTypeId ?? string.Empty,
                    r.RoleTypeName ?? string.Empty
                );

                var matchingMarks = goldSiteGroupMarks
                    .Where(m =>
                        m.PartyId == incoming.PartyId
                        && m.RoleTypeId == r.RoleTypeId
                        && !string.IsNullOrWhiteSpace(m.SpeciesTypeId))
                    .ToList();

                var speciesManaged = new List<ManagedSpecies>();

                if (!partyRoleRole.IsCphHolderRole)
                {
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
                }

                var partyRole = PartyRole.Create(
                    partyRoleSite,
                    partyRoleRole,
                    speciesManaged);

                partyRoles.Add(partyRole);
            }

            party.SetRoles(partyRoles);
        }

        return await Task.FromResult(party);
    }

    private static async Task<Party> UpdatePartyAsync(
        string goldSiteId,
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
            incoming.LastUpdatedDate,
            incoming.PartyTitleTypeIdentifier,
            incoming.PartyFirstName,
            incoming.PartyLastName,
            incoming.PartyFullName,
            incoming.PartyId,
            incoming.PartyTypeId,
            PartyStatusFormatters.FormatPartyStatus(incoming.Deleted),
            incoming.Deleted);

        party.SetAddress(
            incoming.LastUpdatedDate,
            updatedAddress);

        party.AddOrUpdatePrimaryCommunication(
            incoming.LastUpdatedDate,
            updatedCommunication);

        var roleList = incoming.Roles?
            .Where(r => !string.IsNullOrWhiteSpace(r.RoleTypeId))
            .ToList();

        if (roleList?.Count > 0)
        {
            foreach (var r in roleList)
            {
                var partyRoleSite = PartyRoleSite.Create(goldSiteId);

                var partyRoleRole = PartyRoleRole.Create(
                    r.RoleTypeId ?? string.Empty,
                    r.RoleTypeName ?? string.Empty
                );

                var matchingMarks = goldSiteGroupMarks
                    .Where(m =>
                        m.PartyId == incoming.PartyId
                        && m.RoleTypeId == r.RoleTypeId
                        && !string.IsNullOrWhiteSpace(m.SpeciesTypeId))
                    .ToList();

                var speciesManaged = new List<ManagedSpecies>();

                if (!partyRoleRole.IsCphHolderRole)
                {
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
                }

                var partyRole = PartyRole.Create(
                    partyRoleSite,
                    partyRoleRole,
                    speciesManaged);

                party.AddOrUpdateRole(party.LastUpdatedDate, partyRole);
            }
        }

        return party;
    }

    public static void EnrichPartyRoleWithSiteInformation(
        List<PartyDocument> goldParties,
        SiteDocument? goldSite)
    {
        if (goldSite == null)
            return;

        if (goldParties?.Count == 0)
            return;

        foreach (var goldParty in goldParties ?? [])
        {
            if (goldParty == null)
                continue;

            var party = goldParty.ToDomain();
            if (party.Roles == null) continue;

            foreach (var partyRole in party.Roles.Where(x => x.Site?.Id == goldSite.Id))
            {
                partyRole.Site?.ApplyChanges(
                    goldSite.Name,
                    goldSite.State,
                    goldSite.LastUpdatedDate);

                if (goldSite.Identifiers != null && goldSite.Identifiers.Count > 0)
                {
                    var identifiers = goldSite.Identifiers.Select(i => i.ToDomain()).ToList();
                    partyRole.Site?.SetIdentifiers(identifiers);
                }
            }

            goldParty.UpdatePartyRoleSitesFromDomain(party);
        }
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