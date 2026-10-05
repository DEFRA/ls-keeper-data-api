using FluentAssertions;
using KeeperData.Core.Services;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Storage.Sqlite;
using KeeperData.Infrastructure.Database.Repositories;
using KeeperData.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class HoldingDetailRepositoryTests : IDisposable
{
    private static readonly string[] s_singleHoldingCph = ["10/001/0001"];

    private readonly Mock<IReadModelSqliteCacheService> _mockCacheService = new();
    private readonly Mock<ILogger<HoldingDetailRepository>> _mockLogger = new();
    private readonly HoldingDetailRepository _repository;
    private readonly string _tempDir;
    private readonly string _dbPath;

    public HoldingDetailRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"holding-detail-repo-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "krds-readmodel-test.sqlite");

        InitializeDatabase(_dbPath);

        _mockLogger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        _mockCacheService.Setup(x => x.GetCurrentDbPath()).Returns(_dbPath);
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, null));
        _repository = new HoldingDetailRepository(_mockCacheService.Object, _mockLogger.Object);
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
                'The Street', 'WORCESTER', 'Some Location', 'TT5 2UU', 'England', 0, 0, 'SS0000000200'
            );
            """);

        // 2. Insert Party
        Execute(connection, """
            INSERT INTO Party (
                Id, SourcePartyId, PersonTitle, GivenName, Initials, FamilyName, OrganisationName, Email, Mobile, Telephone,
                AddressLine1, AddressStreet, AddressTown, AddressLocality, AddressNation, AddressPostcode, AddressCountryCode
            ) VALUES (
                'party-1', 'C000000', 'MISS', NULL, 'J', 'Example', NULL, NULL, NULL, '01234 567890',
                'Example Farm', 'The Street', 'WORCESTER', 'Some Location', 'England', 'TT5 2UU', 'GB'
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

        assoc.Address.AddressLine1.Should().Be("Example Farm");
        assoc.Address.AddressLine2.Should().Be("The Street");
        assoc.Address.AddressTown.Should().Be("WORCESTER");
        assoc.Address.AddressLocality.Should().Be("Some Location");
        assoc.Address.AddressNation.Should().Be("England");
        assoc.Address.AddressPostcode.Should().Be("TT5 2UU");
        assoc.Address.AddressCountryCode.Should().Be("GB");

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
                'Layer Road', 'COLCHESTER', 'Great Wigborough', 'CO5 7RR', 'England', 595600, 215900, 'TL9560015900'
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
    public async Task GivenTypedColumns_WhenGettingHoldingDetail_ThenNumericFieldsAreReadAsIntegers()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        Execute(connection, """
            INSERT INTO Holding (Id, Cph, Udprn, Easting, Northing)
            VALUES ('typed-holding', '44/555/6666', 10001234, 595600, 215900);
            """);

        var result = await _repository.GetHoldingDetailByCphAsync("44/555/6666");

        result.Should().NotBeNull();
        result!.Location.Address.Udprn.Should().Be(10001234);
        result.Location.Easting.Should().Be(595600);
        result.Location.Northing.Should().Be(215900);
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
    public async Task GivenPartyWithoutAddressColumns_WhenGettingHoldingDetail_ThenAddressIsPresentWithNullMembers()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        Execute(connection, """
            INSERT INTO Holding (Id, Cph) VALUES ('no-address-holding', '33/444/5555');
            INSERT INTO Party (Id, SourcePartyId, GivenName, FamilyName)
            VALUES ('party-no-address', 'C888888', 'Jane', 'Doe');
            INSERT INTO PartyRole (Id, PartyId, HoldingId, Role)
            VALUES ('role-no-address', 'party-no-address', 'no-address-holding', 'holder');
            """);

        var result = await _repository.GetHoldingDetailByCphAsync("33/444/5555");

        result.Should().NotBeNull();
        var address = result!.Associations.Should().ContainSingle().Subject.Address;
        address.Should().NotBeNull();
        address.AddressLine1.Should().BeNull();
        address.AddressLine2.Should().BeNull();
        address.AddressTown.Should().BeNull();
        address.AddressLocality.Should().BeNull();
        address.AddressNation.Should().BeNull();
        address.AddressPostcode.Should().BeNull();
        address.AddressCountryCode.Should().BeNull();
    }

    [Fact]
    public async Task GivenMarkWithAnOpenEndedHerd_WhenGettingHoldingDetail_ThenMarkRemainsOpenEnded()
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
        mark.EndDate.Should().BeNull();
        mark.Species.Should().BeEquivalentTo(["CTT", "SHP", "PG"]);
    }

    [Fact]
    public async Task GivenMarkWithOnlyClosedHerds_WhenGettingHoldingDetail_ThenUsesLatestEndDate()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        Execute(connection, """
            INSERT INTO Holding (Id, Cph) VALUES ('closed-mark-holding', '99/888/6666');
            INSERT INTO Herd (Id, HoldingId, Herdmark, AnimalGroupFromDate, AnimalGroupToDate, AnimalSpeciesCode)
            VALUES
                ('closed-herd-a', 'closed-mark-holding', 'MARK20', 1200000000, 1300000000, 'CTT'),
                ('closed-herd-b', 'closed-mark-holding', 'MARK20', 1100000000, 1400000000, 'SHP');
            """);

        var result = await _repository.GetHoldingDetailByCphAsync("99/888/6666");

        result.Should().NotBeNull();
        result!.Marks.Should().ContainSingle();
        result.Marks[0].StartDate.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1100000000));
        result.Marks[0].EndDate.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1400000000));
    }

    [Fact]
    public async Task GivenNoCachedReadModel_WhenGettingPagedHoldings_ThenThrowsInvalidOperationException()
    {
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns((SqliteSnapshot?)null);

        var act = async () => await _repository.GetPagedHoldingsAsync(1, 10, "asc", "cph");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The SAM read model is not cached locally, so holding details cannot be resolved.");
    }

    [Theory]
    [InlineData("green fields", 2)]
    [InlineData("GREEN", 2)]
    [InlineData("13/169/0007", 1)]
    [InlineData("  13/169/0007  ", 1)]
    [InlineData("131690007", 1)]
    [InlineData("CO5 7RR", 1)]
    [InlineData("co57rr", 1)]
    [InlineData("farming ltd", 1)]
    [InlineData("12345678", 1)]
    [InlineData("MRS", 1)]
    [InlineData("Sheila", 1)]
    [InlineData("X", 1)]
    [InlineData("Keeper-Six", 1)]
    [InlineData("100023456789", 1)]
    [InlineData("alice@example.com", 1)]
    [InlineData("+44 (0)7700 900123", 1)]
    [InlineData("4407700900123", 1)]
    [InlineData("01234 567890", 1)]
    [InlineData("01234567890", 1)]
    public async Task SearchHoldings_MatchesIndexedFields(string search, int expectedCount)
    {
        SeedSearchHoldings();
        var (indexPath, documentCount) = await HoldingSearchIndex.BuildAsync(_dbPath, CancellationToken.None);
        documentCount.Should().Be(3);
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, null, indexPath));

        var result = await _repository.SearchHoldingsAsync(1, 10, "asc", "cph", search);

        result.TotalCount.Should().Be(expectedCount);
        result.Items.Should().HaveCount(expectedCount);
    }

    [Theory]
    [InlineData("10A")]
    [InlineData("12B")]
    [InlineData("10A-12B High Street")]
    public async Task SearchHoldings_MatchesFormattedStreetNumber(string search)
    {
        using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
        {
            connection.Open();
            Execute(connection, """
                INSERT INTO Holding (Id, Cph, PaonStartNumber, PaonStartNumberSuffix,
                    PaonEndNumber, PaonEndNumberSuffix, Street)
                VALUES ('range-holding', '12/345/6789', '10', 'A', '12', 'B', 'High Street');
                """);
        }

        var (indexPath, _) = await HoldingSearchIndex.BuildAsync(_dbPath, CancellationToken.None);
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, null, indexPath));

        var result = await _repository.SearchHoldingsAsync(1, 10, "asc", "cph", search);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Location.Address.AddressLine1.Should().Be("10A-12B High Street");
    }

    [Theory]
    [InlineData("12")]
    [InlineData("B")]
    public async Task SearchHoldings_MatchesStandaloneEndNumberAndSuffix(string search)
    {
        using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
        {
            connection.Open();
            Execute(connection, """
                INSERT INTO Holding (Id, Cph, PaonEndNumber, PaonEndNumberSuffix)
                VALUES ('end-only', '99/345/6789', '12', 'B');
                """);
        }

        var (indexPath, _) = await HoldingSearchIndex.BuildAsync(_dbPath, CancellationToken.None);
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, null, indexPath));

        var result = await _repository.SearchHoldingsAsync(1, 10, "asc", "cph", search);
        result.Items.Should().ContainSingle().Which.Identifier.Should().Be("99/345/6789");
    }

    [Fact]
    public async Task BuildSearchIndex_ReleasesTheIndexFile()
    {
        var (indexPath, _) = await HoldingSearchIndex.BuildAsync(_dbPath, CancellationToken.None);

        using var exclusiveRead = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.None);
        exclusiveRead.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task BuildSearchIndex_WhenIndexAlreadyExists_RebuildsFromCurrentHoldings()
    {
        var (indexPath, initialCount) = await HoldingSearchIndex.BuildAsync(_dbPath, CancellationToken.None);
        initialCount.Should().Be(0);

        using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
        {
            connection.Open();
            Execute(connection, "INSERT INTO Holding (Id, Cph, FeatureName) VALUES ('new-holding', '12/345/6789', 'New Farm');");
        }

        var (rebuiltPath, rebuiltCount) = await HoldingSearchIndex.BuildAsync(_dbPath, CancellationToken.None);
        rebuiltPath.Should().Be(indexPath);
        rebuiltCount.Should().Be(1);
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, null, rebuiltPath));

        var result = await _repository.SearchHoldingsAsync(1, 10, "asc", "cph", "New Farm");
        result.Items.Should().ContainSingle().Which.Identifier.Should().Be("12/345/6789");
    }

    [Fact]
    public async Task SearchHoldings_PaginatesAndRespectsExistingSort()
    {
        SeedSearchHoldings();
        var (indexPath, _) = await HoldingSearchIndex.BuildAsync(_dbPath, CancellationToken.None);
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, null, indexPath));

        var first = await _repository.SearchHoldingsAsync(1, 1, "desc", "name", "green");
        var second = await _repository.SearchHoldingsAsync(2, 1, "desc", "name", "green");

        first.TotalCount.Should().Be(2);
        first.Items.Select(x => x.Name).Should().Equal("Land At Test Farm");
        second.TotalCount.Should().Be(2);
        second.Items.Select(x => x.Name).Should().Equal("Green Fields Farm");
    }

    [Theory]
    [InlineData("cph", "asc")]
    [InlineData("cph", "desc")]
    [InlineData("name", "asc")]
    [InlineData("name", "desc")]
    [InlineData("holdingType", "asc")]
    [InlineData("holdingType", "desc")]
    [InlineData("startDate", "asc")]
    [InlineData("startDate", "desc")]
    [InlineData("endDate", "asc")]
    [InlineData("endDate", "desc")]
    public async Task SearchHoldings_WithDifferentOrderFields_OrdersByRequestedField(string order, string sort)
    {
        SeedSearchHoldings();
        var (indexPath, _) = await HoldingSearchIndex.BuildAsync(_dbPath, CancellationToken.None);
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, null, indexPath));

        var result = await _repository.SearchHoldingsAsync(1, 10, sort, order, "green");
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchHoldings_WhenPageExceedsResults_ReturnsEmptyListAndPreservesTotalCount()
    {
        SeedSearchHoldings();
        var (indexPath, _) = await HoldingSearchIndex.BuildAsync(_dbPath, CancellationToken.None);
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, null, indexPath));

        var result = await _repository.SearchHoldingsAsync(10, 10, "asc", "cph", "green");
        result.TotalCount.Should().Be(2);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchHoldings_NoMatches_ReturnsEmptyPage()
    {
        SeedSearchHoldings();
        var (indexPath, _) = await HoldingSearchIndex.BuildAsync(_dbPath, CancellationToken.None);
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, null, indexPath));

        var result = await _repository.SearchHoldingsAsync(1, 10, "asc", "cph", "nonexistent");

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("---")]
    [InlineData("!?/ ")]
    [InlineData("   ")]
    public async Task SearchHoldings_WithoutSearchTerms_ReturnsEmptyPage(string search)
    {
        var timestamp = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, timestamp));

        var result = await _repository.SearchHoldingsAsync(1, 10, "asc", "cph", search);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.DataTimestamp.Should().Be(timestamp);
    }

    [Fact]
    public async Task SearchHoldings_WithoutIndex_ThrowsUnavailable()
    {
        var act = async () => await _repository.SearchHoldingsAsync(1, 10, "asc", "cph", "green");
        await act.Should().ThrowAsync<SearchIndexUnavailableException>().WithMessage("The holding search index is not available.");
    }

    [Fact]
    public async Task SearchHoldings_WhenIndexFileIsCorrupt_ThrowsUnavailable()
    {
        var indexPath = Path.Combine(Path.GetDirectoryName(_dbPath)!, "holdings-search.sqlite");
        await File.WriteAllTextAsync(indexPath, "not a SQLite database");
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, null, indexPath));

        var act = async () => await _repository.SearchHoldingsAsync(1, 10, "asc", "cph", "green");

        var assertion = await act.Should().ThrowAsync<SearchIndexUnavailableException>();
        assertion.Which.InnerException.Should().BeOfType<SqliteException>();
    }

    [Fact]
    public async Task SearchHoldings_WhenIndexHasNoSearchTable_ThrowsUnavailable()
    {
        var indexPath = Path.Combine(Path.GetDirectoryName(_dbPath)!, "holdings-search.sqlite");
        using (var connection = new SqliteConnection($"Data Source={indexPath}"))
            connection.Open();
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(new SqliteSnapshot(_dbPath, null, indexPath));

        var act = async () => await _repository.SearchHoldingsAsync(1, 10, "asc", "cph", "green");

        var assertion = await act.Should().ThrowAsync<SearchIndexUnavailableException>();
        assertion.Which.InnerException.Should().BeOfType<SqliteException>();
    }

    private void SeedSearchHoldings()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        Execute(connection, """
            INSERT INTO Holding (Id, Cph, FeatureName, CphType, Udprn, Postcode)
            VALUES ('h-1', '13/169/0007', 'Land At Test Farm', 'permanent', '100023456789', 'CO5 7RR'),
                   ('h-2', '13/169/0008', 'Green Fields Farm', 'permanent', null, 'AA1 1AA'),
                   ('h-3', '13/169/0009', 'Other Place', 'temporary', null, 'BB1 1BB');
            INSERT INTO Party (Id, SourcePartyId, OrganisationName, PersonTitle, GivenName, Initials, FamilyName, Email, Mobile, Telephone)
            VALUES ('p-1', '12345678', 'Green Fields Farming Ltd', null, null, null, null, 'alice@example.com', '+44 (0)7700 900123', '01234 567890'),
                   ('p-2', 'C161215', null, 'MRS', 'Sheila', 'X', 'Keeper-Six', null, null, null);
            INSERT INTO PartyRole (Id, PartyId, HoldingId, Role)
            VALUES ('r-1', 'p-1', 'h-1', 'holder'),
                   ('r-2', 'p-1', 'h-1', 'keeper'),
                   ('r-3', 'p-2', 'h-1', 'owner');
            """);
    }

    [Fact]
    public async Task GivenEmptyDatabase_WhenGettingPagedHoldings_ThenReturnsEmptyAndZeroTotalCount()
    {
        var (items, totalCount, _) = await _repository.GetPagedHoldingsAsync(1, 10, "asc", "cph");

        items.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GivenMultipleHoldings_WhenGettingPagedHoldings_ThenPaginatesAndSortsAscending()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        for (var i = 1; i <= 5; i++)
        {
            Execute(connection, $"""
                INSERT INTO Holding (Id, Cph, FeatureName, CphType)
                VALUES ('h-{i}', '10/001/000{i}', 'Farm {i}', 'permanent');
                """);
        }

        // Page 1 (2 items)
        var page1 = await _repository.GetPagedHoldingsAsync(1, 2, "asc", "cph");
        page1.TotalCount.Should().Be(5);
        page1.Items.Should().HaveCount(2);
        page1.Items[0].Identifier.Should().Be("10/001/0001");
        page1.Items[1].Identifier.Should().Be("10/001/0002");

        // Page 2 (2 items)
        var page2 = await _repository.GetPagedHoldingsAsync(2, 2, "asc", "cph");
        page2.TotalCount.Should().Be(5);
        page2.Items.Should().HaveCount(2);
        page2.Items[0].Identifier.Should().Be("10/001/0003");
        page2.Items[1].Identifier.Should().Be("10/001/0004");

        // Page 3 (1 item)
        var page3 = await _repository.GetPagedHoldingsAsync(3, 2, "asc", "cph");
        page3.TotalCount.Should().Be(5);
        page3.Items.Should().HaveCount(1);
        page3.Items[0].Identifier.Should().Be("10/001/0005");
    }

    [Fact]
    public async Task GivenMultipleHoldings_WhenGettingPagedHoldingsDescending_ThenSortsDescending()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        for (var i = 1; i <= 3; i++)
        {
            Execute(connection, $"""
                INSERT INTO Holding (Id, Cph)
                VALUES ('h-desc-{i}', '10/001/000{i}');
                """);
        }

        var result = await _repository.GetPagedHoldingsAsync(1, 3, "desc", "cph");
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
        result.Items[0].Identifier.Should().Be("10/001/0003");
        result.Items[1].Identifier.Should().Be("10/001/0002");
        result.Items[2].Identifier.Should().Be("10/001/0001");
    }

    [Theory]
    [InlineData("cph", "asc", "10/001/0001")]
    [InlineData("cph", "desc", "10/001/0003")]
    [InlineData("identifier", "asc", "10/001/0001")]
    [InlineData("name", "asc", "10/001/0002")]
    [InlineData("name", "desc", "10/001/0001")]
    [InlineData("holdingType", "asc", "10/001/0003")]
    [InlineData("holdingType", "desc", "10/001/0001")]
    [InlineData("startDate", "asc", "10/001/0002")]
    [InlineData("startDate", "desc", "10/001/0001")]
    [InlineData("endDate", "asc", "10/001/0003")]
    [InlineData("endDate", "desc", "10/001/0001")]
    public async Task GivenDifferentOrderFields_WhenGettingPagedHoldings_ThenOrdersByRequestedField(string order, string sort, string expectedFirstCph)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        Execute(connection, """
            INSERT INTO Holding (Id, Cph, FeatureName, CphType, StartDate, EndDate)
            VALUES
                ('h-1', '10/001/0001', 'Zed', 'temporary', 300, 300),
                ('h-2', '10/001/0002', 'Alpha', 'permanent', 100, 200),
                ('h-3', '10/001/0003', 'Middle', 'common', 200, 100);
            """);

        var result = await _repository.GetPagedHoldingsAsync(1, 2, sort, order);

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.Items[0].Identifier.Should().Be(expectedFirstCph);
    }

    [Fact]
    public async Task GivenEqualSortValues_WhenGettingPages_ThenUsesCphAsTieBreaker()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        Execute(connection, """
            INSERT INTO Holding (Id, Cph, FeatureName)
            VALUES
                ('h-2', '10/001/0002', 'Same'),
                ('h-1', '10/001/0001', 'Same');
            """);

        var firstPage = await _repository.GetPagedHoldingsAsync(1, 1, "asc", "name");
        var secondPage = await _repository.GetPagedHoldingsAsync(2, 1, "asc", "name");

        firstPage.Items[0].Identifier.Should().Be("10/001/0001");
        secondPage.Items[0].Identifier.Should().Be("10/001/0002");
    }

    [Fact]
    public async Task GivenMaximumPage_WhenGettingPagedHoldings_ThenReturnsEmptyPage()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        Execute(connection, "INSERT INTO Holding (Id, Cph) VALUES ('h-1', '10/001/0001');");

        var result = await _repository.GetPagedHoldingsAsync(int.MaxValue, 100, "asc", "cph");

        result.TotalCount.Should().Be(1);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenMultipleHoldingsWithChildCollections_WhenGettingPagedHoldings_ThenBatchedQueriesPopulateCorrectly()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        // 2 Holdings
        Execute(connection, """
            INSERT INTO Holding (Id, Cph, FeatureName, CphType)
            VALUES
                ('h-batch-1', '20/001/0001', 'Batch Farm 1', 'permanent'),
                ('h-batch-2', '20/001/0002', 'Batch Farm 2', 'temporary');
            """);

        // Parties & Roles
        Execute(connection, """
            INSERT INTO Party (Id, SourcePartyId, PersonTitle, GivenName, FamilyName)
            VALUES
                ('p-1', 'C000001', 'MR', 'Alice', 'KeeperOne'),
                ('p-2', 'C000002', 'MS', 'Bob', 'KeeperTwo');

            INSERT INTO PartyRole (Id, PartyId, HoldingId, Role)
            VALUES
                ('pr-1', 'p-1', 'h-batch-1', 'keeper'),
                ('pr-2', 'p-2', 'h-batch-2', 'owner');
            """);

        // Allowed Species
        Execute(connection, """
            INSERT INTO HoldingAnimalProfile (Id, HoldingId, AnimalSpeciesCode)
            VALUES
                ('hap-1', 'h-batch-1', 'CTT'),
                ('hap-2', 'h-batch-2', 'PG'),
                ('hap-3', 'h-batch-2', 'SHP');
            """);

        // Herds & Marks
        Execute(connection, """
            INSERT INTO Herd (Id, HoldingId, Herdmark, AnimalGroupFromDate, AnimalSpeciesCode)
            VALUES
                ('herd-1', 'h-batch-1', 'MARK-A', 1200000000, 'CTT'),
                ('herd-2', 'h-batch-2', 'MARK-B', 1300000000, 'SHP');
            """);

        var result = await _repository.GetPagedHoldingsAsync(1, 10, "asc", "cph");

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);

        var holding1 = result.Items.First(h => h.Identifier == "20/001/0001");
        holding1.Name.Should().Be("Batch Farm 1");
        holding1.HoldingType.Should().Be("permanent");
        holding1.Associations.Should().ContainSingle(a => a.CustomerNumber == "C000001");
        holding1.AllowedSpecies.Should().BeEquivalentTo(["CTT"]);
        holding1.Marks.Should().ContainSingle(m => m.Mark == "MARK-A");

        var holding2 = result.Items.First(h => h.Identifier == "20/001/0002");
        holding2.Name.Should().Be("Batch Farm 2");
        holding2.HoldingType.Should().Be("temporary");
        holding2.Associations.Should().ContainSingle(a => a.CustomerNumber == "C000002");
        holding2.AllowedSpecies.Should().BeEquivalentTo(["PG", "SHP"]);
        holding2.Marks.Should().ContainSingle(m => m.Mark == "MARK-B");
    }

    [Fact]
    public async Task GivenPageOutOfBounds_WhenGettingPagedHoldings_ThenReturnsEmptyWithTotalCount()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        Execute(connection, """
            INSERT INTO Holding (Id, Cph)
            VALUES ('h-oob-1', '30/001/0001');
            """);

        var result = await _repository.GetPagedHoldingsAsync(10, 10, "asc", "cph");

        result.TotalCount.Should().Be(1);
        result.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public async Task GivenCacheChangesAfterSnapshotCapture_WhenGettingPage_ThenReturnsCapturedDataAndTimestamp(bool seedHolding, int page)
    {
        if (seedHolding)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            Execute(connection, "INSERT INTO Holding (Id, Cph) VALUES ('snapshot-holding', '10/001/0001');");
        }

        var timestamp = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        var originalSnapshot = new SqliteSnapshot(_dbPath, timestamp);
        var currentSnapshot = originalSnapshot;
        _mockCacheService.Setup(x => x.GetCurrentSnapshot()).Returns(() =>
        {
            var captured = currentSnapshot;
            currentSnapshot = new SqliteSnapshot("new-snapshot.sqlite", timestamp.AddDays(1));
            return captured;
        });
        _mockCacheService.SetupGet(x => x.DataTimestamp).Returns(() => currentSnapshot.DataTimestamp);

        var result = await _repository.GetPagedHoldingsAsync(page, 10, "asc", "cph");

        result.DataTimestamp.Should().Be(timestamp);
        result.TotalCount.Should().Be(seedHolding ? 1 : 0);
        result.Items.Select(x => x.Identifier).Should().Equal(
            seedHolding && page == 1 ? s_singleHoldingCph : Array.Empty<string>());
        _mockCacheService.Verify(x => x.GetCurrentSnapshot(), Times.Once);
        _mockCacheService.VerifyGet(x => x.DataTimestamp, Times.Never);
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
                Udprn INTEGER,
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
                Easting INTEGER,
                Northing INTEGER,
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
                Telephone TEXT,
                AddressLine1 TEXT,
                AddressStreet TEXT,
                AddressTown TEXT,
                AddressLocality TEXT,
                AddressNation TEXT,
                AddressPostcode TEXT,
                AddressCountryCode TEXT
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