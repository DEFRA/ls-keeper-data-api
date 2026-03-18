using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
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
        var cache = new Mock<IReferenceDataCache>();
        cache.Setup(c => c.ActivityMaps).Returns(new[] { returnedDocument });
        var sut = new ActivityCodeLookupService(cache.Object);

        var returned = await sut.FindByActivityCodeAsync(key, CancellationToken.None);

        returned.premiseActivityType.Should().Be(activityCode);
        returned.premiseType.Should().Be(premiseTypeCode);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData(null)]
    public async Task WhenNotFound_ShouldReturnNull(string? key)
    {
        var cache = new Mock<IReferenceDataCache>();
        cache.Setup(c => c.ActivityMaps).Returns(Array.Empty<FacilityBusinessActivityMapDocument>());
        var sut = new ActivityCodeLookupService(cache.Object);

        var returned = await sut.FindByActivityCodeAsync(key, CancellationToken.None);

        returned.premiseActivityType.Should().BeNull();
        returned.premiseType.Should().BeNull();
    }
}