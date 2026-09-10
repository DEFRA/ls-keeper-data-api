using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace KeeperData.Infrastructure.Database.Repositories;

public class HoldingDetailRepository(IReadModelSqliteCacheService cacheService) : IHoldingDetailRepository
{
    private readonly IReadModelSqliteCacheService _cacheService = cacheService;

    public async Task<HoldingDetail?> GetHoldingDetailByCphAsync(string cph, CancellationToken cancellationToken = default)
    {
        var dbPath = _cacheService.GetCurrentDbPath()
            ?? throw new InvalidOperationException("The SAM read model is not cached locally, so holding details cannot be resolved.");

        await using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        await connection.OpenAsync(cancellationToken);

        // 1 — Holding
        var holdingRow = await ReadHoldingAsync(connection, cph, cancellationToken);
        if (holdingRow is null)
        {
            return null;
        }

        var holdingId = holdingRow.Value.HoldingId;

        // 2 — Associations, roles and role species
        var associations = await ReadAssociationsAsync(connection, holdingId, cancellationToken);

        // 3 — Allowed species
        var allowedSpecies = await ReadAllowedSpeciesAsync(connection, holdingId, cancellationToken);

        // 4 — Marks
        var marks = await ReadMarksAsync(connection, holdingId, cancellationToken);

        return new HoldingDetail(
            Identifier: holdingRow.Value.Cph,
            HoldingType: holdingRow.Value.CphType,
            Name: holdingRow.Value.Name,
            StartDate: holdingRow.Value.StartDate,
            EndDate: holdingRow.Value.EndDate,
            Location: holdingRow.Value.Location,
            Associations: associations,
            AllowedSpecies: allowedSpecies,
            Marks: marks);
    }

