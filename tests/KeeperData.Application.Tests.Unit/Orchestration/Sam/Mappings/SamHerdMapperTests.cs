using FluentAssertions;
using KeeperData.Application.Orchestration.Sam.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Mappings;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Sam.Mappings;

public class SamHerdMapperTests
{
    private readonly Mock<IProductionUsageLookupService> _productionUsageLookupServiceMock = new();
    // private readonly Mock<IProductionTypeLookupService> _productionTypeLookupServiceMock = new();
    private readonly Mock<ISpeciesTypeLookupService> _speciesTypeLookupServiceMock = new();

    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveProductionUsage;
    // private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveProductionType;
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveSpeciesType;

    public SamHerdMapperTests()
    {
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
    public async Task GivenNullableRawHerds_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamHerdMapper.ToSilver(
            DateTime.UtcNow,
            null!,
            _resolveProductionUsage,
            // _resolveProductionType,
            _resolveSpeciesType,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public async Task GivenEmptyRawHerds_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamHerdMapper.ToSilver(
            DateTime.UtcNow,
            [],
            _resolveProductionUsage,
            // _resolveProductionType,
            _resolveSpeciesType,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public async Task GivenFindProductionUsageDoesNotMatch_WhenCallingToSilver_ShouldReturnEmptyProductionUsageDetails()
    {
        _productionUsageLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));

        var records = GenerateSamHerds(quantityHerds: 1, quantityParties: 2);

        var results = await SamHerdMapper.ToSilver(
            DateTime.UtcNow,
            records,
            _resolveProductionUsage,
            // _resolveProductionType,
            _resolveSpeciesType,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(1);

        var herd = results[0];

        herd.Should().NotBeNull();
        herd.ProductionUsageCode.Should().Be(records[0].AnimalPurposeCodeUnwrapped);
        herd.ProductionUsageId.Should().BeNull();
    }

    [Fact]
    public async Task GivenFindSpeciesTypeDoesNotMatch_WhenCallingToSilver_ShouldReturnEmptySpeciesTypeDetails()
    {
        _speciesTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));

        var records = GenerateSamHerds(quantityHerds: 1, quantityParties: 2);

        var results = await SamHerdMapper.ToSilver(
            DateTime.UtcNow,
            records,
            _resolveProductionUsage,
            // _resolveProductionType,
            _resolveSpeciesType,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(1);

        var herd = results[0];

        herd.Should().NotBeNull();
        herd.SpeciesTypeCode.Should().Be(records[0].AnimalSpeciesCodeUnwrapped);
        herd.SpeciesTypeId.Should().BeNull();
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [InlineData(2, 4)]
    public async Task GivenRawHerds_WhenCallingToSilver_ShouldReturnPopulatedList(int quantityHerds, int quantityParties)
    {
        var records = GenerateSamHerds(quantityHerds, quantityParties);

        var results = await SamHerdMapper.ToSilver(
            DateTime.UtcNow,
            records,
            _resolveProductionUsage,
            // _resolveProductionType,
            _resolveSpeciesType,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(quantityHerds);

        for (var i = 0; i < quantityHerds; i++)
        {
            VerifySamHerdMappings.VerifyMapping_From_SamHerd_To_SamHerdDocument(records[i], results[i]);
        }
    }

    private static List<SamHerd> GenerateSamHerds(int quantityHerds, int quantityParties)
    {
        var factory = new MockSamRawDataFactory();

        var holdingIdentifier = CphGenerator.GenerateFormattedCph();

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