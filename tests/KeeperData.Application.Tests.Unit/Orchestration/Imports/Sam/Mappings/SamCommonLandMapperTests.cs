using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SamCommonLandMapperTests
{
    [Fact]
    public void ToSilver_WithNullInput_ShouldReturnEmptyList()
    {
        // Arrange
        List<SamCommonLand>? rawCommonLands = null;

        // Act
        var result = SamCommonLandMapper.ToSilver(rawCommonLands!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToSilver_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var rawCommonLands = new List<SamCommonLand>();

        // Act
        var result = SamCommonLandMapper.ToSilver(rawCommonLands);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToSilver_WithDefinitionRecordOnly_ShouldCreateSamHoldingDocument()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var rawCommonLands = new List<SamCommonLand>
        {
            new()
            {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                IsDeleted = false,
                MAIN_CPH = "-",
                COMMON_CPH = "00/000/0001",
                COMMON_LAND_PREMISE_ID = "546196",
                BUSINESS_USAGE = "Common Land",
                PREMISES_NAME = "Test Common Land",
                ADDRESS_LINE_1 = "Land off Road",
                ADDRESS_LINE_2 = "Village",
                ADDRESS_LINE_3 = "District",
                LOCAL_AUTH_NAME = "Test Council",
                COUNTRY = "England",
                POSTCODE = "AB12 3CD",
                EASTING = "422473",
                NORTHING = "569204",
                LINK_ID = "-1",
                CONTIGUOUS_COMMON = "No"
            }
        };

        // Act
        var result = SamCommonLandMapper.ToSilver(rawCommonLands);

        // Assert
        result.Should().HaveCount(1);
        var holding = result[0];
        holding.CountyParishHoldingNumber.Should().Be("00/000/0001");
        holding.LocationName.Should().Be("Test Common Land");
        holding.LastUpdatedBatchId.Should().Be(1);
        holding.CreatedDate.Should().Be(now);
        holding.LastUpdatedDate.Should().Be(now);
        holding.Deleted.Should().BeFalse();
        holding.HoldingStatus.Should().Be(HoldingStatusType.Active.GetDescription());
        holding.SiteTypeCode.Should().Be("CL");
        holding.LocalAuthorityName.Should().Be("Test Council");
        holding.AssociatedMainHoldings.Should().BeEmpty();
        holding.Location.Should().NotBeNull();
        holding.Location!.Easting.Should().Be(422473);
        holding.Location.Northing.Should().Be(569204);
        holding.Location.Address.Should().NotBeNull();
        holding.Location.Address!.AddressLine.Should().Be("Land off Road");
        holding.Location.Address.AddressLocality.Should().Be("Village");
        holding.Location.Address.AddressStreet.Should().Be("District");
        holding.Location.Address.AddressPostCode.Should().Be("AB12 3CD");
        holding.Location.Address.CountryCode.Should().Be("England");
    }

    [Fact]
    public void ToSilver_WithDeletedRecord_ShouldMapDeletedStatus()
    {
        // Arrange
        var rawCommonLands = new List<SamCommonLand>
        {
            new()
            {
                BATCH_ID = 1,
                IsDeleted = true,
                MAIN_CPH = "-",
                COMMON_CPH = "00/000/0001",
                PREMISES_NAME = "Deleted Land",
                ADDRESS_LINE_1 = "Some Address"
            }
        };

        // Act
        var result = SamCommonLandMapper.ToSilver(rawCommonLands);

        // Assert
        result.Should().HaveCount(1);
        result[0].Deleted.Should().BeTrue();
        result[0].HoldingStatus.Should().Be(HoldingStatusType.Inactive.GetDescription());
    }

    [Fact]
    public void ToSilver_WithPlaceholderPremisesName_ShouldSetToNull()
    {
        // Arrange
        var rawCommonLands = new List<SamCommonLand>
        {
            new()
            {
                MAIN_CPH = "-",
                COMMON_CPH = "00/000/0001",
                PREMISES_NAME = "-",
                ADDRESS_LINE_1 = "Address"
            }
        };

        // Act
        var result = SamCommonLandMapper.ToSilver(rawCommonLands);

        // Assert
        result[0].LocationName.Should().BeNull();
    }

    [Fact]
    public void ToSilver_WithEmptyCommonCph_ShouldBeFiltered()
    {
        // Arrange
        var rawCommonLands = new List<SamCommonLand>
        {
            new()
            {
                MAIN_CPH = "-",
                COMMON_CPH = "",
                PREMISES_NAME = "Test"
            },
            new()
            {
                MAIN_CPH = "-",
                COMMON_CPH = "   ",
                PREMISES_NAME = "Test2"
            }
        };

        // Act
        var result = SamCommonLandMapper.ToSilver(rawCommonLands);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToSilver_WithInvalidEastingNorthing_ShouldSetToNull()
    {
        // Arrange
        var rawCommonLands = new List<SamCommonLand>
        {
            new()
            {
                MAIN_CPH = "-",
                COMMON_CPH = "00/000/0001",
                PREMISES_NAME = "Test",
                ADDRESS_LINE_1 = "Address",
                EASTING = "invalid",
                NORTHING = "also_invalid"
            }
        };

        // Act
        var result = SamCommonLandMapper.ToSilver(rawCommonLands);

        // Assert
        result[0].Location!.Easting.Should().BeNull();
        result[0].Location!.Northing.Should().BeNull();
    }

    [Fact]
    public void ToSilver_WithFutureDate_ShouldNormaliseToNull()
    {
        // Arrange
        var rawCommonLands = new List<SamCommonLand>
        {
            new()
            {
                MAIN_CPH = "-",
                COMMON_CPH = "00/000/0001",
                ADDRESS_LINE_1 = "Address"
            },
            new()
            {
                MAIN_CPH = "12/345/6789",
                COMMON_CPH = "00/000/0001",
                START_DATE = "3000-01-01",
                END_DATE = "3001-12-31"
            }
        };

        // Act
        var result = SamCommonLandMapper.ToSilver(rawCommonLands);

        // Assert
        result[1].AssociatedMainHoldings[0].StartDate.Should().BeNull();
        result[1].AssociatedMainHoldings[0].EndDate.Should().BeNull();
    }

    [Fact]
    public void ToAssociatedCommonLands_WithNullInput_ShouldReturnEmptyList()
    {
        // Arrange
        List<SamCommonLand>? rawCommonLands = null;

        // Act
        var result = SamCommonLandMapper.ToAssociatedCommonLands(rawCommonLands!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToAssociatedCommonLands_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var rawCommonLands = new List<SamCommonLand>();

        // Act
        var result = SamCommonLandMapper.ToAssociatedCommonLands(rawCommonLands);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToAssociatedCommonLands_WithRelationshipRecords_ShouldMapCorrectly()
    {
        // Arrange
        var rawCommonLands = new List<SamCommonLand>
        {
            new()
            {
                MAIN_CPH = "12/345/6789",
                COMMON_CPH = "00/000/0001",
                CONTIGUOUS_COMMON = "Yes",
                START_DATE = "2020-01-01",
                END_DATE = "2024-12-31"
            },
            new()
            {
                MAIN_CPH = "98/765/4321",
                COMMON_CPH = "00/000/0002",
                CONTIGUOUS_COMMON = "No",
                START_DATE = "2021-06-01",
                END_DATE = null
            }
        };

        // Act
        var result = SamCommonLandMapper.ToAssociatedCommonLands(rawCommonLands);

        // Assert
        result.Should().HaveCount(2);
        var assoc1 = result[0];
        assoc1.HoldingIdentifier.Should().Be("00/000/0001");
        assoc1.ContiguousFlag.Should().BeTrue();
        assoc1.StartDate.Should().Be("2020-01-01");
        assoc1.EndDate.Should().Be("2024-12-31");

        var assoc2 = result[1];
        assoc2.HoldingIdentifier.Should().Be("00/000/0002");
        assoc2.ContiguousFlag.Should().BeFalse();
        assoc2.StartDate.Should().Be("2021-06-01");
        assoc2.EndDate.Should().BeNull();
    }

    [Fact]
    public void ToAssociatedCommonLands_WithEmptyCommonCph_ShouldFilterOut()
    {
        // Arrange
        var rawCommonLands = new List<SamCommonLand>
        {
            new()
            {
                MAIN_CPH = "12/345/6789",
                COMMON_CPH = "",
                CONTIGUOUS_COMMON = "Yes"
            },
            new()
            {
                MAIN_CPH = "98/765/4321",
                COMMON_CPH = "   ",
                CONTIGUOUS_COMMON = "No"
            }
        };

        // Act
        var result = SamCommonLandMapper.ToAssociatedCommonLands(rawCommonLands);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Yes", true)]
    [InlineData("yes", true)]
    [InlineData("YES", true)]
    [InlineData("No", false)]
    [InlineData("no", false)]
    [InlineData("NO", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("Unknown", false)]
    public void ToAssociatedCommonLands_WithVariousContiguousValues_ShouldMapCorrectly(string? contiguousValue, bool expected)
    {
        // Arrange
        var rawCommonLands = new List<SamCommonLand>
        {
            new()
            {
                MAIN_CPH = "12/345/6789",
                COMMON_CPH = "00/000/0001",
                CONTIGUOUS_COMMON = contiguousValue
            }
        };

        // Act
        var result = SamCommonLandMapper.ToAssociatedCommonLands(rawCommonLands);

        // Assert
        result[0].ContiguousFlag.Should().Be(expected);
    }

    [Theory]
    [InlineData("2020-01-01", "2020-01-01")]
    [InlineData("01/01/2020", "2020-01-01")] // Different formats should parse
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("invalid-date", "invalid-date")] // Invalid dates should be kept as-is
    [InlineData("3000-01-01", null)] // Future dates should become null
    public void ToAssociatedCommonLands_WithVariousDateFormats_ShouldNormaliseCorrectly(string? inputDate, string? expectedDate)
    {
        // Arrange
        var rawCommonLands = new List<SamCommonLand>
        {
            new()
            {
                MAIN_CPH = "12/345/6789",
                COMMON_CPH = "00/000/0001",
                START_DATE = inputDate
            }
        };

        // Act
        var result = SamCommonLandMapper.ToAssociatedCommonLands(rawCommonLands);

        // Assert
        result[0].StartDate.Should().Be(expectedDate);
    }
}