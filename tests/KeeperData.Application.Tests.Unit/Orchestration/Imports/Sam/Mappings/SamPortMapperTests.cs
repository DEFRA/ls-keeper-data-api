using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SamPortMapperTests
{
    [Fact]
    public void GivenNullableRawPorts_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = SamPortMapper.ToSilver((List<SamPort>?)null!);

        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public void GivenEmptyRawPorts_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = SamPortMapper.ToSilver([]);

        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public void GivenRawPortsWithEmptyCph_WhenCallingToSilver_ShouldFilterOutEmptyCph()
    {
        var rawPorts = new List<SamPort>
        {
            new() { CPH = "12/345/6789", PREMISES_NAME = "Valid Port" },
            new() { CPH = "", PREMISES_NAME = "Empty CPH" },
            new() { CPH = null!, PREMISES_NAME = "Null CPH" },
            new() { CPH = "  ", PREMISES_NAME = "Whitespace CPH" }
        };

        var results = SamPortMapper.ToSilver(rawPorts);

        results.Should().HaveCount(1);
        results[0].CountyParishHoldingNumber.Should().Be("12/345/6789");
    }

    [Fact]
    public void GivenSingleRawPort_WhenCallingToSilver_ShouldMapAllProperties()
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

        var result = SamPortMapper.ToSilver(rawPort);

        result.Should().NotBeNull();
        result.Id.Should().BeNull();
        result.LastUpdatedBatchId.Should().Be(42);
        result.CreatedDate.Should().Be(new DateTime(2025, 1, 1, 10, 0, 0));
        result.LastUpdatedDate.Should().Be(new DateTime(2025, 1, 2, 15, 30, 0));
        result.Deleted.Should().BeFalse();
        result.ChangeType.Should().Be("I");
        result.CountyParishHoldingNumber.Should().Be("12/345/6789");
        result.PremisesName.Should().Be("Test Port Name");
        result.AddressLine1.Should().Be("Address Line 1");
        result.AddressLine2.Should().Be("Address Line 2");
        result.AddressLine3.Should().Be("Address Line 3");
        result.Postcode.Should().Be("AB12 3CD");
        result.MapReference.Should().Be("SK123456");
        result.Easting.Should().Be(400000);
        result.Northing.Should().Be(500000);
    }

    [Fact]
    public void GivenRawPortWithNullDates_WhenCallingToSilver_ShouldUseUtcNow()
    {
        var rawPort = new SamPort
        {
            CPH = "12/345/6789",
            CreatedAtUtc = null,
            UpdatedAtUtc = null
        };

        var beforeMapping = DateTime.UtcNow;
        var result = SamPortMapper.ToSilver(rawPort);
        var afterMapping = DateTime.UtcNow;

        result.CreatedDate.Should().BeOnOrAfter(beforeMapping).And.BeOnOrBefore(afterMapping);
        result.LastUpdatedDate.Should().BeOnOrAfter(beforeMapping).And.BeOnOrBefore(afterMapping);
    }

    [Fact]
    public void GivenRawPortWithNullIsDeleted_WhenCallingToSilver_ShouldDefaultToFalse()
    {
        var rawPort = new SamPort
        {
            CPH = "12/345/6789",
            IsDeleted = null
        };

        var result = SamPortMapper.ToSilver(rawPort);

        result.Deleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void GivenMultipleRawPorts_WhenCallingToSilver_ShouldMapAll(int count)
    {
        var rawPorts = Enumerable.Range(1, count).Select(i => new SamPort
        {
            CPH = $"12/345/{6000 + i}",
            PREMISES_NAME = $"Port {i}",
            BATCH_ID = i
        }).ToList();

        var results = SamPortMapper.ToSilver(rawPorts);

        results.Should().HaveCount(count);
        for (var i = 0; i < count; i++)
        {
            results[i].CountyParishHoldingNumber.Should().Be($"12/345/{6001 + i}");
            results[i].PremisesName.Should().Be($"Port {i + 1}");
            results[i].LastUpdatedBatchId.Should().Be(i + 1);
        }
    }

    [Fact]
    public void GivenNullableSilverPorts_WhenCallingToGold_ShouldReturnEmptyList()
    {
        List<SamPortDocument>? nullPorts = null;
        var results = SamPortMapper.ToGold(nullPorts!, "12/345/6789");

        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public void GivenEmptySilverPorts_WhenCallingToGold_ShouldReturnEmptyList()
    {
        var results = SamPortMapper.ToGold([], "12/345/6789");

        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public void GivenSilverPort_WhenCallingToGold_ShouldMapAllProperties()
    {
        var holdingIdentifier = "12/345/6789";
        var silverPort = new SamPortDocument
        {
            Id = "silver-id",
            CreatedDate = new DateTime(2025, 1, 1, 10, 0, 0),
            LastUpdatedDate = new DateTime(2025, 1, 2, 15, 30, 0),
            Deleted = false,
            ChangeType = "U",
            CountyParishHoldingNumber = "12/345/6789",
            PremisesName = "Gold Port Name",
            AddressLine1 = "Gold Address 1",
            AddressLine2 = "Gold Address 2",
            AddressLine3 = "Gold Address 3",
            Postcode = "EF45 6GH",
            MapReference = "SU987654",
            Easting = 600000,
            Northing = 700000
        };

        var result = SamPortMapper.ToGold(silverPort, holdingIdentifier);

        result.Should().NotBeNull();
        result.Id.Should().BeNull();
        result.HoldingIdentifier.Should().Be(holdingIdentifier);
        result.Name.Should().Be("Gold Port Name");
        result.CreatedDate.Should().Be(new DateTime(2025, 1, 1, 10, 0, 0));
        result.LastUpdatedDate.Should().Be(new DateTime(2025, 1, 2, 15, 30, 0));
        result.Deleted.Should().BeFalse();
        result.ChangeType.Should().Be("U");
        result.AddressLine1.Should().Be("Gold Address 1");
        result.AddressLine2.Should().Be("Gold Address 2");
        result.AddressLine3.Should().Be("Gold Address 3");
        result.Postcode.Should().Be("EF45 6GH");
        result.MapReference.Should().Be("SU987654");
        result.Easting.Should().Be(600000);
        result.Northing.Should().Be(700000);
        result.Source.Should().Be("SAM");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void GivenMultipleSilverPorts_WhenCallingToGold_ShouldMapAll(int count)
    {
        var holdingIdentifier = "12/345/6789";
        var silverPorts = Enumerable.Range(1, count).Select(i => new SamPortDocument
        {
            CountyParishHoldingNumber = "12/345/6789",
            PremisesName = $"Gold Port {i}",
            ChangeType = "I"
        }).ToList();

        var results = SamPortMapper.ToGold(silverPorts, holdingIdentifier);

        results.Should().HaveCount(count);
        for (var i = 0; i < count; i++)
        {
            results[i].HoldingIdentifier.Should().Be(holdingIdentifier);
            results[i].Name.Should().Be($"Gold Port {i + 1}");
            results[i].Source.Should().Be("SAM");
        }
    }

    [Fact]
    public void GivenSilverPortWithNullValues_WhenCallingToGold_ShouldHandleNulls()
    {
        var holdingIdentifier = "12/345/6789";
        var silverPort = new SamPortDocument
        {
            CountyParishHoldingNumber = "12/345/6789",
            PremisesName = null,
            AddressLine1 = null,
            AddressLine2 = null,
            AddressLine3 = null,
            Postcode = null,
            MapReference = null,
            Easting = null,
            Northing = null
        };

        var result = SamPortMapper.ToGold(silverPort, holdingIdentifier);

        result.Should().NotBeNull();
        result.Name.Should().BeNull();
        result.AddressLine1.Should().BeNull();
        result.AddressLine2.Should().BeNull();
        result.AddressLine3.Should().BeNull();
        result.Postcode.Should().BeNull();
        result.MapReference.Should().BeNull();
        result.Easting.Should().BeNull();
        result.Northing.Should().BeNull();
    }

    [Fact]
    public void GivenDifferentHoldingIdentifier_WhenCallingToGold_ShouldUseProvidedIdentifier()
    {
        var providedIdentifier = "99/888/7777";
        var silverPort = new SamPortDocument
        {
            CountyParishHoldingNumber = "12/345/6789",
            PremisesName = "Test Port"
        };

        var result = SamPortMapper.ToGold(silverPort, providedIdentifier);

        result.HoldingIdentifier.Should().Be(providedIdentifier);
    }

    [Fact]
    public void GivenPortWithDeletedFlag_WhenCallingToSilver_ShouldPreserveDeletedFlag()
    {
        var rawPort = new SamPort
        {
            CPH = "12/345/6789",
            IsDeleted = true
        };

        var result = SamPortMapper.ToSilver(rawPort);

        result.Deleted.Should().BeTrue();
    }

    [Fact]
    public void GivenPortWithDeletedFlag_WhenCallingToGold_ShouldPreserveDeletedFlag()
    {
        var silverPort = new SamPortDocument
        {
            CountyParishHoldingNumber = "12/345/6789",
            Deleted = true
        };

        var result = SamPortMapper.ToGold(silverPort, "12/345/6789");

        result.Deleted.Should().BeTrue();
    }
}