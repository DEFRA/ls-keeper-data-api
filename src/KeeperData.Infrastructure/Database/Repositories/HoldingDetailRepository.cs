using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Text.Json;

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

    public async Task<(List<HoldingDetail> Items, int TotalCount)> GetPagedHoldingsAsync(
        int page,
        int pageSize,
        string? sort,
        string? order,
        CancellationToken cancellationToken = default)
    {
        var dbPath = _cacheService.GetCurrentDbPath()
            ?? throw new InvalidOperationException("The SAM read model is not cached locally, so holding details cannot be resolved.");

        await using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        await connection.OpenAsync(cancellationToken);

        // 1 — Total count
        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Holding;";
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        if (totalCount == 0)
        {
            return ([], 0);
        }

        // 2 — Paged holdings
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Max(1, pageSize);
        var offset = ((long)safePage - 1) * safePageSize;
        var sortDirection = string.Equals(sort, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        var sortColumn = order?.ToLowerInvariant() switch
        {
            "name" => "name",
            "holdingtype" => "holdingtype",
            "startdate" => "startdate",
            "enddate" => "enddate",
            _ => "cph"
        };

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
            ORDER BY
                CASE WHEN $order = 'name' AND $sort = 'asc' THEN h.FeatureName END ASC,
                CASE WHEN $order = 'name' AND $sort = 'desc' THEN h.FeatureName END DESC,
                CASE WHEN $order = 'holdingtype' AND $sort = 'asc' THEN h.CphType END ASC,
                CASE WHEN $order = 'holdingtype' AND $sort = 'desc' THEN h.CphType END DESC,
                CASE WHEN $order = 'startdate' AND $sort = 'asc' THEN h.StartDate END ASC,
                CASE WHEN $order = 'startdate' AND $sort = 'desc' THEN h.StartDate END DESC,
                CASE WHEN $order = 'enddate' AND $sort = 'asc' THEN h.EndDate END ASC,
                CASE WHEN $order = 'enddate' AND $sort = 'desc' THEN h.EndDate END DESC,
                CASE WHEN $sort = 'asc' THEN h.Cph END ASC,
                CASE WHEN $sort = 'desc' THEN h.Cph END DESC
            LIMIT $pageSize OFFSET $offset;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqliteParameter("$order", sortColumn));
        command.Parameters.Add(new SqliteParameter("$sort", sortDirection));
        command.Parameters.Add(new SqliteParameter("$pageSize", safePageSize));
        command.Parameters.Add(new SqliteParameter("$offset", offset));

        var holdingRows = new List<HoldingRowData>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                holdingRows.Add(MapHoldingRow(reader));
            }
        }

        if (holdingRows.Count == 0)
        {
            return ([], totalCount);
        }

        var holdingIds = holdingRows.Select(h => h.HoldingId).ToList();

        // 3 — Batched Associations
        var associationsMap = await ReadBatchAssociationsAsync(connection, holdingIds, cancellationToken);

        // 4 — Batched Allowed Species
        var speciesMap = await ReadBatchAllowedSpeciesAsync(connection, holdingIds, cancellationToken);

        // 5 — Batched Marks
        var marksMap = await ReadBatchMarksAsync(connection, holdingIds, cancellationToken);

        var items = new List<HoldingDetail>(holdingRows.Count);
        foreach (var row in holdingRows)
        {
            var associations = associationsMap.GetValueOrDefault(row.HoldingId) ?? [];
            var allowedSpecies = speciesMap.GetValueOrDefault(row.HoldingId) ?? [];
            var marks = marksMap.GetValueOrDefault(row.HoldingId) ?? [];

            items.Add(new HoldingDetail(
                Identifier: row.Cph,
                HoldingType: row.CphType,
                Name: row.Name,
                StartDate: row.StartDate,
                EndDate: row.EndDate,
                Location: row.Location,
                Associations: associations,
                AllowedSpecies: allowedSpecies,
                Marks: marks));
        }

        return (items, totalCount);
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
        Dictionary<string, PartyAccumulator> partyMap,
        int offset = 0)
    {
        var sourcePartyId = reader.GetString(0 + offset);
        var personTitle = GetNullableString(reader, 1 + offset);
        var givenName = GetNullableString(reader, 2 + offset);
        var initials = GetNullableString(reader, 3 + offset);
        var familyName = GetNullableString(reader, 4 + offset);
        var organisationName = GetNullableString(reader, 5 + offset);
        var email = GetNullableString(reader, 6 + offset);
        var mobile = GetNullableString(reader, 7 + offset);
        var telephone = GetNullableString(reader, 8 + offset);
        var roleCode = reader.GetString(9 + offset);
        var speciesCode = GetNullableString(reader, 10 + offset);

        if (!partyMap.TryGetValue(sourcePartyId, out var accumulator))
        {
            var displayName = AssembleDisplayName(organisationName, personTitle, givenName, initials, familyName);
            var partyType = !string.IsNullOrWhiteSpace(organisationName) ? "organisation" : "person";

            accumulator = new PartyAccumulator
            {
                CustomerNumber = sourcePartyId,
                Title = personTitle,
                FirstName = givenName,
                LastName = familyName,
                Name = displayName,
                PartyType = partyType,
                Email = email,
                Mobile = mobile,
                Telephone = telephone
            };
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
        Dictionary<string, MarkAccumulator> markMap,
        int offset = 0)
    {
        if (reader.IsDBNull(0 + offset))
        {
            return;
        }

        var herdmark = reader.GetString(0 + offset);
        var fromDate = GetNullableInt64(reader, 1 + offset);
        var toDate = GetNullableInt64(reader, 2 + offset);
        var species = GetNullableString(reader, 3 + offset);

        if (!markMap.TryGetValue(herdmark, out var accumulator))
        {
            accumulator = new MarkAccumulator(herdmark);
            markMap[herdmark] = accumulator;
        }

        accumulator.AddRow(fromDate, toDate, species);
    }

    private static async Task<Dictionary<string, List<HoldingAssociation>>> ReadBatchAssociationsAsync(
        SqliteConnection connection,
        IReadOnlyList<string> holdingIds,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                r.HoldingId,
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
            WHERE r.HoldingId IN (SELECT value FROM json_each($holdingIds))
            ORDER BY r.HoldingId, p.SourcePartyId, r.Role, d.AnimalSpeciesCode;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqliteParameter("$holdingIds", JsonSerializer.Serialize(holdingIds)));

        var resultMap = new Dictionary<string, Dictionary<string, PartyAccumulator>>(StringComparer.OrdinalIgnoreCase);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var holdingId = reader.GetString(0);
            if (!resultMap.TryGetValue(holdingId, out var partyMap))
            {
                partyMap = new Dictionary<string, PartyAccumulator>(StringComparer.OrdinalIgnoreCase);
                resultMap[holdingId] = partyMap;
            }

            ProcessAssociationRow(reader, partyMap, offset: 1);
        }

        return resultMap.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Values.Select(p => p.ToHoldingAssociation()).ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, List<string>>> ReadBatchAllowedSpeciesAsync(
        SqliteConnection connection,
        IReadOnlyList<string> holdingIds,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DISTINCT a.HoldingId, a.AnimalSpeciesCode
            FROM HoldingAnimalProfile AS a
            WHERE a.HoldingId IN (SELECT value FROM json_each($holdingIds))
            ORDER BY a.HoldingId, a.AnimalSpeciesCode;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqliteParameter("$holdingIds", JsonSerializer.Serialize(holdingIds)));

        var resultMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var holdingId = reader.GetString(0);
            if (!reader.IsDBNull(1))
            {
                if (!resultMap.TryGetValue(holdingId, out var speciesList))
                {
                    speciesList = new List<string>();
                    resultMap[holdingId] = speciesList;
                }
                speciesList.Add(reader.GetString(1));
            }
        }

        return resultMap;
    }

    private static async Task<Dictionary<string, List<HoldingMark>>> ReadBatchMarksAsync(
        SqliteConnection connection,
        IReadOnlyList<string> holdingIds,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                d.HoldingId,
                d.Herdmark,
                d.AnimalGroupFromDate,
                d.AnimalGroupToDate,
                d.AnimalSpeciesCode
            FROM Herd AS d
            WHERE d.HoldingId IN (SELECT value FROM json_each($holdingIds))
            ORDER BY d.HoldingId, d.Herdmark, d.AnimalSpeciesCode;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqliteParameter("$holdingIds", JsonSerializer.Serialize(holdingIds)));

        var resultMap = new Dictionary<string, Dictionary<string, MarkAccumulator>>(StringComparer.OrdinalIgnoreCase);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var holdingId = reader.GetString(0);
            if (!resultMap.TryGetValue(holdingId, out var markMap))
            {
                markMap = new Dictionary<string, MarkAccumulator>(StringComparer.OrdinalIgnoreCase);
                resultMap[holdingId] = markMap;
            }

            ProcessMarkRow(reader, markMap, offset: 1);
        }

        return resultMap.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Values.Select(m => m.ToHoldingMark()).ToList(),
            StringComparer.OrdinalIgnoreCase);
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

    private sealed class PartyAccumulator
    {
        public required string CustomerNumber { get; init; }
        public string? Title { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Name { get; init; }
        public required string PartyType { get; init; }
        public string? Email { get; init; }
        public string? Mobile { get; init; }
        public string? Telephone { get; init; }

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
                CustomerNumber: CustomerNumber,
                Title: Title,
                FirstName: FirstName,
                LastName: LastName,
                Name: Name,
                PartyType: PartyType,
                Email: Email,
                Mobile: Mobile,
                Telephone: Telephone,
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