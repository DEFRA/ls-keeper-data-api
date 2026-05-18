using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Core.Tests.Unit.ApiClients.DataBridgeApi.Contracts;

public class SamCommonLandTests
{
    [Fact]
    public void IsDefinitionRecord_WhenMainCphIsDash_ShouldReturnTrue()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            MAIN_CPH = "-",
            COMMON_CPH = "00/000/0001"
        };

        // Act
        var result = commonLand.IsDefinitionRecord;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDefinitionRecord_WhenMainCphIsNull_ShouldReturnTrue()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            MAIN_CPH = string.Empty,
            COMMON_CPH = "00/000/0001"
        };

        // Act
        var result = commonLand.IsDefinitionRecord;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDefinitionRecord_WhenMainCphIsEmpty_ShouldReturnTrue()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            MAIN_CPH = string.Empty,
            COMMON_CPH = "00/000/0001"
        };

        // Act
        var result = commonLand.IsDefinitionRecord;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDefinitionRecord_WhenMainCphIsWhitespace_ShouldReturnTrue()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            MAIN_CPH = "   ",
            COMMON_CPH = "00/000/0001"
        };

        // Act
        var result = commonLand.IsDefinitionRecord;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDefinitionRecord_WhenMainCphHasValue_ShouldReturnFalse()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            MAIN_CPH = "12/345/6789",
            COMMON_CPH = "00/000/0001"
        };

        // Act
        var result = commonLand.IsDefinitionRecord;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRelationshipRecord_WhenMainCphIsDash_ShouldReturnFalse()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            MAIN_CPH = "-",
            COMMON_CPH = "00/000/0001"
        };

        // Act
        var result = commonLand.IsRelationshipRecord;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRelationshipRecord_WhenMainCphHasValue_ShouldReturnTrue()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            MAIN_CPH = "12/345/6789",
            COMMON_CPH = "00/000/0001"
        };

        // Act
        var result = commonLand.IsRelationshipRecord;

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("12/345/6789", "00/000/0001", true)]
    [InlineData("-", "00/000/0001", false)]
    [InlineData("", "00/000/0001", false)]
    [InlineData("  ", "00/000/0001", false)]
    public void IsDefinitionRecord_AndIsRelationshipRecord_ShouldBeOpposite(string mainCph, string commonCph, bool expectedRelationship)
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            MAIN_CPH = mainCph,
            COMMON_CPH = commonCph
        };

        // Act & Assert
        commonLand.IsDefinitionRecord.Should().Be(!expectedRelationship);
        commonLand.IsRelationshipRecord.Should().Be(expectedRelationship);
    }

    [Fact]
    public void SamCommonLand_ShouldInheritFromBronzeBase()
    {
        // Arrange & Act
        var commonLand = new SamCommonLand();

        // Assert
        commonLand.Should().BeAssignableTo<BronzeBase>();
    }

    [Fact]
    public void SamCommonLand_ShouldHaveRequiredProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var commonLand = new SamCommonLand
        {
            BATCH_ID = 1,
            CHANGE_TYPE = "I",
            IsDeleted = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            COMMON_LAND_PREMISE_ID = "546196",
            MAIN_CPH = "-",
            COMMON_CPH = "00/000/0001",
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
            CONTIGUOUS_COMMON = "No",
            START_DATE = "2020-01-01",
            END_DATE = null
        };

        // Act & Assert
        commonLand.BATCH_ID.Should().Be(1);
        commonLand.CHANGE_TYPE.Should().Be("I");
        commonLand.IsDeleted.Should().BeFalse();
        commonLand.CreatedAtUtc.Should().Be(now);
        commonLand.UpdatedAtUtc.Should().Be(now);
        commonLand.COMMON_LAND_PREMISE_ID.Should().Be("546196");
        commonLand.MAIN_CPH.Should().Be("-");
        commonLand.COMMON_CPH.Should().Be("00/000/0001");
        commonLand.BUSINESS_USAGE.Should().Be("Common Land");
        commonLand.PREMISES_NAME.Should().Be("Test Common Land");
        commonLand.ADDRESS_LINE_1.Should().Be("Land off Road");
        commonLand.ADDRESS_LINE_2.Should().Be("Village");
        commonLand.ADDRESS_LINE_3.Should().Be("District");
        commonLand.LOCAL_AUTH_NAME.Should().Be("Test Council");
        commonLand.COUNTRY.Should().Be("England");
        commonLand.POSTCODE.Should().Be("AB12 3CD");
        commonLand.EASTING.Should().Be("422473");
        commonLand.NORTHING.Should().Be("569204");
        commonLand.LINK_ID.Should().Be("-1");
        commonLand.CONTIGUOUS_COMMON.Should().Be("No");
        commonLand.START_DATE.Should().Be("2020-01-01");
        commonLand.END_DATE.Should().BeNull();
    }

    [Fact]
    public void SamCommonLand_AllPropertiesShouldBeNullable_ExceptRequiredOnes()
    {
        // Arrange & Act
        var commonLand = new SamCommonLand
        {
            MAIN_CPH = string.Empty,
            COMMON_CPH = string.Empty
        };

        // Assert
        commonLand.COMMON_LAND_PREMISE_ID.Should().BeNull();
        commonLand.BUSINESS_USAGE.Should().BeNull();
        commonLand.PREMISES_NAME.Should().BeNull();
        commonLand.ADDRESS_LINE_1.Should().BeNull();
        commonLand.ADDRESS_LINE_2.Should().BeNull();
        commonLand.ADDRESS_LINE_3.Should().BeNull();
        commonLand.LOCAL_AUTH_NAME.Should().BeNull();
        commonLand.COUNTRY.Should().BeNull();
        commonLand.POSTCODE.Should().BeNull();
        commonLand.EASTING.Should().BeNull();
        commonLand.NORTHING.Should().BeNull();
        commonLand.LINK_ID.Should().BeNull();
        commonLand.CONTIGUOUS_COMMON.Should().BeNull();
        commonLand.START_DATE.Should().BeNull();
        commonLand.END_DATE.Should().BeNull();
    }
}