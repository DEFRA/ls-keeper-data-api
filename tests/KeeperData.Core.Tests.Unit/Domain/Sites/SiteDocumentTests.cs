using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Sites;

namespace KeeperData.Core.Tests.Unit.Domain.Sites;

public class SiteDocumentTests
{
    [Fact]
    public void WhenSiteIsEmpty_ToDomainShouldMapCorrectly()
    {
        var sut = new SiteDocument() { Id = "", Name = "" };
        var expected = new Site("", DateTime.MinValue, DateTime.MinValue, "", DateTime.MinValue, null, null, null, null, false, null, null, null, null, null, null, null);

        var result = sut.ToDomain();

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void WhenSiteHasActivities_ToDomainShouldMapCorrectly()
    {
        var lastUpdatedDate = new DateTime(2001, 1, 1);

        var sut = EmptySiteDocument();
        sut.Activities = [MakeSiteActivityDocument("act-id", "pat-id", "pat-code", "pat-name")];
        sut.LastUpdatedDate = lastUpdatedDate;
        var expected = EmptySite(lastUpdatedDate);
        expected.SetActivities([new SiteActivity("act-id", new SiteActivityType("pat-id", DateTime.MinValue, "pat-code", "pat-name"), DateTime.MinValue, DateTime.MinValue, DateTime.MinValue)], lastUpdatedDate);

        var result = sut.ToDomain();

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void WhenSiteHasMultipleActivities_ToDomainShouldMapAllOfThem()
    {
        var lastUpdatedDate = new DateTime(2001, 1, 1);

        var sut = EmptySiteDocument();
        sut.Activities = [MakeSiteActivityDocument("act-1-id"), MakeSiteActivityDocument("act-2-id"), MakeSiteActivityDocument("act-3-id")];
        sut.LastUpdatedDate = lastUpdatedDate;

        var result = sut.ToDomain();

        result.Activities.Select(x => x.Id).Should().BeEquivalentTo(["act-1-id", "act-2-id", "act-3-id"]);
    }

    [Fact]
    public void FromDomain_WithPermanentLandHoldingIdentifier_ShouldMapCorrectly()
    {
        var permanentLandHoldingId = "12/345/9999";
        var site = Site.Create(
            "site-id",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Site",
            DateTime.UtcNow,
            null,
            null,
            "SAM",
            null,
            false,
            null,
            "PERMANENT",
            null,
            null,
            permanentLandHoldingId);

        var result = SiteDocument.FromDomain(site);

        result.PermanentLandHoldingIdentifier.Should().Be(permanentLandHoldingId);
    }

    [Fact]
    public void FromDomain_WithNullPermanentLandHoldingIdentifier_ShouldBeNull()
    {
        var site = Site.Create(
            "site-id",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Site",
            DateTime.UtcNow,
            null,
            null,
            "SAM",
            null,
            false,
            null,
            "PERMANENT",
            null,
            null,
            null);

        var result = SiteDocument.FromDomain(site);

        result.PermanentLandHoldingIdentifier.Should().BeNull();
    }

    [Fact]
    public void ToDomain_WithPermanentLandHoldingIdentifier_ShouldMapCorrectly()
    {
        var permanentLandHoldingId = "12/345/9999";
        var siteDocument = new SiteDocument
        {
            Id = "site-id",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            PermanentLandHoldingIdentifier = permanentLandHoldingId
        };

        var result = siteDocument.ToDomain();

        result.PermanentLandHoldingIdentifier.Should().Be(permanentLandHoldingId);
    }

    [Fact]
    public void ToDomain_WithNullPermanentLandHoldingIdentifier_ShouldBeNull()
    {
        var siteDocument = new SiteDocument
        {
            Id = "site-id",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            PermanentLandHoldingIdentifier = null
        };

        var result = siteDocument.ToDomain();

        result.PermanentLandHoldingIdentifier.Should().BeNull();
    }

    private static SiteActivityDocument MakeSiteActivityDocument(string id, string patId = "pat-id", string patCode = "pat-code", string patName = "pat-name")
    {
        return new SiteActivityDocument() { IdentifierId = id, Type = new SiteActivityTypeSummaryDocument() { IdentifierId = patId, Code = patCode, Name = patName } };
    }

    private static Site EmptySite(DateTime? lastUpdatedDate = null)
    {
        lastUpdatedDate ??= DateTime.MinValue;
        return new Site("", DateTime.MinValue, lastUpdatedDate!.Value, "", DateTime.MinValue, null, null, null, null, false, null, null, null, null, null, null, null);
    }

    private static SiteDocument EmptySiteDocument()
    {
        return new SiteDocument() { Id = "", Name = "" };
    }
}