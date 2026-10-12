using Microsoft.Data.Sqlite;
using KeeperData.Infrastructure.Database.Repositories;

namespace KeeperData.Infrastructure.Services;

/// <summary>Builds an FTS5 index beside a validated, immutable read model snapshot.</summary>
internal static class HoldingSearchIndex
{
    [Flags]
    internal enum SearchableHoldingField
    {
        None = 0,
        Cph = 1 << 0,
        HoldingName = 1 << 1,
        BusinessName = 1 << 2,
        AddressLine1 = 1 << 3,
        AddressLine2 = 1 << 4,
        Town = 1 << 5,
        Locality = 1 << 6,
        Postcode = 1 << 7,
        Herdmark = 1 << 8,
        All = Cph | HoldingName | BusinessName | AddressLine1 | AddressLine2 | Town | Locality | Postcode | Herdmark
    }

    internal static Task<(string Path, long DocumentCount)> BuildAsync(
        string dbPath,
        CancellationToken cancellationToken) =>
        BuildAsync(dbPath, null, cancellationToken);

    internal static async Task<(string Path, long DocumentCount)> BuildAsync(
        string dbPath,
        IReadOnlyList<string>? searchableFields,
        CancellationToken cancellationToken = default)
    {
        var enabledFields = ParseSearchableFields(searchableFields);
        var indexPath = Path.Combine(Path.GetDirectoryName(dbPath)!, "holdings-search.sqlite");

        await using var connection = await InitializeIndexConnectionAsync(indexPath, dbPath, cancellationToken);
        await PopulateIndexAsync(connection, enabledFields, cancellationToken);
        var documentCount = await OptimizeAndCountDocumentsAsync(connection, cancellationToken);

        await connection.CloseAsync();
        return (indexPath, documentCount);
    }

