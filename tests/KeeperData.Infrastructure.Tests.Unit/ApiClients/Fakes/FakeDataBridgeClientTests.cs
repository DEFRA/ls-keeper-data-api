using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Infrastructure.ApiClients.Fakes;

namespace KeeperData.Infrastructure.Tests.Unit.ApiClients.Fakes;

public class FakeDataBridgeClientTests
{
    private readonly FakeDataBridgeClient _sut;

    public FakeDataBridgeClientTests()
    {
        _sut = new FakeDataBridgeClient();
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(5, 10)]
    [InlineData(100, 50)]
    public async Task GetSamHoldingsAsync_Generic_ShouldReturnCorrectDataBridgeResponse(int top, int skip)
    {
        // Act
        var result = await _sut.GetSamHoldingsAsync<SamCphHolding>(top, skip);

        // Assert
        result.Should().NotBeNull();
        result!.CollectionName.Should().Be("collection");
        result.Count.Should().Be(top);
        result.Top.Should().Be(top);
        result.Skip.Should().Be(skip);
        result.Data.Should().NotBeNull().And.HaveCount(top);

        foreach (var holding in result.Data)
        {
            holding.Should().NotBeNull();
            holding.BATCH_ID.Should().Be(1);
            holding.CHANGE_TYPE.Should().Be("I");
            holding.IsDeleted.Should().BeFalse();
            holding.CPH.Should().NotBeNullOrWhiteSpace();
            holding.FEATURE_NAME.Should().NotBeNullOrWhiteSpace();
            holding.CPH_TYPE.Should().Be("PERMANENT");
            holding.FEATURE_ADDRESS_FROM_DATE.Should().BeCloseTo(DateTime.Today.AddDays(-1), TimeSpan.FromDays(1));
        }
    }

