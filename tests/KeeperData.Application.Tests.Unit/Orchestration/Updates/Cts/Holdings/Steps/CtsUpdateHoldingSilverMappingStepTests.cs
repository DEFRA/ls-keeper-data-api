using FluentAssertions;
using KeeperData.Application.Orchestration.Updates.Cts.Holdings;
using KeeperData.Application.Orchestration.Updates.Cts.Holdings.Steps;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Updates.Cts.Holdings.Steps;

public class CtsUpdateHoldingSilverMappingStepTests
{
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ILogger<CtsUpdateHoldingSilverMappingStep>> _loggerMock = new();
    private readonly CtsUpdateHoldingSilverMappingStep _sut;

    public CtsUpdateHoldingSilverMappingStepTests()
    {
        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid.NewGuid().ToString(), "RoleName"));

        _sut = new CtsUpdateHoldingSilverMappingStep(_roleTypeLookupServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldMapRawDataToSilver_WhenRawDataExists()
    {
        // Arrange
        var cph = CphGenerator.GenerateCtsFormattedLidIdentifier("AH");
        var factory = new MockCtsRawDataFactory();

        var context = new CtsUpdateHoldingContext
        {
            Cph = cph,
            CurrentDateTime = DateTime.UtcNow,
            RawHolding = factory.CreateMockHolding("I", 1, cph),
            RawAgents = [factory.CreateMockAgentOrKeeper("I", 1, cph)],
            RawKeepers = [factory.CreateMockAgentOrKeeper("I", 1, cph)]
        };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.SilverHolding.Should().NotBeNull();
        context.SilverHolding!.CountyParishHoldingNumber.Should().Be(context.CphTrimmed);

        context.SilverParties.Should().HaveCount(2); // 1 Agent + 1 Keeper
        context.SilverPartyRoles.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldDoNothing_WhenRawDataIsNull()
    {
        // Arrange
        var context = new CtsUpdateHoldingContext
        {
            Cph = "AH-EMPTY",
            CurrentDateTime = DateTime.UtcNow,
            RawHolding = null,
            RawAgents = [],
            RawKeepers = []
        };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.SilverHolding.Should().BeNull();
        context.SilverParties.Should().BeEmpty();
        context.SilverPartyRoles.Should().BeEmpty();
    }
}