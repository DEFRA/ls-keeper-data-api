using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Infrastructure.ApiClients.Decorators;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace KeeperData.Infrastructure.Tests.Unit.ApiClients;

public class DataBridgeClientAnonymizerPortsTests
{
    private readonly IDataBridgeClient _innerClient = Substitute.For<IDataBridgeClient>();
    private readonly ILogger<DataBridgeClientAnonymizer> _logger = Substitute.For<ILogger<DataBridgeClientAnonymizer>>();
    private readonly DataBridgeClientAnonymizer _sut;

    public DataBridgeClientAnonymizerPortsTests()
    {
        _sut = new DataBridgeClientAnonymizer(_innerClient, _logger);
    }

    [Fact]
    public async Task GetSamPortsAsync_ShouldAnonymizeLocationAndAddressFields()
    {
        var port = new SamPort
        {
            CPH = "12/345/6789",
            PREMISES_NAME = "Harbour Port Terminal",
            ADDRESS_LINE_1 = "10 Port Street",
            ADDRESS_LINE_2 = "Harbour District",
            ADDRESS_LINE_3 = "Liverpool",
            POSTCODE = "L1 8JQ",
            MAP_REFERENCE = "SJ34509055",
            EASTING = 334509,
            NORTHING = 390550
        };

        _innerClient.GetSamPortsAsync("12/345/6789", Arg.Any<CancellationToken>())
            .Returns([port]);

        var result = await _sut.GetSamPortsAsync("12/345/6789", CancellationToken.None);

        result.Should().HaveCount(1);
        var p = result[0];
        p.CPH.Should().Be("12/345/6789"); // CPH should NOT be anonymized
        p.PREMISES_NAME.Should().NotBe("Harbour Port Terminal").And.NotBeNullOrWhiteSpace();
        p.ADDRESS_LINE_1.Should().NotBe("10 Port Street").And.NotBeNullOrWhiteSpace();
        p.ADDRESS_LINE_2.Should().NotBe("Harbour District").And.NotBeNullOrWhiteSpace();
        p.ADDRESS_LINE_3.Should().NotBe("Liverpool").And.NotBeNullOrWhiteSpace();
        p.POSTCODE.Should().NotBe("L1 8JQ").And.NotBeNullOrWhiteSpace().And.MatchRegex(@"^[A-Z]{2}\d \d[A-Z]{2}$");
        p.MAP_REFERENCE.Should().NotBe("SJ34509055").And.MatchRegex("^[A-Z]{2}[0-9]{8}$");
        p.EASTING.Should().BeInRange(100000, 999999).And.NotBe(334509);
        p.NORTHING.Should().BeInRange(200000, 999999).And.NotBe(390550);
    }

    [Fact]
    public async Task GetSamPortsAsync_ShouldNotAnonymizeNullFields()
    {
        var port = new SamPort
        {
            CPH = "12/345/6789",
            PREMISES_NAME = null,
            ADDRESS_LINE_1 = null,
            ADDRESS_LINE_2 = null,
            ADDRESS_LINE_3 = null,
            POSTCODE = null,
            MAP_REFERENCE = null,
            EASTING = null,
            NORTHING = null
        };

        _innerClient.GetSamPortsAsync("12/345/6789", Arg.Any<CancellationToken>())
            .Returns([port]);

        var result = await _sut.GetSamPortsAsync("12/345/6789", CancellationToken.None);

        var p = result[0];
        p.PREMISES_NAME.Should().BeNull();
        p.ADDRESS_LINE_1.Should().BeNull();
        p.ADDRESS_LINE_2.Should().BeNull();
        p.ADDRESS_LINE_3.Should().BeNull();
        p.POSTCODE.Should().BeNull();
        p.MAP_REFERENCE.Should().BeNull();
        p.EASTING.Should().BeNull();
        p.NORTHING.Should().BeNull();
    }

