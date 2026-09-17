using FluentAssertions;
using KeeperData.Api.Worker.Tasks;
using KeeperData.Core.Locking;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeeperData.Api.Tests.Component.Orchestration.ChangeScanning.Sam.Bulk;

[Collection("ScanOrchestration")]
public class SamBulkScanOrchestratorTests(AppTestFixture appTestFixture)
{
    private readonly AppTestFixture _appTestFixture = appTestFixture;

    [Fact]
    public async Task StartSamBulkScan_WithValidRequest_ShouldExecuteOrchestration()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();

        var samScanTask = scope.ServiceProvider.GetRequiredService<ISamScanTask>();

        // Act - Spin-wait to ensure previous tests' background tasks have completely finished
        Guid? scanCorrelationId = null;
        for (int i = 0; i < 50; i++)
        {
            scanCorrelationId = await samScanTask.StartAsync(forceBulk: true);
            if (scanCorrelationId != null) break;
            await Task.Delay(100);
        }

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

        // Act 1 - Spin-wait to manually acquire the lock, waiting out any lingering background tasks
        IDistributedLockHandle? manualLock = null;
        for (int i = 0; i < 50; i++)
        {
            manualLock = await distributedLock.TryAcquireAsync("SamScanTask", TimeSpan.FromMinutes(5));
            if (manualLock != null) break;
            await Task.Delay(100);
        }

        manualLock.Should().NotBeNull("test setup must acquire the initial lock");

        await using (manualLock)
        {
            // Act 2 - Try to start the scan while we explicitly hold the lock
            var scanCorrelationId = await samScanTask.StartAsync(forceBulk: true);

            // Assert
            scanCorrelationId.Should().BeNull("the orchestration should fail to acquire the lock and return null");
        }
    }
}