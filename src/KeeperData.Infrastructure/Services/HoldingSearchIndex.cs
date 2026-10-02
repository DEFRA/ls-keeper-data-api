using Microsoft.Data.Sqlite;

namespace KeeperData.Infrastructure.Services;

/// <summary>Builds an FTS5 index beside a validated, immutable read model snapshot.</summary>
internal static class HoldingSearchIndex
{
    internal static async Task<(string Path, long DocumentCount)> BuildAsync(string dbPath, CancellationToken cancellationToken)
    {
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
            create.CommandText = "CREATE VIRTUAL TABLE HoldingSearch USING fts5(HoldingId UNINDEXED, SearchText, tokenize='unicode61');";
            await create.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var populate = connection.CreateCommand())
        {
            populate.CommandText = """
                INSERT INTO HoldingSearch (HoldingId, SearchText)
                WITH distinct_roles AS (
                    SELECT DISTINCT r.HoldingId, r.PartyId
                    FROM source.PartyRole AS r
                ),
                party_text AS (
                    SELECT d.HoldingId, group_concat(
                        coalesce(p.SourcePartyId, '') || ' ' || coalesce(p.OrganisationName, '') || ' ' ||
                        coalesce(p.PersonTitle, '') || ' ' || coalesce(p.GivenName, '') || ' ' ||
                        coalesce(p.Initials, '') || ' ' || coalesce(p.FamilyName, '') || ' ' ||
                        coalesce(p.Email, '') || ' ' || coalesce(p.Mobile, '') || ' ' ||
                        replace(replace(replace(replace(replace(coalesce(p.Mobile, ''), ' ', ''), '-', ''), '+', ''), '(', ''), ')', '') || ' ' ||
                        coalesce(p.Telephone, '') || ' ' ||
                        replace(replace(replace(replace(replace(coalesce(p.Telephone, ''), ' ', ''), '-', ''), '+', ''), '(', ''), ')', ''), ' ') AS SearchText
                    FROM distinct_roles AS d JOIN source.Party AS p ON p.Id = d.PartyId
                    GROUP BY d.HoldingId
                )
                SELECT h.Id,
                    coalesce(h.Cph, '') || ' ' || replace(coalesce(h.Cph, ''), '/', '') || ' ' ||
                    coalesce(h.FeatureName, '') || ' ' || coalesce(h.CphType, '') || ' ' ||
                    coalesce(h.Udprn, '') || ' ' ||
                    coalesce(h.PaonDescription, '') || ' ' || coalesce(h.PaonStartNumber, '') || ' ' ||
                    coalesce(h.PaonStartNumberSuffix, '') || ' ' ||
                    coalesce(h.PaonStartNumber, '') || coalesce(h.PaonStartNumberSuffix, '') || ' ' ||
                    coalesce(h.PaonEndNumber, '') || ' ' || coalesce(h.PaonEndNumberSuffix, '') || ' ' ||
                    coalesce(h.PaonEndNumber, '') || coalesce(h.PaonEndNumberSuffix, '') || ' ' ||
                    coalesce(h.Street, '') || ' ' ||
                    coalesce(h.Locality, '') || ' ' || coalesce(h.Town, '') || ' ' ||
                    coalesce(h.Postcode, '') || ' ' || replace(coalesce(h.Postcode, ''), ' ', '') || ' ' ||
                    coalesce(h.OsMapReference, '') || ' ' || coalesce(party_text.SearchText, '')
                FROM source.Holding AS h LEFT JOIN party_text ON party_text.HoldingId = h.Id;
                """;
            await populate.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var optimize = connection.CreateCommand())
        {
            optimize.CommandText = "INSERT INTO HoldingSearch(HoldingSearch) VALUES('optimize');";
            await optimize.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var count = connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM HoldingSearch";
        var documentCount = (long)(await count.ExecuteScalarAsync(cancellationToken))!;
        await connection.CloseAsync();
        return (indexPath, documentCount);
    }
}