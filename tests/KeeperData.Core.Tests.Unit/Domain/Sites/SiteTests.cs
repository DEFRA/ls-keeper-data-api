using FluentAssertions;
using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Domain.Sites;

namespace KeeperData.Core.Tests.Unit.Domain.Sites;

public class SiteTests
{
    [Fact]
    public void UpdateLocationFromNull_ShouldUpdateLastUpdatedDate()
    {
        var oldLastUpdatedDate = new DateTime(2020, 1, 1);
        var newLastUpdatedDate = new DateTime(2025, 1, 1);
        var sut = CreateSite(oldLastUpdatedDate, null);

        sut.SetLocation(newLastUpdatedDate, "os-ref", 1.0, 2.0, null, null);

        sut.LastUpdatedDate.Should().Be(newLastUpdatedDate);
        sut.Location!.OsMapReference.Should().Be("os-ref");
    }

    [Fact]
    public void UpdateLocation_ShouldUpdateLastUpdatedDate()
    {
        var oldLastUpdatedDate = new DateTime(2020, 1, 1);
        var newLastUpdatedDate = new DateTime(2025, 1, 1);
        var sut = CreateSite(oldLastUpdatedDate, new Location("id", oldLastUpdatedDate, "old-os-ref", 0, 0, null, null));

        sut.SetLocation(newLastUpdatedDate, "os-ref", 1.0, 2.0, null, null);

        sut.LastUpdatedDate.Should().Be(newLastUpdatedDate);
        sut.Location!.OsMapReference.Should().Be("os-ref");
    }

    [Fact]
    public void UpdateSiteIdFromNull_ShouldUpdateLastUpdatedDate()
    {
        var oldLastUpdatedDate = new DateTime(2020, 1, 1);
        var newLastUpdatedDate = new DateTime(2025, 1, 1);
        var sut = CreateSite(oldLastUpdatedDate, null);

        sut.SetSiteIdentifier(
            DateTime.MinValue,
            "site-id",
            new SiteIdentifierType("sit-id", "sit-code", "sit-name", null),
            null,
            newLastUpdatedDate);

        sut.LastUpdatedDate.Should().Be(newLastUpdatedDate);
        sut.Identifiers.First().Identifier.Should().Be("site-id");
        sut.Identifiers.First().Type.Id.Should().Be("sit-id");
    }

    [Fact]
    public void UpdateSiteId_ShouldUpdateLastUpdatedDate()
    {
        var oldLastUpdatedDate = new DateTime(2020, 1, 1);
        var newLastUpdatedDate = new DateTime(2025, 1, 1);
        var sut = CreateSite(oldLastUpdatedDate, null);

        sut.SetSiteIdentifier(
            DateTime.MinValue,
            "site-id",
            new SiteIdentifierType("sit-id", "sit-code", "sit-name", null),
            null,
            oldLastUpdatedDate);

        sut.SetSiteIdentifier(
            DateTime.MinValue,
            "new-site-id",
            new SiteIdentifierType("sit-id", "sit-code", "sit-name", null),
            null,
            newLastUpdatedDate);

        sut.LastUpdatedDate.Should().Be(newLastUpdatedDate);
        sut.Identifiers.First().Identifier.Should().Be("new-site-id");
        sut.Identifiers.First().Type.Id.Should().Be("sit-id");
    }

    [Fact]
    public void UpdateActivitiesFromNull_ShouldUpdateLastUpdatedDate()
    {
        var oldLastUpdatedDate = new DateTime(2020, 1, 1);
        var newLastUpdatedDate = new DateTime(2025, 1, 1);
        var sut = CreateSite(oldLastUpdatedDate, null);

        sut.SetActivities([new SiteActivity("act-id", new SiteActivityType("sat-id", null, "sat-code", "sat-name"), null, null, DateTime.MinValue)], newLastUpdatedDate);

        sut.LastUpdatedDate.Should().Be(newLastUpdatedDate);
        sut.Activities.First().Type.Code.Should().Be("sat-code");
    }

    [Fact]
    public void UpdateActivities_ShouldUpdateLastUpdatedDate()
    {
        var oldLastUpdatedDate = new DateTime(2020, 1, 1);
        var newLastUpdatedDate = new DateTime(2025, 1, 1);
        var sut = CreateSite(oldLastUpdatedDate, null);
        var oldActStartDate = new DateTime(2000, 1, 1);
        sut.SetActivities([new SiteActivity("act-id", new SiteActivityType("sat-id", null, "sat-code", "sat-name"), oldActStartDate, null, DateTime.MinValue)], oldLastUpdatedDate);

        var newActStartDate = new DateTime(2021, 1, 1);
        sut.SetActivities([new SiteActivity("act-id", new SiteActivityType("sat-id", null, "sat-code", "sat-name"), newActStartDate, null, DateTime.MinValue)], newLastUpdatedDate);

        sut.LastUpdatedDate.Should().Be(newLastUpdatedDate);
        sut.Activities.First().StartDate.Should().Be(newActStartDate);
    }

    [Fact]
    public void UpdateActivitiesWithSameActivities_ShouldNotUpdate()
    {
        var oldLastUpdatedDate = new DateTime(2020, 1, 1);
        var newLastUpdatedDate = new DateTime(2025, 1, 1);
        var sut = CreateSite(oldLastUpdatedDate, null);
        var oldActStartDate = new DateTime(2000, 1, 1);
        sut.SetActivities([
            CreateSiteActivity("act-1-id"),
            CreateSiteActivity("act-2-id"),
            CreateSiteActivity("act-3-id")], oldLastUpdatedDate);

        var newActStartDate = new DateTime(2021, 1, 1);
        sut.SetActivities([
            CreateSiteActivity("act-1-id"),
            CreateSiteActivity("act-2-id"),
            CreateSiteActivity("act-3-id")], newLastUpdatedDate);

        sut.LastUpdatedDate.Should().Be(oldLastUpdatedDate);
        sut.Activities.Select(x => x.Id).Should().BeEquivalentTo(["act-1-id", "act-2-id", "act-3-id"]);
    }

    [Fact]
    public void UpdateActivities_ShouldRemoveOrphans()
    {
        var oldLastUpdatedDate = new DateTime(2020, 1, 1);
        var newLastUpdatedDate = new DateTime(2025, 1, 1);
        var sut = CreateSite(oldLastUpdatedDate, null);
        var oldActStartDate = new DateTime(2000, 1, 1);
        sut.SetActivities([
            CreateSiteActivity("act-1-id"),
            CreateSiteActivity("act-2-id"),
            CreateSiteActivity("act-3-id")], oldLastUpdatedDate);

        var newActStartDate = new DateTime(2021, 1, 1);
        sut.SetActivities([
            CreateSiteActivity("act-2-id"),
            CreateSiteActivity("act-4-id")], newLastUpdatedDate);

        sut.LastUpdatedDate.Should().Be(newLastUpdatedDate);
        sut.Activities.Select(x => x.Id).Should().BeEquivalentTo(["act-2-id", "act-4-id"]);
    }

    private static SiteActivity CreateSiteActivity(string actId)
    {
        return new SiteActivity(actId, new SiteActivityType("sat-id", null, "sat-code", "sat-name"), null, null, DateTime.MinValue);
    }

    private static Site CreateSite(DateTime lastUpdatedDate, Location? location = null)
    {
        return new Site("id", DateTime.MinValue, lastUpdatedDate, "site-name", DateTime.MinValue, null, null, null, null, false, null, location);
    }
}