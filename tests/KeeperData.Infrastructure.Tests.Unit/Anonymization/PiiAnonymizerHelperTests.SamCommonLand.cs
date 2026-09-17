using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Infrastructure.Anonymization;

namespace KeeperData.Infrastructure.Tests.Unit.Anonymization;

public partial class PiiAnonymizerHelperTests
{
    [Fact]
    public void AnonymizeSamCommonLand_ShouldAnonymizeAddressLine1()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            ADDRESS_LINE_1 = "123 Real Street"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        commonLand.ADDRESS_LINE_1.Should().NotBeNull();
        commonLand.ADDRESS_LINE_1.Should().NotBe("123 Real Street");
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldAnonymizeAddressLine2()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            ADDRESS_LINE_2 = "Flat 5"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        commonLand.ADDRESS_LINE_2.Should().NotBeNull();
        commonLand.ADDRESS_LINE_2.Should().NotBe("Flat 5");
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldAnonymizeAddressLine3()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            ADDRESS_LINE_3 = "Manchester"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        commonLand.ADDRESS_LINE_3.Should().NotBeNull();
        commonLand.ADDRESS_LINE_3.Should().NotBe("Manchester");
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldAnonymizePostcode()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            POSTCODE = "M20 2XY"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        commonLand.POSTCODE.Should().NotBeNull();
        commonLand.POSTCODE.Should().NotBe("M20 2XY");
        commonLand.POSTCODE.Should().MatchRegex(@"^[A-Z]{2}\d \d[A-Z]{2}$");
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldAnonymizePremisesName()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            PREMISES_NAME = "Smiths Common Land"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        commonLand.PREMISES_NAME.Should().NotBeNull();
        commonLand.PREMISES_NAME.Should().NotBe("Smiths Common Land");
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldNotAnonymizePremisesName_WhenItIsPlaceholder()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            PREMISES_NAME = "-"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        commonLand.PREMISES_NAME.Should().Be("-");
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldAnonymizeEasting()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            EASTING = "422473"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        commonLand.EASTING.Should().NotBeNull();
        commonLand.EASTING.Should().NotBe("422473");
        int.Parse(commonLand.EASTING!).Should().BeInRange(100000, 999999);
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldAnonymizeNorthing()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            NORTHING = "569204"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        commonLand.NORTHING.Should().NotBeNull();
        commonLand.NORTHING.Should().NotBe("569204");
        int.Parse(commonLand.NORTHING!).Should().BeInRange(200000, 999999);
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldNotModifyNullFields()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            ADDRESS_LINE_1 = null,
            ADDRESS_LINE_2 = null,
            ADDRESS_LINE_3 = null,
            POSTCODE = null,
            PREMISES_NAME = null,
            EASTING = null,
            NORTHING = null
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        commonLand.ADDRESS_LINE_1.Should().BeNull();
        commonLand.ADDRESS_LINE_2.Should().BeNull();
        commonLand.ADDRESS_LINE_3.Should().BeNull();
        commonLand.POSTCODE.Should().BeNull();
        commonLand.PREMISES_NAME.Should().BeNull();
        commonLand.EASTING.Should().BeNull();
        commonLand.NORTHING.Should().BeNull();
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldProduceDeterministicResults_BasedOnCommonCph()
    {
        // Arrange
        var commonLand1 = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            ADDRESS_LINE_1 = "Original Address",
            PREMISES_NAME = "Original Name",
            POSTCODE = "AB12 3CD"
        };

