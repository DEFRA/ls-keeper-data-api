using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class ActivityCodeLookupServiceTests
{
    [Fact]
    public async Task ShouldReturnRequiredProperties()
    {
        var key = "abc";
        var activityCode = "activityCode";
        var premiseTypeCode = "premiseTypeCode";
        var returnedDocument = new FacilityBusinessActivityMapDocument { IdentifierId = "id1", FacilityActivityCode = key, AssociatedPremiseActivityCode = activityCode, AssociatedPremiseTypeCode = premiseTypeCode };
        var repo = new Mock<IFacilityBusinessActivityMapRepository>();
        repo.Setup(x => x.FindByActivityCodeAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(returnedDocument);
        var sut = new ActivityCodeLookupService(repo.Object);

        var returned = await sut.FindByActivityCodeAsync(key);

        returned.premiseActivityType.Should().Be(activityCode);
        returned.premiseType.Should().Be(premiseTypeCode);
    }

    [Fact]
    public async Task WhenNotFound_ShouldReturnNull()
    {
        var key = "invalid";
        var repo = new Mock<IFacilityBusinessActivityMapRepository>();
        var sut = new ActivityCodeLookupService(repo.Object);

        var returned = await sut.FindByActivityCodeAsync(key);

        returned.premiseActivityType.Should().BeNull();
        returned.premiseType.Should().BeNull();
    }
}