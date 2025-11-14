using AutoFixture;
using FluentAssertions;
using KeeperData.Application.Extensions;
using KeeperData.Application.Orchestration.Imports.Sam.Holders;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Moq.Contrib.HttpClient;
using System.Linq.Expressions;
using System.Net;

namespace KeeperData.Api.Tests.Component.Orchestration.Imports.Sam.Holders;

public class SamHolderImportOrchestratorTests
{
    private readonly Mock<IGenericRepository<SamPartyDocument>> _silverPartyRepositoryMock = new();
    private readonly Mock<ISilverSitePartyRoleRelationshipRepository> _silverSitePartyRoleRelationshipRepositoryMock = new();

    private readonly Mock<IGenericRepository<PartyDocument>> _goldPartyRepositoryMock = new();
    private readonly Mock<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>> _goldSitePartyRoleRelationshipRepositoryMock = new();

    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();

    private readonly Fixture _fixture;

    public SamHolderImportOrchestratorTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenAHolderIdentifier_WhenExecutingSamHolderImportOrchestrator_ShouldProcessAllStepsSuccessfully()
    {
        var (_, _, holders, _, _) = new MockSamRawDataFactory().CreateMockData(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            holdingCount: 0,
            holderCount: 1,
            herdCount: 0,
            partyCount: 0);

        var holderIdentifier = holders.First().PARTY_ID;

        var holdersUri = GetHoldersQueryUris(holderIdentifier);

        SetupRepositoryMocks();
        SetupLookupServiceMocks();

        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsScoped(_silverPartyRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_silverSitePartyRoleRelationshipRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_goldPartyRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_goldSitePartyRoleRelationshipRepositoryMock.Object);

        // Register lookup service mocks
        factory.OverrideServiceAsScoped(_countryIdentifierLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_roleTypeLookupServiceMock.Object);

        SetupDataBridgeApiRequest(factory, holdersUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(holders));

        var result = await ExecuteTestAsync(factory, holderIdentifier);

        VerifyDataBridgeApiEndpointCalled(factory, holdersUri, Times.Once());

        VerifyRawDataTypes(result, holderIdentifier);
        VerifySilverDataTypes(result, holderIdentifier);
        VerifyGoldDataTypes(result, holderIdentifier);
    }

    private static async Task<SamHolderImportContext> ExecuteTestAsync(AppWebApplicationFactory factory, string holderIdentifier)
    {
        var mockPoller = new Mock<IQueuePoller>();
        factory.OverrideServiceAsSingleton(mockPoller.Object);

        var context = new SamHolderImportContext
        {
            PartyId = holderIdentifier,
            BatchId = 1,
            CurrentDateTime = DateTime.UtcNow
        };

        using var scope = factory.Services.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<SamHolderImportOrchestrator>();
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

    private static void VerifyRawDataTypes(SamHolderImportContext context, string holderIdentifier)
    {
        context.RawHolders.Should().NotBeNull().And.HaveCount(1);
        context.RawHolders[0].PARTY_ID.Should().Be(holderIdentifier);
    }

    private static void VerifySilverDataTypes(SamHolderImportContext context, string holderIdentifier)
    {
        context.SilverParties.Should().NotBeNull().And.HaveCount(1);
        context.SilverPartyRoles.Should().NotBeNull().And.HaveCount(1);

        for (var i = 0; i < context.SilverPartyRoles.Count; i++)
        {
            context.SilverPartyRoles[i].PartyId.Should().Be(holderIdentifier);
            context.SilverPartyRoles[i].SourceRoleName.Should().Be(InferredRoleType.CphHolder.GetDescription());
        }
    }

    private static void VerifyGoldDataTypes(SamHolderImportContext context, string holdingIdentifier)
    {
        // TODO - Verify PartyDocuments
        // TODO - Verify SitePartyRoleRelationshipDocuments
    }

    private static string GetHoldersQueryUris(string holderIdentifier)
    {
        var holdersUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHolders,
            new { },
            DataBridgeQueries.SamPartyByPartyId(holderIdentifier));

        return holdersUri;
    }

    private void SetupRepositoryMocks(
        int existingSilverPartiesCount = 0,
        int existingGoldPartiesCount = 0)
    {
        var existingSilverParties = existingSilverPartiesCount == 0
            ? []
            : _fixture.CreateMany<SamPartyDocument>(existingSilverPartiesCount);

        var existingGoldParties = existingGoldPartiesCount == 0
            ? []
            : _fixture.CreateMany<PartyDocument>(existingGoldPartiesCount);

        // Silver Party
        _silverPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(existingSilverParties.FirstOrDefault()));

        _silverPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Role Relationships
        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Core.Documents.Silver.SitePartyRoleRelationshipDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>, Core.Documents.Silver.SitePartyRoleRelationshipDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Party
        _goldPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<PartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(existingGoldParties.FirstOrDefault()));

        _goldPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<PartyDocument>, PartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Site Party Role Relationships
        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Core.Documents.SitePartyRoleRelationshipDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<Core.Documents.SitePartyRoleRelationshipDocument>, Core.Documents.SitePartyRoleRelationshipDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupLookupServiceMocks()
    {
        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));
    }
}