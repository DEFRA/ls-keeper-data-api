using KeeperData.Core.Storage.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace KeeperData.Infrastructure.Storage.Sources;

/// <summary>
/// Provides self-contained SQLite seed databases for local development and testing
/// when an external data bridge service is unavailable.
/// </summary>
[ExcludeFromCodeCoverage]
public class FakeSqliteArtifactSource : ISqliteArtifactSource
{
    private static readonly Action<ILogger, string, Exception?> s_logCphArtifact = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(1, nameof(GetLatestAsync)),
        "FakeSqliteArtifactSource returning fake CPH artifact {FileName}");
    private static readonly Action<ILogger, string, Exception?> s_logReadModelArtifact = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(2, nameof(GetLatestAsync)),
        "FakeSqliteArtifactSource returning fake ReadModel artifact {FileName}");
    private static readonly Action<ILogger, string, Exception?> s_logCphSeeded = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(3, nameof(DownloadAsync)),
        "FakeSqliteArtifactSource seeded CPH database to {LocalPath}");
    private static readonly Action<ILogger, string, Exception?> s_logReadModelSeeded = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(4, nameof(DownloadAsync)),
        "FakeSqliteArtifactSource seeded ReadModel database to {LocalPath}");

    private readonly ILogger<FakeSqliteArtifactSource> _logger;
    private readonly string _cphsFileName;
    private readonly string _readModelFileName;

    public FakeSqliteArtifactSource(ILogger<FakeSqliteArtifactSource> logger)
    {
        _logger = logger;
        var now = DateTime.UtcNow;
        _cphsFileName = $"cphs_{now:yyyyMMdd}T120000Z.sqlite";
        _readModelFileName = $"krds-db_{now:yyyyMMdd}120000.sqlite";
    }

    public Task<SqliteArtifact?> GetLatestAsync(string latestArtifactRoute, CancellationToken cancellationToken)
    {
        if (latestArtifactRoute.Contains("cphs", StringComparison.OrdinalIgnoreCase))
        {
            s_logCphArtifact(_logger, _cphsFileName, null);
            return Task.FromResult<SqliteArtifact?>(new SqliteArtifact
            {
                ObjectKey = _cphsFileName,
                DownloadUrl = $"fake://sqlite/{_cphsFileName}",
                Size = 4096,
                LastModified = DateTimeOffset.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
        }

        if (latestArtifactRoute.Contains("staging", StringComparison.OrdinalIgnoreCase) ||
            latestArtifactRoute.Contains("readmodel", StringComparison.OrdinalIgnoreCase) ||
            latestArtifactRoute.Contains("krds", StringComparison.OrdinalIgnoreCase))
        {
            s_logReadModelArtifact(_logger, _readModelFileName, null);
            return Task.FromResult<SqliteArtifact?>(new SqliteArtifact
            {
                ObjectKey = _readModelFileName,
                DownloadUrl = $"fake://sqlite/{_readModelFileName}",
                Size = 16384,
                LastModified = DateTimeOffset.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
        }

        _logger.LogWarning("FakeSqliteArtifactSource holds no fake artifact for route {Route}", latestArtifactRoute);
        return Task.FromResult<SqliteArtifact?>(null);
    }

    public async Task DownloadAsync(SqliteArtifact artifact, string localPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(localPath))
        {
            File.Delete(localPath);
        }

        if (artifact.FileName.StartsWith("cphs_", StringComparison.OrdinalIgnoreCase))
        {
            await SeedCphsDatabaseAsync(localPath, cancellationToken);
            s_logCphSeeded(_logger, localPath, null);
            return;
        }

        if (artifact.FileName.StartsWith("krds-db_", StringComparison.OrdinalIgnoreCase))
        {
            await SeedReadModelDatabaseAsync(localPath, cancellationToken);
            s_logReadModelSeeded(_logger, localPath, null);
            return;
        }

        throw new InvalidOperationException($"Unsupported artifact file pattern: {artifact.FileName}");
    }

    private static async Task SeedCphsDatabaseAsync(string localPath, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection($"Data Source={localPath}");
        await connection.OpenAsync(cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS cphs (
                    cph TEXT PRIMARY KEY
                );
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken))
        {
            foreach (var cph in SeedHoldings.Select(h => h.Cph))
            {
                await using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = "INSERT INTO cphs (cph) VALUES ($cph);";
                insertCmd.Parameters.Add(new SqliteParameter("$cph", cph));
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        await connection.CloseAsync();
        SqliteConnection.ClearPool(connection);
    }

    private static async Task SeedReadModelDatabaseAsync(string localPath, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection($"Data Source={localPath}");
        await connection.OpenAsync(cancellationToken);

        await using (var schemaCmd = connection.CreateCommand())
        {
            schemaCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Holding (
                    Id TEXT PRIMARY KEY,
                    Cph TEXT NOT NULL UNIQUE,
                    FeatureName TEXT,
                    CphType TEXT,
                    StartDate INTEGER,
                    EndDate INTEGER,
                    Udprn TEXT,
                    PaonDescription TEXT,
                    PaonStartNumber TEXT,
                    PaonStartNumberSuffix TEXT,
                    PaonEndNumber TEXT,
                    PaonEndNumberSuffix TEXT,
                    Street TEXT,
                    Town TEXT,
                    Locality TEXT,
                    Postcode TEXT,
                    UkInternalCode TEXT,
                    Easting TEXT,
                    Northing TEXT,
                    OsMapReference TEXT
                );

                CREATE TABLE IF NOT EXISTS Party (
                    Id TEXT PRIMARY KEY,
                    SourcePartyId TEXT NOT NULL UNIQUE,
                    PersonTitle TEXT,
                    GivenName TEXT,
                    Initials TEXT,
                    FamilyName TEXT,
                    OrganisationName TEXT,
                    Email TEXT,
                    Mobile TEXT,
                    Telephone TEXT
                );

                CREATE TABLE IF NOT EXISTS Herd (
                    Id TEXT PRIMARY KEY,
                    HoldingId TEXT NOT NULL,
                    Herdmark TEXT NOT NULL,
                    AnimalGroupFromDate INTEGER,
                    AnimalGroupToDate INTEGER,
                    AnimalSpeciesCode TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS PartyRole (
                    Id TEXT PRIMARY KEY,
                    PartyId TEXT NOT NULL,
                    HoldingId TEXT NOT NULL,
                    HerdId TEXT,
                    Role TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS HoldingAnimalProfile (
                    Id TEXT PRIMARY KEY,
                    HoldingId TEXT NOT NULL,
                    AnimalSpeciesCode TEXT NOT NULL
                );
                """;
            await schemaCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken))
        {
            foreach (var h in SeedHoldings)
            {
                // 1. Insert Holding
                await using var holdingCmd = connection.CreateCommand();
                holdingCmd.Transaction = transaction;
                holdingCmd.CommandText = """
                    INSERT INTO Holding (
                        Id, Cph, FeatureName, CphType, StartDate, EndDate, Udprn,
                        PaonDescription, PaonStartNumber, PaonStartNumberSuffix, PaonEndNumber, PaonEndNumberSuffix,
                        Street, Town, Locality, Postcode, UkInternalCode, Easting, Northing, OsMapReference
                    ) VALUES (
                        $Id, $Cph, $FeatureName, $CphType, $StartDate, $EndDate, $Udprn,
                        $PaonDescription, $PaonStartNumber, $PaonStartNumberSuffix, $PaonEndNumber, $PaonEndNumberSuffix,
                        $Street, $Town, $Locality, $Postcode, $UkInternalCode, $Easting, $Northing, $OsMapReference
                    );
                    """;
                holdingCmd.Parameters.Add(new SqliteParameter("$Id", h.HoldingId));
                holdingCmd.Parameters.Add(new SqliteParameter("$Cph", h.Cph));
                holdingCmd.Parameters.Add(new SqliteParameter("$FeatureName", (object?)h.FeatureName ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$CphType", (object?)h.CphType ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$StartDate", (object?)h.StartDate ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$EndDate", (object?)h.EndDate ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$Udprn", (object?)h.Udprn ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$PaonDescription", (object?)h.PaonDescription ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$PaonStartNumber", (object?)h.PaonStartNumber ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$PaonStartNumberSuffix", (object?)h.PaonStartNumberSuffix ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$PaonEndNumber", (object?)h.PaonEndNumber ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$PaonEndNumberSuffix", (object?)h.PaonEndNumberSuffix ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$Street", (object?)h.Street ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$Town", (object?)h.Town ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$Locality", (object?)h.Locality ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$Postcode", (object?)h.Postcode ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$UkInternalCode", (object?)h.UkInternalCode ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$Easting", (object?)h.Easting ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$Northing", (object?)h.Northing ?? DBNull.Value));
                holdingCmd.Parameters.Add(new SqliteParameter("$OsMapReference", (object?)h.OsMapReference ?? DBNull.Value));
                await holdingCmd.ExecuteNonQueryAsync(cancellationToken);

                // 2. Insert Party
                var p = h.Party;
                await using var partyCmd = connection.CreateCommand();
                partyCmd.Transaction = transaction;
                partyCmd.CommandText = """
                    INSERT INTO Party (
                        Id, SourcePartyId, PersonTitle, GivenName, Initials, FamilyName, OrganisationName, Email, Mobile, Telephone
                    ) VALUES (
                        $Id, $SourcePartyId, $PersonTitle, $GivenName, $Initials, $FamilyName, $OrganisationName, $Email, $Mobile, $Telephone
                    );
                    """;
                partyCmd.Parameters.Add(new SqliteParameter("$Id", p.PartyId));
                partyCmd.Parameters.Add(new SqliteParameter("$SourcePartyId", p.SourcePartyId));
                partyCmd.Parameters.Add(new SqliteParameter("$PersonTitle", (object?)p.PersonTitle ?? DBNull.Value));
                partyCmd.Parameters.Add(new SqliteParameter("$GivenName", (object?)p.GivenName ?? DBNull.Value));
                partyCmd.Parameters.Add(new SqliteParameter("$Initials", (object?)p.Initials ?? DBNull.Value));
                partyCmd.Parameters.Add(new SqliteParameter("$FamilyName", (object?)p.FamilyName ?? DBNull.Value));
                partyCmd.Parameters.Add(new SqliteParameter("$OrganisationName", (object?)p.OrganisationName ?? DBNull.Value));
                partyCmd.Parameters.Add(new SqliteParameter("$Email", (object?)p.Email ?? DBNull.Value));
                partyCmd.Parameters.Add(new SqliteParameter("$Mobile", (object?)p.Mobile ?? DBNull.Value));
                partyCmd.Parameters.Add(new SqliteParameter("$Telephone", (object?)p.Telephone ?? DBNull.Value));
                await partyCmd.ExecuteNonQueryAsync(cancellationToken);

                // 3. Insert Herds
                foreach (var herd in h.Herds)
                {
                    await using var herdCmd = connection.CreateCommand();
                    herdCmd.Transaction = transaction;
                    herdCmd.CommandText = """
                        INSERT INTO Herd (Id, HoldingId, Herdmark, AnimalGroupFromDate, AnimalGroupToDate, AnimalSpeciesCode)
                        VALUES ($Id, $HoldingId, $Herdmark, $AnimalGroupFromDate, $AnimalGroupToDate, $AnimalSpeciesCode);
                        """;
                    herdCmd.Parameters.Add(new SqliteParameter("$Id", herd.HerdId));
                    herdCmd.Parameters.Add(new SqliteParameter("$HoldingId", h.HoldingId));
                    herdCmd.Parameters.Add(new SqliteParameter("$Herdmark", herd.Herdmark));
                    herdCmd.Parameters.Add(new SqliteParameter("$AnimalGroupFromDate", (object?)herd.FromDate ?? DBNull.Value));
                    herdCmd.Parameters.Add(new SqliteParameter("$AnimalGroupToDate", (object?)herd.ToDate ?? DBNull.Value));
                    herdCmd.Parameters.Add(new SqliteParameter("$AnimalSpeciesCode", herd.Species));
                    await herdCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                // 4. Insert Party Roles
                foreach (var role in h.Roles)
                {
                    await using var roleCmd = connection.CreateCommand();
                    roleCmd.Transaction = transaction;
                    roleCmd.CommandText = """
                        INSERT INTO PartyRole (Id, PartyId, HoldingId, HerdId, Role)
                        VALUES ($Id, $PartyId, $HoldingId, $HerdId, $Role);
                        """;
                    roleCmd.Parameters.Add(new SqliteParameter("$Id", role.RoleId));
                    roleCmd.Parameters.Add(new SqliteParameter("$PartyId", p.PartyId));
                    roleCmd.Parameters.Add(new SqliteParameter("$HoldingId", h.HoldingId));
                    roleCmd.Parameters.Add(new SqliteParameter("$HerdId", (object?)role.HerdId ?? DBNull.Value));
                    roleCmd.Parameters.Add(new SqliteParameter("$Role", role.Role));
                    await roleCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                // 5. Insert Holding Animal Profiles
                foreach (var species in h.AllowedSpecies)
                {
                    await using var speciesCmd = connection.CreateCommand();
                    speciesCmd.Transaction = transaction;
                    speciesCmd.CommandText = """
                        INSERT INTO HoldingAnimalProfile (Id, HoldingId, AnimalSpeciesCode)
                        VALUES ($Id, $HoldingId, $AnimalSpeciesCode);
                        """;
                    speciesCmd.Parameters.Add(new SqliteParameter("$Id", $"hap-{h.HoldingId}-{species}"));
                    speciesCmd.Parameters.Add(new SqliteParameter("$HoldingId", h.HoldingId));
                    speciesCmd.Parameters.Add(new SqliteParameter("$AnimalSpeciesCode", species));
                    await speciesCmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }

        await connection.CloseAsync();
        SqliteConnection.ClearPool(connection);
    }

    private sealed record SeedHoldingRecord(
        string HoldingId,
        string Cph,
        string? FeatureName,
        string? CphType,
        long? StartDate,
        long? EndDate,
        string? Udprn,
        string? PaonDescription,
        string? PaonStartNumber,
        string? PaonStartNumberSuffix,
        string? PaonEndNumber,
        string? PaonEndNumberSuffix,
        string? Street,
        string? Town,
        string? Locality,
        string? Postcode,
        string? UkInternalCode,
        string? Easting,
        string? Northing,
        string? OsMapReference,
        SeedPartyRecord Party,
        IReadOnlyList<SeedHerdRecord> Herds,
        IReadOnlyList<SeedRoleRecord> Roles,
        IReadOnlyList<string> AllowedSpecies);

    private sealed record SeedPartyRecord(
        string PartyId,
        string SourcePartyId,
        string? PersonTitle,
        string? GivenName,
        string? Initials,
        string? FamilyName,
        string? OrganisationName,
        string? Email,
        string? Mobile,
        string? Telephone);

    private sealed record SeedHerdRecord(
        string HerdId,
        string Herdmark,
        long? FromDate,
        long? ToDate,
        string Species);

    private sealed record SeedRoleRecord(
        string RoleId,
        string? HerdId,
        string Role);

    private static readonly IReadOnlyList<SeedHoldingRecord> SeedHoldings = LoadSeedHoldings();

    private static IReadOnlyList<SeedHoldingRecord> LoadSeedHoldings()
    {
        using var stream = typeof(FakeSqliteArtifactSource).Assembly
            .GetManifestResourceStream("FakeSqliteHoldings.json")
            ?? throw new InvalidOperationException("Fake SQLite holding seed data is missing.");

        return JsonSerializer.Deserialize<List<SeedHoldingRecord>>(stream)
            ?? throw new InvalidOperationException("Fake SQLite holding seed data is invalid.");
    }
}