    private static async Task<SqliteConnection> InitializeIndexConnectionAsync(
        string indexPath,
        string dbPath,
        CancellationToken cancellationToken)
    {
        if (File.Exists(indexPath))
        {
            File.Delete(indexPath);
        }

        var connection = new SqliteConnection($"Data Source={indexPath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await ExecutePragmasAsync(connection, cancellationToken);
        await AttachSourceDatabaseAsync(connection, dbPath, cancellationToken);
        await CreateVirtualTableAsync(connection, cancellationToken);

        return connection;
    }

    private static async Task ExecutePragmasAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = """
            PRAGMA synchronous = OFF;
            PRAGMA journal_mode = MEMORY;
            PRAGMA temp_store = MEMORY;
            PRAGMA cache_size = -64000;
            """;
        await pragma.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task AttachSourceDatabaseAsync(SqliteConnection connection, string dbPath, CancellationToken cancellationToken)
    {
        await using var attach = connection.CreateCommand();
        attach.CommandText = "ATTACH DATABASE $source AS source";
        attach.Parameters.AddWithValue("$source", dbPath);
        await attach.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CreateVirtualTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var create = connection.CreateCommand();
        create.CommandText = """
            CREATE VIRTUAL TABLE HoldingSearch USING fts5(
                HoldingId UNINDEXED,
                Cph,
                HoldingName,
                BusinessName,
                AddressLine1,
                AddressLine2,
                Town,
                Locality,
                Postcode,
                Herdmark,
                tokenize='unicode61'
            );
            """;
        await create.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task PopulateIndexAsync(
        SqliteConnection connection,
        SearchableHoldingField enabledFields,
        CancellationToken cancellationToken)
    {
        var availableTables = await GetAvailableSourceTablesAsync(connection, cancellationToken);
        var hasPartyRoles = availableTables.Contains("PartyRole") && availableTables.Contains("Party");
        var hasHerds = availableTables.Contains("Herd");

        var selectSql = BuildSourceSelectSql(hasPartyRoles, hasHerds);

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var insertCmd = CreateInsertCommand(connection, transaction);
        var writer = new HoldingIndexWriter(insertCmd);

        await using var selectCmd = connection.CreateCommand();
        selectCmd.Transaction = transaction;
        selectCmd.CommandText = selectSql;
        await using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            await IndexHoldingRecordAsync(reader, writer, enabledFields, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static SqliteCommand CreateInsertCommand(SqliteConnection connection, SqliteTransaction transaction)
    {
        var insertCmd = connection.CreateCommand();
        insertCmd.Transaction = transaction;
        insertCmd.CommandText = """
            INSERT INTO HoldingSearch (
                HoldingId, Cph, HoldingName, BusinessName, AddressLine1, AddressLine2, Town, Locality, Postcode, Herdmark
            ) VALUES (
                $holdingId, $cph, $name, $business, $line1, $line2, $town, $locality, $postcode, $herdmark
            );
            """;

        insertCmd.Parameters.Add("$holdingId", SqliteType.Text);
        insertCmd.Parameters.Add("$cph", SqliteType.Text);
        insertCmd.Parameters.Add("$name", SqliteType.Text);
        insertCmd.Parameters.Add("$business", SqliteType.Text);
        insertCmd.Parameters.Add("$line1", SqliteType.Text);
        insertCmd.Parameters.Add("$line2", SqliteType.Text);
        insertCmd.Parameters.Add("$town", SqliteType.Text);
        insertCmd.Parameters.Add("$locality", SqliteType.Text);
        insertCmd.Parameters.Add("$postcode", SqliteType.Text);
        insertCmd.Parameters.Add("$herdmark", SqliteType.Text);

        return insertCmd;
    }

    private static async Task IndexHoldingRecordAsync(
        SqliteDataReader reader,
        HoldingIndexWriter writer,
        SearchableHoldingField enabledFields,
        CancellationToken cancellationToken)
    {
        var row = ReadSourceHoldingRow(reader);
        var (line1, line2) = HoldingDetailRepository.AssembleAddressLines(row.Saon, row.Paon, row.Street);

        var businessList = SplitEntries(row.BusinessNames);
        var herdList = SplitEntries(row.Herdmarks);

        await writer.WritePrimaryRecordAsync(row, line1, line2, businessList, herdList, enabledFields, cancellationToken);
        await IndexSecondaryEntriesAsync(writer, row, line1, line2, businessList, herdList, enabledFields, cancellationToken);
    }

    private static async Task IndexSecondaryEntriesAsync(
        HoldingIndexWriter writer,
        SourceHoldingRow row,
        string? line1,
        string? line2,
        string[] businessList,
        string[] herdList,
        SearchableHoldingField enabledFields,
        CancellationToken cancellationToken)
    {
        await IndexAdditionalBusinessesAsync(writer, businessList, enabledFields, cancellationToken);
        await IndexAdditionalHerdmarksAsync(writer, herdList, enabledFields, cancellationToken);
        await IndexAddressAliasesAsync(writer, row, line1, line2, enabledFields, cancellationToken);
    }

    private static async Task IndexAdditionalBusinessesAsync(
        HoldingIndexWriter writer,
        string[] businessList,
        SearchableHoldingField enabledFields,
        CancellationToken cancellationToken)
    {
        if (!enabledFields.HasFlag(SearchableHoldingField.BusinessName) || businessList.Length <= 1)
        {
            return;
        }

        for (var i = 1; i < businessList.Length; i++)
        {
            writer.ClearParameters();
            writer.SetBusiness(businessList[i]);
            await writer.ExecuteInsertAsync(cancellationToken);
        }
    }

    private static async Task IndexAdditionalHerdmarksAsync(
        HoldingIndexWriter writer,
        string[] herdList,
        SearchableHoldingField enabledFields,
        CancellationToken cancellationToken)
    {
        if (!enabledFields.HasFlag(SearchableHoldingField.Herdmark) || herdList.Length <= 1)
        {
            return;
        }

        for (var i = 1; i < herdList.Length; i++)
        {
            writer.ClearParameters();
            writer.SetHerdmark(FormatHerdmarkToken(herdList[i]));
            await writer.ExecuteInsertAsync(cancellationToken);
        }
    }

    private static async Task IndexAddressAliasesAsync(
        HoldingIndexWriter writer,
        SourceHoldingRow row,
        string? line1,
        string? line2,
        SearchableHoldingField enabledFields,
        CancellationToken cancellationToken)
    {
        if (enabledFields.HasFlag(SearchableHoldingField.AddressLine1))
        {
            await IndexLine1AliasesAsync(writer, row, line1, line2, cancellationToken);
        }

        if (enabledFields.HasFlag(SearchableHoldingField.AddressLine2) && !string.IsNullOrEmpty(line2))
        {
            await IndexLine2AliasesAsync(writer, row.Paon, line2, cancellationToken);
        }
    }

    private static async Task IndexLine1AliasesAsync(
        HoldingIndexWriter writer,
        SourceHoldingRow row,
        string? line1,
        string? line2,
        CancellationToken cancellationToken)
    {
        var pairs = string.IsNullOrEmpty(line2)
            ? new[]
            {
                (row.Saon.StartNumber, row.Saon.StartNumberSuffix),
                (row.Saon.EndNumber, row.Saon.EndNumberSuffix),
                (row.Paon.StartNumber, row.Paon.StartNumberSuffix),
                (row.Paon.EndNumber, row.Paon.EndNumberSuffix)
            }
            : new[]
            {
                (row.Saon.StartNumber, row.Saon.StartNumberSuffix),
                (row.Saon.EndNumber, row.Saon.EndNumberSuffix)
            };

        var aliases = ExtractDistinctAliases(line1, pairs);
        foreach (var alias in aliases)
        {
            writer.ClearParameters();
            writer.SetLine1(alias);
            await writer.ExecuteInsertAsync(cancellationToken);
        }
    }

    private static async Task IndexLine2AliasesAsync(
        HoldingIndexWriter writer,
        AonComponents paon,
        string line2,
        CancellationToken cancellationToken)
    {
        var pairs = new[]
        {
            (paon.StartNumber, paon.StartNumberSuffix),
            (paon.EndNumber, paon.EndNumberSuffix)
        };

        var aliases = ExtractDistinctAliases(line2, pairs);
        foreach (var alias in aliases)
        {
            writer.ClearParameters();
            writer.SetLine2(alias);
            await writer.ExecuteInsertAsync(cancellationToken);
        }
    }

    private static async Task<long> OptimizeAndCountDocumentsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using (var optimize = connection.CreateCommand())
        {
            optimize.CommandText = "INSERT INTO HoldingSearch(HoldingSearch) VALUES('optimize');";
            await optimize.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var count = connection.CreateCommand();
        count.CommandText = "SELECT COUNT(DISTINCT HoldingId) FROM HoldingSearch";
        return (long)(await count.ExecuteScalarAsync(cancellationToken))!;
    }

    private static string[] SplitEntries(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string ResolveField(
        SearchableHoldingField enabledFields,
        SearchableHoldingField field,
        string? value,
        Func<string, string>? formatter = null)
    {
        if (!enabledFields.HasFlag(field) || string.IsNullOrEmpty(value))
        {
            return "";
        }

        return formatter is not null ? formatter(value) : value;
    }

    private static string ResolveFirstListField(
        SearchableHoldingField enabledFields,
        SearchableHoldingField field,
        string[] list,
        Func<string, string>? formatter = null)
    {
        if (!enabledFields.HasFlag(field) || list.Length == 0)
        {
            return "";
        }

        return formatter is not null ? formatter(list[0]) : list[0];
    }

    private readonly record struct SourceHoldingRow(
        string Id,
        string? Cph,
        string? FeatureName,
        AonComponents Saon,
        AonComponents Paon,
        string? Street,
        string? Town,
        string? Locality,
        string? Postcode,
        string? BusinessNames,
        string? Herdmarks);

    private static SourceHoldingRow ReadSourceHoldingRow(SqliteDataReader reader)
    {
        return new SourceHoldingRow(
            Id: reader.GetString(0),
            Cph: GetNullableString(reader, 1),
            FeatureName: GetNullableString(reader, 2),
            Saon: new AonComponents(
                GetNullableString(reader, 3),
                GetNullableString(reader, 4),
                GetNullableString(reader, 5),
                GetNullableString(reader, 6),
                GetNullableString(reader, 7)),
            Paon: new AonComponents(
                GetNullableString(reader, 8),
                GetNullableString(reader, 9),
                GetNullableString(reader, 10),
                GetNullableString(reader, 11),
                GetNullableString(reader, 12)),
            Street: GetNullableString(reader, 13),
            Town: GetNullableString(reader, 14),
            Locality: GetNullableString(reader, 15),
            Postcode: GetNullableString(reader, 16),
            BusinessNames: GetNullableString(reader, 17),
            Herdmarks: GetNullableString(reader, 18));
    }

    private static string? GetNullableString(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private sealed class HoldingIndexWriter(SqliteCommand insertCmd)
    {
        private readonly SqliteParameter _pId = insertCmd.Parameters["$holdingId"];
        private readonly SqliteParameter _pCph = insertCmd.Parameters["$cph"];
        private readonly SqliteParameter _pName = insertCmd.Parameters["$name"];
        private readonly SqliteParameter _pBusiness = insertCmd.Parameters["$business"];
        private readonly SqliteParameter _pLine1 = insertCmd.Parameters["$line1"];
        private readonly SqliteParameter _pLine2 = insertCmd.Parameters["$line2"];
        private readonly SqliteParameter _pTown = insertCmd.Parameters["$town"];
        private readonly SqliteParameter _pLocality = insertCmd.Parameters["$locality"];
        private readonly SqliteParameter _pPostcode = insertCmd.Parameters["$postcode"];
        private readonly SqliteParameter _pHerdmark = insertCmd.Parameters["$herdmark"];

        public void ClearParameters()
        {
            _pCph.Value = "";
            _pName.Value = "";
            _pBusiness.Value = "";
            _pLine1.Value = "";
            _pLine2.Value = "";
            _pTown.Value = "";
            _pLocality.Value = "";
            _pPostcode.Value = "";
            _pHerdmark.Value = "";
        }

        public void SetBusiness(string value) => _pBusiness.Value = value;
        public void SetHerdmark(string value) => _pHerdmark.Value = value;
        public void SetLine1(string value) => _pLine1.Value = value;
        public void SetLine2(string value) => _pLine2.Value = value;

        public Task<int> ExecuteInsertAsync(CancellationToken cancellationToken) =>
            insertCmd.ExecuteNonQueryAsync(cancellationToken);

        public async Task WritePrimaryRecordAsync(
            SourceHoldingRow row,
            string? line1,
            string? line2,
            string[] businessList,
            string[] herdList,
            SearchableHoldingField enabledFields,
            CancellationToken cancellationToken)
        {
            _pId.Value = row.Id;
            _pCph.Value = ResolveField(enabledFields, SearchableHoldingField.Cph, row.Cph, FormatCphTokens);
            _pName.Value = ResolveField(enabledFields, SearchableHoldingField.HoldingName, row.FeatureName);
            _pBusiness.Value = ResolveFirstListField(enabledFields, SearchableHoldingField.BusinessName, businessList);
            _pLine1.Value = ResolveField(enabledFields, SearchableHoldingField.AddressLine1, line1);
            _pLine2.Value = ResolveField(enabledFields, SearchableHoldingField.AddressLine2, line2);
            _pTown.Value = ResolveField(enabledFields, SearchableHoldingField.Town, row.Town);
            _pLocality.Value = ResolveField(enabledFields, SearchableHoldingField.Locality, row.Locality);
            _pPostcode.Value = ResolveField(enabledFields, SearchableHoldingField.Postcode, row.Postcode, FormatPostcodeTokens);
            _pHerdmark.Value = ResolveFirstListField(enabledFields, SearchableHoldingField.Herdmark, herdList, FormatHerdmarkToken);

            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<HashSet<string>> GetAvailableSourceTablesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM source.sqlite_master WHERE type='table';";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }

    private static string BuildSourceSelectSql(bool hasPartyRoles, bool hasHerds)
    {
        var cte = new List<string>();
        var joins = new List<string>();

        if (hasPartyRoles)
        {
            cte.Add("""
                distinct_parties AS (
                    SELECT DISTINCT r.HoldingId, p.OrganisationName
                    FROM source.PartyRole AS r
                    JOIN source.Party AS p ON p.Id = r.PartyId
                    WHERE p.OrganisationName IS NOT NULL AND length(trim(p.OrganisationName)) > 0
                ),
                party_agg AS (
                    SELECT HoldingId, group_concat(OrganisationName, char(10)) AS BusinessNames
                    FROM distinct_parties
                    GROUP BY HoldingId
                )
                """);
            joins.Add("LEFT JOIN party_agg ON party_agg.HoldingId = h.Id");
        }

        if (hasHerds)
        {
            cte.Add("""
                distinct_herds AS (
                    SELECT DISTINCT d.HoldingId, d.Herdmark
                    FROM source.Herd AS d
                    WHERE d.Herdmark IS NOT NULL AND length(trim(d.Herdmark)) > 0
                ),
                herd_agg AS (
                    SELECT HoldingId, group_concat(Herdmark, char(10)) AS Herdmarks
                    FROM distinct_herds
                    GROUP BY HoldingId
                )
                """);
            joins.Add("LEFT JOIN herd_agg ON herd_agg.HoldingId = h.Id");
        }

        var cteClause = cte.Count > 0 ? "WITH " + string.Join(",\n", cte) : "";
        var businessCol = hasPartyRoles ? "party_agg.BusinessNames" : "NULL AS BusinessNames";
        var herdCol = hasHerds ? "herd_agg.Herdmarks" : "NULL AS Herdmarks";
        var joinClause = string.Join("\n", joins);

        return $"""
            {cteClause}
            SELECT
                h.Id,
                h.Cph,
                h.FeatureName,
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
                {businessCol},
                {herdCol}
            FROM source.Holding AS h
            {joinClause};
            """;
    }

    private static string FormatCphTokens(string cph)
    {
        var rawDigits = cph.Replace("/", "");
        var parts = cph.Split('/');
        if (parts.Length == 3)
        {
            var county = parts[0];
            var parish = parts[1];
            var holding = parts[2];
            return $"{cph} {rawDigits} {holding} {parish}/{holding} {parish}{holding}";
        }
        return $"{cph} {rawDigits}";
    }

    private static List<string> ExtractDistinctAliases(string? line, IEnumerable<(string? Num, string? Suffix)> pairs)
    {
        var aliases = new List<string>();
        foreach (var (num, suffix) in pairs)
        {
            if (!string.IsNullOrEmpty(num) && !string.IsNullOrEmpty(suffix))
            {
                aliases.Add(num);
                aliases.Add(suffix);
            }
        }

        var words = new HashSet<string>((line ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
        return aliases
            .Where(a => !string.IsNullOrWhiteSpace(a) && !words.Contains(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FormatPostcodeTokens(string postcode)
    {
        var unspaced = postcode.Replace(" ", "").Trim();
        var tokens = new List<string> { postcode, unspaced };

        if (unspaced.Length > 3)
        {
            var inward = unspaced[^3..];
            var outward = unspaced[..^3];
            tokens.Add(inward);
            tokens.Add($"{outward} {inward}");
        }
        else if (postcode.Contains(' '))
        {
            var parts = postcode.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            tokens.Add(parts[^1]);
        }

        return string.Join(" ", tokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct());
    }

    private static string FormatHerdmarkToken(string raw)
    {
        var m = raw.Trim();
        if (string.IsNullOrEmpty(m))
            return "";

        var unspaced = m.Replace(" ", "");
        if (m.StartsWith("UK", StringComparison.OrdinalIgnoreCase))
        {
            var stripped = m[2..].Trim();
            return $"{m} {unspaced} {stripped}";
        }

        return $"{m} {unspaced} UK {m} UK{unspaced}";
    }

    private static SearchableHoldingField ParseSearchableFields(IReadOnlyList<string>? fields)
    {
        if (fields is null || fields.Count == 0)
            return SearchableHoldingField.All;

        var result = SearchableHoldingField.None;
        foreach (var raw in fields)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new ArgumentException("Searchable fields configuration cannot contain empty values.", nameof(fields));

            var key = raw.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
            result |= key switch
            {
                "cph" or "cphnumber" => SearchableHoldingField.Cph,
                "holdingname" or "name" => SearchableHoldingField.HoldingName,
                "businessname" or "organisationname" or "business" or "associatedbusinessname" or "associatedbusinessorganisationname" or "associatedbusiness" => SearchableHoldingField.BusinessName,
                "addressline1" or "holdingaddressline1" => SearchableHoldingField.AddressLine1,
                "addressline2" or "holdingaddressline2" => SearchableHoldingField.AddressLine2,
                "address" or "holdingaddress" or "holdingaddressline1andaddressline2" => SearchableHoldingField.AddressLine1 | SearchableHoldingField.AddressLine2,
                "town" => SearchableHoldingField.Town,
                "locality" => SearchableHoldingField.Locality,
                "townandlocality" => SearchableHoldingField.Town | SearchableHoldingField.Locality,
                "postcode" => SearchableHoldingField.Postcode,
                "herdmark" or "herd" or "herds" => SearchableHoldingField.Herdmark,
                _ => throw new ArgumentException($"Invalid searchable field '{raw}'.", nameof(fields))
            };
        }

        return result;
    }
}