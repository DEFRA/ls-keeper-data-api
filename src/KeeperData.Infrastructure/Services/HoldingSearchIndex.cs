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
        if (File.Exists(indexPath))
            File.Delete(indexPath);

        await using var connection = new SqliteConnection($"Data Source={indexPath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = """
                PRAGMA synchronous = OFF;
                PRAGMA journal_mode = MEMORY;
                PRAGMA temp_store = MEMORY;
                PRAGMA cache_size = -64000;
                """;
            await pragma.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var attach = connection.CreateCommand())
        {
            attach.CommandText = "ATTACH DATABASE $source AS source";
            attach.Parameters.AddWithValue("$source", dbPath);
            await attach.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var create = connection.CreateCommand())
        {
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

        var availableTables = await GetAvailableSourceTablesAsync(connection, cancellationToken);
        var hasPartyRoles = availableTables.Contains("PartyRole") && availableTables.Contains("Party");
        var hasHerds = availableTables.Contains("Herd");

        var selectSql = BuildSourceSelectSql(hasPartyRoles, hasHerds);

        await using (var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken))
        {
            await using var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = transaction;
            insertCmd.CommandText = """
                INSERT INTO HoldingSearch (
                    HoldingId, Cph, HoldingName, BusinessName, AddressLine1, AddressLine2, Town, Locality, Postcode, Herdmark
                ) VALUES (
                    $holdingId, $cph, $name, $business, $line1, $line2, $town, $locality, $postcode, $herdmark
                );
                """;

            var pId = insertCmd.Parameters.Add("$holdingId", SqliteType.Text);
            var pCph = insertCmd.Parameters.Add("$cph", SqliteType.Text);
            var pName = insertCmd.Parameters.Add("$name", SqliteType.Text);
            var pBusiness = insertCmd.Parameters.Add("$business", SqliteType.Text);
            var pLine1 = insertCmd.Parameters.Add("$line1", SqliteType.Text);
            var pLine2 = insertCmd.Parameters.Add("$line2", SqliteType.Text);
            var pTown = insertCmd.Parameters.Add("$town", SqliteType.Text);
            var pLocality = insertCmd.Parameters.Add("$locality", SqliteType.Text);
            var pPostcode = insertCmd.Parameters.Add("$postcode", SqliteType.Text);
            var pHerdmark = insertCmd.Parameters.Add("$herdmark", SqliteType.Text);

            await using (var selectCmd = connection.CreateCommand())
            {
                selectCmd.Transaction = transaction;
                selectCmd.CommandText = selectSql;
                await using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetString(0);
                    var cph = reader.IsDBNull(1) ? null : reader.GetString(1);
                    var featureName = reader.IsDBNull(2) ? null : reader.GetString(2);

                    var saonDesc = reader.IsDBNull(3) ? null : reader.GetString(3);
                    var saonStartNum = reader.IsDBNull(4) ? null : reader.GetString(4);
                    var saonStartSuffix = reader.IsDBNull(5) ? null : reader.GetString(5);
                    var saonEndNum = reader.IsDBNull(6) ? null : reader.GetString(6);
                    var saonEndSuffix = reader.IsDBNull(7) ? null : reader.GetString(7);

                    var paonDesc = reader.IsDBNull(8) ? null : reader.GetString(8);
                    var paonStartNum = reader.IsDBNull(9) ? null : reader.GetString(9);
                    var paonStartSuffix = reader.IsDBNull(10) ? null : reader.GetString(10);
                    var paonEndNum = reader.IsDBNull(11) ? null : reader.GetString(11);
                    var paonEndSuffix = reader.IsDBNull(12) ? null : reader.GetString(12);

                    var street = reader.IsDBNull(13) ? null : reader.GetString(13);
                    var town = reader.IsDBNull(14) ? null : reader.GetString(14);
                    var locality = reader.IsDBNull(15) ? null : reader.GetString(15);
                    var postcode = reader.IsDBNull(16) ? null : reader.GetString(16);
                    var businessNames = reader.IsDBNull(17) ? null : reader.GetString(17);
                    var herdmarks = reader.IsDBNull(18) ? null : reader.GetString(18);

                    var (line1, line2) = HoldingDetailRepository.AssembleAddressLines(
                        saonDesc, saonStartNum, saonStartSuffix, saonEndNum, saonEndSuffix,
                        paonDesc, paonStartNum, paonStartSuffix, paonEndNum, paonEndSuffix,
                        street);

                    var businessList = (businessNames ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var herdList = (herdmarks ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    pId.Value = id;

                    pCph.Value = (enabledFields.HasFlag(SearchableHoldingField.Cph) && !string.IsNullOrEmpty(cph))
                        ? FormatCphTokens(cph)
                        : "";

                    pName.Value = (enabledFields.HasFlag(SearchableHoldingField.HoldingName) && !string.IsNullOrEmpty(featureName))
                        ? featureName
                        : "";

                    pBusiness.Value = (enabledFields.HasFlag(SearchableHoldingField.BusinessName) && businessList.Length > 0)
                        ? businessList[0]
                        : "";

                    pLine1.Value = (enabledFields.HasFlag(SearchableHoldingField.AddressLine1) && !string.IsNullOrEmpty(line1))
                        ? line1
                        : "";

                    pLine2.Value = (enabledFields.HasFlag(SearchableHoldingField.AddressLine2) && !string.IsNullOrEmpty(line2))
                        ? line2
                        : "";

                    pTown.Value = (enabledFields.HasFlag(SearchableHoldingField.Town) && !string.IsNullOrEmpty(town))
                        ? town
                        : "";

                    pLocality.Value = (enabledFields.HasFlag(SearchableHoldingField.Locality) && !string.IsNullOrEmpty(locality))
                        ? locality
                        : "";

                    pPostcode.Value = (enabledFields.HasFlag(SearchableHoldingField.Postcode) && !string.IsNullOrEmpty(postcode))
                        ? FormatPostcodeTokens(postcode)
                        : "";

                    pHerdmark.Value = (enabledFields.HasFlag(SearchableHoldingField.Herdmark) && herdList.Length > 0)
                        ? FormatHerdmarkToken(herdList[0])
                        : "";

                    await insertCmd.ExecuteNonQueryAsync(cancellationToken);

                    void ClearParameters()
                    {
                        pCph.Value = "";
                        pName.Value = "";
                        pBusiness.Value = "";
                        pLine1.Value = "";
                        pLine2.Value = "";
                        pTown.Value = "";
                        pLocality.Value = "";
                        pPostcode.Value = "";
                        pHerdmark.Value = "";
                    }

                    if (enabledFields.HasFlag(SearchableHoldingField.BusinessName) && businessList.Length > 1)
                    {
                        for (var i = 1; i < businessList.Length; i++)
                        {
                            ClearParameters();
                            pBusiness.Value = businessList[i];
                            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }

                    if (enabledFields.HasFlag(SearchableHoldingField.Herdmark) && herdList.Length > 1)
                    {
                        for (var i = 1; i < herdList.Length; i++)
                        {
                            ClearParameters();
                            pHerdmark.Value = FormatHerdmarkToken(herdList[i]);
                            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }

                    var line1AliasPairs = string.IsNullOrEmpty(line2)
                        ? new[] { (saonStartNum, saonStartSuffix), (saonEndNum, saonEndSuffix), (paonStartNum, paonStartSuffix), (paonEndNum, paonEndSuffix) }
                        : new[] { (saonStartNum, saonStartSuffix), (saonEndNum, saonEndSuffix) };

                    if (enabledFields.HasFlag(SearchableHoldingField.AddressLine1))
                    {
                        var line1Aliases = ExtractDistinctAliases(line1, line1AliasPairs);
                        foreach (var alias in line1Aliases)
                        {
                            ClearParameters();
                            pLine1.Value = alias;
                            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }

                    if (enabledFields.HasFlag(SearchableHoldingField.AddressLine2) && !string.IsNullOrEmpty(line2))
                    {
                        var line2AliasPairs = new[] { (paonStartNum, paonStartSuffix), (paonEndNum, paonEndSuffix) };
                        var line2Aliases = ExtractDistinctAliases(line2, line2AliasPairs);
                        foreach (var alias in line2Aliases)
                        {
                            ClearParameters();
                            pLine2.Value = alias;
                            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }

        await using (var optimize = connection.CreateCommand())
        {
            optimize.CommandText = "INSERT INTO HoldingSearch(HoldingSearch) VALUES('optimize');";
            await optimize.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var count = connection.CreateCommand();
        count.CommandText = "SELECT COUNT(DISTINCT HoldingId) FROM HoldingSearch";
        var documentCount = (long)(await count.ExecuteScalarAsync(cancellationToken))!;
        await connection.CloseAsync();
        return (indexPath, documentCount);
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