        var commonLand2 = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            ADDRESS_LINE_1 = "Different Address",
            PREMISES_NAME = "Different Name",
            POSTCODE = "XY98 7ZW"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand1);
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand2);

        // Assert
        commonLand1.ADDRESS_LINE_1.Should().Be(commonLand2.ADDRESS_LINE_1);
        commonLand1.PREMISES_NAME.Should().Be(commonLand2.PREMISES_NAME);
        commonLand1.POSTCODE.Should().Be(commonLand2.POSTCODE);
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldProduceDifferentResults_ForDifferentCommonCphs()
    {
        // Arrange
        var commonLand1 = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            ADDRESS_LINE_1 = "Same Address",
            PREMISES_NAME = "Same Name"
        };

        var commonLand2 = new SamCommonLand
        {
            COMMON_CPH = "00/000/0002",
            MAIN_CPH = "-",
            ADDRESS_LINE_1 = "Same Address",
            PREMISES_NAME = "Same Name"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand1);
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand2);

        // Assert
        commonLand1.ADDRESS_LINE_1.Should().NotBe(commonLand2.ADDRESS_LINE_1);
        commonLand1.PREMISES_NAME.Should().NotBe(commonLand2.PREMISES_NAME);
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldPreserveNonPiiFields()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            BATCH_ID = 123,
            CHANGE_TYPE = "I",
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "12/345/6789",
            BUSINESS_USAGE = "Common Land",
            LOCAL_AUTH_NAME = "Test Council",
            COUNTRY = "England",
            LINK_ID = "123",
            CONTIGUOUS_COMMON = "Yes",
            START_DATE = "2020-01-01",
            END_DATE = "2024-12-31",
            ADDRESS_LINE_1 = "Test Address",
            PREMISES_NAME = "Test Premises"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        commonLand.BATCH_ID.Should().Be(123);
        commonLand.CHANGE_TYPE.Should().Be("I");
        commonLand.COMMON_CPH.Should().Be("00/000/0001");
        commonLand.MAIN_CPH.Should().Be("12/345/6789");
        commonLand.BUSINESS_USAGE.Should().Be("Common Land");
        commonLand.LOCAL_AUTH_NAME.Should().Be("Test Council");
        commonLand.COUNTRY.Should().Be("England");
        commonLand.LINK_ID.Should().Be("123");
        commonLand.CONTIGUOUS_COMMON.Should().Be("Yes");
        commonLand.START_DATE.Should().Be("2020-01-01");
        commonLand.END_DATE.Should().Be("2024-12-31");
    }

    [Fact]
    public void AnonymizeResponse_ShouldAnonymizeSamCommonLandData()
    {
        // Arrange
        var response = new DataBridgeResponse<SamCommonLand>
        {
            CollectionName = "SamCommonLands",
            Count = 2,
            Data = new List<SamCommonLand>
            {
                new()
                {
                    COMMON_CPH = "00/000/0001",
                    MAIN_CPH = "-",
                    ADDRESS_LINE_1 = "Secret Street",
                    PREMISES_NAME = "Private Land"
                },
                new()
                {
                    COMMON_CPH = "00/000/0002",
                    MAIN_CPH = "12/345/6789",
                    ADDRESS_LINE_1 = "Confidential Road",
                    POSTCODE = "AB12 3CD"
                }
            }
        };

        // Act
        PiiAnonymizerHelper.AnonymizeResponse(response);

        // Assert
        response.Data[0].ADDRESS_LINE_1.Should().NotBe("Secret Street");
        response.Data[0].PREMISES_NAME.Should().NotBe("Private Land");
        response.Data[1].ADDRESS_LINE_1.Should().NotBe("Confidential Road");
        response.Data[1].POSTCODE.Should().NotBe("AB12 3CD");
    }

    [Fact]
    public void AnonymizeResponse_ShouldHandleEmptyCommonCph()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            COMMON_CPH = "",
            MAIN_CPH = "-",
            ADDRESS_LINE_1 = "Test Address"
        };

        // Act
        var act = () => PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        act.Should().NotThrow();
        commonLand.ADDRESS_LINE_1.Should().NotBe("Test Address");
    }

    [Fact]
    public void AnonymizeResponse_ShouldHandleNullResponse()
    {
        // Arrange
        DataBridgeResponse<SamCommonLand>? response = null;

        // Act
        var act = () => PiiAnonymizerHelper.AnonymizeResponse(response);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AnonymizeResponse_ShouldHandleEmptyDataList()
    {
        // Arrange
        var response = new DataBridgeResponse<SamCommonLand>
        {
            CollectionName = "SamCommonLands",
            Count = 0,
            Data = []
        };

        // Act
        var act = () => PiiAnonymizerHelper.AnonymizeResponse(response);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AnonymizeSamCommonLand_ShouldAnonymizeAllPiiFields_InSingleCall()
    {
        // Arrange
        var commonLand = new SamCommonLand
        {
            COMMON_CPH = "00/000/0001",
            MAIN_CPH = "-",
            ADDRESS_LINE_1 = "123 Real Street",
            ADDRESS_LINE_2 = "Flat 5",
            ADDRESS_LINE_3 = "Manchester",
            POSTCODE = "M20 2XY",
            PREMISES_NAME = "Smiths Common Land",
            EASTING = "422473",
            NORTHING = "569204"
        };

        // Act
        PiiAnonymizerHelper.AnonymizeSamCommonLand(commonLand);

        // Assert
        commonLand.ADDRESS_LINE_1.Should().NotBe("123 Real Street");
        commonLand.ADDRESS_LINE_2.Should().NotBe("Flat 5");
        commonLand.ADDRESS_LINE_3.Should().NotBe("Manchester");
        commonLand.POSTCODE.Should().NotBe("M20 2XY");
        commonLand.PREMISES_NAME.Should().NotBe("Smiths Common Land");
        commonLand.EASTING.Should().NotBe("422473");
        commonLand.NORTHING.Should().NotBe("569204");
    }
}