using FluentAssertions;
using KeeperData.Core.Services;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class HoldingDetailRepositoryTests : IDisposable
{
    private readonly Mock<IReadModelSqliteCacheService> _mockCacheService = new();
    private readonly HoldingDetailRepository _repository;
    private readonly string _tempDir;
    private readonly string _dbPath;

    public HoldingDetailRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"holding-detail-repo-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "krds-readmodel-test.sqlite");

        InitializeDatabase(_dbPath);

        _mockCacheService.Setup(x => x.GetCurrentDbPath()).Returns(_dbPath);
        _repository = new HoldingDetailRepository(_mockCacheService.Object);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public async Task GivenNoCachedReadModel_WhenGettingHoldingDetail_ThenThrowsInvalidOperationException()
    {
        _mockCacheService.Setup(x => x.GetCurrentDbPath()).Returns((string?)null);

        var act = async () => await _repository.GetHoldingDetailByCphAsync("13/169/0007");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The SAM read model is not cached locally, so holding details cannot be resolved.");
    }

    [Fact]
    public async Task GivenNonExistentCph_WhenGettingHoldingDetail_ThenReturnsNull()
    {
        var result = await _repository.GetHoldingDetailByCphAsync("99/999/9999");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GivenWorkedExample_10_024_0247_WhenGettingHoldingDetail_ThenMatchesExpectedShape()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        // 1. Insert Holding
        Execute(connection, """
            INSERT INTO Holding (
                Id, Cph, FeatureName, CphType, StartDate, EndDate, Udprn,
                PaonDescription, PaonStartNumber, PaonStartNumberSuffix, PaonEndNumber, PaonEndNumberSuffix,
                Street, Town, Locality, Postcode, UkInternalCode, Easting, Northing, OsMapReference
            ) VALUES (
                'holding-10-024-0247', '10/024/0247', NULL, 'permanent', 1310515200, NULL, NULL,
                NULL, NULL, NULL, NULL, NULL,
                'The Street', 'WORCESTER', 'Some Location', 'TT5 2UU', 'England', '000000', '000000', 'SS0000000200'
            );
            """);

        // 2. Insert Party
        Execute(connection, """
            INSERT INTO Party (
                Id, SourcePartyId, PersonTitle, GivenName, Initials, FamilyName, OrganisationName, Email, Mobile, Telephone
            ) VALUES (
                'party-1', 'C000000', 'MISS', NULL, 'J', 'Example', NULL, NULL, NULL, '01234 567890'
            );
            """);

        // 3. Insert Herds
        Execute(connection, """
            INSERT INTO Herd (Id, HoldingId, Herdmark, AnimalGroupFromDate, AnimalGroupToDate, AnimalSpeciesCode)
            VALUES
                ('herd-1', 'holding-10-024-0247', '360396', 1216166400, NULL, 'CTT'),
                ('herd-2', 'holding-10-024-0247', '372893', 1216166400, NULL, 'SHP'),
                ('herd-3', 'holding-10-024-0247', '373074', 1216166400, NULL, 'SHP');
            """);

        // 4. Insert PartyRoles
        Execute(connection, """
            INSERT INTO PartyRole (Id, PartyId, HoldingId, HerdId, Role)
            VALUES
                ('role-1', 'party-1', 'holding-10-024-0247', NULL, 'holder'),
                ('role-2', 'party-1', 'holding-10-024-0247', 'herd-1', 'keeper'),
                ('role-3', 'party-1', 'holding-10-024-0247', 'herd-2', 'keeper'),
                ('role-4', 'party-1', 'holding-10-024-0247', 'herd-1', 'owner'),
                ('role-5', 'party-1', 'holding-10-024-0247', 'herd-2', 'owner');
            """);

        // 5. Insert HoldingAnimalProfile
        Execute(connection, """
            INSERT INTO HoldingAnimalProfile (Id, HoldingId, AnimalSpeciesCode)
            VALUES
                ('hap-1', 'holding-10-024-0247', 'CTT'),
                ('hap-2', 'holding-10-024-0247', 'SHP');
            """);

        var result = await _repository.GetHoldingDetailByCphAsync("10/024/0247");

        result.Should().NotBeNull();
        result!.Identifier.Should().Be("10/024/0247");
        result.HoldingType.Should().Be("permanent");
        result.Name.Should().BeNull();
        result.StartDate.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1310515200));
        result.EndDate.Should().BeNull();

        result.Location.OsMapReference.Should().Be("SS0000000200");
        result.Location.Easting.Should().Be(0);
        result.Location.Northing.Should().Be(0);
        result.Location.Address.Udprn.Should().BeNull();
        result.Location.Address.AddressLine1.Should().Be("The Street");
        result.Location.Address.AddressLine2.Should().BeNull();
        result.Location.Address.PostTown.Should().Be("WORCESTER");
        result.Location.Address.Locality.Should().Be("Some Location");
        result.Location.Address.Postcode.Should().Be("TT5 2UU");
        result.Location.Address.Country.Should().Be("England");

        result.Associations.Should().HaveCount(1);
        var assoc = result.Associations[0];
        assoc.CustomerNumber.Should().Be("C000000");
        assoc.Title.Should().Be("MISS");
        assoc.FirstName.Should().BeNull();
        assoc.LastName.Should().Be("Example");
        assoc.Name.Should().Be("MISS J Example");
        assoc.PartyType.Should().Be("person");
        assoc.Email.Should().BeNull();
        assoc.Mobile.Should().BeNull();
        assoc.Telephone.Should().Be("01234 567890");

        assoc.Roles.Should().HaveCount(3);
        var holderRole = assoc.Roles.First(r => r.Code == "holder");
        holderRole.Species.Should().BeEmpty();

        var keeperRole = assoc.Roles.First(r => r.Code == "keeper");
        keeperRole.Species.Should().BeEquivalentTo(["CTT", "SHP"]);

        var ownerRole = assoc.Roles.First(r => r.Code == "owner");
        ownerRole.Species.Should().BeEquivalentTo(["CTT", "SHP"]);

        result.AllowedSpecies.Should().BeEquivalentTo(["CTT", "SHP"]);

        result.Marks.Should().HaveCount(3);
        result.Marks.Select(m => m.Mark).Should().BeEquivalentTo(["360396", "372893", "373074"]);
    }

    [Fact]
    public async Task GivenSparseHolding_WhenGettingHoldingDetail_ThenReturnsNullFieldsAndEmptyCollections()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        Execute(connection, """
            INSERT INTO Holding (
                Id, Cph, FeatureName, CphType, StartDate, EndDate, Udprn,
                PaonDescription, PaonStartNumber, PaonStartNumberSuffix, PaonEndNumber, PaonEndNumberSuffix,
                Street, Town, Locality, Postcode, UkInternalCode, Easting, Northing, OsMapReference
            ) VALUES (
                'sparse-holding', '55/444/3333', NULL, 'temporary', NULL, NULL, NULL,
                NULL, NULL, NULL, NULL, NULL,
                NULL, 'YORK', NULL, 'YO1 1AA', NULL, NULL, NULL, NULL
            );
            """);

        var result = await _repository.GetHoldingDetailByCphAsync("55/444/3333");

        result.Should().NotBeNull();
        result!.Identifier.Should().Be("55/444/3333");
        result.HoldingType.Should().Be("temporary");
        result.Name.Should().BeNull();
        result.StartDate.Should().BeNull();
        result.EndDate.Should().BeNull();
        result.Location.OsMapReference.Should().BeNull();
        result.Location.Easting.Should().BeNull();
        result.Location.Northing.Should().BeNull();
        result.Location.Address.AddressLine1.Should().BeNull();
        result.Location.Address.AddressLine2.Should().BeNull();
        result.Location.Address.PostTown.Should().Be("YORK");
        result.Location.Address.Locality.Should().BeNull();
        result.Location.Address.Postcode.Should().Be("YO1 1AA");
        result.Location.Address.Country.Should().BeNull();

        result.Associations.Should().BeEmpty();
        result.AllowedSpecies.Should().BeEmpty();
        result.Marks.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenPaonAndStreet_WhenGettingHoldingDetail_ThenAddressLinesAssembledCorrectly()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        Execute(connection, """
            INSERT INTO Holding (
                Id, Cph, FeatureName, CphType, StartDate, EndDate, Udprn,
                PaonDescription, PaonStartNumber, PaonStartNumberSuffix, PaonEndNumber, PaonEndNumberSuffix,
                Street, Town, Locality, Postcode, UkInternalCode, Easting, Northing, OsMapReference
            ) VALUES (
                'addr-holding-1', '13/169/0007', 'Land At Test Farm 06', 'permanent', 1773014400, NULL, NULL,
                'Test Farm 06', NULL, NULL, NULL, NULL,
                'Layer Road', 'COLCHESTER', 'Great Wigborough', 'CO5 7RR', 'England', '595600', '215900', 'TL9560015900'
            );
            """);

        var result = await _repository.GetHoldingDetailByCphAsync("13/169/0007");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Land At Test Farm 06");
        result.Location.Address.AddressLine1.Should().Be("Test Farm 06");
        result.Location.Address.AddressLine2.Should().Be("Layer Road");
        result.Location.Address.PostTown.Should().Be("COLCHESTER");
        result.Location.Address.Locality.Should().Be("Great Wigborough");
        result.Location.Address.Postcode.Should().Be("CO5 7RR");
        result.Location.Address.Country.Should().Be("England");
    }

    [Fact]
    public async Task GivenPaonNumbersWithRange_WhenGettingHoldingDetail_ThenStreetLineHasRange()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        Execute(connection, """
            INSERT INTO Holding (
                Id, Cph, FeatureName, CphType, StartDate, EndDate, Udprn,
                PaonDescription, PaonStartNumber, PaonStartNumberSuffix, PaonEndNumber, PaonEndNumberSuffix,
                Street, Town, Locality, Postcode, UkInternalCode, Easting, Northing, OsMapReference
            ) VALUES (
                'range-holding', '12/345/6789', NULL, 'permanent', NULL, NULL, NULL,
                NULL, '10', 'A', '12', 'B',
                'High Street', 'LONDON', NULL, 'SW1A 1AA', 'England', NULL, NULL, NULL
            );
            """);

        var result = await _repository.GetHoldingDetailByCphAsync("12/345/6789");

        result.Should().NotBeNull();
        result!.Location.Address.AddressLine1.Should().Be("10A-12B High Street");
        result.Location.Address.AddressLine2.Should().BeNull();
    }

    [Fact]
    public async Task GivenOrganisationParty_WhenGettingHoldingDetail_ThenPartyTypeIsOrganisationAndNameIsOrganisationName()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        Execute(connection, """
            INSERT INTO Holding (Id, Cph) VALUES ('org-holding', '11/222/3333');
            INSERT INTO Party (Id, SourcePartyId, PersonTitle, GivenName, FamilyName, OrganisationName)
            VALUES ('party-org', 'C999999', 'MR', 'John', 'Smith', 'Farming Co Ltd');
            INSERT INTO PartyRole (Id, PartyId, HoldingId, Role)
            VALUES ('role-org', 'party-org', 'org-holding', 'owner');
            """);

        var result = await _repository.GetHoldingDetailByCphAsync("11/222/3333");

        result.Should().NotBeNull();
        result!.Associations.Should().HaveCount(1);
        var assoc = result.Associations[0];
        assoc.PartyType.Should().Be("organisation");
        assoc.Name.Should().Be("Farming Co Ltd");
        assoc.Roles.Should().ContainSingle(r => r.Code == "owner");
    }

    [Fact]
    public async Task GivenMarkSpanningMultipleRows_WhenGettingHoldingDetail_ThenAggregatesMinStartMaxEndAndDistinctSpecies()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        Execute(connection, """
            INSERT INTO Holding (Id, Cph) VALUES ('mark-multi-holding', '99/888/7777');
            INSERT INTO Herd (Id, HoldingId, Herdmark, AnimalGroupFromDate, AnimalGroupToDate, AnimalSpeciesCode)
            VALUES
                ('herd-a', 'mark-multi-holding', 'MARK10', 1200000000, 1300000000, 'CTT'),
                ('herd-b', 'mark-multi-holding', 'MARK10', 1100000000, 1400000000, 'SHP'),
                ('herd-c', 'mark-multi-holding', 'MARK10', 1150000000, NULL, 'PG');
            """);

        var result = await _repository.GetHoldingDetailByCphAsync("99/888/7777");

        result.Should().NotBeNull();
        result!.Marks.Should().HaveCount(1);
        var mark = result.Marks[0];
        mark.Mark.Should().Be("MARK10");
        mark.StartDate.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1100000000));
        mark.EndDate.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1400000000));
        mark.Species.Should().BeEquivalentTo(["CTT", "SHP", "PG"]);
    }

    private static void InitializeDatabase(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();

        Execute(connection, """
            CREATE TABLE Holding (
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

            CREATE TABLE Party (
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

            CREATE TABLE Herd (
                Id TEXT PRIMARY KEY,
                HoldingId TEXT NOT NULL,
                Herdmark TEXT NOT NULL,
                AnimalGroupFromDate INTEGER,
                AnimalGroupToDate INTEGER,
                AnimalSpeciesCode TEXT NOT NULL
            );

            CREATE TABLE PartyRole (
                Id TEXT PRIMARY KEY,
                PartyId TEXT NOT NULL,
                HoldingId TEXT NOT NULL,
                HerdId TEXT,
                Role TEXT NOT NULL
            );

            CREATE TABLE HoldingAnimalProfile (
                Id TEXT PRIMARY KEY,
                HoldingId TEXT NOT NULL,
                AnimalSpeciesCode TEXT NOT NULL
            );
            """);
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
