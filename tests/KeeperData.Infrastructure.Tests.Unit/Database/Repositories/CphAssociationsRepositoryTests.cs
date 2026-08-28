using FluentAssertions;
using KeeperData.Core.Services;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class CphAssociationsRepositoryTests : IDisposable
{
    private const string Email = "jane.farmer@example.com";
    private static readonly string[] OwnerRole = ["owner"];

    private readonly Mock<IReadModelSqliteCacheService> _mockCacheService = new();
    private readonly CphAssociationsRepository _repository;
    private readonly string _tempDir;

    public CphAssociationsRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"read-model-repo-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _repository = new CphAssociationsRepository(_mockCacheService.Object);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public async Task GivenNoCachedReadModel_WhenFinding_ThenThrowsRatherThanReturningAnEmptySnapshot()
    {
        _mockCacheService.Setup(x => x.GetCurrentDbPath()).Returns((string?)null);

        var act = async () => await _repository.FindByEmailAsync(Email, OwnerRole);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GivenNoRolesRequested_WhenFinding_ThenReturnsEmpty()
    {
        var result = await _repository.FindByEmailAsync(Email, []);

        result.Should().BeEmpty();
        _mockCacheService.Verify(x => x.GetCurrentDbPath(), Times.Never);
    }

    [Fact]
    public async Task GivenAMatchingParty_WhenFinding_ThenReturnsTheHoldingCph()
    {
        SetupReadModel(
            parties: [("party-1", "source-party-1", Email)],
            holdings: [("holding-1", "57/103/2335", "Test Holding")],
            partyRoles: [("role-1", "party-1", "holding-1", null, "owner")]);

        var result = await _repository.FindByEmailAsync(Email, OwnerRole);

        result.Should().HaveCount(1);
        result[0].PartyRoleId.Should().Be("role-1");
        result[0].CphNumber.Should().Be("57/103/2335");
        result[0].Role.Should().Be("owner");
        result[0].PartyId.Should().Be("source-party-1");
        result[0].HoldingId.Should().Be("holding-1");
        result[0].HoldingName.Should().Be("Test Holding");
    }

    [Fact]
    public async Task GivenTheEmailIsStoredInADifferentCase_WhenFinding_ThenItStillMatches()
    {
        SetupReadModel(
            parties: [("party-1", "source-party-1", "Jane.Farmer@Example.com")],
            holdings: [("holding-1", "57/103/2335", "Test Holding")],
            partyRoles: [("role-1", "party-1", "holding-1", null, "owner")]);

        var result = await _repository.FindByEmailAsync("  JANE.FARMER@EXAMPLE.COM  ", OwnerRole);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GivenAnOutOfScopeRole_WhenFinding_ThenTheHoldingIsExcluded()
    {
        SetupReadModel(
            parties: [("party-1", "source-party-1", Email)],
            holdings: [("holding-1", "57/103/2335", "Test Holding"), ("holding-2", "12/345/6789", "Other Holding")],
            partyRoles:
            [
                ("role-1", "party-1", "holding-1", null, "owner"),
                ("role-2", "party-1", "holding-2", null, "keeper")
            ]);

        var result = await _repository.FindByEmailAsync(Email, OwnerRole);

        result.Should().HaveCount(1);
        result[0].CphNumber.Should().Be("57/103/2335");
    }

    [Fact]
    public async Task GivenConfiguredRoles_WhenFinding_ThenAllRequestedRolesAreReturnedInOrder()
    {
        SetupReadModel(
            parties: [("party-1", "source-party-1", Email)],
            holdings: [("holding-1", "57/103/2335", "Test Holding"), ("holding-2", "12/345/6789", "Other Holding")],
            partyRoles:
            [
                ("role-1", "party-1", "holding-1", null, "owner"),
                ("role-2", "party-1", "holding-2", null, "keeper")
            ]);

        var result = await _repository.FindByEmailAsync(Email, ["Owner", "KEEPER"]);

        result.Should().HaveCount(2);
        result.Select(association => association.CphNumber).Should().ContainInOrder("12/345/6789", "57/103/2335");
    }

    [Fact]
    public async Task GivenTheSameHoldingAndRoleAcrossHerds_WhenFinding_ThenAssociationsAreDeduplicated()
    {
        SetupReadModel(
            parties: [("party-1", "source-party-1", Email)],
            holdings: [("holding-1", "57/103/2335", "Test Holding")],
            partyRoles:
            [
                ("role-1", "party-1", "holding-1", "herd-1", "owner"),
                ("role-2", "party-1", "holding-1", "herd-2", "owner")
            ]);

        var result = await _repository.FindByEmailAsync(Email, OwnerRole);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GivenTwoPartiesSharingAnEmail_WhenFinding_ThenBothHoldingsAreReturned()
    {
        SetupReadModel(
            parties: [("party-1", "source-party-1", Email), ("party-2", "source-party-2", Email)],
            holdings: [("holding-1", "57/103/2335", "Test Holding"), ("holding-2", "12/345/6789", "Other Holding")],
            partyRoles:
            [
                ("role-1", "party-1", "holding-1", null, "owner"),
                ("role-2", "party-2", "holding-2", null, "owner")
            ]);

        var result = await _repository.FindByEmailAsync(Email, OwnerRole);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GivenNoPartyForTheEmail_WhenFinding_ThenReturnsEmptySoTheSnapshotIsCleared()
    {
        SetupReadModel(
            parties: [("party-1", "source-party-1", "someone.else@example.com")],
            holdings: [("holding-1", "57/103/2335", "Test Holding")],
            partyRoles: [("role-1", "party-1", "holding-1", null, "owner")]);

        var result = await _repository.FindByEmailAsync(Email, OwnerRole);

        result.Should().BeEmpty();
    }

    private void SetupReadModel(
        List<(string Id, string SourcePartyId, string? Email)> parties,
        List<(string Id, string Cph, string? FeatureName)> holdings,
        List<(string Id, string PartyId, string HoldingId, string? HerdId, string Role)> partyRoles)
    {
        var path = Path.Combine(_tempDir, $"krds-db_{Guid.NewGuid():N}.sqlite");

        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();

        Execute(connection, """
            CREATE TABLE Party (
                Id TEXT PRIMARY KEY,
                SourcePartyId TEXT NOT NULL UNIQUE,
                GivenName TEXT,
                FamilyName TEXT,
                OrganisationName TEXT,
                Email TEXT
            );
            CREATE TABLE Holding (
                Id TEXT PRIMARY KEY,
                Cph TEXT NOT NULL UNIQUE,
                FeatureName TEXT,
                CphType TEXT
            );
            CREATE TABLE PartyRole (
                Id TEXT PRIMARY KEY,
                PartyId TEXT NOT NULL,
                HoldingId TEXT NOT NULL,
                HerdId TEXT,
                Role TEXT NOT NULL CHECK (Role IN ('owner', 'holder', 'keeper'))
            );
            """);

        foreach (var party in parties)
        {
            Execute(
                connection,
                "INSERT INTO Party (Id, SourcePartyId, Email) VALUES (@id, @sourceId, @email)",
                ("@id", party.Id),
                ("@sourceId", party.SourcePartyId),
                ("@email", party.Email));
        }

        foreach (var holding in holdings)
        {
            Execute(
                connection,
                "INSERT INTO Holding (Id, Cph, FeatureName) VALUES (@id, @cph, @featureName)",
                ("@id", holding.Id),
                ("@cph", holding.Cph),
                ("@featureName", holding.FeatureName));
        }

        foreach (var partyRole in partyRoles)
        {
            Execute(
                connection,
                "INSERT INTO PartyRole (Id, PartyId, HoldingId, HerdId, Role) VALUES (@id, @partyId, @holdingId, @herdId, @role)",
                ("@id", partyRole.Id),
                ("@partyId", partyRole.PartyId),
                ("@holdingId", partyRole.HoldingId),
                ("@herdId", partyRole.HerdId),
                ("@role", partyRole.Role));
        }

        _mockCacheService.Setup(x => x.GetCurrentDbPath()).Returns(path);
    }

    private static void Execute(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        command.ExecuteNonQuery();
    }
}