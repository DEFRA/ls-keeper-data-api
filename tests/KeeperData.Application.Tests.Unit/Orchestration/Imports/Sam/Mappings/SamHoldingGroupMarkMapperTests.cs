using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Mappings;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SamHoldingGroupMarkMapperTests
{
    // Holdings
    private readonly Mock<IPremiseActivityTypeLookupService> _premiseActivityTypeLookupServiceMock = new();
    private readonly Mock<IPremiseTypeLookupService> _premiseTypeLookupServiceMock = new();
    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();
    private readonly Mock<IActivityCodeLookupService> _activityCodeLookupService = new();

    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolvePremiseActivityType;
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolvePremiseType;
    private readonly Func<string?, string?, CancellationToken, Task<(string?, string?, string?)>> _resolveCountry;

    // Herds
    private readonly Mock<IProductionUsageLookupService> _productionUsageLookupServiceMock = new();
    // private readonly Mock<IProductionTypeLookupService> _productionTypeLookupServiceMock = new();
    private readonly Mock<ISpeciesTypeLookupService> _speciesTypeLookupServiceMock = new();

    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveProductionUsage;
    // private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveProductionType;
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveSpeciesType;

    public SamHoldingGroupMarkMapperTests()
    {
        // Holdings
        _premiseActivityTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _premiseTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, string? internalCode, CancellationToken token) => (Guid.NewGuid().ToString(), input, input));

        _resolvePremiseActivityType = _premiseActivityTypeLookupServiceMock.Object.FindAsync;
        _resolvePremiseType = _premiseTypeLookupServiceMock.Object.FindAsync;
        _resolveCountry = _countryIdentifierLookupServiceMock.Object.FindAsync;

        // Herds
        _productionUsageLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _speciesTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _resolveProductionUsage = _productionUsageLookupServiceMock.Object.FindAsync;
        // _resolveProductionType = _productionTypeLookupServiceMock.Object.FindAsync;
        _resolveSpeciesType = _speciesTypeLookupServiceMock.Object.FindAsync;
    }

    [Fact]
    public void GivenNullableHoldings_WhenEnrichingWithGroupMarks_ShouldReturnEmptyList()
    {
        var results = SamHoldingGroupMarkMapper.EnrichHoldingsWithGroupMarks(silverHoldings: null!,
            silverHerds: []);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public void GivenEmptyHoldings_WhenEnrichingWithGroupMarks_ShouldReturnEmptyList()
    {
        var results = SamHoldingGroupMarkMapper.EnrichHoldingsWithGroupMarks(silverHoldings: [],
            silverHerds: []);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    public async Task GivenHoldingsExist_WhenEnrichingWithGroupMarks_ShouldReturnPopulatedGroupMarks(
        int quantityHoldings,
        int quantityHerds)
    {
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();

        var rawHoldings = GenerateSamCphHolding(holdingIdentifier, quantityHoldings);
        var rawHerds = GenerateSamHerds(holdingIdentifier, quantityHerds, quantityParties: 2);

        var silverHoldings = await SamHoldingMapper.ToSilver(
            rawHoldings,
            _resolvePremiseActivityType,
            _resolvePremiseType,
            _resolveCountry,
            CancellationToken.None);

        silverHoldings.Should().NotBeNull();
        silverHoldings.Count.Should().Be(quantityHoldings);

        var silverHerds = await SamHerdMapper.ToSilver(
            rawHerds,
            _resolveProductionUsage,
            _resolveSpeciesType,
            CancellationToken.None);

        silverHerds.Should().NotBeNull();
        silverHerds.Count.Should().Be(quantityHerds);

        var results = SamHoldingGroupMarkMapper.EnrichHoldingsWithGroupMarks(
            silverHoldings,
            silverHerds);

        for (var i = 0; i < quantityHoldings; i++)
        {
            VerifySamHoldingGroupMarkMappings.VerifyMappingEnrichingWithGroupMarks(silverHerds, results[i]);
        }
    }

    private static List<SamCphHolding> GenerateSamCphHolding(string holdingIdentifier, int quantity)
    {
        var records = new List<SamCphHolding>();
        var factory = new MockSamRawDataFactory();
        for (var i = 0; i < quantity; i++)
        {
            records.Add(factory.CreateMockHolding(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: holdingIdentifier,
                endDate: DateTime.UtcNow.Date));
        }
        return records;
    }

    private static List<SamHerd> GenerateSamHerds(string holdingIdentifier, int quantityHerds, int quantityParties)
    {
        var factory = new MockSamRawDataFactory();

        var partyIds = Enumerable.Range(0, quantityParties)
            .Select(_ => Guid.NewGuid().ToString())
            .ToList();

        var records = Enumerable.Range(0, quantityHerds)
            .Select(_ => factory.CreateMockHerd(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: holdingIdentifier,
                partyIds: partyIds))
            .ToList();

        return records;
    }
}