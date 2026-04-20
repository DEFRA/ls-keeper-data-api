using FluentAssertions;
using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeeperData.Api.Tests.Component.Orchestration.ChangeScanning.Cts.Bulk;

public class CtsBulkScanOrchestratorTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture = appTestFixture;

    [Fact]
    public async Task StartCtsBulkScan_WithValidRequest_ShouldExecuteOrchestration()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        var ctsScanTask = scope.ServiceProvider.GetRequiredService<ICtsScanTask>();

        // Act
        var scanCorrelationId = await ctsScanTask.StartAsync(forceBulk: true);

        // Assert
        scanCorrelationId.Should().NotBeNull("orchestration should start successfully and return a correlation ID");
        scanCorrelationId.Should().NotBe(Guid.Empty, "correlation ID should be a valid GUID");
    }

    [Fact]
    public async Task StartCtsBulkScan_WhenDistributedLockCannotBeAcquired_ShouldReturnNull()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        using var scope1 = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        using var scope2 = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        var firstScanTask = scope1.ServiceProvider.GetRequiredService<ICtsScanTask>();
        var secondScanTask = scope2.ServiceProvider.GetRequiredService<ICtsScanTask>();

        // Act - Start both scans concurrently to ensure lock contention
        var firstScanTaskExecution = firstScanTask.StartAsync(forceBulk: true);
        var secondScanTaskExecution = secondScanTask.StartAsync(forceBulk: true);

        var results = await Task.WhenAll(firstScanTaskExecution, secondScanTaskExecution);

        // Assert - One should succeed, one should fail
        var successfulScans = results.Where(r => r != null).ToList();
        var failedScans = results.Where(r => r == null).ToList();

        successfulScans.Should().ContainSingle("exactly one orchestration should acquire the lock and start successfully");
        failedScans.Should().ContainSingle("exactly one orchestration should fail to acquire the lock");
    }
}