    [Fact]
    public async Task GetSamPortsAsync_WithMultiplePorts_ShouldAnonymizeAll()
    {
        var ports = new List<SamPort>
        {
            new() { CPH = "12/345/6789", PREMISES_NAME = "Port A", POSTCODE = "L1 8JQ" },
            new() { CPH = "12/345/6789", PREMISES_NAME = "Port B", POSTCODE = "L2 9ZX" }
        };

        _innerClient.GetSamPortsAsync("12/345/6789", Arg.Any<CancellationToken>())
            .Returns(ports);

        var result = await _sut.GetSamPortsAsync("12/345/6789", CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].PREMISES_NAME.Should().NotBe("Port A");
        result[0].POSTCODE.Should().NotBe("L1 8JQ");
        result[1].PREMISES_NAME.Should().NotBe("Port B");
        result[1].POSTCODE.Should().NotBe("L2 9ZX");
    }

    [Fact]
    public async Task GetSamPortsAsync_ShouldBeDeterministic_SameCphProducesSameOutput()
    {
        var port1 = new SamPort
        {
            CPH = "12/345/6789",
            PREMISES_NAME = "Original Port",
            POSTCODE = "L1 8JQ"
        };

        var port2 = new SamPort
        {
            CPH = "12/345/6789",
            PREMISES_NAME = "Original Port",
            POSTCODE = "L1 8JQ"
        };

        _innerClient.GetSamPortsAsync("12/345/6789", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new List<SamPort> { port1 }),
                     _ => Task.FromResult(new List<SamPort> { port2 }));

        var result1 = await _sut.GetSamPortsAsync("12/345/6789", CancellationToken.None);
        var result2 = await _sut.GetSamPortsAsync("12/345/6789", CancellationToken.None);

        result1[0].PREMISES_NAME.Should().Be(result2[0].PREMISES_NAME);
        result1[0].POSTCODE.Should().Be(result2[0].POSTCODE);
        result1[0].MAP_REFERENCE.Should().Be(result2[0].MAP_REFERENCE);
        result1[0].EASTING.Should().Be(result2[0].EASTING);
        result1[0].NORTHING.Should().Be(result2[0].NORTHING);
    }

    [Fact]
    public async Task GetSamPortsAsync_ShouldPreserveNonPiiFields()
    {
        var port = new SamPort
        {
            CPH = "12/345/6789",
            BATCH_ID = 42,
            CHANGE_TYPE = "I",
            IsDeleted = false,
            PREMISES_NAME = "Real Port Name",
            POSTCODE = "L1 8JQ"
        };

        _innerClient.GetSamPortsAsync("12/345/6789", Arg.Any<CancellationToken>())
            .Returns([port]);

        var result = await _sut.GetSamPortsAsync("12/345/6789", CancellationToken.None);

        var p = result[0];
        p.CPH.Should().Be("12/345/6789");
        p.BATCH_ID.Should().Be(42);
        p.CHANGE_TYPE.Should().Be("I");
        p.IsDeleted.Should().BeFalse();
        p.PREMISES_NAME.Should().NotBe("Real Port Name");
        p.POSTCODE.Should().NotBe("L1 8JQ");
    }

    [Fact]
    public async Task GenericGetSamPortsAsync_ShouldAnonymize()
    {
        var response = new DataBridgeResponse<SamPort>
        {
            CollectionName = "ports",
            Count = 1,
            Data = [new SamPort { CPH = "12/345/6789", PREMISES_NAME = "Secret Port", POSTCODE = "L1 8JQ" }]
        };

        _innerClient.GetSamPortsAsync<SamPort>(10, 0, null, null, null, Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.GetSamPortsAsync<SamPort>(10, 0, cancellationToken: CancellationToken.None);

        result.Should().NotBeNull();
        result!.Data[0].PREMISES_NAME.Should().NotBe("Secret Port");
        result.Data[0].POSTCODE.Should().NotBe("L1 8JQ");
    }

    [Fact]
    public async Task GetSamPortsAsync_WhenAnonymizationFails_ShouldLogErrorAndContinue()
    {
        var goodPort = new SamPort { CPH = "12/345/6789", PREMISES_NAME = "Good Port" };
        var badPort = new SamPort { CPH = null!, PREMISES_NAME = "Bad Port" };

        _innerClient.GetSamPortsAsync("12/345/6789", Arg.Any<CancellationToken>())
            .Returns([badPort, goodPort]);

        var result = await _sut.GetSamPortsAsync("12/345/6789", CancellationToken.None);

        result.Should().HaveCount(2);
        result[1].PREMISES_NAME.Should().NotBe("Good Port");
    }
}