using FluentAssertions;
using KeeperData.Application.Orchestration.Updates.Cts.Keepers;
using KeeperData.Application.Orchestration.Updates.Cts.Keepers.Steps;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Updates.Cts.Keepers.Steps;

public class CtsUpdateKeeperSilverMappingStepTests
{
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ILogger<CtsUpdateKeeperSilverMappingStep>> _loggerMock = new();
    private readonly CtsUpdateKeeperSilverMappingStep _sut;

    public CtsUpdateKeeperSilverMappingStepTests()
    {
        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid.NewGuid().ToString(), "Keeper"));

        _sut = new CtsUpdateKeeperSilverMappingStep(_roleTypeLookupServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldMapRawDataToSilver()
    {
        var factory = new MockCtsRawDataFactory();
        var keeper = factory.CreateMockAgentOrKeeper("I", 1, CphGenerator.GenerateFormattedCph());

        var context = new CtsUpdateKeeperContext
        {
            PartyId = keeper.PAR_ID,
            CurrentDateTime = DateTime.UtcNow,
            RawKeeper = keeper
        };

        await _sut.ExecuteAsync(context, CancellationToken.None);

        context.SilverParty.Should().NotBeNull();
        context.SilverParty!.PartyId.Should().Be(keeper.PAR_ID);
        context.SilverPartyRoles.Should().ContainSingle();
    }
}