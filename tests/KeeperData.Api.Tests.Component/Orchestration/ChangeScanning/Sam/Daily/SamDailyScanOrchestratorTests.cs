using FluentAssertions;
using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeeperData.Api.Tests.Component.Orchestration.ChangeScanning.Sam.Daily;

public class SamDailyScanOrchestratorTests : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture;

    public SamDailyScanOrchestratorTests(AppTestFixture appTestFixture)
    {
        _appTestFixture = appTestFixture;
    }

    [Fact]
    public async Task StartSamDailyScan_WithValidRequest_ShouldExecuteOrchestration()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        var samDailyScanTask = scope.ServiceProvider.GetRequiredService<ISamDailyScanTask>();

        // Act
        var scanCorrelationId = await samDailyScanTask.StartAsync();

        // Assert
        scanCorrelationId.Should().NotBeNull("orchestration should start successfully and return a correlation ID");
        scanCorrelationId.Should().NotBe(Guid.Empty, "correlation ID should be a valid GUID");
    }

    [Fact]
    public async Task StartSamDailyScan_WhenDistributedLockCannotBeAcquired_ShouldReturnNull()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        using var scope1 = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        using var scope2 = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        var firstScanTask = scope1.ServiceProvider.GetRequiredService<ISamDailyScanTask>();
        var secondScanTask = scope2.ServiceProvider.GetRequiredService<ISamDailyScanTask>();

        // Act - Start first scan to hold the distributed lock
        var firstCorrelationId = await firstScanTask.StartAsync();

        // Act - Try to start second scan immediately (should fail to acquire lock)
        var secondCorrelationId = await secondScanTask.StartAsync();

        // Assert
        firstCorrelationId.Should().NotBeNull("first orchestration should start successfully");
        secondCorrelationId.Should().BeNull("second orchestration should fail due to distributed lock being held");
    }
}