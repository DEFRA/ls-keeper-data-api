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
        var expected = new Site("", DateTime.MinValue, DateTime.MinValue, "", DateTime.MinValue, null, null, null, null, false, null, null);

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

    private static SiteActivityDocument MakeSiteActivityDocument(string id, string patId = "pat-id", string patCode = "pat-code", string patName = "pat-name")
    {
        return new SiteActivityDocument() { IdentifierId = id, Type = new PremisesActivityTypeSummaryDocument() { IdentifierId = patId, Code = patCode, Name = patName } };
    }

    private static Site EmptySite(DateTime? lastUpdatedDate = null)
    {
        lastUpdatedDate ??= DateTime.MinValue;
        return new Site("", DateTime.MinValue, lastUpdatedDate!.Value, "", DateTime.MinValue, null, null, null, null, false, null, null);
    }

    private static SiteDocument EmptySiteDocument()
    {
        return new SiteDocument() { Id = "", Name = "" };
    }
}