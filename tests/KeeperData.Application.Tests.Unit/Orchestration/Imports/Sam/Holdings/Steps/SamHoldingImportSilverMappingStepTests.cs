using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Holdings.Steps;

public class SamHoldingImportSilverMappingStepTests
{
    private readonly Mock<ISiteActivityTypeLookupService> _siteActivityTypeLookupServiceMock = new();
    private readonly Mock<ISiteTypeLookupService> _siteTypeLookupServiceMock = new();
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();
    private readonly Mock<IProductionUsageLookupService> _productionUsageLookupServiceMock = new();
    private readonly Mock<ISpeciesTypeLookupService> _speciesTypeLookupServiceMock = new();
    private readonly Mock<ILogger<SamHoldingImportSilverMappingStep>> _loggerMock = new();
    private readonly SamHoldingImportSilverMappingStep _sut;

    public SamHoldingImportSilverMappingStepTests()
    {
        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null, null));

        _sut = new SamHoldingImportSilverMappingStep(
            _siteActivityTypeLookupServiceMock.Object,
            _siteTypeLookupServiceMock.Object,
            _roleTypeLookupServiceMock.Object,
            _countryIdentifierLookupServiceMock.Object,
            _productionUsageLookupServiceMock.Object,
            _speciesTypeLookupServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GivenRawPortsExist_WhenExecuting_ShouldAddPortsToSilverHoldings()
    {
        // Arrange
        var cph = "12/345/6789";
        var context = new SamHoldingImportContext
        {
            Cph = cph,
            RawPorts =
            [
                new SamPort { CPH = cph, PREMISES_NAME = "Test Port" }
            ]
        };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.SilverHoldings.Should().HaveCount(1);
        context.SilverHoldings[0].CphTypeIdentifier.Should().Be(HoldingIdentifierType.PRTN.ToString());
        context.SilverHoldings[0].CountyParishHoldingNumber.Should().Be(cph);
    }

    [Fact]
    public async Task GivenMultipleRawPorts_WhenExecuting_ShouldAddAllPortsToSilverHoldings()
    {
        // Arrange
        var cph = "12/345/6789";
        var context = new SamHoldingImportContext
        {
            Cph = cph,
            RawPorts =
            [
                new SamPort { CPH = cph, PREMISES_NAME = "Port A" },
                new SamPort { CPH = cph, PREMISES_NAME = "Port B" }
            ]
        };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.SilverHoldings.Should().HaveCount(2);
        context.SilverHoldings.Should().AllSatisfy(h =>
            h.CphTypeIdentifier.Should().Be(HoldingIdentifierType.PRTN.ToString()));
    }

    [Fact]
    public async Task GivenNoRawPorts_WhenExecuting_ShouldNotAddPortsToSilverHoldings()
    {
        // Arrange
        var context = new SamHoldingImportContext
        {
            Cph = "12/345/6789",
            RawPorts = []
        };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.SilverHoldings.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenRawPortsExist_WhenExecuting_ShouldLogMappedPortCount()
    {
        // Arrange
        var cph = "12/345/6789";
        var context = new SamHoldingImportContext
        {
            Cph = cph,
            RawPorts =
            [
                new SamPort { CPH = cph, PREMISES_NAME = "Test Port" }
            ]
        };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Mapped") && v.ToString()!.Contains("port") && v.ToString()!.Contains(cph)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenNoRawPorts_WhenExecuting_ShouldNotLogPortMapping()
    {
        // Arrange
        var context = new SamHoldingImportContext
        {
            Cph = "12/345/6789",
            RawPorts = []
        };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Mapped") && v.ToString()!.Contains("port")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
