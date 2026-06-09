using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SamPortMapperTests
{
    [Fact]
    public async Task GivenNullableRawPorts_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamPortMapper.ToSilver(
            (List<SamPort>?)null!,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenEmptyRawPorts_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamPortMapper.ToSilver(
            [],
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenRawPortsWithEmptyCph_WhenCallingToSilver_ShouldFilterOutEmptyCph()
    {
        var rawPorts = new List<SamPort>
        {
            new() { CPH = "12/345/6789", PREMISES_NAME = "Valid Port" },
            new() { CPH = "", PREMISES_NAME = "Empty CPH" },
            new() { CPH = null!, PREMISES_NAME = "Null CPH" },
            new() { CPH = "  ", PREMISES_NAME = "Whitespace CPH" }
        };

        var results = await SamPortMapper.ToSilver(
            rawPorts,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].CountyParishHoldingNumber.Should().Be("12/345/6789");
    }

    [Fact]
    public async Task GivenSingleRawPort_WhenCallingToSilver_ShouldMapAllProperties()
    {
        var rawPort = new SamPort
        {
            BATCH_ID = 42,
            CHANGE_TYPE = "I",
            CreatedAtUtc = new DateTime(2025, 1, 1, 10, 0, 0),
            UpdatedAtUtc = new DateTime(2025, 1, 2, 15, 30, 0),
            IsDeleted = false,
            CPH = "12/345/6789",
            PREMISES_NAME = "Test Port Name",
            ADDRESS_LINE_1 = "Address Line 1",
            ADDRESS_LINE_2 = "Address Line 2",
            ADDRESS_LINE_3 = "Address Line 3",
            POSTCODE = "AB12 3CD",
            MAP_REFERENCE = "SK123456",
            EASTING = 400000,
            NORTHING = 500000
        };

        var result = await SamPortMapper.ToSilver(
            rawPort,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>(("country-id", "GB", "United Kingdom")),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().BeNull();
        result.LastUpdatedBatchId.Should().Be(42);
        result.CreatedDate.Should().Be(new DateTime(2025, 1, 1, 10, 0, 0));
        result.LastUpdatedDate.Should().Be(new DateTime(2025, 1, 2, 15, 30, 0));
        result.Deleted.Should().BeFalse();
        result.CountyParishHoldingNumber.Should().Be("12/345/6789");
        result.LocationName.Should().Be("Test Port Name");
        result.CphTypeIdentifier.Should().Be(HoldingIdentifierType.PRTN.ToString());
        result.Location.Should().NotBeNull();
        result.Location!.Address.Should().NotBeNull();
        result.Location.Address!.AddressLine.Should().Be("Address Line 1");
        result.Location.Address.AddressLocality.Should().Be("Address Line 2");
        result.Location.Address.AddressStreet.Should().Be("Address Line 3");
        result.Location.Address.AddressPostCode.Should().Be("AB12 3CD");
        result.Location.OsMapReference.Should().Be("SK123456");
        result.Location.Easting.Should().Be(400000);
        result.Location.Northing.Should().Be(500000);
    }

    [Fact]
    public async Task GivenRawPortWithNullDates_WhenCallingToSilver_ShouldUseUtcNow()
    {
        var rawPort = new SamPort
        {
            CPH = "12/345/6789",
            CreatedAtUtc = null,
            UpdatedAtUtc = null
        };

        var beforeMapping = DateTime.UtcNow;
        var result = await SamPortMapper.ToSilver(
            rawPort,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);
        var afterMapping = DateTime.UtcNow;

        result.CreatedDate.Should().BeOnOrAfter(beforeMapping).And.BeOnOrBefore(afterMapping);
        result.LastUpdatedDate.Should().BeOnOrAfter(beforeMapping).And.BeOnOrBefore(afterMapping);
        result.HoldingStartDate.Should().BeOnOrAfter(beforeMapping).And.BeOnOrBefore(afterMapping);
    }

    [Fact]
    public async Task GivenRawPortWithNullIsDeleted_WhenCallingToSilver_ShouldDefaultToFalse()
    {
        var rawPort = new SamPort
        {
            CPH = "12/345/6789",
            IsDeleted = null
        };

        var result = await SamPortMapper.ToSilver(
            rawPort,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        result.Deleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task GivenMultipleRawPorts_WhenCallingToSilver_ShouldMapAll(int count)
    {
        var rawPorts = Enumerable.Range(1, count).Select(i => new SamPort
        {
            CPH = $"12/345/{6000 + i}",
            PREMISES_NAME = $"Port {i}",
            BATCH_ID = i
        }).ToList();

        var results = await SamPortMapper.ToSilver(
            rawPorts,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        results.Should().HaveCount(count);
        for (var i = 0; i < count; i++)
        {
            results[i].CountyParishHoldingNumber.Should().Be($"12/345/{6001 + i}");
            results[i].LocationName.Should().Be($"Port {i + 1}");
            results[i].LastUpdatedBatchId.Should().Be(i + 1);
        }
    }

    [Fact]
    public async Task GivenPortWithDeletedFlag_WhenCallingToSilver_ShouldPreserveDeletedFlag()
    {
        var rawPort = new SamPort
        {
            CPH = "12/345/6789",
            IsDeleted = true
        };

        var result = await SamPortMapper.ToSilver(
            rawPort,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        result.Deleted.Should().BeTrue();
    }

    [Fact]
    public async Task GivenRawPort_WhenCallingToSilver_ShouldSetCphTypeIdentifierToPRTN()
    {
        var rawPort = new SamPort
        {
            CPH = "12/345/6789",
            PREMISES_NAME = "Test Port"
        };

        var result = await SamPortMapper.ToSilver(
            rawPort,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        result.CphTypeIdentifier.Should().Be(HoldingIdentifierType.PRTN.ToString());
    }

    [Fact]
    public async Task GivenRawPort_WhenCallingToSilver_ShouldSetHoldingSpecificFieldsToNull()
    {
        var rawPort = new SamPort
        {
            CPH = "12/345/6789",
            PREMISES_NAME = "Test Port"
        };

        var result = await SamPortMapper.ToSilver(
            rawPort,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        result.SecondaryCph.Should().BeNull();
        result.CphRelationshipType.Should().BeNull();
        result.AlternativeHoldingIdentifier.Should().BeNull();
        result.SourceFacilityTypeCode.Should().BeNull();
        result.SourceFacilityBusinessActivityCode.Should().BeNull();
        result.SourceFacilitySubBusinessActivityCode.Should().BeNull();
        result.SpeciesTypeCode.Should().BeNull();
        result.ProductionUsageCodeList.Should().BeEmpty();
        result.DiseaseType.Should().BeNull();
        result.Interval.Should().BeNull();
        result.IntervalUnitOfTime.Should().BeNull();
        result.MovementRestrictionReasonCode.Should().BeNull();
    }

    [Fact]
    public async Task GivenRawPort_WhenCallingToSilver_ShouldResolveCountry()
    {
        var rawPort = new SamPort
        {
            CPH = "12/345/6789",
            PREMISES_NAME = "Test Port"
        };

        var countryId = "country-123";
        var countryCode = "GB";

        var result = await SamPortMapper.ToSilver(
            rawPort,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((countryId, countryCode, "United Kingdom")),
            CancellationToken.None);

        result.Location!.Address!.CountryIdentifier.Should().Be(countryId);
        result.Location.Address.CountryCode.Should().Be(countryCode);
    }

    [Fact]
    public async Task GivenRawPortWithNullAddress_WhenCallingToSilver_ShouldHandleNullValues()
    {
        var rawPort = new SamPort
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

        var result = await SamPortMapper.ToSilver(
            rawPort,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        result.LocationName.Should().BeNull();
        result.Location!.Address!.AddressLine.Should().BeNull();
        result.Location.Address.AddressLocality.Should().BeNull();
        result.Location.Address.AddressStreet.Should().BeNull();
        result.Location.Address.AddressPostCode.Should().BeNull();
        result.Location.OsMapReference.Should().BeNull();
        result.Location.Easting.Should().BeNull();
        result.Location.Northing.Should().BeNull();
    }

    [Fact]
    public async Task GivenRawPort_WhenCallingToSilver_ShouldCreateLocationAndAddressWithIds()
    {
        var rawPort = new SamPort
        {
            CPH = "12/345/6789",
            PREMISES_NAME = "Test Port"
        };

        var result = await SamPortMapper.ToSilver(
            rawPort,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        result.Location.Should().NotBeNull();
        result.Location!.IdentifierId.Should().NotBeNullOrWhiteSpace();
        result.Location.Address.Should().NotBeNull();
        result.Location.Address!.IdentifierId.Should().NotBeNullOrWhiteSpace();
        result.Communication.Should().NotBeNull();
        result.Communication!.IdentifierId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GivenRawPort_WhenCallingToSilver_ShouldSetHoldingStatusBasedOnDeletedFlag()
    {
        var activePort = new SamPort
        {
            CPH = "12/345/6789",
            IsDeleted = false
        };

        var deletedPort = new SamPort
        {
            CPH = "98/765/4321",
            IsDeleted = true
        };

        var activeResult = await SamPortMapper.ToSilver(
            activePort,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        var deletedResult = await SamPortMapper.ToSilver(
            deletedPort,
            (_, _, _) => Task.FromResult<(string?, string?, string?)>((null, null, null)),
            CancellationToken.None);

        activeResult.HoldingStatus.Should().Be(HoldingStatusType.Active.GetDescription());
        deletedResult.HoldingStatus.Should().Be(HoldingStatusType.Inactive.GetDescription());
    }
}