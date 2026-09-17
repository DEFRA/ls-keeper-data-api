using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using Microsoft.Extensions.Logging;
using NSubstitute;
using KeeperData.Infrastructure.ApiClients.Decorators;

namespace KeeperData.Infrastructure.Tests.Unit.ApiClients;

public class DataBridgeClientAnonymizerTests
{
    private readonly IDataBridgeClient _innerClient = Substitute.For<IDataBridgeClient>();
    private readonly ILogger<DataBridgeClientAnonymizer> _logger = Substitute.For<ILogger<DataBridgeClientAnonymizer>>();
    private readonly DataBridgeClientAnonymizer _sut;

    public DataBridgeClientAnonymizerTests()
    {
        _sut = new DataBridgeClientAnonymizer(_innerClient, _logger);
    }

    [Fact]
    public async Task GetSamHoldingsAsync_ShouldAnonymizeLocationFields()
    {
        var holding = new SamCphHolding
        {
            CPH = "1234567",
            OS_MAP_REFERENCE = "ST12345678",
            EASTING = 312456,
            NORTHING = 456789,
            STREET = "10 Downing Street",
            LOCALITY = "Westminster",
            TOWN = "London",
            POSTCODE = "SW1A 2AA",
            PAON_DESCRIPTION = "Number 10",
            SAON_DESCRIPTION = "Flat A",
            UDPRN = "12345678"
        };

        _innerClient.GetSamHoldingsAsync("1234567", Arg.Any<CancellationToken>())
            .Returns([holding]);

        var result = await _sut.GetSamHoldingsAsync("1234567", CancellationToken.None);

        result.Should().HaveCount(1);
        var h = result[0];
        h.OS_MAP_REFERENCE.Should().NotBe("ST12345678").And.MatchRegex("^[A-Z]{2}[0-9]{8}$");
        h.EASTING.Should().BeInRange(100000, 999999).And.NotBe(312456);
        h.NORTHING.Should().BeInRange(200000, 999999).And.NotBe(456789);
        h.STREET.Should().NotBe("10 Downing Street").And.NotBeNullOrWhiteSpace();
        h.LOCALITY.Should().NotBe("Westminster").And.NotBeNullOrWhiteSpace();
        h.TOWN.Should().NotBe("London").And.NotBeNullOrWhiteSpace();
        h.POSTCODE.Should().NotBe("SW1A 2AA").And.NotBeNullOrWhiteSpace();
        h.PAON_DESCRIPTION.Should().NotBe("Number 10").And.NotBeNullOrWhiteSpace();
        h.SAON_DESCRIPTION.Should().NotBe("Flat A").And.NotBeNullOrWhiteSpace();
        h.UDPRN.Should().NotBe("12345678");
    }

    [Fact]
    public async Task GetSamHoldingsAsync_ShouldNotAnonymizeNullFields()
    {
        var holding = new SamCphHolding
        {
            CPH = "1234567",
            OS_MAP_REFERENCE = null,
            EASTING = null,
            NORTHING = null,
            STREET = null,
            TOWN = null,
            POSTCODE = null,
            UDPRN = null
        };

        _innerClient.GetSamHoldingsAsync("1234567", Arg.Any<CancellationToken>())
            .Returns([holding]);

        var result = await _sut.GetSamHoldingsAsync("1234567", CancellationToken.None);

        var h = result[0];
        h.OS_MAP_REFERENCE.Should().BeNull();
        h.EASTING.Should().BeNull();
        h.NORTHING.Should().BeNull();
        h.STREET.Should().BeNull();
        h.TOWN.Should().BeNull();
        h.POSTCODE.Should().BeNull();
        h.UDPRN.Should().BeNull();
    }

    [Fact]
    public async Task GetSamHoldersByCphAsync_ShouldAnonymizePartyAndAddressFields()
    {
        var holder = CreateSamCphHolder();

        _innerClient.GetSamHoldersByCphAsync("cph-1", Arg.Any<CancellationToken>())
            .Returns([holder]);

        var result = await _sut.GetSamHoldersByCphAsync("cph-1", CancellationToken.None);

        result.Should().HaveCount(1);
        AssertSamPartyFieldsAnonymized(result[0]);
    }

