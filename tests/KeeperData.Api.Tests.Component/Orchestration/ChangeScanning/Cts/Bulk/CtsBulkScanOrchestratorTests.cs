using FluentAssertions;
using KeeperData.Api.Worker.Tasks;
using KeeperData.Core.Locking;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeeperData.Api.Tests.Component.Orchestration.ChangeScanning.Cts.Bulk;

[Collection("ScanOrchestration")]
public class CtsBulkScanOrchestratorTests(AppTestFixture appTestFixture)
{
    private readonly AppTestFixture _appTestFixture = appTestFixture;

    [SkippableFact]
    public async Task StartCtsBulkScan_WithValidRequest_ShouldExecuteOrchestration()
    {
        Skip.If(TestEnvironmentHelper.IsRunningInCi(), "This test requires local environment");

        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();

        var ctsScanTask = scope.ServiceProvider.GetRequiredService<ICtsScanTask>();

        // Act - Spin-wait to ensure previous tests' background tasks have completely finished
        Guid? scanCorrelationId = null;
        for (int i = 0; i < 50; i++)
        {
            scanCorrelationId = await ctsScanTask.StartAsync(forceBulk: true);
            if (scanCorrelationId != null) break;
            await Task.Delay(100);
        }

        // Assert
        scanCorrelationId.Should().NotBeNull("orchestration should start successfully and return a correlation ID");
        scanCorrelationId.Should().NotBe(Guid.Empty, "correlation ID should be a valid GUID");
    }

    [SkippableFact]
    public async Task StartCtsBulkScan_WhenDistributedLockCannotBeAcquired_ShouldReturnNull()
    {
        Skip.If(TestEnvironmentHelper.IsRunningInCi(), "This test requires local environment");

        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();

        var ctsScanTask = scope.ServiceProvider.GetRequiredService<ICtsScanTask>();
        var distributedLock = scope.ServiceProvider.GetRequiredService<IDistributedLock>();

        // Act 1 - Spin-wait to manually acquire the lock, waiting out any lingering background tasks
        IDistributedLockHandle? manualLock = null;
        for (int i = 0; i < 50; i++)
        {
            manualLock = await distributedLock.TryAcquireAsync("CtsScanTask", TimeSpan.FromMinutes(5));
            if (manualLock != null) break;
            await Task.Delay(100);
        }

        manualLock.Should().NotBeNull("test setup must acquire the initial lock");

        await using (manualLock)
        {
            // Act 2 - Try to start the scan while we explicitly hold the lock
            var scanCorrelationId = await ctsScanTask.StartAsync(forceBulk: true);

            // Assert
            scanCorrelationId.Should().BeNull("the orchestration should fail to acquire the lock and return null");
        }
    }
}