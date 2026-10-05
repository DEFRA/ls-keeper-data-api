using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using KeeperData.Core.Storage.Sqlite;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Text.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace KeeperData.Infrastructure.Database.Repositories;

public partial class HoldingDetailRepository(IReadModelSqliteCacheService cacheService, ILogger<HoldingDetailRepository>? logger = null) : IHoldingDetailRepository
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

    private const string SelectHoldingColumns = """
        SELECT
            h.Id,
            h.Cph,
            h.FeatureName,
            h.CphType,
            h.StartDate,
            h.EndDate,
            h.Udprn,
            h.SaonDescription,
            h.SaonStartNumber,
            h.SaonStartNumberSuffix,
            h.SaonEndNumber,
            h.SaonEndNumberSuffix,
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
        """;

    private const string SearchJoinClause = " JOIN search_index.HoldingSearch AS s ON s.HoldingId = h.Id WHERE s.SearchText MATCH $match";
    private const string BaseHoldingsQuery = SelectHoldingColumns;
    private const string BaseSearchQuery = SelectHoldingColumns + SearchJoinClause;

    // Holdings queries
    private const string HoldingsByCphAsc = BaseHoldingsQuery + " ORDER BY h.Cph ASC LIMIT $pageSize OFFSET $offset;";
    private const string HoldingsByCphDesc = BaseHoldingsQuery + " ORDER BY h.Cph DESC LIMIT $pageSize OFFSET $offset;";
    private const string HoldingsByNameAsc = BaseHoldingsQuery + " ORDER BY h.FeatureName ASC, h.Cph ASC LIMIT $pageSize OFFSET $offset;";
    private const string HoldingsByNameDesc = BaseHoldingsQuery + " ORDER BY h.FeatureName DESC, h.Cph DESC LIMIT $pageSize OFFSET $offset;";
    private const string HoldingsByTypeAsc = BaseHoldingsQuery + " ORDER BY h.CphType ASC, h.Cph ASC LIMIT $pageSize OFFSET $offset;";
    private const string HoldingsByTypeDesc = BaseHoldingsQuery + " ORDER BY h.CphType DESC, h.Cph DESC LIMIT $pageSize OFFSET $offset;";
    private const string HoldingsByStartDateAsc = BaseHoldingsQuery + " ORDER BY h.StartDate ASC, h.Cph ASC LIMIT $pageSize OFFSET $offset;";
    private const string HoldingsByStartDateDesc = BaseHoldingsQuery + " ORDER BY h.StartDate DESC, h.Cph DESC LIMIT $pageSize OFFSET $offset;";
    private const string HoldingsByEndDateAsc = BaseHoldingsQuery + " ORDER BY h.EndDate ASC, h.Cph ASC LIMIT $pageSize OFFSET $offset;";
    private const string HoldingsByEndDateDesc = BaseHoldingsQuery + " ORDER BY h.EndDate DESC, h.Cph DESC LIMIT $pageSize OFFSET $offset;";

    // Search queries
    private const string SearchByCphAsc = BaseSearchQuery + " ORDER BY h.Cph ASC LIMIT $pageSize OFFSET $offset;";
    private const string SearchByCphDesc = BaseSearchQuery + " ORDER BY h.Cph DESC LIMIT $pageSize OFFSET $offset;";
    private const string SearchByNameAsc = BaseSearchQuery + " ORDER BY h.FeatureName ASC, h.Cph ASC LIMIT $pageSize OFFSET $offset;";
    private const string SearchByNameDesc = BaseSearchQuery + " ORDER BY h.FeatureName DESC, h.Cph DESC LIMIT $pageSize OFFSET $offset;";
    private const string SearchByTypeAsc = BaseSearchQuery + " ORDER BY h.CphType ASC, h.Cph ASC LIMIT $pageSize OFFSET $offset;";
    private const string SearchByTypeDesc = BaseSearchQuery + " ORDER BY h.CphType DESC, h.Cph DESC LIMIT $pageSize OFFSET $offset;";
    private const string SearchByStartDateAsc = BaseSearchQuery + " ORDER BY h.StartDate ASC, h.Cph ASC LIMIT $pageSize OFFSET $offset;";
    private const string SearchByStartDateDesc = BaseSearchQuery + " ORDER BY h.StartDate DESC, h.Cph DESC LIMIT $pageSize OFFSET $offset;";
    private const string SearchByEndDateAsc = BaseSearchQuery + " ORDER BY h.EndDate ASC, h.Cph ASC LIMIT $pageSize OFFSET $offset;";
    private const string SearchByEndDateDesc = BaseSearchQuery + " ORDER BY h.EndDate DESC, h.Cph DESC LIMIT $pageSize OFFSET $offset;";

    public async Task<(List<HoldingDetail> Items, int TotalCount, DateTime? DataTimestamp)> GetPagedHoldingsAsync(
        int page,
        int pageSize,
        string? sort,
        string? order,
        CancellationToken cancellationToken = default)
    {
        var snapshot = _cacheService.GetCurrentSnapshot()
            ?? throw new InvalidOperationException("The SAM read model is not cached locally, so holding details cannot be resolved.");

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = snapshot.DbPath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = true
        };
        await using var connection = new SqliteConnection(connectionString.ToString());
        await connection.OpenAsync(cancellationToken);

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Holding;";
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));
        if (totalCount == 0)
        {
            return ([], 0, snapshot.DataTimestamp);
        }

        var (safePageSize, offset) = CalculatePaging(page, pageSize);
        var descending = string.Equals(sort, "desc", StringComparison.OrdinalIgnoreCase);

        await using var command = connection.CreateCommand();
        command.CommandText = ResolveHoldingsSql(order, descending);
        command.Parameters.Add(new SqliteParameter("$pageSize", safePageSize));
        command.Parameters.Add(new SqliteParameter("$offset", offset));

        var holdingRows = await ReadHoldingRowsAsync(command, cancellationToken);
        if (holdingRows.Count == 0)
        {
            return ([], totalCount, snapshot.DataTimestamp);
        }

        var items = await HydrateHoldingDetailsAsync(connection, holdingRows, cancellationToken);
        return (items, totalCount, snapshot.DataTimestamp);
    }

    public async Task<(List<HoldingDetail> Items, int TotalCount, DateTime? DataTimestamp)> SearchHoldingsAsync(
        int page, int pageSize, string? sort, string? order, string search,
        CancellationToken cancellationToken = default)
    {
        var snapshot = _cacheService.GetCurrentSnapshot()
            ?? throw new InvalidOperationException("The SAM read model is not cached locally, so holding details cannot be resolved.");

        var match = ToFtsQuery(search);
        if (string.IsNullOrEmpty(match))
        {
            return ([], 0, snapshot.DataTimestamp);
        }

        if (snapshot.SearchIndexPath is null || !File.Exists(snapshot.SearchIndexPath))
        {
            throw new SearchIndexUnavailableException();
        }

        try
        {
            return await ExecuteSearchHoldingsAsync(snapshot, match, page, pageSize, sort, order, cancellationToken);
        }
        catch (SqliteException ex)
        {
            logger?.LogError(ex, "Holding search index could not be read");
            throw new SearchIndexUnavailableException("The holding search index is not available.", ex);
        }
    }

    private async Task<(List<HoldingDetail> Items, int TotalCount, DateTime? DataTimestamp)> ExecuteSearchHoldingsAsync(
        SqliteSnapshot snapshot,
        string match,
        int page,
        int pageSize,
        string? sort,
        string? order,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Search connections are not pooled because ATTACH persists on pooled SQLite connections
        // and could hold locks or expose an index from an older snapshot.
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = snapshot.DbPath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        };
        await using var connection = new SqliteConnection(connectionString.ToString());
        await connection.OpenAsync(cancellationToken);

        await AttachSearchIndexAsync(connection, snapshot.SearchIndexPath!, cancellationToken);

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM search_index.HoldingSearch WHERE SearchText MATCH $match;";
        countCommand.Parameters.Add(new SqliteParameter("$match", match));
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        if (totalCount == 0)
        {
            LogSearchCompleted(stopwatch, totalCount);
            return ([], 0, snapshot.DataTimestamp);
        }

        var (safePageSize, offset) = CalculatePaging(page, pageSize);
        var descending = string.Equals(sort, "desc", StringComparison.OrdinalIgnoreCase);

        await using var command = connection.CreateCommand();
        command.CommandText = ResolveSearchSql(order, descending);
        command.Parameters.Add(new SqliteParameter("$pageSize", safePageSize));
        command.Parameters.Add(new SqliteParameter("$offset", offset));
        command.Parameters.Add(new SqliteParameter("$match", match));

        var holdingRows = await ReadHoldingRowsAsync(command, cancellationToken);
        if (holdingRows.Count == 0)
        {
            LogSearchCompleted(stopwatch, totalCount);
            return ([], totalCount, snapshot.DataTimestamp);
        }

        var items = await HydrateHoldingDetailsAsync(connection, holdingRows, cancellationToken);
        LogSearchCompleted(stopwatch, totalCount);
        return (items, totalCount, snapshot.DataTimestamp);
    }

    private static (int PageSize, long Offset) CalculatePaging(int page, int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Max(1, pageSize);
        var offset = ((long)safePage - 1) * safePageSize;
        return (safePageSize, offset);
    }

    private static async Task AttachSearchIndexAsync(
        SqliteConnection connection,
        string searchIndexPath,
        CancellationToken cancellationToken)
    {
        await using var attach = connection.CreateCommand();
        attach.CommandText = "ATTACH DATABASE $indexPath AS search_index";
        attach.Parameters.AddWithValue("$indexPath", searchIndexPath);
        await attach.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<HoldingRowData>> ReadHoldingRowsAsync(
        SqliteCommand command,
        CancellationToken cancellationToken)
    {
        var holdingRows = new List<HoldingRowData>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var row = new RowAccessor(reader);
        while (await reader.ReadAsync(cancellationToken))
        {
            holdingRows.Add(MapHoldingRow(row));
        }

        return holdingRows;
    }

    private static async Task<List<HoldingDetail>> HydrateHoldingDetailsAsync(
        SqliteConnection connection,
        List<HoldingRowData> holdingRows,
        CancellationToken cancellationToken)
    {
        var holdingIds = holdingRows.Select(h => h.HoldingId).ToList();

        var associationsMap = await ReadBatchAssociationsAsync(connection, holdingIds, cancellationToken);
        var speciesMap = await ReadBatchAllowedSpeciesAsync(connection, holdingIds, cancellationToken);
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

        return items;
    }

    private void LogSearchCompleted(Stopwatch stopwatch, int matchCount)
    {
        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            logger.LogInformation(
                "Holding search completed in {DurationMs}ms with {MatchCount} matches",
                stopwatch.ElapsedMilliseconds,
                matchCount);
        }
    }

    private static string ResolveHoldingsSql(string? order, bool descending) =>
        (order?.ToLowerInvariant(), descending) switch
        {
            ("name", false) => HoldingsByNameAsc,
            ("name", true) => HoldingsByNameDesc,
            ("holdingtype", false) => HoldingsByTypeAsc,
            ("holdingtype", true) => HoldingsByTypeDesc,
            ("startdate", false) => HoldingsByStartDateAsc,
            ("startdate", true) => HoldingsByStartDateDesc,
            ("enddate", false) => HoldingsByEndDateAsc,
            ("enddate", true) => HoldingsByEndDateDesc,
            (_, false) => HoldingsByCphAsc,
            _ => HoldingsByCphDesc
        };

    private static string ResolveSearchSql(string? order, bool descending) =>
        (order?.ToLowerInvariant(), descending) switch
        {
            ("name", false) => SearchByNameAsc,
            ("name", true) => SearchByNameDesc,
            ("holdingtype", false) => SearchByTypeAsc,
            ("holdingtype", true) => SearchByTypeDesc,
            ("startdate", false) => SearchByStartDateAsc,
            ("startdate", true) => SearchByStartDateDesc,
            ("enddate", false) => SearchByEndDateAsc,
            ("enddate", true) => SearchByEndDateDesc,
            (_, false) => SearchByCphAsc,
            _ => SearchByCphDesc
        };

    private static string ToFtsQuery(string search)
    {
        search = search.Trim();
        if (CphWithSlashesRegex().IsMatch(search))
            return $"\"{search.Replace("/", "", StringComparison.Ordinal)}\"";
        if (CphNineDigitsRegex().IsMatch(search))
            return $"\"{search}\"";

        var terms = SearchTermsRegex().Matches(search)
            .Select(match => $"\"{match.Value}\"*");
        return string.Join(" AND ", terms);
    }

    [GeneratedRegex(@"^\d{2}/\d{3}/\d{4}$")]
    private static partial Regex CphWithSlashesRegex();

    [GeneratedRegex(@"^\d{9}$")]
    private static partial Regex CphNineDigitsRegex();

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex SearchTermsRegex();

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
                h.SaonDescription,
                h.SaonStartNumber,
                h.SaonStartNumberSuffix,
                h.SaonEndNumber,
                h.SaonEndNumberSuffix,
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

        return MapHoldingRow(new RowAccessor(reader));
    }

    private static HoldingRowData MapHoldingRow(RowAccessor row)
    {
        var holdingId = row.GetString("Id");
        var holdingCph = row.GetString("Cph");
        var name = row.GetNullableString("FeatureName");
        var cphType = row.GetNullableString("CphType");
        var startDate = row.GetEpoch("StartDate");
        var endDate = row.GetEpoch("EndDate");
        var udprn = row.GetNullableInt64("Udprn");

        var postTown = row.GetNullableString("Town");
        var locality = row.GetNullableString("Locality");
        var postcode = row.GetNullableString("Postcode");
        var country = row.GetNullableString("UkInternalCode");

        var easting = row.GetNullableInt32("Easting");
        var northing = row.GetNullableInt32("Northing");
        var osMapReference = row.GetNullableString("OsMapReference");

        var (addressLine1, addressLine2) = AssembleAddressLines(
            row.GetNullableString("SaonDescription"),
            row.GetNullableString("SaonStartNumber"),
            row.GetNullableString("SaonStartNumberSuffix"),
            row.GetNullableString("SaonEndNumber"),
            row.GetNullableString("SaonEndNumberSuffix"),
            row.GetNullableString("PaonDescription"),
            row.GetNullableString("PaonStartNumber"),
            row.GetNullableString("PaonStartNumberSuffix"),
            row.GetNullableString("PaonEndNumber"),
            row.GetNullableString("PaonEndNumberSuffix"),
            row.GetNullableString("Street"));

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
                p.AddressLine1,
                p.AddressStreet,
                p.AddressTown,
                p.AddressLocality,
                p.AddressNation,
                p.AddressPostcode,
                p.AddressCountryCode,
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
        var row = new RowAccessor(reader);
        var partyMap = new Dictionary<string, PartyAccumulator>(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync(cancellationToken))
        {
            ProcessAssociationRow(row, partyMap);
        }

        return partyMap.Values.Select(p => p.ToHoldingAssociation()).ToList();
    }

    private static void ProcessAssociationRow(
        RowAccessor row,
        Dictionary<string, PartyAccumulator> partyMap)
    {
        var sourcePartyId = row.GetString("SourcePartyId");
        var personTitle = row.GetNullableString("PersonTitle");
        var givenName = row.GetNullableString("GivenName");
        var initials = row.GetNullableString("Initials");
        var familyName = row.GetNullableString("FamilyName");
        var organisationName = row.GetNullableString("OrganisationName");
        var email = row.GetNullableString("Email");
        var mobile = row.GetNullableString("Mobile");
        var telephone = row.GetNullableString("Telephone");
        var addressLine1 = row.GetNullableString("AddressLine1");
        var addressLine2 = row.GetNullableString("AddressStreet");
        var addressTown = row.GetNullableString("AddressTown");
        var addressLocality = row.GetNullableString("AddressLocality");
        var addressNation = row.GetNullableString("AddressNation");
        var addressPostcode = row.GetNullableString("AddressPostcode");
        var addressCountryCode = row.GetNullableString("AddressCountryCode");
        var roleCode = row.GetString("Role");
        var speciesCode = row.GetNullableString("AnimalSpeciesCode");

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
                Telephone = telephone,
                Address = new PartyAddress(
                    AddressLine1: addressLine1,
                    AddressLine2: addressLine2,
                    AddressTown: addressTown,
                    AddressLocality: addressLocality,
                    AddressNation: addressNation,
                    AddressPostcode: addressPostcode,
                    AddressCountryCode: addressCountryCode)
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
        var row = new RowAccessor(reader);
        var speciesList = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var species = row.GetNullableString("AnimalSpeciesCode");
            if (species is not null)
            {
                speciesList.Add(species);
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
        var row = new RowAccessor(reader);
        var markMap = new Dictionary<string, MarkAccumulator>(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync(cancellationToken))
        {
            ProcessMarkRow(row, markMap);
        }

        return markMap.Values.Select(m => m.ToHoldingMark()).ToList();
    }

    private static void ProcessMarkRow(
        RowAccessor row,
        Dictionary<string, MarkAccumulator> markMap)
    {
        var herdmark = row.GetNullableString("Herdmark");
        if (herdmark is null)
        {
            return;
        }

        var fromDate = row.GetNullableInt64("AnimalGroupFromDate");
        var toDate = row.GetNullableInt64("AnimalGroupToDate");
        var species = row.GetNullableString("AnimalSpeciesCode");

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
                p.AddressLine1,
                p.AddressStreet,
                p.AddressTown,
                p.AddressLocality,
                p.AddressNation,
                p.AddressPostcode,
                p.AddressCountryCode,
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
        var row = new RowAccessor(reader);
        while (await reader.ReadAsync(cancellationToken))
        {
            var holdingId = row.GetString("HoldingId");
            if (!resultMap.TryGetValue(holdingId, out var partyMap))
            {
                partyMap = new Dictionary<string, PartyAccumulator>(StringComparer.OrdinalIgnoreCase);
                resultMap[holdingId] = partyMap;
            }

            ProcessAssociationRow(row, partyMap);
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
        var row = new RowAccessor(reader);
        while (await reader.ReadAsync(cancellationToken))
        {
            var holdingId = row.GetString("HoldingId");
            var species = row.GetNullableString("AnimalSpeciesCode");
            if (species is null)
            {
                continue;
            }

            if (!resultMap.TryGetValue(holdingId, out var speciesList))
            {
                speciesList = new List<string>();
                resultMap[holdingId] = speciesList;
            }

            speciesList.Add(species);
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
        var row = new RowAccessor(reader);
        while (await reader.ReadAsync(cancellationToken))
        {
            var holdingId = row.GetString("HoldingId");
            if (!resultMap.TryGetValue(holdingId, out var markMap))
            {
                markMap = new Dictionary<string, MarkAccumulator>(StringComparer.OrdinalIgnoreCase);
                resultMap[holdingId] = markMap;
            }

            ProcessMarkRow(row, markMap);
        }

        return resultMap.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Values.Select(m => m.ToHoldingMark()).ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static (string? AddressLine1, string? AddressLine2) AssembleAddressLines(
        string? saonDescription,
        string? saonStartNumber,
        string? saonStartNumberSuffix,
        string? saonEndNumber,
        string? saonEndNumberSuffix,
        string? paonDescription,
        string? paonStartNumber,
        string? paonStartNumberSuffix,
        string? paonEndNumber,
        string? paonEndNumberSuffix,
        string? street)
    {
        var saonLine = FormatAon(
            saonDescription,
            saonStartNumber,
            saonStartNumberSuffix,
            saonEndNumber,
            saonEndNumberSuffix);

        var paonNumber = FormatNumberRange(paonStartNumber, paonStartNumberSuffix, paonEndNumber, paonEndNumberSuffix);
        var paonDescriptionLine = Trimmed(paonDescription);
        var streetName = Trimmed(street);

        // The PAON number belongs with the thoroughfare; when Street is empty the source carries
        // the thoroughfare in PaonDescription instead, so the number binds to that.
        string? buildingLine;
        string? streetLine;
        if (streetName is not null)
        {
            buildingLine = paonDescriptionLine;
            streetLine = JoinWithSpace(paonNumber, streetName);
        }
        else
        {
            buildingLine = null;
            streetLine = JoinWithSpace(paonNumber, paonDescriptionLine);
        }

        var lines = new[] { saonLine, buildingLine, streetLine }
            .Where(line => line is not null)
            .ToArray();

        return lines.Length switch
        {
            0 => (null, null),
            1 => (lines[0], null),
            // Fold any overflow into line 1 so the thoroughfare always lands in the last populated line.
            _ => (string.Join(", ", lines[..^1]), lines[^1])
        };
    }

    private static string? FormatAon(
        string? description,
        string? startNumber,
        string? startNumberSuffix,
        string? endNumber,
        string? endNumberSuffix)
    {
        var number = FormatNumberRange(startNumber, startNumberSuffix, endNumber, endNumberSuffix);
        return JoinWithSpace(Trimmed(description), number);
    }

    private static string? FormatNumberRange(
        string? startNumber,
        string? startNumberSuffix,
        string? endNumber,
        string? endNumberSuffix)
    {
        var start = Combine(startNumber, startNumberSuffix);
        var end = Combine(endNumber, endNumberSuffix);

        if (start is null)
        {
            return end;
        }

        return end is null || string.Equals(start, end, StringComparison.OrdinalIgnoreCase)
            ? start
            : $"{start}-{end}";
    }

    private static string? JoinWithSpace(string? first, string? second)
    {
        if (first is null)
        {
            return second;
        }

        return second is null ? first : $"{first} {second}";
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

    /// <summary>
    /// Reads columns by name so that changing the column order in a query cannot silently
    /// misalign the mapping. Ordinals are resolved once per reader, not per row.
    /// </summary>
    private sealed class RowAccessor(DbDataReader reader)
    {
        private Dictionary<string, int>? _ordinals;

        private int Ordinal(string name)
        {
            // Resolved lazily because the column list is only reliably available once a row is read.
            if (_ordinals is null)
            {
                _ordinals = new Dictionary<string, int>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    _ordinals[reader.GetName(i)] = i;
                }
            }

            return _ordinals.TryGetValue(name, out var ordinal)
                ? ordinal
                : throw new InvalidOperationException($"The query result does not contain a column named '{name}'.");
        }

        public string GetString(string name) => reader.GetString(Ordinal(name));

        public string? GetNullableString(string name)
        {
            var ordinal = Ordinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        public long? GetNullableInt64(string name)
        {
            var ordinal = Ordinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
        }

        public int? GetNullableInt32(string name)
        {
            var ordinal = Ordinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }

        public DateTimeOffset? GetEpoch(string name)
        {
            var ordinal = Ordinal(name);
            return reader.IsDBNull(ordinal)
                ? null
                : DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(ordinal));
        }
    }

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
        public required PartyAddress Address { get; init; }

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
                Address: Address,
                Roles: roleList);
        }
    }

    private sealed class MarkAccumulator(string mark)
    {
        private long? _minStartDate;
        private long? _maxEndDate;
        private bool _hasOpenEndedRow;
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
            else
            {
                _hasOpenEndedRow = true;
            }

            if (!string.IsNullOrWhiteSpace(species))
            {
                _species.Add(species);
            }
        }

        public HoldingMark ToHoldingMark()
        {
            var startDate = _minStartDate.HasValue ? DateTimeOffset.FromUnixTimeSeconds(_minStartDate.Value) : (DateTimeOffset?)null;
            var endDate = !_hasOpenEndedRow && _maxEndDate.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(_maxEndDate.Value)
                : (DateTimeOffset?)null;

            return new HoldingMark(
                Mark: mark,
                StartDate: startDate,
                EndDate: endDate,
                Species: _species.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList());
        }
    }
}