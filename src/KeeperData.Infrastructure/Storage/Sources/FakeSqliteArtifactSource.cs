using KeeperData.Core.Storage.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace KeeperData.Infrastructure.Storage.Sources;

/// <summary>
/// Provides self-contained SQLite seed databases for local development and testing
/// when an external data bridge service is unavailable.
/// </summary>
[ExcludeFromCodeCoverage]
public class FakeSqliteArtifactSource : ISqliteArtifactSource
{
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
            _logger.LogInformation("FakeSqliteArtifactSource returning fake CPH artifact {FileName}", _cphsFileName);
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
            _logger.LogInformation("FakeSqliteArtifactSource returning fake ReadModel artifact {FileName}", _readModelFileName);
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
            _logger.LogInformation("FakeSqliteArtifactSource seeded CPH database to {LocalPath}", localPath);
            return;
        }

        if (artifact.FileName.StartsWith("krds-db_", StringComparison.OrdinalIgnoreCase))
        {
            await SeedReadModelDatabaseAsync(localPath, cancellationToken);
            _logger.LogInformation("FakeSqliteArtifactSource seeded ReadModel database to {LocalPath}", localPath);
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

        await using (var transaction = connection.BeginTransaction())
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

        await using (var transaction = connection.BeginTransaction())
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

    private static readonly IReadOnlyList<SeedHoldingRecord> SeedHoldings =
    [
        new(
            HoldingId: "holding-10-024-0247",
            Cph: "10/024/0247",
            FeatureName: null,
            CphType: "permanent",
            StartDate: 1310515200,
            EndDate: null,
            Udprn: null,
            PaonDescription: null,
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "The Street",
            Town: "WORCESTER",
            Locality: "Some Location",
            Postcode: "TT5 2UU",
            UkInternalCode: "England",
            Easting: "000000",
            Northing: "000000",
            OsMapReference: "SS0000000200",
            Party: new(
                PartyId: "party-10-024-0247",
                SourcePartyId: "C000000",
                PersonTitle: "MISS",
                GivenName: null,
                Initials: "J",
                FamilyName: "Example",
                OrganisationName: null,
                Email: null,
                Mobile: null,
                Telephone: "01234 567890"),
            Herds:
            [
                new("herd-10-024-0247-1", "360396", 1216166400, null, "CTT"),
                new("herd-10-024-0247-2", "372893", 1216166400, null, "SHP"),
                new("herd-10-024-0247-3", "373074", 1216166400, null, "SHP")
            ],
            Roles:
            [
                new("pr-10-024-0247-1", null, "holder"),
                new("pr-10-024-0247-2", "herd-10-024-0247-1", "keeper"),
                new("pr-10-024-0247-3", "herd-10-024-0247-2", "keeper"),
                new("pr-10-024-0247-4", "herd-10-024-0247-1", "owner"),
                new("pr-10-024-0247-5", "herd-10-024-0247-2", "owner")
            ],
            AllowedSpecies: ["CTT", "SHP"]),

        new(
            HoldingId: "holding-13-169-0007",
            Cph: "13/169/0007",
            FeatureName: "Land At Test Farm 06",
            CphType: "permanent",
            StartDate: 1773014400,
            EndDate: null,
            Udprn: null,
            PaonDescription: "Test Farm 06",
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Layer Road",
            Town: "COLCHESTER",
            Locality: "Great Wigborough",
            Postcode: "CO5 7RR",
            UkInternalCode: "England",
            Easting: "595600",
            Northing: "215900",
            OsMapReference: "TL9560015900",
            Party: new(
                PartyId: "party-13-169-0007",
                SourcePartyId: "C131690007",
                PersonTitle: null,
                GivenName: null,
                Initials: null,
                FamilyName: null,
                OrganisationName: "Green Fields Farming Ltd",
                Email: "contact@greenfields.co.uk",
                Mobile: null,
                Telephone: "01206 123456"),
            Herds:
            [
                new("herd-13-169-0007-1", "131690", 1609459200, null, "CTT"),
                new("herd-13-169-0007-2", "131691", 1609459200, null, "SHP")
            ],
            Roles:
            [
                new("pr-13-169-0007-1", null, "holder"),
                new("pr-13-169-0007-2", "herd-13-169-0007-1", "keeper"),
                new("pr-13-169-0007-3", "herd-13-169-0007-2", "keeper")
            ],
            AllowedSpecies: ["CTT", "SHP"]),

        new(
            HoldingId: "holding-15-001-0001",
            Cph: "15/001/0001",
            FeatureName: "Meadow View Holding",
            CphType: "permanent",
            StartDate: 1609459200,
            EndDate: null,
            Udprn: "10001234",
            PaonDescription: "Meadow View",
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "High Street",
            Town: "CHELMSFORD",
            Locality: "Broomfield",
            Postcode: "CM1 7XX",
            UkInternalCode: "England",
            Easting: "570000",
            Northing: "207000",
            OsMapReference: "TL7000007000",
            Party: new(
                PartyId: "party-15-001-0001",
                SourcePartyId: "C150010001",
                PersonTitle: "Mr",
                GivenName: "Arthur",
                Initials: "A",
                FamilyName: "Dent",
                OrganisationName: null,
                Email: "arthur.dent@example.com",
                Mobile: "07700 900123",
                Telephone: null),
            Herds:
            [
                new("herd-15-001-0001-1", "150011", 1609459200, null, "CTT")
            ],
            Roles:
            [
                new("pr-15-001-0001-1", null, "holder"),
                new("pr-15-001-0001-2", "herd-15-001-0001-1", "keeper")
            ],
            AllowedSpecies: ["CTT"]),

        new(
            HoldingId: "holding-22-100-0001",
            Cph: "22/100/0001",
            FeatureName: "Oak Ridge Farm",
            CphType: "permanent",
            StartDate: 1609459200,
            EndDate: null,
            Udprn: null,
            PaonDescription: "Oak Ridge Farmhouse",
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Valley Road",
            Town: "NORWICH",
            Locality: "Cringleford",
            Postcode: "NR4 7AA",
            UkInternalCode: "England",
            Easting: "618000",
            Northing: "305000",
            OsMapReference: "TG1800005000",
            Party: new(
                PartyId: "party-22-100-0001",
                SourcePartyId: "C221000001",
                PersonTitle: null,
                GivenName: null,
                Initials: null,
                FamilyName: null,
                OrganisationName: "Oak Ridge Livestock Co",
                Email: "info@oakridgelf.co.uk",
                Mobile: null,
                Telephone: "01603 555001"),
            Herds:
            [
                new("herd-22-100-0001-1", "221001", 1609459200, null, "CTT"),
                new("herd-22-100-0001-2", "221002", 1609459200, null, "PIG")
            ],
            Roles:
            [
                new("pr-22-100-0001-1", null, "holder"),
                new("pr-22-100-0001-2", "herd-22-100-0001-1", "keeper"),
                new("pr-22-100-0001-3", "herd-22-100-0001-2", "keeper")
            ],
            AllowedSpecies: ["CTT", "PIG"]),

        new(
            HoldingId: "holding-22-100-0002",
            Cph: "22/100/0002",
            FeatureName: "Willow Brook Pastures",
            CphType: "permanent",
            StartDate: 1609459200,
            EndDate: null,
            Udprn: null,
            PaonDescription: "Willow Brook",
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Mill Lane",
            Town: "IPSWICH",
            Locality: "Kesgrave",
            Postcode: "IP1 1BB",
            UkInternalCode: "England",
            Easting: "622000",
            Northing: "245000",
            OsMapReference: "TM2200045000",
            Party: new(
                PartyId: "party-22-100-0002",
                SourcePartyId: "C221000002",
                PersonTitle: "Mrs",
                GivenName: "Sarah",
                Initials: "S",
                FamilyName: "Jenkins",
                OrganisationName: null,
                Email: "sarah@willowbrook.co.uk",
                Mobile: null,
                Telephone: "01473 555002"),
            Herds:
            [
                new("herd-22-100-0002-1", "221003", 1609459200, null, "SHP")
            ],
            Roles:
            [
                new("pr-22-100-0002-1", null, "holder"),
                new("pr-22-100-0002-2", "herd-22-100-0002-1", "keeper")
            ],
            AllowedSpecies: ["SHP"]),

        new(
            HoldingId: "holding-22-100-0003",
            Cph: "22/100/0003",
            FeatureName: "Hilltop Sheep Station",
            CphType: "permanent",
            StartDate: 1609459200,
            EndDate: null,
            Udprn: null,
            PaonDescription: null,
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Church Road",
            Town: "BURY ST EDMUNDS",
            Locality: "Rougham",
            Postcode: "IP33 3CC",
            UkInternalCode: "England",
            Easting: "588000",
            Northing: "264000",
            OsMapReference: "TL8800064000",
            Party: new(
                PartyId: "party-22-100-0003",
                SourcePartyId: "C221000003",
                PersonTitle: "Mr",
                GivenName: "David",
                Initials: "D",
                FamilyName: "Miller",
                OrganisationName: null,
                Email: null,
                Mobile: "07700 900223",
                Telephone: null),
            Herds:
            [
                new("herd-22-100-0003-1", "221004", 1609459200, null, "SHP"),
                new("herd-22-100-0003-2", "221005", 1609459200, null, "CAP")
            ],
            Roles:
            [
                new("pr-22-100-0003-1", null, "holder"),
                new("pr-22-100-0003-2", "herd-22-100-0003-1", "keeper"),
                new("pr-22-100-0003-3", "herd-22-100-0003-2", "keeper")
            ],
            AllowedSpecies: ["CAP", "SHP"]),

        new(
            HoldingId: "holding-34-200-0010",
            Cph: "34/200/0010",
            FeatureName: "Sunnyside Farm",
            CphType: "permanent",
            StartDate: 1609459200,
            EndDate: null,
            Udprn: null,
            PaonDescription: "Sunnyside",
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Gloucester Road",
            Town: "GLOUCESTER",
            Locality: "Churchdown",
            Postcode: "GL1 2DD",
            UkInternalCode: "England",
            Easting: "386000",
            Northing: "219000",
            OsMapReference: "SO8600019000",
            Party: new(
                PartyId: "party-34-200-0010",
                SourcePartyId: "C342000010",
                PersonTitle: null,
                GivenName: null,
                Initials: null,
                FamilyName: null,
                OrganisationName: "Sunnyside Agriculture Ltd",
                Email: "admin@sunnyside.co.uk",
                Mobile: null,
                Telephone: "01452 555010"),
            Herds:
            [
                new("herd-34-200-0010-1", "342001", 1609459200, null, "CTT")
            ],
            Roles:
            [
                new("pr-34-200-0010-1", null, "holder"),
                new("pr-34-200-0010-2", "herd-34-200-0010-1", "keeper")
            ],
            AllowedSpecies: ["CTT"]),

        new(
            HoldingId: "holding-34-200-0011",
            Cph: "34/200/0011",
            FeatureName: "Brookfield Dairy Farm",
            CphType: "permanent",
            StartDate: 1609459200,
            EndDate: null,
            Udprn: null,
            PaonDescription: null,
            PaonStartNumber: "12",
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Brookfield Way",
            Town: "CHELTENHAM",
            Locality: "Prestbury",
            Postcode: "GL50 3EE",
            UkInternalCode: "England",
            Easting: "395000",
            Northing: "223000",
            OsMapReference: "SO9500023000",
            Party: new(
                PartyId: "party-34-200-0011",
                SourcePartyId: "C342000011",
                PersonTitle: "Mr",
                GivenName: "James",
                Initials: "J",
                FamilyName: "Wilson",
                OrganisationName: null,
                Email: "j.wilson@brookfielddairy.co.uk",
                Mobile: null,
                Telephone: null),
            Herds:
            [
                new("herd-34-200-0011-1", "342002", 1609459200, null, "CTT")
            ],
            Roles:
            [
                new("pr-34-200-0011-1", null, "holder"),
                new("pr-34-200-0011-2", "herd-34-200-0011-1", "keeper")
            ],
            AllowedSpecies: ["CTT"]),

        new(
            HoldingId: "holding-45-300-0100",
            Cph: "45/300/0100",
            FeatureName: "Redwood Estate",
            CphType: "permanent",
            StartDate: 1609459200,
            EndDate: null,
            Udprn: null,
            PaonDescription: "Redwood House",
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Estate Drive",
            Town: "EXETER",
            Locality: "Topsham",
            Postcode: "EX1 1FF",
            UkInternalCode: "England",
            Easting: "296000",
            Northing: "088000",
            OsMapReference: "SX9600088000",
            Party: new(
                PartyId: "party-45-300-0100",
                SourcePartyId: "C453000100",
                PersonTitle: null,
                GivenName: null,
                Initials: null,
                FamilyName: null,
                OrganisationName: "Redwood Farming Trust",
                Email: "trustees@redwoodestate.co.uk",
                Mobile: null,
                Telephone: "01392 555100"),
            Herds:
            [
                new("herd-45-300-0100-1", "453001", 1609459200, null, "CTT"),
                new("herd-45-300-0100-2", "453002", 1609459200, null, "SHP")
            ],
            Roles:
            [
                new("pr-45-300-0100-1", null, "holder"),
                new("pr-45-300-0100-2", "herd-45-300-0100-1", "keeper"),
                new("pr-45-300-0100-3", "herd-45-300-0100-2", "keeper")
            ],
            AllowedSpecies: ["CTT", "SHP"]),

        new(
            HoldingId: "holding-45-300-0101",
            Cph: "45/300/0101",
            FeatureName: "Dartmoor Grazing Lands",
            CphType: "temporary",
            StartDate: 1672531200,
            EndDate: 1735689600,
            Udprn: null,
            PaonDescription: null,
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Moorland Track",
            Town: "PLYMOUTH",
            Locality: "Yelverton",
            Postcode: "PL1 2GG",
            UkInternalCode: "England",
            Easting: "253000",
            Northing: "067000",
            OsMapReference: "SX5300067000",
            Party: new(
                PartyId: "party-45-300-0101",
                SourcePartyId: "C453000101",
                PersonTitle: "Ms",
                GivenName: "Emma",
                Initials: "E",
                FamilyName: "Watson",
                OrganisationName: null,
                Email: null,
                Mobile: "07700 900334",
                Telephone: null),
            Herds:
            [
                new("herd-45-300-0101-1", "453003", 1672531200, 1735689600, "SHP")
            ],
            Roles:
            [
                new("pr-45-300-0101-1", null, "holder"),
                new("pr-45-300-0101-2", "herd-45-300-0101-1", "keeper")
            ],
            AllowedSpecies: ["SHP"]),

        new(
            HoldingId: "holding-45-300-0102",
            Cph: "45/300/0102",
            FeatureName: "Highland View Farm",
            CphType: "permanent",
            StartDate: 1609459200,
            EndDate: null,
            Udprn: null,
            PaonDescription: "Highland View",
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Bridgwater Road",
            Town: "TAUNTON",
            Locality: "Monkton Heathfield",
            Postcode: "TA1 3HH",
            UkInternalCode: "England",
            Easting: "325000",
            Northing: "126000",
            OsMapReference: "ST2500026000",
            Party: new(
                PartyId: "party-45-300-0102",
                SourcePartyId: "C453000102",
                PersonTitle: null,
                GivenName: null,
                Initials: null,
                FamilyName: null,
                OrganisationName: "Highland Pastures Ltd",
                Email: "contact@highlandpastures.co.uk",
                Mobile: null,
                Telephone: null),
            Herds:
            [
                new("herd-45-300-0102-1", "453004", 1609459200, null, "CTT")
            ],
            Roles:
            [
                new("pr-45-300-0102-1", null, "holder"),
                new("pr-45-300-0102-2", "herd-45-300-0102-1", "keeper")
            ],
            AllowedSpecies: ["CTT"]),

        new(
            HoldingId: "holding-56-400-0001",
            Cph: "56/400/0001",
            FeatureName: "Cotswold Heritage Farm",
            CphType: "permanent",
            StartDate: 1609459200,
            EndDate: null,
            Udprn: null,
            PaonDescription: null,
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Fosse Way",
            Town: "CIRENCESTER",
            Locality: "North Cerney",
            Postcode: "GL7 1JJ",
            UkInternalCode: "England",
            Easting: "402000",
            Northing: "207000",
            OsMapReference: "SP0200007000",
            Party: new(
                PartyId: "party-56-400-0001",
                SourcePartyId: "C564000001",
                PersonTitle: "Mr",
                GivenName: "George",
                Initials: "G",
                FamilyName: "Baker",
                OrganisationName: null,
                Email: null,
                Mobile: null,
                Telephone: "01285 555001"),
            Herds:
            [
                new("herd-56-400-0001-1", "564001", 1609459200, null, "SHP"),
                new("herd-56-400-0001-2", "564002", 1609459200, null, "PIG")
            ],
            Roles:
            [
                new("pr-56-400-0001-1", null, "holder"),
                new("pr-56-400-0001-2", "herd-56-400-0001-1", "keeper"),
                new("pr-56-400-0001-3", "herd-56-400-0001-2", "keeper")
            ],
            AllowedSpecies: ["PIG", "SHP"]),

        new(
            HoldingId: "holding-67-500-0001",
            Cph: "67/500/0001",
            FeatureName: "Pennine Valley Rearing",
            CphType: "permanent",
            StartDate: 1609459200,
            EndDate: null,
            Udprn: null,
            PaonDescription: null,
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Otley Road",
            Town: "LEEDS",
            Locality: "Guiseley",
            Postcode: "LS1 4KK",
            UkInternalCode: "England",
            Easting: "419000",
            Northing: "442000",
            OsMapReference: "SE1900042000",
            Party: new(
                PartyId: "party-67-500-0001",
                SourcePartyId: "C675000001",
                PersonTitle: null,
                GivenName: null,
                Initials: null,
                FamilyName: null,
                OrganisationName: "Pennine Livestock Group",
                Email: "info@penninelivestock.co.uk",
                Mobile: null,
                Telephone: "0113 555001"),
            Herds:
            [
                new("herd-67-500-0001-1", "675001", 1609459200, null, "CTT"),
                new("herd-67-500-0001-2", "675002", 1609459200, null, "SHP")
            ],
            Roles:
            [
                new("pr-67-500-0001-1", null, "holder"),
                new("pr-67-500-0001-2", "herd-67-500-0001-1", "keeper"),
                new("pr-67-500-0001-3", "herd-67-500-0001-2", "keeper")
            ],
            AllowedSpecies: ["CTT", "SHP"]),

        new(
            HoldingId: "holding-78-600-0001",
            Cph: "78/600/0001",
            FeatureName: "Waveney Valley Grazing",
            CphType: "temporary",
            StartDate: 1672531200,
            EndDate: 1735689600,
            Udprn: null,
            PaonDescription: null,
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Low Road",
            Town: "DISS",
            Locality: "Scole",
            Postcode: "IP22 4LL",
            UkInternalCode: "England",
            Easting: "614000",
            Northing: "278000",
            OsMapReference: "TM1400078000",
            Party: new(
                PartyId: "party-78-600-0001",
                SourcePartyId: "C786000001",
                PersonTitle: "Mr",
                GivenName: "Robert",
                Initials: "R",
                FamilyName: "Taylor",
                OrganisationName: null,
                Email: null,
                Mobile: "07700 900556",
                Telephone: null),
            Herds:
            [
                new("herd-78-600-0001-1", "786001", 1672531200, 1735689600, "CTT")
            ],
            Roles:
            [
                new("pr-78-600-0001-1", null, "holder"),
                new("pr-78-600-0001-2", "herd-78-600-0001-1", "keeper")
            ],
            AllowedSpecies: ["CTT"]),

        new(
            HoldingId: "holding-89-700-0001",
            Cph: "89/700/0001",
            FeatureName: "Solway Firth Pastures",
            CphType: "permanent",
            StartDate: 1609459200,
            EndDate: null,
            Udprn: null,
            PaonDescription: null,
            PaonStartNumber: null,
            PaonStartNumberSuffix: null,
            PaonEndNumber: null,
            PaonEndNumberSuffix: null,
            Street: "Wigton Road",
            Town: "CARLISLE",
            Locality: "Kirkbampton",
            Postcode: "CA1 1MM",
            UkInternalCode: "England",
            Easting: "330000",
            Northing: "555000",
            OsMapReference: "NY3000055000",
            Party: new(
                PartyId: "party-89-700-0001",
                SourcePartyId: "C897000001",
                PersonTitle: null,
                GivenName: null,
                Initials: null,
                FamilyName: null,
                OrganisationName: "Solway Agri Ltd",
                Email: "enquiries@solwayagri.co.uk",
                Mobile: null,
                Telephone: "01228 555001"),
            Herds:
            [
                new("herd-89-700-0001-1", "897001", 1609459200, null, "CTT"),
                new("herd-89-700-0001-2", "897002", 1609459200, null, "SHP")
            ],
            Roles:
            [
                new("pr-89-700-0001-1", null, "holder"),
                new("pr-89-700-0001-2", "herd-89-700-0001-1", "keeper"),
                new("pr-89-700-0001-3", "herd-89-700-0001-2", "keeper")
            ],
            AllowedSpecies: ["CTT", "SHP"])
    ];
}