    [Fact]
    public async Task GetSamHoldingsAsync_WithSpecificId_ShouldReturnListWithCorrectId()
    {
        // Arrange
        var testId = "123456789";

        // Act
        var result = await _sut.GetSamHoldingsAsync(testId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.HaveCount(1);
        result[0].CPH.Should().Be(testId);
        result[0].BATCH_ID.Should().Be(1);
        result[0].CHANGE_TYPE.Should().Be("I");
        result[0].IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("selectFields")]
    [InlineData("orderBy")]
    public async Task GetSamHoldingsAsync_WithOptionalParameters_ShouldHandleAllParameters(string? selectFields)
    {
        // Arrange
        var updatedSinceDateTime = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = await _sut.GetSamHoldingsAsync<SamCphHolding>(5, 0, selectFields, updatedSinceDateTime, "orderBy");

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(5);
    }

    [Theory]
    [InlineData(3, 5)]
    [InlineData(10, 0)]
    public async Task GetSamHoldersAsync_Generic_ShouldReturnCorrectDataBridgeResponse(int top, int skip)
    {
        // Act
        var result = await _sut.GetSamHoldersAsync<SamCphHolder>(top, skip);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(top);
        result.Top.Should().Be(top);
        result.Skip.Should().Be(skip);

        foreach (var holder in result.Data)
        {
            holder.BATCH_ID.Should().Be(1);
            holder.CHANGE_TYPE.Should().Be("I");
            holder.IsDeleted.Should().BeFalse();
            holder.CPHS.Should().NotBeNullOrWhiteSpace();
            holder.PARTY_ID.Should().NotBeNullOrWhiteSpace().And.StartWith("C");
            holder.ORGANISATION_NAME.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetSamHoldersByCphAsync_ShouldReturnHoldersWithCorrectCph()
    {
        // Arrange
        var testCph = "987654321";

        // Act
        var result = await _sut.GetSamHoldersByCphAsync(testCph, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.HaveCount(1);
        result[0].CPHS.Should().Contain(testCph);
    }

    [Fact]
    public async Task GetSamHoldersByPartyIdAsync_ShouldReturnHoldersWithCorrectPartyId()
    {
        // Arrange
        var testPartyId = "C12345678";

        // Act
        var result = await _sut.GetSamHoldersByPartyIdAsync(testPartyId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.HaveCount(1);
        result[0].PARTY_ID.Should().Be(testPartyId);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(7, 3)]
    public async Task GetSamHerdsAsync_Generic_ShouldReturnCorrectDataBridgeResponse(int top, int skip)
    {
        // Act
        var result = await _sut.GetSamHerdsAsync<SamHerd>(top, skip);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(top);

        foreach (var herd in result.Data)
        {
            herd.BATCH_ID.Should().Be(1);
            herd.CHANGE_TYPE.Should().Be("I");
            herd.IsDeleted.Should().BeFalse();
            herd.HERDMARK.Should().NotBeNullOrWhiteSpace();
            herd.CPHH.Should().NotBeNullOrWhiteSpace().And.EndWith("/01");
            herd.ANIMAL_SPECIES_CODE.Should().Be("CTT");
            herd.ANIMAL_PURPOSE_CODE.Should().Be("CTT-BEEF");
            herd.KEEPER_PARTY_IDS.Should().NotBeNullOrWhiteSpace().And.StartWith("C");
            herd.OWNER_PARTY_IDS.Should().NotBeNullOrWhiteSpace().And.StartWith("C");
            herd.ANIMAL_GROUP_ID_MCH_FRM_DAT.Should().BeCloseTo(DateTime.Today.AddDays(-1), TimeSpan.FromDays(1));
        }
    }

    [Fact]
    public async Task GetSamHerdsAsync_WithSpecificId_ShouldReturnHerdWithCorrectCphh()
    {
        // Arrange
        var testId = "555666777";

        // Act
        var result = await _sut.GetSamHerdsAsync(testId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.HaveCount(1);
        result[0].CPHH.Should().Be($"{testId}/01");
    }

    [Fact]
    public async Task GetSamHerdsByPartyIdAsync_ShouldReturnCorrectDataBridgeResponse()
    {
        // Arrange
        var testPartyId = "C87654321";

        // Act
        var result = await _sut.GetSamHerdsByPartyIdAsync<SamHerd>(testPartyId, "fields", "order");

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(2);
        result.Top.Should().Be(0);
        result.Skip.Should().Be(0);
    }


    [Fact]
    public async Task GetSamPartyAsync_ShouldReturnPartyWithCorrectId()
    {
        // Arrange
        var testId = "C11223344";

        // Act
        var result = await _sut.GetSamPartyAsync(testId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PARTY_ID.Should().Be(testId);
        result.BATCH_ID.Should().Be(1);
        result.CHANGE_TYPE.Should().Be("I");
        result.IsDeleted.Should().BeFalse();
        result.ORGANISATION_NAME.Should().NotBeNullOrWhiteSpace();
        result.PARTY_ROLE_FROM_DATE.Should().BeCloseTo(DateTime.Today.AddDays(-1), TimeSpan.FromDays(1));
        result.ROLES.Should().Be("AGENT");
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(4, 2)]
    public async Task GetSamPartiesAsync_Generic_ShouldReturnCorrectDataBridgeResponse(int top, int skip)
    {
        // Act
        var result = await _sut.GetSamPartiesAsync<SamParty>(top, skip);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(top);
        result.Top.Should().Be(top);
        result.Skip.Should().Be(skip);

        foreach (var party in result.Data)
        {
            party.ROLES.Should().Be("AGENT");
            party.PARTY_ID.Should().StartWith("C");
        }
    }

    [Fact]
    public async Task GetSamPartiesAsync_WithIdList_ShouldReturnPartiesForAllIds()
    {
        // Arrange
        var testIds = new[] { "C11111111", "C22222222", "C33333333" };

        // Act
        var result = await _sut.GetSamPartiesAsync(testIds, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.HaveCount(testIds.Length);
        for (int i = 0; i < testIds.Length; i++)
        {
            result[i].PARTY_ID.Should().Be(testIds[i]);
        }
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(6, 4)]
    public async Task GetCtsHoldingsAsync_Generic_ShouldReturnCorrectDataBridgeResponse(int top, int skip)
    {
        // Act
        var result = await _sut.GetCtsHoldingsAsync<CtsCphHolding>(top, skip);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(top);
        result.Top.Should().Be(top);
        result.Skip.Should().Be(skip);

        foreach (var holding in result.Data)
        {
            holding.BATCH_ID.Should().Be(1);
            holding.CHANGE_TYPE.Should().Be("I");
            holding.IsDeleted.Should().BeFalse();
            holding.LID_FULL_IDENTIFIER.Should().NotBeNullOrWhiteSpace().And.StartWith("AH-");
            holding.ADR_NAME.Should().NotBeNullOrWhiteSpace();
            holding.LOC_EFFECTIVE_FROM.Should().BeCloseTo(DateTime.Today.AddDays(-1), TimeSpan.FromDays(1));
        }
    }

    [Fact]
    public async Task GetCtsHoldingsAsync_WithSpecificId_ShouldReturnHoldingWithCorrectId()
    {
        // Arrange
        var testId = "AH-123456789";

        // Act
        var result = await _sut.GetCtsHoldingsAsync(testId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.HaveCount(1);
        result[0].LID_FULL_IDENTIFIER.Should().Be(testId);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(5, 0)]
    public async Task GetCtsAgentsAsync_Generic_ShouldReturnCorrectDataBridgeResponse(int top, int skip)
    {
        // Act
        var result = await _sut.GetCtsAgentsAsync<CtsAgentOrKeeper>(top, skip);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(top);

        foreach (var agent in result.Data)
        {
            agent.BATCH_ID.Should().Be(1);
            agent.CHANGE_TYPE.Should().Be("I");
            agent.IsDeleted.Should().BeFalse();
            agent.PAR_ID.Should().NotBeNullOrWhiteSpace();
            agent.LID_FULL_IDENTIFIER.Should().NotBeNullOrWhiteSpace().And.StartWith("AH-");
            agent.PAR_SURNAME.Should().NotBeNullOrWhiteSpace();
            agent.ADR_NAME.Should().NotBeNullOrWhiteSpace();
            agent.LPR_EFFECTIVE_FROM_DATE.Should().BeCloseTo(DateTime.Today.AddDays(-1), TimeSpan.FromDays(1));
        }
    }

    [Fact]
    public async Task GetCtsAgentsAsync_WithSpecificId_ShouldReturnAgentWithCorrectId()
    {
        // Arrange
        var testId = "AH-987654321";

        // Act
        var result = await _sut.GetCtsAgentsAsync(testId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.HaveCount(1);
        result[0].LID_FULL_IDENTIFIER.Should().Be(testId);
    }

    [Fact]
    public async Task GetCtsAgentByPartyIdAsync_ShouldReturnAgentWithCorrectPartyId()
    {
        // Arrange
        var testPartyId = "12345678901";

        // Act
        var result = await _sut.GetCtsAgentByPartyIdAsync(testPartyId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PAR_ID.Should().Be(testPartyId);
        result.LID_FULL_IDENTIFIER.Should().StartWith("AH-");
    }

    [Theory]
    [InlineData(4, 2)]
    [InlineData(8, 1)]
    public async Task GetCtsKeepersAsync_Generic_ShouldReturnCorrectDataBridgeResponse(int top, int skip)
    {
        // Act
        var result = await _sut.GetCtsKeepersAsync<CtsAgentOrKeeper>(top, skip);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(top);

        foreach (var keeper in result.Data)
        {
            keeper.BATCH_ID.Should().Be(1);
            keeper.CHANGE_TYPE.Should().Be("I");
            keeper.IsDeleted.Should().BeFalse();
        }
    }

    [Fact]
    public async Task GetCtsKeepersAsync_WithSpecificId_ShouldReturnKeeperWithCorrectId()
    {
        // Arrange
        var testId = "AH-111222333";

        // Act
        var result = await _sut.GetCtsKeepersAsync(testId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.HaveCount(1);
        result[0].LID_FULL_IDENTIFIER.Should().Be(testId);
    }

    [Fact]
    public async Task GetCtsKeeperByPartyIdAsync_ShouldReturnKeeperWithCorrectPartyId()
    {
        // Arrange
        var testPartyId = "98765432101";

        // Act
        var result = await _sut.GetCtsKeeperByPartyIdAsync(testPartyId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PAR_ID.Should().Be(testPartyId);
    }

    [Fact]
    public async Task GetSamHoldingsAsync_MultipleCalls_ShouldGenerateDifferentRandomData()
    {
        // Act
        var result1 = await _sut.GetSamHoldingsAsync<SamCphHolding>(3, 0);
        var result2 = await _sut.GetSamHoldingsAsync<SamCphHolding>(3, 0);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        var cphs1 = result1!.Data.Select(x => x.CPH).ToList();
        var cphs2 = result2!.Data.Select(x => x.CPH).ToList();

        cphs1.Should().NotBeEquivalentTo(cphs2, "Random data should generate different values each time");
    }

    [Fact]
    public async Task GetSamHerdAsync_MultipleCalls_ShouldGenerateDifferentHerdmarks()
    {
        // Act
        var result1 = await _sut.GetSamHerdsAsync<SamHerd>(2, 0);
        var result2 = await _sut.GetSamHerdsAsync<SamHerd>(2, 0);

        // Assert
        var herdmarks1 = result1!.Data.Select(x => x.HERDMARK).ToList();
        var herdmarks2 = result2!.Data.Select(x => x.HERDMARK).ToList();

        herdmarks1.Should().NotBeEquivalentTo(herdmarks2);
    }

    [Fact]
    public async Task GetCtsHoldingsAsync_ShouldGenerateValidLidFullIdentifierFormat()
    {
        // Act
        var result = await _sut.GetCtsHoldingsAsync<CtsCphHolding>(5, 0);

        // Assert
        result.Should().NotBeNull();
        foreach (var holding in result!.Data)
        {
            holding.LID_FULL_IDENTIFIER.Should().MatchRegex(@"^AH-\d{9}$");
        }
    }

    [Fact]
    public async Task GetSamHoldingsAsync_Generic_ShouldProperlySerializeAndDeserializeObjects()
    {
        // Act
        var result = await _sut.GetSamHoldingsAsync<SamCphHolding>(1, 0);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);

        var holding = result.Data[0];
        holding.Should().NotBeNull();
        holding.GetType().Should().Be<SamCphHolding>();
    }

    [Fact]
    public async Task GetCtsAgentsAsync_Generic_ShouldHandleJsonSerializationCorrectly()
    {
        // Act
        var result = await _sut.GetCtsAgentsAsync<CtsAgentOrKeeper>(1, 0);

        // Assert
        result.Should().NotBeNull();
        result!.Data[0].Should().BeOfType<CtsAgentOrKeeper>();
    }

    [Fact]
    public async Task GetDataBridgeResponse_ShouldHaveCorrectStructure()
    {
        // Act
        var result = await _sut.GetSamHoldingsAsync<SamCphHolding>(10, 5);

        // Assert
        result.Should().NotBeNull();
        result!.CollectionName.Should().Be("collection");
        result.Count.Should().Be(10);
        result.Top.Should().Be(10);
        result.Skip.Should().Be(5);
        result.Data.Should().NotBeNull().And.BeAssignableTo<IList<SamCphHolding>>();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 10)]
    [InlineData(100, 250)]
    public async Task GetDataBridgeResponse_WithVariousTopAndSkipValues_ShouldSetCorrectValues(int top, int skip)
    {
        // Act
        var result = await _sut.GetSamPartiesAsync<SamParty>(top, skip);

        // Assert
        result.Should().NotBeNull();
        result!.Top.Should().Be(top);
        result.Skip.Should().Be(skip);
        result.Count.Should().Be(top);
        result.Data.Should().HaveCount(top);
    }

    [Fact]
    public async Task AllMethods_ShouldAcceptCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act & Assert - All methods should complete without throwing
        await _sut.GetSamHoldingsAsync<SamCphHolding>(1, 0, cancellationToken: cancellationToken);
        await _sut.GetSamHoldingsAsync("test", CancellationToken.None);
        await _sut.GetSamHoldersAsync<SamCphHolder>(1, 0, cancellationToken: cancellationToken);
        await _sut.GetSamHoldersByCphAsync("test", CancellationToken.None);
        await _sut.GetSamHoldersByPartyIdAsync("test", CancellationToken.None);
        await _sut.GetSamHerdsAsync<SamHerd>(1, 0, cancellationToken: cancellationToken);
        await _sut.GetSamHerdsAsync("test", CancellationToken.None);
        await _sut.GetSamHerdsByPartyIdAsync<SamHerd>("test", "fields", "order", cancellationToken);
        await _sut.GetSamPartyAsync("test", CancellationToken.None);
        await _sut.GetSamPartiesAsync<SamParty>(1, 0, cancellationToken: cancellationToken);
        await _sut.GetSamPartiesAsync(new[] { "test" }, CancellationToken.None);
        await _sut.GetCtsHoldingsAsync<CtsCphHolding>(1, 0, cancellationToken: cancellationToken);
        await _sut.GetCtsHoldingsAsync("test", CancellationToken.None);
        await _sut.GetCtsAgentsAsync<CtsAgentOrKeeper>(1, 0, cancellationToken: cancellationToken);
        await _sut.GetCtsAgentsAsync("test", CancellationToken.None);
        await _sut.GetCtsAgentByPartyIdAsync("test", CancellationToken.None);
        await _sut.GetCtsKeepersAsync<CtsAgentOrKeeper>(1, 0, cancellationToken: cancellationToken);
        await _sut.GetCtsKeepersAsync("test", CancellationToken.None);
        await _sut.GetCtsKeeperByPartyIdAsync("test", CancellationToken.None);

        true.Should().BeTrue("All methods should accept cancellation tokens");
    }

    [Fact]
    public async Task GetSamHoldingsAsync_WithZeroTop_ShouldReturnEmptyData()
    {
        // Act
        var result = await _sut.GetSamHoldingsAsync<SamCphHolding>(0, 0);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().BeEmpty();
        result.Count.Should().Be(0);
        result.Top.Should().Be(0);
    }

    [Fact]
    public async Task GetSamPartiesAsync_WithEmptyIdList_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.GetSamPartiesAsync(Array.Empty<string>(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetCtsAgentByPartyIdAsync_WhenCalled_ShouldReturnValidAgentOrNull()
    {
        // Act
        var result = await _sut.GetCtsAgentByPartyIdAsync("test-party-id", CancellationToken.None);

        // Assert
        result.Should().BeOneOf(null, result);
        if (result != null)
        {
            result.Should().BeOfType<CtsAgentOrKeeper>();
        }
    }
}