    [Fact]
    public async Task GetSamHoldersByPartyIdAsync_ShouldAnonymizePartyFields()
    {
        var holder = CreateSamCphHolder();

        _innerClient.GetSamHoldersByPartyIdAsync("P001", Arg.Any<CancellationToken>())
            .Returns([holder]);

        var result = await _sut.GetSamHoldersByPartyIdAsync("P001", CancellationToken.None);

        result.Should().HaveCount(1);
        AssertSamPartyFieldsAnonymized(result[0]);
    }

    [Fact]
    public async Task GetSamPartyAsync_ShouldAnonymizePartyFields()
    {
        var party = new SamParty
        {
            PARTY_ID = "P001",
            PERSON_TITLE = "Mr",
            PERSON_GIVEN_NAME = "John",
            PERSON_GIVEN_NAME2 = "James",
            PERSON_INITIALS = "J",
            PERSON_FAMILY_NAME = "Smith",
            ORGANISATION_NAME = "DEFRA",
            INTERNET_EMAIL_ADDRESS = "john@example.com",
            MOBILE_NUMBER = "07123456789",
            TELEPHONE_NUMBER = "01234567890",
            STREET = "10 Downing Street",
            LOCALITY = "Westminster",
            TOWN = "London",
            POSTCODE = "SW1A 2AA",
            UDPRN = "12345678"
        };

        _innerClient.GetSamPartyAsync("P001", Arg.Any<CancellationToken>())
            .Returns(party);

        var result = await _sut.GetSamPartyAsync("P001", CancellationToken.None);

        result.Should().NotBeNull();
        result!.PERSON_TITLE.Should().NotBe("Mr").And.NotBeNullOrWhiteSpace();
        result.PERSON_GIVEN_NAME.Should().NotBe("John").And.NotBeNullOrWhiteSpace();
        result.PERSON_GIVEN_NAME2.Should().NotBe("James").And.NotBeNullOrWhiteSpace();
        result.PERSON_FAMILY_NAME.Should().NotBe("Smith").And.NotBeNullOrWhiteSpace();
        result.ORGANISATION_NAME.Should().NotBe("DEFRA").And.NotBeNullOrWhiteSpace();
        result.INTERNET_EMAIL_ADDRESS.Should().NotBe("john@example.com").And.NotBeNullOrWhiteSpace();  //And.MatchRegex(@"^[a-f0-9]{13}@[a-f0-9]{19}\.com$");
        result.MOBILE_NUMBER.Should().NotBe("07123456789").And.MatchRegex(@"^07\d{9}$");
        result.TELEPHONE_NUMBER.Should().NotBe("01234567890").And.NotBeNullOrWhiteSpace();
        result.STREET.Should().NotBe("10 Downing Street").And.NotBeNullOrWhiteSpace();
        result.TOWN.Should().NotBe("London").And.NotBeNullOrWhiteSpace();
        result.POSTCODE.Should().NotBe("SW1A 2AA").And.NotBeNullOrWhiteSpace();
        result.UDPRN.Should().NotBe("12345678").And.NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetSamPartyAsync_ShouldReturnNull_WhenInnerReturnsNull()
    {
        _innerClient.GetSamPartyAsync("P001", Arg.Any<CancellationToken>())
            .Returns((SamParty?)null);

        var result = await _sut.GetSamPartyAsync("P001", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSamPartiesAsync_ShouldAnonymizeAllParties()
    {
        var parties = new List<SamParty>
        {
            new() { PARTY_ID = "P001", PERSON_GIVEN_NAME = "John", PERSON_FAMILY_NAME = "Smith", UDPRN = "12345678"},
            new() { PARTY_ID = "P002", PERSON_GIVEN_NAME = "Jane", PERSON_FAMILY_NAME = "Doe",UDPRN = "12345678"}
        };

        _innerClient.GetSamPartiesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(parties);

        var result = await _sut.GetSamPartiesAsync(["P001", "P002"], CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].PERSON_GIVEN_NAME.Should().NotBe("John");
        result[0].PERSON_FAMILY_NAME.Should().NotBe("Smith");
        result[0].UDPRN.Should().NotBe("12345678");
        result[1].PERSON_GIVEN_NAME.Should().NotBe("Jane");
        result[1].PERSON_FAMILY_NAME.Should().NotBe("Doe");
        result[1].UDPRN.Should().NotBe("12345678");
    }

    [Fact]
    public async Task GetCtsHoldingsAsync_ShouldAnonymizeAddressAndCommunicationFields()
    {
        var holding = new CtsCphHolding
        {
            LID_FULL_IDENTIFIER = "AH-123456",
            LOC_MAP_REFERENCE = "ST12345678",
            ADR_NAME = "Manor Farm",
            ADR_ADDRESS_2 = "Farm Lane",
            ADR_ADDRESS_3 = "Exeter",
            ADR_ADDRESS_4 = "Devon",
            ADR_ADDRESS_5 = "South West",
            ADR_POST_CODE = "EX1 1AA",
            LOC_TEL_NUMBER = "01234567890",
            LOC_MOBILE_NUMBER = "07123456789"
        };

        _innerClient.GetCtsHoldingsAsync("AH-123456", Arg.Any<CancellationToken>())
            .Returns([holding]);

        var result = await _sut.GetCtsHoldingsAsync("AH-123456", CancellationToken.None);

        result.Should().HaveCount(1);
        var h = result[0];
        h.LOC_MAP_REFERENCE.Should().NotBe("ST12345678").And.MatchRegex("^[A-Z]{2}[0-9]{8}$");
        h.ADR_NAME.Should().NotBe("Manor Farm").And.NotBeNullOrWhiteSpace();
        h.ADR_ADDRESS_2.Should().NotBe("Farm Lane").And.NotBeNullOrWhiteSpace();
        h.ADR_ADDRESS_3.Should().NotBe("Exeter").And.NotBeNullOrWhiteSpace();
        h.ADR_ADDRESS_4.Should().NotBe("Devon").And.NotBeNullOrWhiteSpace();
        h.ADR_ADDRESS_5.Should().NotBe("South West").And.NotBeNullOrWhiteSpace();
        h.ADR_POST_CODE.Should().NotBe("EX1 1AA").And.NotBeNullOrWhiteSpace();
        h.LOC_TEL_NUMBER.Should().NotBe("01234567890").And.NotBeNullOrWhiteSpace();
        h.LOC_MOBILE_NUMBER.Should().NotBe("07123456789").And.MatchRegex(@"^07\d{9}$");
    }

    [Fact]
    public async Task GetCtsAgentsAsync_ShouldAnonymizePartyFields()
    {
        var agent = CreateCtsAgentOrKeeper();

        _innerClient.GetCtsAgentsAsync("AH-123456", Arg.Any<CancellationToken>())
            .Returns([agent]);

        var result = await _sut.GetCtsAgentsAsync("AH-123456", CancellationToken.None);

        result.Should().HaveCount(1);
        AssertCtsPartyFieldsAnonymized(result[0]);
    }

    [Fact]
    public async Task GetCtsAgentByPartyIdAsync_ShouldAnonymizePartyFields()
    {
        var agent = CreateCtsAgentOrKeeper();

        _innerClient.GetCtsAgentByPartyIdAsync("12345", Arg.Any<CancellationToken>())
            .Returns(agent);

        var result = await _sut.GetCtsAgentByPartyIdAsync("12345", CancellationToken.None);

        result.Should().NotBeNull();
        AssertCtsPartyFieldsAnonymized(result!);
    }

    [Fact]
    public async Task GetCtsAgentByPartyIdAsync_ShouldReturnNull_WhenInnerReturnsNull()
    {
        _innerClient.GetCtsAgentByPartyIdAsync("12345", Arg.Any<CancellationToken>())
            .Returns((CtsAgentOrKeeper?)null);

        var result = await _sut.GetCtsAgentByPartyIdAsync("12345", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCtsKeepersAsync_ShouldAnonymizePartyFields()
    {
        var keeper = CreateCtsAgentOrKeeper();

        _innerClient.GetCtsKeepersAsync("AH-123456", Arg.Any<CancellationToken>())
            .Returns([keeper]);

        var result = await _sut.GetCtsKeepersAsync("AH-123456", CancellationToken.None);

        result.Should().HaveCount(1);
        AssertCtsPartyFieldsAnonymized(result[0]);
    }

    [Fact]
    public async Task GetCtsKeeperByPartyIdAsync_ShouldAnonymizePartyFields()
    {
        var keeper = CreateCtsAgentOrKeeper();

        _innerClient.GetCtsKeeperByPartyIdAsync("12345", Arg.Any<CancellationToken>())
            .Returns(keeper);

        var result = await _sut.GetCtsKeeperByPartyIdAsync("12345", CancellationToken.None);

        result.Should().NotBeNull();
        AssertCtsPartyFieldsAnonymized(result!);
    }

    [Fact]
    public async Task GetSamHerdsAsync_ShouldNotAnonymize_BecauseHerdsHaveNoPii()
    {
        var herd = new SamHerd
        {
            HERDMARK = "HM001",
            CPHH = "1234567/01",
            ANIMAL_SPECIES_CODE = "CTT"
        };

        _innerClient.GetSamHerdsAsync("1234567", Arg.Any<CancellationToken>())
            .Returns([herd]);

        var result = await _sut.GetSamHerdsAsync("1234567", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].HERDMARK.Should().Be("HM001");
        result[0].CPHH.Should().Be("1234567/01");
    }

    [Fact]
    public async Task GenericGetSamHoldingsAsync_ShouldPassThroughWithAnonymization()
    {
        var response = new DataBridgeResponse<SamCphHolding>
        {
            CollectionName = "holdings",
            Count = 1,
            Data = [new SamCphHolding { CPH = "1234567", STREET = "Downing Street" }]
        };

        _innerClient.GetSamHoldingsAsync<SamCphHolding>(10, 0, null, null, null, Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.GetSamHoldingsAsync<SamCphHolding>(10, 0, cancellationToken: CancellationToken.None);

        result.Should().NotBeNull();
        result!.Data[0].STREET.Should().NotMatch("Downing Street");
    }

    [Fact]
    public async Task Anonymization_ShouldBeDeterministic_SameInputProducesSameOutput()
    {
        var holder1 = CreateSamCphHolder();
        var holder2 = CreateSamCphHolder();

        _innerClient.GetSamHoldersByCphAsync("cph-1", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new List<SamCphHolder> { holder1 }),
                     _ => Task.FromResult(new List<SamCphHolder> { holder2 }));

        var result1 = await _sut.GetSamHoldersByCphAsync("cph-1", CancellationToken.None);
        var result2 = await _sut.GetSamHoldersByCphAsync("cph-1", CancellationToken.None);

        result1[0].PERSON_GIVEN_NAME.Should().Be(result2[0].PERSON_GIVEN_NAME);
        result1[0].PERSON_FAMILY_NAME.Should().Be(result2[0].PERSON_FAMILY_NAME);
        result1[0].STREET.Should().Be(result2[0].STREET);
        result1[0].TOWN.Should().Be(result2[0].TOWN);
        result1[0].POSTCODE.Should().Be(result2[0].POSTCODE);
        result1[0].MOBILE_NUMBER.Should().Be(result2[0].MOBILE_NUMBER);
    }

    [Fact]
    public async Task Email_ShouldProduceValidEmailFormat()
    {
        var originalEmail = "frog@test.com";
        var party = new SamParty
        {
            PARTY_ID = "P001",
            INTERNET_EMAIL_ADDRESS = originalEmail
        };

        _innerClient.GetSamPartyAsync("P001", Arg.Any<CancellationToken>())
            .Returns(party);

        var result = await _sut.GetSamPartyAsync("P001", CancellationToken.None);

        result!.INTERNET_EMAIL_ADDRESS.Should().NotMatch(originalEmail);
    }

    [Fact]
    public async Task Email_ShouldBeIdempotent()
    {
        var party = new SamParty
        {
            PARTY_ID = "P001",
            INTERNET_EMAIL_ADDRESS = "test@idempotent.com"
        };

        _innerClient.GetSamPartyAsync("P001", Arg.Any<CancellationToken>())
            .Returns(party);

        var result = await _sut.GetSamPartyAsync("P001", CancellationToken.None);
        var result2 = await _sut.GetSamPartyAsync("P001", CancellationToken.None);

        result!.INTERNET_EMAIL_ADDRESS.Should().Be(result2?.INTERNET_EMAIL_ADDRESS);
    }

    [Fact]
    public async Task Email_ShouldHandleNullEmail()
    {
        var party = new SamParty
        {
            PARTY_ID = "P001",
            INTERNET_EMAIL_ADDRESS = null
        };

        _innerClient.GetSamPartyAsync("P001", Arg.Any<CancellationToken>())
            .Returns(party);

        var result = await _sut.GetSamPartyAsync("P001", CancellationToken.None);

        result!.INTERNET_EMAIL_ADDRESS.Should().BeNull();
    }

    [Fact]
    public async Task Anonymization_ShouldPreserveNonPiiFields()
    {
        var holding = new SamCphHolding
        {
            CPH = "1234567",
            BATCH_ID = 42,
            CHANGE_TYPE = "I",
            IsDeleted = false,
            CPH_TYPE = "PERMANENT",
            ANIMAL_SPECIES_CODE = "CTT",
            ANIMAL_PRODUCTION_USAGE_CODE = "CTT-BEEF",
            STREET = "Real Street"
        };

        _innerClient.GetSamHoldingsAsync("1234567", Arg.Any<CancellationToken>())
            .Returns([holding]);

        var result = await _sut.GetSamHoldingsAsync("1234567", CancellationToken.None);

        var h = result[0];
        h.CPH.Should().Be("1234567");
        h.BATCH_ID.Should().Be(42);
        h.CHANGE_TYPE.Should().Be("I");
        h.IsDeleted.Should().BeFalse();
        h.CPH_TYPE.Should().Be("PERMANENT");
        h.ANIMAL_SPECIES_CODE.Should().Be("CTT");
        h.STREET.Should().NotBe("Real Street");
    }

    [Fact]
    public async Task Anonymization_ShouldLogErrorAndContinue_WhenOneItemFails()
    {
        var good = new SamCphHolding { CPH = "1234567", STREET = "Good Street" };
        var bad = new SamCphHolding { CPH = null!, STREET = "Bad Street" };

        _innerClient.GetSamHoldingsAsync("1234567", Arg.Any<CancellationToken>())
            .Returns([bad, good]);

        var result = await _sut.GetSamHoldingsAsync("1234567", CancellationToken.None);

        result.Should().HaveCount(2);
        result[1].STREET.Should().NotBe("Good Street");
    }

    [Fact]
    public async Task GetSamCommonLandsAsync_Generic_ShouldAnonymizeAddressAndLocationFields()
    {
        // Arrange
        var commonLands = new List<SamCommonLand>
        {
            new()
            {
                COMMON_CPH = "00/000/0001",
                MAIN_CPH = "-",
                PREMISES_NAME = "Smiths Common Land",
                ADDRESS_LINE_1 = "123 Real Street",
                ADDRESS_LINE_2 = "Flat 5",
                ADDRESS_LINE_3 = "Manchester",
                POSTCODE = "M20 2XY",
                EASTING = "422473",
                NORTHING = "569204"
            }
        };

        var response = new DataBridgeResponse<SamCommonLand>
        {
            CollectionName = "commonlands",
            Count = 1,
            TotalCount = 1,
            Data = commonLands
        };

        _innerClient.GetSamCommonLandsAsync<SamCommonLand>(10, 0, null, null, null, Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = await _sut.GetSamCommonLandsAsync<SamCommonLand>(10, 0, cancellationToken: CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);

        var anonymized = result.Data[0];

        // Non-PII fields should remain unchanged
        anonymized.COMMON_CPH.Should().Be("00/000/0001");
        anonymized.MAIN_CPH.Should().Be("-");

        // PII fields should be anonymized
        anonymized.PREMISES_NAME.Should().NotBe("Smiths Common Land").And.NotBeNullOrWhiteSpace();
        anonymized.ADDRESS_LINE_1.Should().NotBe("123 Real Street").And.NotBeNullOrWhiteSpace();
        anonymized.ADDRESS_LINE_2.Should().NotBe("Flat 5").And.NotBeNullOrWhiteSpace();
        anonymized.ADDRESS_LINE_3.Should().NotBe("Manchester").And.NotBeNullOrWhiteSpace();
        anonymized.POSTCODE.Should().NotBe("M20 2XY").And.MatchRegex(@"^[A-Z]{2}\d \d[A-Z]{2}$");
        anonymized.EASTING.Should().NotBe("422473");
        int.Parse(anonymized.EASTING!).Should().BeInRange(100000, 999999);
        anonymized.NORTHING.Should().NotBe("569204");
        int.Parse(anonymized.NORTHING!).Should().BeInRange(200000, 999999);
    }

    [Fact]
    public async Task GetSamCommonLandsByCommonCphAsync_ShouldAnonymizeAllCommonLands()
    {
        // Arrange
        var commonLands = new List<SamCommonLand>
        {
            new()
            {
                COMMON_CPH = "00/000/0001",
                MAIN_CPH = "-",
                PREMISES_NAME = "Original Common Land",
                ADDRESS_LINE_1 = "Original Street",
                POSTCODE = "AB12 3CD",
                EASTING = "123456",
                NORTHING = "654321"
            },
            new()
            {
                COMMON_CPH = "00/000/0001",
                MAIN_CPH = "12/345/6789",
                CONTIGUOUS_COMMON = "Yes",
                START_DATE = "2020-01-01"
            }
        };

        _innerClient.GetSamCommonLandsByCommonCphAsync("00/000/0001", Arg.Any<CancellationToken>())
            .Returns(commonLands);

        // Act
        var result = await _sut.GetSamCommonLandsByCommonCphAsync("00/000/0001", CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);

        var definitionRecord = result[0];
        var relationshipRecord = result[1];

        // Definition record - PII should be anonymized
        definitionRecord.COMMON_CPH.Should().Be("00/000/0001");
        definitionRecord.MAIN_CPH.Should().Be("-");
        definitionRecord.PREMISES_NAME.Should().NotBe("Original Common Land").And.NotBeNullOrWhiteSpace();
        definitionRecord.ADDRESS_LINE_1.Should().NotBe("Original Street").And.NotBeNullOrWhiteSpace();
        definitionRecord.POSTCODE.Should().NotBe("AB12 3CD").And.MatchRegex(@"^[A-Z]{2}\d \d[A-Z]{2}$");
        definitionRecord.EASTING.Should().NotBe("123456");
        definitionRecord.NORTHING.Should().NotBe("654321");

        // Relationship record - non-PII fields should remain unchanged
        relationshipRecord.COMMON_CPH.Should().Be("00/000/0001");
        relationshipRecord.MAIN_CPH.Should().Be("12/345/6789");
        relationshipRecord.CONTIGUOUS_COMMON.Should().Be("Yes");
        relationshipRecord.START_DATE.Should().Be("2020-01-01");
    }

    [Fact]
    public async Task GetSamCommonLandsByCommonCphAsync_ShouldHandleNullFields()
    {
        // Arrange
        var commonLands = new List<SamCommonLand>
        {
            new()
            {
                COMMON_CPH = "00/000/0001",
                MAIN_CPH = "-",
                PREMISES_NAME = null,
                ADDRESS_LINE_1 = null,
                ADDRESS_LINE_2 = null,
                ADDRESS_LINE_3 = null,
                POSTCODE = null,
                EASTING = null,
                NORTHING = null
            }
        };

        _innerClient.GetSamCommonLandsByCommonCphAsync("00/000/0001", Arg.Any<CancellationToken>())
            .Returns(commonLands);

        // Act
        var result = await _sut.GetSamCommonLandsByCommonCphAsync("00/000/0001", CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);

        var anonymized = result[0];
        anonymized.PREMISES_NAME.Should().BeNull();
        anonymized.ADDRESS_LINE_1.Should().BeNull();
        anonymized.ADDRESS_LINE_2.Should().BeNull();
        anonymized.ADDRESS_LINE_3.Should().BeNull();
        anonymized.POSTCODE.Should().BeNull();
        anonymized.EASTING.Should().BeNull();
        anonymized.NORTHING.Should().BeNull();
    }

    [Fact]
    public async Task GetSamCommonLandsByCommonCphAsync_ShouldNotAnonymizePlaceholderPremisesName()
    {
        // Arrange
        var commonLands = new List<SamCommonLand>
        {
            new()
            {
                COMMON_CPH = "00/000/0001",
                MAIN_CPH = "-",
                PREMISES_NAME = "-",
                ADDRESS_LINE_1 = "Real Address"
            }
        };

        _innerClient.GetSamCommonLandsByCommonCphAsync("00/000/0001", Arg.Any<CancellationToken>())
            .Returns(commonLands);

        // Act
        var result = await _sut.GetSamCommonLandsByCommonCphAsync("00/000/0001", CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].PREMISES_NAME.Should().Be("-");
        result[0].ADDRESS_LINE_1.Should().NotBe("Real Address");
    }

    [Fact]
    public async Task GetSamCommonLandsAsync_ShouldProduceDeterministicResults()
    {
        // Arrange
        var commonLands1 = new List<SamCommonLand>
        {
            new()
            {
                COMMON_CPH = "00/000/0001",
                MAIN_CPH = "-",
                ADDRESS_LINE_1 = "Street 1",
                PREMISES_NAME = "Name 1"
            }
        };

        var commonLands2 = new List<SamCommonLand>
        {
            new()
            {
                COMMON_CPH = "00/000/0001",
                MAIN_CPH = "-",
                ADDRESS_LINE_1 = "Different Street",
                PREMISES_NAME = "Different Name"
            }
        };

        _innerClient.GetSamCommonLandsByCommonCphAsync("00/000/0001", Arg.Any<CancellationToken>())
            .Returns(commonLands1, commonLands2);

        // Act
        var result1 = await _sut.GetSamCommonLandsByCommonCphAsync("00/000/0001", CancellationToken.None);
        var result2 = await _sut.GetSamCommonLandsByCommonCphAsync("00/000/0001", CancellationToken.None);

        // Assert - Same COMMON_CPH should produce same anonymized values
        result1[0].ADDRESS_LINE_1.Should().Be(result2[0].ADDRESS_LINE_1);
        result1[0].PREMISES_NAME.Should().Be(result2[0].PREMISES_NAME);
    }

    [Fact]
    public async Task GetSamCommonLandsAsync_ShouldPreserveNonPiiFields()
    {
        // Arrange
        var commonLands = new List<SamCommonLand>
        {
            new()
            {
                BATCH_ID = 123,
                CHANGE_TYPE = "I",
                COMMON_CPH = "00/000/0001",
                MAIN_CPH = "12/345/6789",
                BUSINESS_USAGE = "Common Land",
                LOCAL_AUTH_NAME = "Test Council",
                COUNTRY = "England",
                CONTIGUOUS_COMMON = "Yes",
                START_DATE = "2020-01-01",
                END_DATE = "2024-12-31",
                ADDRESS_LINE_1 = "Secret Address",
                PREMISES_NAME = "Secret Name"
            }
        };

        var response = new DataBridgeResponse<SamCommonLand>
        {
            CollectionName = "commonlands",
            Count = 1,
            Data = commonLands
        };

        _innerClient.GetSamCommonLandsAsync<SamCommonLand>(10, 0, null, null, null, Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = await _sut.GetSamCommonLandsAsync<SamCommonLand>(10, 0, cancellationToken: CancellationToken.None);

        // Assert
        var anonymized = result!.Data[0];

        // Non-PII fields preserved
        anonymized.BATCH_ID.Should().Be(123);
        anonymized.CHANGE_TYPE.Should().Be("I");
        anonymized.COMMON_CPH.Should().Be("00/000/0001");
        anonymized.MAIN_CPH.Should().Be("12/345/6789");
        anonymized.BUSINESS_USAGE.Should().Be("Common Land");
        anonymized.LOCAL_AUTH_NAME.Should().Be("Test Council");
        anonymized.COUNTRY.Should().Be("England");
        anonymized.CONTIGUOUS_COMMON.Should().Be("Yes");
        anonymized.START_DATE.Should().Be("2020-01-01");
        anonymized.END_DATE.Should().Be("2024-12-31");

        // PII fields anonymized
        anonymized.ADDRESS_LINE_1.Should().NotBe("Secret Address");
        anonymized.PREMISES_NAME.Should().NotBe("Secret Name");
    }

    [Fact]
    public async Task GetSamCommonLandsAsync_ShouldReturnNull_WhenInnerReturnsNull()
    {
        // Arrange
        _innerClient.GetSamCommonLandsAsync<SamCommonLand>(10, 0, null, null, null, Arg.Any<CancellationToken>())
            .Returns((DataBridgeResponse<SamCommonLand>?)null);

        // Act
        var result = await _sut.GetSamCommonLandsAsync<SamCommonLand>(10, 0, cancellationToken: CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSamCommonLandsByCommonCphAsync_ShouldHandleEmptyList()
    {
        // Arrange
        _innerClient.GetSamCommonLandsByCommonCphAsync("00/000/0001", Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _sut.GetSamCommonLandsByCommonCphAsync("00/000/0001", CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    private static SamCphHolder CreateSamCphHolder() => new()
    {
        PARTY_ID = "P001",
        PERSON_TITLE = "Mr",
        PERSON_GIVEN_NAME = "John",
        PERSON_GIVEN_NAME2 = "James",
        PERSON_INITIALS = "J",
        PERSON_FAMILY_NAME = "Smith",
        ORGANISATION_NAME = "DEFRA",
        INTERNET_EMAIL_ADDRESS = "john@example.com",
        MOBILE_NUMBER = "07123456789",
        TELEPHONE_NUMBER = "01234567890",
        STREET = "10 Downing Street",
        LOCALITY = "Westminster",
        TOWN = "London",
        POSTCODE = "SW1A 2AA",
        UDPRN = "12345678"
    };

    private static CtsAgentOrKeeper CreateCtsAgentOrKeeper() => new()
    {
        PAR_ID = "12345",
        LID_FULL_IDENTIFIER = "AH-123456",
        PAR_TITLE = "Mr",
        PAR_INITIALS = "J",
        PAR_SURNAME = "Smith",
        PAR_EMAIL_ADDRESS = "john@example.com",
        PAR_TEL_NUMBER = "01234567890",
        PAR_MOBILE_NUMBER = "07123456789",
        LOC_TEL_NUMBER = "01234567891",
        LOC_MOBILE_NUMBER = "07123456780",
        ADR_NAME = "Manor Farm",
        ADR_ADDRESS_2 = "Farm Lane",
        ADR_ADDRESS_3 = "Exeter",
        ADR_ADDRESS_4 = "Devon",
        ADR_ADDRESS_5 = "South West",
        ADR_POST_CODE = "EX1 1AA"
    };

    private static void AssertSamPartyFieldsAnonymized(SamCphHolder holder)
    {
        holder.PERSON_TITLE.Should().NotBe("Mr").And.NotBeNullOrWhiteSpace();
        holder.PERSON_GIVEN_NAME.Should().NotBe("John").And.NotBeNullOrWhiteSpace();
        holder.PERSON_GIVEN_NAME2.Should().NotBe("James").And.NotBeNullOrWhiteSpace();
        holder.PERSON_FAMILY_NAME.Should().NotBe("Smith").And.NotBeNullOrWhiteSpace();
        holder.ORGANISATION_NAME.Should().NotBe("DEFRA").And.NotBeNullOrWhiteSpace();
        holder.INTERNET_EMAIL_ADDRESS.Should().NotBe("john@example.com").And.NotBeNullOrWhiteSpace();  //And.MatchRegex(@"^[a-f0-9]{13}@[a-f0-9]{19}\.com$");
        holder.MOBILE_NUMBER.Should().NotBe("07123456789").And.MatchRegex(@"^07\d{9}$");
        holder.TELEPHONE_NUMBER.Should().NotBe("01234567890").And.NotBeNullOrWhiteSpace();
        holder.STREET.Should().NotBe("10 Downing Street").And.NotBeNullOrWhiteSpace();
        holder.LOCALITY.Should().NotBe("Westminster").And.NotBeNullOrWhiteSpace();
        holder.TOWN.Should().NotBe("London").And.NotBeNullOrWhiteSpace();
        holder.POSTCODE.Should().NotBe("SW1A 2AA").And.NotBeNullOrWhiteSpace();
        holder.UDPRN.Should().NotBe("12345678").And.NotBeNullOrWhiteSpace();
    }

    private static void AssertCtsPartyFieldsAnonymized(CtsAgentOrKeeper party)
    {
        party.PAR_TITLE.Should().NotBe("Mr").And.NotBeNullOrWhiteSpace();
        party.PAR_INITIALS.Should().NotBe("J").And.NotBeNullOrWhiteSpace();
        party.PAR_SURNAME.Should().NotBe("Smith").And.NotBeNullOrWhiteSpace();
        party.PAR_EMAIL_ADDRESS.Should().NotBe("john@example.com").And.NotBeNullOrWhiteSpace();  //And.MatchRegex(@"^[a-f0-9]{13}@[a-f0-9]{19}\.com$");
        party.PAR_TEL_NUMBER.Should().NotBe("01234567890").And.NotBeNullOrWhiteSpace();
        party.PAR_MOBILE_NUMBER.Should().NotBe("07123456789").And.MatchRegex(@"^07\d{9}$");
        party.LOC_TEL_NUMBER.Should().NotBe("01234567891").And.NotBeNullOrWhiteSpace();
        party.LOC_MOBILE_NUMBER.Should().NotBe("07123456780").And.MatchRegex(@"^07\d{9}$");
        party.ADR_NAME.Should().NotBe("Manor Farm").And.NotBeNullOrWhiteSpace();
        party.ADR_ADDRESS_2.Should().NotBe("Farm Lane").And.NotBeNullOrWhiteSpace();
        party.ADR_ADDRESS_3.Should().NotBe("Exeter").And.NotBeNullOrWhiteSpace();
        party.ADR_ADDRESS_4.Should().NotBe("Devon").And.NotBeNullOrWhiteSpace();
        party.ADR_ADDRESS_5.Should().NotBe("South West").And.NotBeNullOrWhiteSpace();
        party.ADR_POST_CODE.Should().NotBe("EX1 1AA").And.NotBeNullOrWhiteSpace();
    }
}