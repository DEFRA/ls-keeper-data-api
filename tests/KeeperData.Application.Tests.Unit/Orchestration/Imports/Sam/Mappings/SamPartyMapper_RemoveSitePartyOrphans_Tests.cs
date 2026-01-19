using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Working;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SamPartyMapper_RemoveSitePartyOrphans_Tests
{
    private const string GoldSiteId = "gold-site-id";
    private readonly Mock<IPartiesRepository> _mockGoldPartyRepo = new();

    [Fact]
    public async Task WhenOrphanListIsEmpty_ShouldDoNothing()
    {
        var result = await WhenIRemoveTheseOrphans([]);

        result.Should().BeEmpty();
        _mockGoldPartyRepo.Verify(repo => repo.FindPartyByCustomerNumber(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenExistingPartyNotFoundInRepo_ShouldDoNothing()
    {
        GivenPartyDoesNotExist("customer-a");

        var result = await WhenIRemoveTheseOrphans([("customer-a", "role-1")]);
        result.Should().BeEmpty();
    }

    private void GivenPartyDoesNotExist(string customerNumber)
    {
        _mockGoldPartyRepo
            .Setup(r => r.FindPartyByCustomerNumber(customerNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyDocument?)null);
    }

    [Fact]
    public async Task WhenThePartyIsFoundAndHasOnlyThatRole_ShouldRemoveTheMatchingRole()
    {
        GivenPartyDocument("party-id", "customer-a")
            .WithRole("role-1", siteId: GoldSiteId);

        var result = await WhenIRemoveTheseOrphans([("customer-a", "role-1")]);

        result.Count.Should().Be(1);
        result.ShouldContainPartyWithId("party-id").WithNoRoles();
    }

    private async Task<List<PartyDocument>> WhenIRemoveTheseOrphans((string customerNumber, string? roleTypeId)[] orphans)
    {
        var orphansToClean = orphans.Select(
            o => new SitePartyRoleRelationship()
            {
                CustomerNumber = o.customerNumber,
                HoldingIdentifier = GoldSiteId,
                RoleTypeId = o.roleTypeId
            }).ToList();
        var result = await WhenIRemoveSitePartyOrphans(orphansToClean);
        return result;
    }

    [Fact]
    public async Task WhenThePartyIsFoundAndHasRoleOfThatTypeForAnotherSite_ShouldNotRemoveThatRole()
    {
        GivenPartyDocument("party-id", "customer-a")
            .WithRole("role-1", "wrong-site")
            .WithRole("role-1", GoldSiteId);

        var result = await WhenIRemoveTheseOrphans([("customer-a", "role-1")]);

        result.Count.Should().Be(1);
        result.ShouldContainPartyWithId("party-id").WithRoles([("role-1", "wrong-site")]);
    }

    [Fact]
    public async Task WhenThePartyIsFoundAndHasRoleByThatIdForANullSite_ShouldNotRemoveThatRole()
    {
        GivenPartyDocument("party-id", "customer-a")
            .WithRole("role-1", null)
            .WithRole("role-1", GoldSiteId);

        var result = await WhenIRemoveTheseOrphans([("customer-a", "role-1")]);

        result.Count.Should().Be(1);
        result.ShouldContainPartyWithId("party-id").WithRoles([("role-1", null)]);
    }

    [Fact]
    public async Task WhenRemovingOrphansAcrossMultipleCustomers_ShouldRemoveCorrectRoles()
    {
        GivenPartyDocument("party-id-1", "customer-a")
            .WithRole("role-1", null)
            .WithRole("role-1", GoldSiteId);
        GivenPartyDocument("party-id-2", "customer-b")
            .WithRole("role-2", GoldSiteId)
            .WithRole("role-3", "other-site")
            .WithRole("role-4", GoldSiteId);
        GivenPartyDocument("party-id-3", "customer-c")
            .WithRole("role-1", GoldSiteId)
            .WithRole("role-2", "other-site")
            .WithRole("role-3", GoldSiteId);

        var result = await WhenIRemoveTheseOrphans([
            ("customer-a", "role-1"),
            ("customer-b", "role-4"),
            ("customer-c", "role-1"),
            ("customer-c", "role-3"),
        ]);

        result.Count.Should().Be(3);
        result.ShouldContainPartyWithId("party-id-1").WithRoles([("role-1", null)]);
        result.ShouldContainPartyWithId("party-id-2").WithRoles([("role-2", GoldSiteId), ("role-3", "other-site")]);
        result.ShouldContainPartyWithId("party-id-3").WithRoles([("role-2", "other-site")]);
    }

    private PartyDocumentExtensions.PartyRoleBuilder GivenPartyDocument(string partyId, string customerNumber)
    {
        var party = new PartyDocument()
        {
            Id = partyId,
            CustomerNumber = customerNumber,
            PartyRoles = new List<PartyRoleWithSiteDocument>()
        };

        _mockGoldPartyRepo
            .Setup(r => r.FindPartyByCustomerNumber(customerNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(party);

        var partyRoleBuilder = new PartyDocumentExtensions.PartyRoleBuilder(party);
        return partyRoleBuilder;
    }

    private async Task<List<PartyDocument>> WhenIRemoveSitePartyOrphans(List<SitePartyRoleRelationship> orphansToClean)
    {
        return await SamPartyMapper.RemoveSitePartyOrphans(GoldSiteId, orphansToClean, _mockGoldPartyRepo.Object, CancellationToken.None);
    }
}


internal static class PartyDocumentExtensions
{
    internal class PartyRoleBuilder(PartyDocument party)
    {
        public PartyRoleBuilder WithRole(string roleTypeId, string? siteId)
        {
            party.PartyRoles.Add(
                new PartyRoleWithSiteDocument()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Role = new PartyRoleRoleDocument() { IdentifierId = roleTypeId },
                    Site = siteId != null ? new PartyRoleSiteDocument() { IdentifierId = siteId } : null
                });
            return this;
        }
    }

    internal class ListOfPartyDocumentAssertion(PartyDocument party)
    {

        public void WithRoles((string roleTypeId, string? siteId)[] valueTuple)
        {
            var actualRoles = party.PartyRoles.Select(x => (x.Role.IdentifierId, x.Site?.IdentifierId)).ToArray();
            actualRoles.Should().BeEquivalentTo(valueTuple);
        }

        public void WithNoRoles()
        {
            party.PartyRoles.Should().BeEmpty();
        }
    }

    public static ListOfPartyDocumentAssertion ShouldContainPartyWithId(this List<PartyDocument> result, string partyId)
    {
        result.Should().Contain(pd => pd.Id == partyId);
        var party = result.Single(x => x.Id == partyId);
        return new ListOfPartyDocumentAssertion(party);
    }
}