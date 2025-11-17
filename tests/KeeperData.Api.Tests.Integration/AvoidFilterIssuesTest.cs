using FluentAssertions;

namespace KeeperData.Api.Tests.Integration;

public class AvoidFilterIssuesTest
{
    [Fact]
    public void AvoidEmptyFilterIssuesInPipeline()
    {
        var result = true;
        result.Should().BeTrue();
    }
}