    private static async Task<HoldingRowData?> ReadHoldingAsync(
        SqliteConnection connection,
        string cph,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                h.Id,
                h.Cph,
                h.FeatureName,
                h.CphType,
                h.StartDate,
                h.EndDate,
                h.Udprn,
                h.PaonDescription,
                h.PaonStartNumber,
                h.PaonStartNumberSuffix,
                h.PaonEndNumber,
                h.PaonEndNumberSuffix,
                h.Street,
                h.Town,
                h.Locality,
                h.Postcode,
                h.UkInternalCode,
                h.Easting,
                h.Northing,
                h.OsMapReference
            FROM Holding AS h
            WHERE h.Cph = $cph;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqliteParameter("$cph", cph));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapHoldingRow(reader);
    }

    private static HoldingRowData MapHoldingRow(DbDataReader reader)
    {
        var holdingId = reader.GetString(0);
        var holdingCph = reader.GetString(1);
        var name = GetNullableString(reader, 2);
        var cphType = GetNullableString(reader, 3);
        var startDate = ReadEpoch(reader, 4);
        var endDate = ReadEpoch(reader, 5);
        var udprn = GetNullableString(reader, 6);

        var paonDescription = GetNullableString(reader, 7);
        var paonStartNumber = GetNullableString(reader, 8);
        var paonStartNumberSuffix = GetNullableString(reader, 9);
        var paonEndNumber = GetNullableString(reader, 10);
        var paonEndNumberSuffix = GetNullableString(reader, 11);
        var street = GetNullableString(reader, 12);

        var postTown = GetNullableString(reader, 13);
        var locality = GetNullableString(reader, 14);
        var postcode = GetNullableString(reader, 15);
        var country = GetNullableString(reader, 16);

        var easting = GetNullableInt32(reader, 17);
        var northing = GetNullableInt32(reader, 18);
        var osMapReference = GetNullableString(reader, 19);

        var (addressLine1, addressLine2) = AssembleAddressLines(
            paonDescription,
            paonStartNumber,
            paonStartNumberSuffix,
            paonEndNumber,
            paonEndNumberSuffix,
            street);

        var location = new HoldingLocation(
            OsMapReference: osMapReference,
            Easting: easting,
            Northing: northing,
            Address: new HoldingAddress(
                Udprn: udprn,
                AddressLine1: addressLine1,
                AddressLine2: addressLine2,
                PostTown: postTown,
                Locality: locality,
                Postcode: postcode,
                Country: country));

        return new HoldingRowData(holdingId, holdingCph, name, cphType, startDate, endDate, location);
    }

    private static async Task<IReadOnlyList<HoldingAssociation>> ReadAssociationsAsync(
        SqliteConnection connection,
        string holdingId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                p.SourcePartyId,
                p.PersonTitle,
                p.GivenName,
                p.Initials,
                p.FamilyName,
                p.OrganisationName,
                p.Email,
                p.Mobile,
                p.Telephone,
                r.Role,
                d.AnimalSpeciesCode
            FROM PartyRole AS r
            JOIN Party AS p ON p.Id = r.PartyId
            LEFT JOIN Herd AS d ON d.Id = r.HerdId
            WHERE r.HoldingId = $holdingId
            ORDER BY p.SourcePartyId, r.Role, d.AnimalSpeciesCode;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqliteParameter("$holdingId", holdingId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var partyMap = new Dictionary<string, PartyAccumulator>(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync(cancellationToken))
        {
            ProcessAssociationRow(reader, partyMap);
        }

        return partyMap.Values.Select(p => p.ToHoldingAssociation()).ToList();
    }

    private static void ProcessAssociationRow(
        DbDataReader reader,
        Dictionary<string, PartyAccumulator> partyMap)
    {
        var sourcePartyId = reader.GetString(0);
        var personTitle = GetNullableString(reader, 1);
        var givenName = GetNullableString(reader, 2);
        var initials = GetNullableString(reader, 3);
        var familyName = GetNullableString(reader, 4);
        var organisationName = GetNullableString(reader, 5);
        var email = GetNullableString(reader, 6);
        var mobile = GetNullableString(reader, 7);
        var telephone = GetNullableString(reader, 8);
        var roleCode = reader.GetString(9);
        var speciesCode = GetNullableString(reader, 10);

        if (!partyMap.TryGetValue(sourcePartyId, out var accumulator))
        {
            var displayName = AssembleDisplayName(organisationName, personTitle, givenName, initials, familyName);
            var partyType = !string.IsNullOrWhiteSpace(organisationName) ? "organisation" : "person";

            accumulator = new PartyAccumulator(
                sourcePartyId,
                personTitle,
                givenName,
                familyName,
                displayName,
                partyType,
                email,
                mobile,
                telephone);
            partyMap[sourcePartyId] = accumulator;
        }

        accumulator.AddRoleSpecies(roleCode, speciesCode);
    }

    private static async Task<IReadOnlyList<string>> ReadAllowedSpeciesAsync(
        SqliteConnection connection,
        string holdingId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DISTINCT a.AnimalSpeciesCode
            FROM HoldingAnimalProfile AS a
            WHERE a.HoldingId = $holdingId
            ORDER BY a.AnimalSpeciesCode;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqliteParameter("$holdingId", holdingId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var speciesList = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
            {
                speciesList.Add(reader.GetString(0));
            }
        }

        return speciesList;
    }

    private static async Task<IReadOnlyList<HoldingMark>> ReadMarksAsync(
        SqliteConnection connection,
        string holdingId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                d.Herdmark,
                d.AnimalGroupFromDate,
                d.AnimalGroupToDate,
                d.AnimalSpeciesCode
            FROM Herd AS d
            WHERE d.HoldingId = $holdingId
            ORDER BY d.Herdmark, d.AnimalSpeciesCode;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqliteParameter("$holdingId", holdingId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var markMap = new Dictionary<string, MarkAccumulator>(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync(cancellationToken))
        {
            ProcessMarkRow(reader, markMap);
        }

        return markMap.Values.Select(m => m.ToHoldingMark()).ToList();
    }

    private static void ProcessMarkRow(
        DbDataReader reader,
        Dictionary<string, MarkAccumulator> markMap)
    {
        if (reader.IsDBNull(0))
        {
            return;
        }

        var herdmark = reader.GetString(0);
        var fromDate = GetNullableInt64(reader, 1);
        var toDate = GetNullableInt64(reader, 2);
        var species = GetNullableString(reader, 3);

        if (!markMap.TryGetValue(herdmark, out var accumulator))
        {
            accumulator = new MarkAccumulator(herdmark);
            markMap[herdmark] = accumulator;
        }

        accumulator.AddRow(fromDate, toDate, species);
    }

    private static (string? AddressLine1, string? AddressLine2) AssembleAddressLines(
        string? paonDescription,
        string? paonStartNumber,
        string? paonStartNumberSuffix,
        string? paonEndNumber,
        string? paonEndNumberSuffix,
        string? street)
    {
        var number = FormatStreetNumber(paonStartNumber, paonStartNumberSuffix, paonEndNumber, paonEndNumberSuffix);
        var trimmedStreet = street?.Trim();
        var streetParts = new List<string>();
        if (!string.IsNullOrEmpty(number))
        {
            streetParts.Add(number);
        }
        if (!string.IsNullOrEmpty(trimmedStreet))
        {
            streetParts.Add(trimmedStreet);
        }

        var streetLine = streetParts.Count > 0 ? string.Join(" ", streetParts).Trim() : null;
        if (string.IsNullOrWhiteSpace(streetLine))
        {
            streetLine = null;
        }

        var paonDesc = string.IsNullOrWhiteSpace(paonDescription) ? null : paonDescription.Trim();

        var addressLine1 = paonDesc ?? streetLine;
        var addressLine2 = paonDesc is null ? null : streetLine;

        return (addressLine1, addressLine2);
    }

    private static string? FormatStreetNumber(
        string? paonStartNumber,
        string? paonStartNumberSuffix,
        string? paonEndNumber,
        string? paonEndNumberSuffix)
    {
        var start = Combine(paonStartNumber, paonStartNumberSuffix);
        var end = Combine(paonEndNumber, paonEndNumberSuffix);

        if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
        {
            return $"{start}-{end}";
        }

        return !string.IsNullOrEmpty(start) ? start : end;
    }

    private static string? Combine(string? a, string? b)
    {
        var result = $"{a?.Trim()}{b?.Trim()}";
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private static string? AssembleDisplayName(
        string? organisationName,
        string? personTitle,
        string? givenName,
        string? initials,
        string? familyName)
    {
        if (!string.IsNullOrWhiteSpace(organisationName))
        {
            return organisationName.Trim();
        }

        var parts = new[] { personTitle, givenName, initials, familyName }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToArray();

        return parts.Length > 0 ? string.Join(" ", parts) : null;
    }

    private static string? GetNullableString(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static long? GetNullableInt64(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);

    private static int? GetNullableInt32(DbDataReader reader, int ordinal)
    {
        var value = GetNullableString(reader, ordinal);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static DateTimeOffset? ReadEpoch(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(ordinal));

    private readonly record struct HoldingRowData(
        string HoldingId,
        string Cph,
        string? Name,
        string? CphType,
        DateTimeOffset? StartDate,
        DateTimeOffset? EndDate,
        HoldingLocation Location);

    private sealed class PartyAccumulator(
        string customerNumber,
        string? title,
        string? firstName,
        string? lastName,
        string? name,
        string partyType,
        string? email,
        string? mobile,
        string? telephone)
    {
        private readonly Dictionary<string, HashSet<string>> _roles = new(StringComparer.OrdinalIgnoreCase);

        public void AddRoleSpecies(string role, string? species)
        {
            if (!_roles.TryGetValue(role, out var speciesSet))
            {
                speciesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _roles[role] = speciesSet;
            }

            if (!string.IsNullOrWhiteSpace(species) && !string.Equals(role, "holder", StringComparison.OrdinalIgnoreCase))
            {
                speciesSet.Add(species);
            }
        }

        public HoldingAssociation ToHoldingAssociation()
        {
            var roleList = _roles.Select(kvp => new HoldingRole(
                Code: kvp.Key.ToLowerInvariant(),
                Species: string.Equals(kvp.Key, "holder", StringComparison.OrdinalIgnoreCase)
                    ? []
                    : kvp.Value.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList()
            )).ToList();

            return new HoldingAssociation(
                CustomerNumber: customerNumber,
                Title: title,
                FirstName: firstName,
                LastName: lastName,
                Name: name,
                PartyType: partyType,
                Email: email,
                Mobile: mobile,
                Telephone: telephone,
                Roles: roleList);
        }
    }

    private sealed class MarkAccumulator(string mark)
    {
        private long? _minStartDate;
        private long? _maxEndDate;
        private readonly HashSet<string> _species = new(StringComparer.OrdinalIgnoreCase);

        public void AddRow(long? fromDate, long? toDate, string? species)
        {
            if (fromDate.HasValue)
            {
                _minStartDate = _minStartDate.HasValue ? Math.Min(_minStartDate.Value, fromDate.Value) : fromDate.Value;
            }

            if (toDate.HasValue)
            {
                _maxEndDate = _maxEndDate.HasValue ? Math.Max(_maxEndDate.Value, toDate.Value) : toDate.Value;
            }

            if (!string.IsNullOrWhiteSpace(species))
            {
                _species.Add(species);
            }
        }

        public HoldingMark ToHoldingMark()
        {
            var startDate = _minStartDate.HasValue ? DateTimeOffset.FromUnixTimeSeconds(_minStartDate.Value) : (DateTimeOffset?)null;
            var endDate = _maxEndDate.HasValue ? DateTimeOffset.FromUnixTimeSeconds(_maxEndDate.Value) : (DateTimeOffset?)null;

            return new HoldingMark(
                Mark: mark,
                StartDate: startDate,
                EndDate: endDate,
                Species: _species.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList());
        }
    }
}