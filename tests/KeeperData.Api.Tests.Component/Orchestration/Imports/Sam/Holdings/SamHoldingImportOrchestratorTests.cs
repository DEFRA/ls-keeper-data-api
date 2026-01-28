using AutoFixture;
using FluentAssertions;
using KeeperData.Api.Tests.Component.Orchestration.Imports.Sam.Mocks;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Moq.Contrib.HttpClient;
using System.Linq.Expressions;
using System.Net;

namespace KeeperData.Api.Tests.Component.Orchestration.Imports.Sam.Holdings;

public class SamHoldingImportOrchestratorTests : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture;
    private readonly Fixture _fixture;

    public SamHoldingImportOrchestratorTests(AppTestFixture appTestFixture)
    {
        _appTestFixture = appTestFixture;
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenAHoldingIdentifier_WhenExecutingSamHoldingImportOrchestrator_ShouldProcessAllStepsSuccessfully()
    {
        var (holdingIdentifier, holdings, holders, herds, parties) = new MockSamRawDataFactory().CreateMockData(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            holdingCount: 1,
            holderCount: 1,
            herdCount: 1,
            partyCount: 1);

        var (holdingsUri, herdsUri, holdersUri, partiesUri) = GetAllQueryUris(holdingIdentifier, parties.Select(x => x.PARTY_ID));

        SetupRepositoryMocks();
        SetupLookupServiceMocks();

        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, holdingsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(holdings));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, herdsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(herds));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, holdersUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(holders));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, partiesUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(parties));

        var result = await ExecuteTestAsync(_appTestFixture.AppWebApplicationFactory, holdingIdentifier);

        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, holdingsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, herdsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, holdersUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, partiesUri, Times.Once());

        VerifyRawDataTypes(result, holdingIdentifier, holders[0].PARTY_ID, parties[0].PARTY_ID);
        VerifySilverDataTypes(result, holdingIdentifier);
        VerifyGoldDataTypes(result, holdingIdentifier);
    }

    [Fact]
    public async Task GivenExistingRelationship_WhenHolderRemovesCph_ThenRelationshipIsDeleted()
    {
        // Arrange
        var cph = "12/345/6002";
        var partyId = "C123456";
        var existingRelationshipId = Guid.NewGuid().ToString();

        // Create fake gold relationship that would exist prior to the import
        var existingRelationships = new List<Core.Documents.SitePartyRoleRelationshipDocument>
        {
            new()
            {
                Id = existingRelationshipId,
                HoldingIdentifier = cph,
                CustomerNumber = partyId,
                RoleTypeId = "role-id-for-holder",
                SpeciesTypeId = "species-id-1"
            }
        };

        SetupRepositoryMocks();
        SetupLookupServiceMocks();

        // SamHoldingImportPersistenceStep calls FindAsync to detect orphans
        _appTestFixture.AppWebApplicationFactory._goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Core.Documents.SitePartyRoleRelationshipDocument, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRelationships);

        // Holding is returned if it still exists
        var holdings = new List<SamCphHolding> { new MockSamRawDataFactory().CreateMockHolding("U", 1, cph) };

        // Simulating the holder dropping the CPH
        var holders = new List<SamCphHolder>();

        var herds = new List<SamHerd>();
        var parties = new List<SamParty>();

        var (holdingsUri, herdsUri, holdersUri, partiesUri) = GetAllQueryUris(cph, []);

        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, holdingsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(holdings));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, herdsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(herds));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, holdersUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(holders));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, partiesUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(parties));

        // Act
        await ExecuteTestAsync(_appTestFixture.AppWebApplicationFactory, cph);

        // Assert
        // Verify that DeleteManyAsync was called on the relationship repository. 
        _appTestFixture.AppWebApplicationFactory._goldSitePartyRoleRelationshipRepositoryMock.Verify(r =>
            r.DeleteManyAsync(
                It.IsAny<FilterDefinition<Core.Documents.SitePartyRoleRelationshipDocument>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static async Task<SamHoldingImportContext> ExecuteTestAsync(AppWebApplicationFactory factory, string holdingIdentifier)
    {
        var mockPoller = new Mock<IQueuePoller>();
        factory.OverrideServiceAsSingleton(mockPoller.Object);

        var context = new SamHoldingImportContext
        {
            Cph = holdingIdentifier,
            BatchId = 1,
            CurrentDateTime = DateTime.UtcNow
        };

        using var scope = factory.Services.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<SamHoldingImportOrchestrator>();
        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        return context;
    }

    private static void SetupDataBridgeApiRequest(AppWebApplicationFactory factory, string uri, HttpStatusCode httpStatusCode, StringContent httpResponseMessage)
    {
        factory.DataBridgeApiClientHttpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TestConstants.DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(httpStatusCode, httpResponseMessage);
    }

    private static void VerifyDataBridgeApiEndpointCalled(AppWebApplicationFactory factory, string requestUrl, Times times)
    {
        factory.DataBridgeApiClientHttpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TestConstants.DataBridgeApiBaseUrl}/{requestUrl}", times);
    }

    private static void VerifyRawDataTypes(SamHoldingImportContext context, string holdingIdentifier, string samHolderPartyId, string samPartyId)
    {
        context.RawHoldings.Should().NotBeNull().And.HaveCount(1);
        context.RawHoldings[0].CPH.Should().Be(holdingIdentifier);

        context.RawHerds.Should().NotBeNull().And.HaveCount(1);
        context.RawHerds[0].CPHH.Should().Be(holdingIdentifier);

        context.RawHolders.Should().NotBeNull().And.HaveCount(1);
        context.RawHolders[0].PARTY_ID.Should().Be(samHolderPartyId);

        context.RawParties.Should().NotBeNull().And.HaveCount(2);
        context.RawParties.Count(x => x.PARTY_ID == samPartyId).Should().Be(1);
        context.RawParties.Count(x => x.PARTY_ID == samHolderPartyId).Should().Be(1);
    }

    private static void VerifySilverDataTypes(SamHoldingImportContext context, string holdingIdentifier)
    {
        context.SilverHoldings.Should().NotBeNull().And.HaveCount(1);
        context.SilverHoldings[0].CountyParishHoldingNumber.Should().Be(holdingIdentifier);
        context.SilverHoldings[0].GroupMarks.Should().NotBeNull().And.HaveCount(1);
        context.SilverHoldings![0].GroupMarks![0].Should().NotBeNull();
        context.SilverHoldings![0].GroupMarks![0].CountyParishHoldingNumber.Should().Be(holdingIdentifier);

        context.SilverParties.Should().NotBeNull().And.HaveCount(2);

        var distinctRoles = context.RawParties.SelectMany(x => x.RoleList)
            .Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role.Trim())
            .ToArray() ?? [];

        var roleList = context.RawParties[0].ROLES?.Split(",")
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .ToArray() ?? [];

        context.SilverPartyRoles.Should().NotBeNull().And.HaveCount(distinctRoles.Length);

        for (var i = 0; i < context.SilverPartyRoles.Count; i++)
        {
            context.SilverPartyRoles[i].HoldingIdentifier.Should().Be(holdingIdentifier);
        }

        context.SilverHerds.Should().NotBeNull().And.HaveCount(1);
        context.SilverHerds[0].CountyParishHoldingHerd.Should().Contain(holdingIdentifier);
        context.SilverHerds[0].CountyParishHoldingNumber.Should().Be(holdingIdentifier);
    }

    private static void VerifyGoldDataTypes(SamHoldingImportContext context, string holdingIdentifier)
    {
        var goldSiteId = context.GoldSite!.Id;

        context.GoldSite.Should().NotBeNull();
        context.GoldSite.Identifiers[0].Identifier.Should().Be(holdingIdentifier);

        context.GoldParties.Should().NotBeNull().And.HaveCount(2);
        for (var i = 0; i < context.GoldParties.Count; i++)
        {
            if (context.GoldParties[i].PartyRoles.Count != 0)
            {
                context.GoldParties[i].PartyRoles[0].Site!.IdentifierId.Should().Be(goldSiteId);
            }
        }

        context.GoldSitePartyRoles.Should().NotBeNull().And.HaveCount(context.SilverPartyRoles.Count);

        for (var i = 0; i < context.GoldSitePartyRoles.Count; i++)
        {
            context.GoldSitePartyRoles[i].HoldingIdentifier.Should().Be(holdingIdentifier);
        }

        for (var i = 0; i < context.GoldSiteGroupMarks.Count; i++)
        {
            context.GoldSiteGroupMarks[i].HoldingIdentifier.Should().Be(holdingIdentifier);
        }
    }

    private static (string holdingsUri, string herdsUri, string holdersUri, string partiesUri) GetAllQueryUris(string holdingIdentifier, IEnumerable<string> partyIds)
    {
        var holdingsUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHoldings,
            new { },
            DataBridgeQueries.SamHoldingsByCph(holdingIdentifier));

        var herdsUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHerds,
            new { },
            DataBridgeQueries.SamHerdsByCph(holdingIdentifier));

        var holdersUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHolders,
            new { },
            DataBridgeQueries.SamHoldersByCph(holdingIdentifier));

        var partiesUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamParties,
            new { },
            DataBridgeQueries.SamPartiesByPartyIds(partyIds));

        return (holdingsUri, herdsUri, holdersUri, partiesUri);
    }

    private void SetupRepositoryMocks(
        int existingSilverHoldingsCount = 0,
        int existingSilverPartiesCount = 0,
        int existingGoldSitesCount = 0,
        int existingGoldPartiesCount = 0)
    {
        var existingSilverHolding = existingSilverHoldingsCount == 0
            ? null : _fixture.Create<SamHoldingDocument>();

        var existingSilverParties = existingSilverPartiesCount == 0
            ? []
            : _fixture.CreateMany<SamPartyDocument>(existingSilverPartiesCount);

        var existingGoldSite = existingGoldSitesCount == 0
            ? null : _fixture.Create<SiteDocument>();

        var existingGoldParties = existingGoldPartiesCount == 0
            ? []
            : _fixture.CreateMany<PartyDocument>(existingGoldPartiesCount);

        // Silver
        CommonRepositoryMocks.SetupDefaultSamSilverRepositoryMocks(_appTestFixture.AppWebApplicationFactory);

        // Gold
        CommonRepositoryMocks.SetupDefaultGoldRepositoryMocks(_appTestFixture.AppWebApplicationFactory);

        // Overrides

        // Gold Site
        _appTestFixture.AppWebApplicationFactory._goldSiteRepositoryMock
            .Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGoldSite);
    }

    private void SetupLookupServiceMocks()
    {
        _appTestFixture.AppWebApplicationFactory._countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, string? internalCode, CancellationToken token) => (Guid.NewGuid().ToString(), input, input));

        _appTestFixture.AppWebApplicationFactory._premiseActivityTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _appTestFixture.AppWebApplicationFactory._premiseTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _appTestFixture.AppWebApplicationFactory._productionTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _appTestFixture.AppWebApplicationFactory._productionUsageLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _appTestFixture.AppWebApplicationFactory._roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input, input));

        _appTestFixture.AppWebApplicationFactory._speciesTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _appTestFixture.AppWebApplicationFactory._siteIdentifierTypeLookupServiceMock.Setup(x => x.GetByCodeAsync(HoldingIdentifierType.CPHN.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SiteIdentifierTypeDocument
            {
                IdentifierId = "6b4ca299-895d-4cdb-95dd-670de71ff328",
                Code = HoldingIdentifierType.CPHN.ToString(),
                Name = HoldingIdentifierType.CPHN.GetDescription()!,
                IsActive = true
            });
    }
}