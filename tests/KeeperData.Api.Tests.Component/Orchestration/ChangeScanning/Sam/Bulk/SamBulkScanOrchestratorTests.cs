using FluentAssertions;
using KeeperData.Api.Worker.Tasks;
using KeeperData.Core.Locking;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeeperData.Api.Tests.Component.Orchestration.ChangeScanning.Sam.Bulk;

public class SamBulkScanOrchestratorTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture = appTestFixture; [Fact]
    public async Task StartSamBulkScan_WithValidRequest_ShouldExecuteOrchestration()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        var samScanTask = scope.ServiceProvider.GetRequiredService<ISamScanTask>();

        // Act
        var scanCorrelationId = await samScanTask.StartAsync(forceBulk: true);

        // Assert
        scanCorrelationId.Should().NotBeNull("orchestration should start successfully and return a correlation ID");
        scanCorrelationId.Should().NotBe(Guid.Empty, "correlation ID should be a valid GUID");
    }

    [Fact]
    public async Task StartSamBulkScan_WhenDistributedLockCannotBeAcquired_ShouldReturnNull()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();

        var samScanTask = scope.ServiceProvider.GetRequiredService<ISamScanTask>();
        var distributedLock = scope.ServiceProvider.GetRequiredService<IDistributedLock>();

        // Act 1 - Manually acquire the lock. 
        // If it succeeds, we hold it. If it returns null, another test is already holding it.
        // The lock is now guaranteed to be unavailable for StartAsync
        await using var manualLock = await distributedLock.TryAcquireAsync("SamScanTask", TimeSpan.FromMinutes(5));

        // Act 2 - Try to start the scan while the lock is unavailable
        var scanCorrelationId = await samScanTask.StartAsync(forceBulk: true);

        // Assert
        scanCorrelationId.Should().BeNull("the orchestration should fail to acquire the lock and return null");
    }
}