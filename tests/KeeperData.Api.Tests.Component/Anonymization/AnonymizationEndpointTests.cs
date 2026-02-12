using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;

namespace KeeperData.Api.Tests.Component.Anonymization;

public class AnonymizationEndpointTests(AppTestFixtureWithAnonymization appTestFixture)
    : IClassFixture<AppTestFixtureWithAnonymization>
{
    private readonly AppTestFixtureWithAnonymization _appTestFixture = appTestFixture;

    [Fact]
    public async Task GetSamHoldings_WithAnonymization_ReturnsAnonymizedPiiData()
    {
        // Arrange
        var originalHoldings = new List<SamCphHolding>
        {
            new()
            {
                CPH = "12/345/6789",
                FEATURE_NAME = "Test Farm",
                OS_MAP_REFERENCE = "SK123456",
                EASTING = 123456,
                NORTHING = 654321,
                STREET = "123 Real Street",
                LOCALITY = "Real Locality",
                TOWN = "Real Town",
                POSTCODE = "AB1 2CD",
                PAON_DESCRIPTION = "Real Building",
                SAON_DESCRIPTION = "Real Apartment"
            }
        };

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHoldings,
            new { },
            DataBridgeQueries.PagedRecords(10, 0, null, null, null));

        SetupDataBridgeApiRequest(uri, HttpStatusCode.OK, () => HttpContentUtility.CreateResponseContentWithEnvelope(originalHoldings));

        // Act
        var result = await ExecuteGetSamHoldingsAsync<SamCphHolding>(10, 0);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);

        var anonymized = result.Data[0];

        // CPH should remain unchanged (not PII)
        anonymized.CPH.Should().Be("12/345/6789");
        anonymized.FEATURE_NAME.Should().Be("Test Farm");

        // PII fields should be anonymized (different from original)
        anonymized.OS_MAP_REFERENCE.Should().NotBe("SK123456");
        anonymized.EASTING.Should().NotBe(123456);
        anonymized.NORTHING.Should().NotBe(654321);
        anonymized.STREET.Should().NotBe("123 Real Street");
        anonymized.LOCALITY.Should().NotBe("Real Locality");
        anonymized.TOWN.Should().NotBe("Real Town");
        anonymized.POSTCODE.Should().NotBe("AB1 2CD");
        anonymized.PAON_DESCRIPTION.Should().NotBe("Real Building");
        anonymized.SAON_DESCRIPTION.Should().NotBe("Real Apartment");

        // Anonymized fields should not be null or empty
        anonymized.OS_MAP_REFERENCE.Should().NotBeNullOrEmpty();
        anonymized.STREET.Should().NotBeNullOrEmpty();
        anonymized.TOWN.Should().NotBeNullOrEmpty();
        anonymized.POSTCODE.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSamHolders_WithAnonymization_ReturnsAnonymizedPersonalData()
    {
        // Arrange
        var originalHolders = new List<SamCphHolder>
        {
            new()
            {
                PARTY_ID = "C1000001",
                PERSON_TITLE = "Mr",
                PERSON_GIVEN_NAME = "John",
                PERSON_GIVEN_NAME2 = "James",
                PERSON_INITIALS = "J",
                PERSON_FAMILY_NAME = "Smith",
                ORGANISATION_NAME = "Smith Farms Ltd",
                INTERNET_EMAIL_ADDRESS = "john.smith@example.com",
                MOBILE_NUMBER = "07700900123",
                TELEPHONE_NUMBER = "01234567890",
                STREET = "123 Farm Lane",
                TOWN = "Farmville",
                POSTCODE = "FA1 2RM"
            }
        };

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHolders,
            new { },
            DataBridgeQueries.SamHoldersByCph("12/345/6789"));

        SetupDataBridgeApiRequest(uri, HttpStatusCode.OK, () => HttpContentUtility.CreateResponseContentWithEnvelope(originalHolders));

        // Act
        var result = await ExecuteGetSamHoldersByCphAsync("12/345/6789");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var anonymized = result[0];

        // Party ID should remain unchanged (identifier, not PII)
        anonymized.PARTY_ID.Should().Be("C1000001");

        // Personal information should be anonymized
        anonymized.PERSON_TITLE.Should().NotBe("Mr");
        anonymized.PERSON_GIVEN_NAME.Should().NotBe("John");
        anonymized.PERSON_GIVEN_NAME2.Should().NotBe("James");
        anonymized.PERSON_FAMILY_NAME.Should().NotBe("Smith");
        anonymized.ORGANISATION_NAME.Should().NotBe("Smith Farms Ltd");
        anonymized.INTERNET_EMAIL_ADDRESS.Should().NotBe("john.smith@example.com");
        anonymized.MOBILE_NUMBER.Should().NotBe("07700900123");
        anonymized.TELEPHONE_NUMBER.Should().NotBe("01234567890");
        anonymized.STREET.Should().NotBe("123 Farm Lane");
        anonymized.TOWN.Should().NotBe("Farmville");
        anonymized.POSTCODE.Should().NotBe("FA1 2RM");

        // Anonymized fields should be valid data
        anonymized.PERSON_GIVEN_NAME.Should().NotBeNullOrEmpty();
        anonymized.PERSON_FAMILY_NAME.Should().NotBeNullOrEmpty();
        anonymized.INTERNET_EMAIL_ADDRESS.Should().Contain("@");
        anonymized.MOBILE_NUMBER.Should().StartWith("07");
        anonymized.TELEPHONE_NUMBER.Should().MatchRegex(@"^\d{5} \d{6}$");
    }

    [Fact]
    public async Task GetSamParties_WithAnonymization_ReturnsAnonymizedData()
    {
        // Arrange
        var originalParties = new List<SamParty>
        {
            new()
            {
                PARTY_ID = "C2000001",
                PERSON_TITLE = "Mrs",
                PERSON_GIVEN_NAME = "Jane",
                PERSON_FAMILY_NAME = "Doe",
                INTERNET_EMAIL_ADDRESS = "jane.doe@example.com",
                MOBILE_NUMBER = "07700900999",
                STREET = "456 Another Street",
                TOWN = "Test City",
                POSTCODE = "TS1 2TC"
            }
        };

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamParties,
            new { },
            DataBridgeQueries.PagedRecords(10, 0, null, null, null));

        SetupDataBridgeApiRequest(uri, HttpStatusCode.OK, () => HttpContentUtility.CreateResponseContentWithEnvelope(originalParties));

        // Act
        var result = await ExecuteGetSamPartiesAsync<SamParty>(10, 0);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);

        var anonymized = result.Data[0];

        anonymized.PARTY_ID.Should().Be("C2000001");
        anonymized.PERSON_TITLE.Should().NotBe("Mrs");
        anonymized.PERSON_GIVEN_NAME.Should().NotBe("Jane");
        anonymized.PERSON_FAMILY_NAME.Should().NotBe("Doe");
        anonymized.INTERNET_EMAIL_ADDRESS.Should().NotBe("jane.doe@example.com");
        anonymized.MOBILE_NUMBER.Should().NotBe("07700900999");
        anonymized.STREET.Should().NotBe("456 Another Street");
        anonymized.TOWN.Should().NotBe("Test City");
        anonymized.POSTCODE.Should().NotBe("TS1 2TC");
    }

    [Fact]
    public async Task GetCtsHoldings_WithAnonymization_ReturnsAnonymizedPiiData()
    {
        // Arrange
        var originalHoldings = new List<CtsCphHolding>
        {
            new()
            {
                LID_FULL_IDENTIFIER = "AG-12/345/6789",
                LOC_MAP_REFERENCE = "TL123456",
                ADR_NAME = "Original Farm Name",
                ADR_ADDRESS_2 = "Original Address Line 2",
                ADR_ADDRESS_3 = "Original Town",
                ADR_ADDRESS_4 = "Original County",
                ADR_POST_CODE = "OR1 2GN",
                LOC_TEL_NUMBER = "01234567890",
                LOC_MOBILE_NUMBER = "07700900111"
            }
        };

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsHoldings,
            new { },
            DataBridgeQueries.PagedRecords(10, 0, null, null, null));

        SetupDataBridgeApiRequest(uri, HttpStatusCode.OK, () => HttpContentUtility.CreateResponseContentWithEnvelope(originalHoldings));

        // Act
        var result = await ExecuteGetCtsHoldingsAsync<CtsCphHolding>(10, 0);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);

        var anonymized = result.Data[0];

        // Identifier should remain unchanged
        anonymized.LID_FULL_IDENTIFIER.Should().Be("AG-12/345/6789");

        // PII fields should be anonymized
        anonymized.LOC_MAP_REFERENCE.Should().NotBe("TL123456");
        anonymized.ADR_NAME.Should().NotBe("Original Farm Name");
        anonymized.ADR_ADDRESS_2.Should().NotBe("Original Address Line 2");
        anonymized.ADR_ADDRESS_3.Should().NotBe("Original Town");
        anonymized.ADR_ADDRESS_4.Should().NotBe("Original County");
        anonymized.ADR_POST_CODE.Should().NotBe("OR1 2GN");
        anonymized.LOC_TEL_NUMBER.Should().NotBe("01234567890");
        anonymized.LOC_MOBILE_NUMBER.Should().NotBe("07700900111");
    }

    [Fact]
    public async Task GetCtsAgents_WithAnonymization_ReturnsAnonymizedPersonalData()
    {
        // Arrange
        var originalAgents = new List<CtsAgentOrKeeper>
        {
            new()
            {
                PAR_ID = "1234567890",
                LID_FULL_IDENTIFIER = "AG-12/345/6789",
                PAR_TITLE = "Dr",
                PAR_INITIALS = "A",
                PAR_SURNAME = "Johnson",
                PAR_EMAIL_ADDRESS = "a.johnson@example.com",
                PAR_TEL_NUMBER = "01234567890",
                PAR_MOBILE_NUMBER = "07700900222",
                ADR_NAME = "Johnson Farm",
                ADR_ADDRESS_2 = "Farm Road",
                ADR_ADDRESS_3 = "Village",
                ADR_POST_CODE = "VL1 2GE"
            }
        };

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsAgents,
            new { },
            DataBridgeQueries.PagedRecords(10, 0, null, null, null));

        SetupDataBridgeApiRequest(uri, HttpStatusCode.OK, () => HttpContentUtility.CreateResponseContentWithEnvelope(originalAgents));

        // Act
        var result = await ExecuteGetCtsAgentsAsync<CtsAgentOrKeeper>(10, 0);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);

        var anonymized = result.Data[0];

        // Identifiers should remain unchanged
        anonymized.PAR_ID.Should().Be("1234567890");
        anonymized.LID_FULL_IDENTIFIER.Should().Be("AG-12/345/6789");

        // Personal information should be anonymized
        anonymized.PAR_TITLE.Should().NotBe("Dr");
        anonymized.PAR_INITIALS.Should().NotBe("A");
        anonymized.PAR_SURNAME.Should().NotBe("Johnson");
        anonymized.PAR_EMAIL_ADDRESS.Should().NotBe("a.johnson@example.com");
        anonymized.PAR_TEL_NUMBER.Should().NotBe("01234567890");
        anonymized.PAR_MOBILE_NUMBER.Should().NotBe("07700900222");
        anonymized.ADR_NAME.Should().NotBe("Johnson Farm");
        anonymized.ADR_ADDRESS_2.Should().NotBe("Farm Road");
        anonymized.ADR_ADDRESS_3.Should().NotBe("Village");
        anonymized.ADR_POST_CODE.Should().NotBe("VL1 2GE");

        // Anonymized data should be valid
        anonymized.PAR_SURNAME.Should().NotBeNullOrEmpty();
        anonymized.PAR_EMAIL_ADDRESS.Should().Contain("@");
    }

    [Fact]
    public async Task GetSamHerds_WithAnonymization_DoesNotAnonymizeNonPiiData()
    {
        // Arrange - SamHerd doesn't contain PII, so should pass through unchanged
        var originalHerds = new List<SamHerd>
        {
            new()
            {
                HERDMARK = "H1000001",
                CPHH = "12/345/6789/01",
                ANIMAL_SPECIES_CODE = "CTT",
                ANIMAL_PURPOSE_CODE = "CTT-BEEF",
                OWNER_PARTY_IDS = "C1000001",
                KEEPER_PARTY_IDS = "C1000001,C1000002"
            }
        };

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHerds,
            new { },
            DataBridgeQueries.PagedRecords(10, 0, null, null, null));

        SetupDataBridgeApiRequest(uri, HttpStatusCode.OK, () => HttpContentUtility.CreateResponseContentWithEnvelope(originalHerds));

        // Act
        var result = await ExecuteGetSamHerdsAsync<SamHerd>(10, 0);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);

        var herd = result.Data[0];

        // SamHerd doesn't contain PII, so all fields should remain unchanged
        herd.HERDMARK.Should().Be("H1000001");
        herd.CPHH.Should().Be("12/345/6789/01");
        herd.ANIMAL_SPECIES_CODE.Should().Be("CTT");
        herd.ANIMAL_PURPOSE_CODE.Should().Be("CTT-BEEF");
        herd.OWNER_PARTY_IDS.Should().Be("C1000001");
        herd.KEEPER_PARTY_IDS.Should().Be("C1000001,C1000002");
    }

    [Fact]
    public async Task GetSamHoldings_WithAnonymization_ProducesDeterministicResults()
    {
        // Arrange - Same CPH should produce same anonymized data (deterministic based on seed)
        var originalHoldings = new List<SamCphHolding>
        {
            new()
            {
                CPH = "12/345/6789",
                STREET = "Original Street",
                TOWN = "Original Town"
            }
        };

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHoldings,
            new { },
            DataBridgeQueries.PagedRecords(10, 0, null, null, null));

        // Setup using factory to create fresh content each time
        SetupDataBridgeApiRequest(uri, HttpStatusCode.OK, () => HttpContentUtility.CreateResponseContentWithEnvelope(originalHoldings));

        // Act
        var result1 = await ExecuteGetSamHoldingsAsync<SamCphHolding>(10, 0);
        var result2 = await ExecuteGetSamHoldingsAsync<SamCphHolding>(10, 0);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        var anonymized1 = result1!.Data[0];
        var anonymized2 = result2!.Data[0];

        // Same CPH should produce same anonymized values (deterministic)
        anonymized1.STREET.Should().Be(anonymized2.STREET);
        anonymized1.TOWN.Should().Be(anonymized2.TOWN);
    }

    [Fact]
    public async Task GetSamHolders_WithNullPiiFields_HandlesGracefully()
    {
        // Arrange
        var originalHolders = new List<SamCphHolder>
        {
            new()
            {
                PARTY_ID = "C3000001",
                PERSON_TITLE = null,
                PERSON_GIVEN_NAME = null,
                PERSON_FAMILY_NAME = null,
                INTERNET_EMAIL_ADDRESS = null,
                MOBILE_NUMBER = null,
                TELEPHONE_NUMBER = null,
                STREET = null,
                TOWN = null,
                POSTCODE = null
            }
        };

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHolders,
            new { },
            DataBridgeQueries.SamHoldersByCph("12/345/6789"));

        SetupDataBridgeApiRequest(uri, HttpStatusCode.OK, () => HttpContentUtility.CreateResponseContentWithEnvelope(originalHolders));

        // Act
        var action = async () => await ExecuteGetSamHoldersByCphAsync("12/345/6789");

        // Assert - should not throw, null fields remain null
        await action.Should().NotThrowAsync();
        var result = await action();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSamParty_SingleParty_WithAnonymization_ReturnsAnonymizedData()
    {
        // Arrange
        var originalParty = new SamParty
        {
            PARTY_ID = "C4000001",
            PERSON_GIVEN_NAME = "Alice",
            PERSON_FAMILY_NAME = "Brown",
            INTERNET_EMAIL_ADDRESS = "alice.brown@example.com"
        };

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamParties,
            new { },
            DataBridgeQueries.SamPartyByPartyId("C4000001"));

        SetupDataBridgeApiRequest(uri, HttpStatusCode.OK, () => HttpContentUtility.CreateResponseContentWithEnvelope(new List<SamParty> { originalParty }));

        // Act
        var result = await ExecuteGetSamPartyAsync("C4000001");

        // Assert
        result.Should().NotBeNull();
        result!.PARTY_ID.Should().Be("C4000001");
        result.PERSON_GIVEN_NAME.Should().NotBe("Alice");
        result.PERSON_FAMILY_NAME.Should().NotBe("Brown");
        result.INTERNET_EMAIL_ADDRESS.Should().NotBe("alice.brown@example.com");
    }

    [Fact]
    public async Task GetCtsKeepers_WithAnonymization_ReturnsAnonymizedPersonalData()
    {
        // Arrange
        var originalKeepers = new List<CtsAgentOrKeeper>
        {
            new()
            {
                PAR_ID = "9876543210",
                LID_FULL_IDENTIFIER = "KP-98/765/4321",
                PAR_SURNAME = "Williams",
                PAR_EMAIL_ADDRESS = "williams@example.com",
                LOC_TEL_NUMBER = "01987654321"
            }
        };

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsKeepers,
            new { },
            DataBridgeQueries.PagedRecords(10, 0, null, null, null));

        SetupDataBridgeApiRequest(uri, HttpStatusCode.OK, () => HttpContentUtility.CreateResponseContentWithEnvelope(originalKeepers));

        // Act
        var result = await ExecuteGetCtsKeepersAsync<CtsAgentOrKeeper>(10, 0);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);

        var anonymized = result.Data[0];
        anonymized.PAR_ID.Should().Be("9876543210");
        anonymized.PAR_SURNAME.Should().NotBe("Williams");
        anonymized.PAR_EMAIL_ADDRESS.Should().NotBe("williams@example.com");
        anonymized.LOC_TEL_NUMBER.Should().NotBe("01987654321");
    }

    // Execute methods using proper scoping
    private async Task<DataBridgeResponse<T>?> ExecuteGetSamHoldingsAsync<T>(int top, int skip)
    {
        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IDataBridgeClient>();
        return await client.GetSamHoldingsAsync<T>(top, skip);
    }

    private async Task<List<SamCphHolder>> ExecuteGetSamHoldersByCphAsync(string cph)
    {
        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IDataBridgeClient>();
        return await client.GetSamHoldersByCphAsync(cph, CancellationToken.None);
    }

    private async Task<DataBridgeResponse<T>?> ExecuteGetSamPartiesAsync<T>(int top, int skip)
    {
        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IDataBridgeClient>();
        return await client.GetSamPartiesAsync<T>(top, skip);
    }

    private async Task<SamParty?> ExecuteGetSamPartyAsync(string partyId)
    {
        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IDataBridgeClient>();
        return await client.GetSamPartyAsync(partyId, CancellationToken.None);
    }

    private async Task<DataBridgeResponse<T>?> ExecuteGetCtsHoldingsAsync<T>(int top, int skip)
    {
        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IDataBridgeClient>();
        return await client.GetCtsHoldingsAsync<T>(top, skip);
    }

    private async Task<DataBridgeResponse<T>?> ExecuteGetCtsAgentsAsync<T>(int top, int skip)
    {
        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IDataBridgeClient>();
        return await client.GetCtsAgentsAsync<T>(top, skip);
    }

    private async Task<DataBridgeResponse<T>?> ExecuteGetCtsKeepersAsync<T>(int top, int skip)
    {
        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IDataBridgeClient>();
        return await client.GetCtsKeepersAsync<T>(top, skip);
    }

    private async Task<DataBridgeResponse<T>?> ExecuteGetSamHerdsAsync<T>(int top, int skip)
    {
        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IDataBridgeClient>();
        return await client.GetSamHerdsAsync<T>(top, skip);
    }

    private void SetupDataBridgeApiRequest(string uri, HttpStatusCode statusCode, Func<StringContent> contentFactory)
    {
        _appTestFixture.DataBridgeApiClientHttpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"{TestConstants.DataBridgeApiBaseUrl}/{uri}")
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = contentFactory()
            });
    }
}