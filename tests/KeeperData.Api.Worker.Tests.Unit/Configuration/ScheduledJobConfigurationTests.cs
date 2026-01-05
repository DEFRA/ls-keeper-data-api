using FluentAssertions;
using KeeperData.Api.Worker.Configuration;
using Xunit;

namespace KeeperData.Api.Worker.Tests.Unit.Configuration;

public class ScheduledJobConfigurationTests
{
    [Fact]
    public void CanSetAndGetProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var config = new ScheduledJobConfiguration
        {
            JobType = "TestJob",
            Enabled = true,
            CronSchedule = "0 0 12 * * ?",
            EnabledFrom = now,
            EnabledTo = now.AddDays(1)
        };

        // Assert
        config.JobType.Should().Be("TestJob");
        config.Enabled.Should().BeTrue();
        config.CronSchedule.Should().Be("0 0 12 * * ?");
        config.EnabledFrom.Should().Be(now);
        config.EnabledTo.Should().Be(now.AddDays(1));
    }
}