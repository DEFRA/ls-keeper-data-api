using FluentAssertions;
using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeeperData.Api.Tests.Component.Orchestration.ChangeScanning.Cts.Daily;

public class CtsDailyScanOrchestratorTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture = appTestFixture;

    [Fact]
    public async Task StartCtsDailyScan_WithValidRequest_ShouldExecuteOrchestration()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        var ctsScanTask = scope.ServiceProvider.GetRequiredService<ICtsScanTask>();

        // Act
        var scanCorrelationId = await ctsScanTask.StartAsync();

        // Assert
        scanCorrelationId.Should().NotBeNull("orchestration should start successfully and return a correlation ID");
        scanCorrelationId.Should().NotBe(Guid.Empty, "correlation ID should be a valid GUID");
    }

    [Fact]
    public async Task StartCtsDailyScan_WhenDistributedLockCannotBeAcquired_ShouldReturnNull()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        using var scope1 = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        using var scope2 = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        var firstScanTask = scope1.ServiceProvider.GetRequiredService<ICtsScanTask>();
        var secondScanTask = scope2.ServiceProvider.GetRequiredService<ICtsScanTask>();

        // Act - Start both scans concurrently to ensure lock contention
        var firstScanTaskExecution = firstScanTask.StartAsync();
        var secondScanTaskExecution = secondScanTask.StartAsync();

        var results = await Task.WhenAll(firstScanTaskExecution, secondScanTaskExecution);

        // Assert - One should succeed, one should fail (or both could fail due to timing)
        var successfulScans = results.Where(r => r != null).ToList();
        var failedScans = results.Where(r => r == null).ToList();

        // At least one should fail (proving the lock works), and at most one should succeed
        failedScans.Should().HaveCountGreaterThanOrEqualTo(1, "at least one orchestration should fail to acquire the lock");
        successfulScans.Should().HaveCountLessThanOrEqualTo(1, "at most one orchestration should acquire the lock and start successfully");
        
        // The total should always be 2
        (successfulScans.Count + failedScans.Count).Should().Be(2, "both scan attempts should complete");
    }
}