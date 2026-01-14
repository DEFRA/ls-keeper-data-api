using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;

namespace KeeperData.Api.Tests.Component.Orchestration.Imports.Sam.Mocks;

public static class CommonRepositoryMocks
{
    public static void SetupDefaultCtsSilverRepositoryMocks(AppWebApplicationFactory appWebApplicationFactory)
    {
        // Holding
        appWebApplicationFactory._silverCtsHoldingRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<CtsHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CtsHoldingDocument?)null);

        appWebApplicationFactory._silverCtsHoldingRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<CtsHoldingDocument>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._silverCtsHoldingRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<CtsHoldingDocument>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Party
        appWebApplicationFactory._silverCtsPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        appWebApplicationFactory._silverCtsPartyRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<CtsPartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._silverCtsPartyRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<CtsPartyDocument>, UpdateDefinition<CtsPartyDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._silverCtsPartyRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<CtsPartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public static void SetupDefaultSamSilverRepositoryMocks(AppWebApplicationFactory appWebApplicationFactory)
    {
        // Silver Holding
        appWebApplicationFactory._silverSamHoldingRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        appWebApplicationFactory._silverSamHoldingRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SamHoldingDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._silverSamHoldingRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamHoldingDocument>, UpdateDefinition<SamHoldingDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._silverSamHoldingRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHoldingDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Party
        appWebApplicationFactory._silverSamPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SamPartyDocument?)null);

        appWebApplicationFactory._silverSamPartyRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SamPartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._silverSamPartyRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, UpdateDefinition<SamPartyDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Herds
        appWebApplicationFactory._silverSamHerdRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHerdDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        appWebApplicationFactory._silverSamHerdRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SamHerdDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._silverSamHerdRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamHerdDocument>, UpdateDefinition<SamHerdDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._silverSamHerdRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public static void SetupDefaultGoldRepositoryMocks(AppWebApplicationFactory appWebApplicationFactory)
    {
        // Gold Site
        appWebApplicationFactory._goldSiteRepositoryMock
            .Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteDocument?)null);

        appWebApplicationFactory._goldSiteRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._goldSiteRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SiteDocument>, UpdateDefinition<SiteDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Party
        appWebApplicationFactory._goldPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<PartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyDocument?)null);

        appWebApplicationFactory._goldPartyRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<PartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._goldPartyRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<PartyDocument>, UpdateDefinition<PartyDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Site Party Role Relationships
        appWebApplicationFactory._goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Core.Documents.SitePartyRoleRelationshipDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        appWebApplicationFactory._goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<Core.Documents.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        appWebApplicationFactory._goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.GetExistingSitePartyRoleRelationships(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }
}