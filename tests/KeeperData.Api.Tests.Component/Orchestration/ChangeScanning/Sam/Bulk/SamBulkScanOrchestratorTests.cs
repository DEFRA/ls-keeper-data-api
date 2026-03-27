using FluentAssertions;
using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeeperData.Api.Tests.Component.Orchestration.ChangeScanning.Sam.Bulk;

public class SamBulkScanOrchestratorTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture = appTestFixture;

    [Fact]
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
        using var scope1 = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        using var scope2 = _appTestFixture.AppWebApplicationFactory.Services.CreateScope();
        var firstScanTask = scope1.ServiceProvider.GetRequiredService<ISamScanTask>();
        var secondScanTask = scope2.ServiceProvider.GetRequiredService<ISamScanTask>();

        // Act - Start first scan to hold the distributed lock
        var firstCorrelationId = await firstScanTask.StartAsync(forceBulk: true);

        // Act - Try to start second scan immediately (should fail to acquire lock)
        var secondCorrelationId = await secondScanTask.StartAsync(forceBulk: true);

        // Assert
        firstCorrelationId.Should().NotBeNull("first orchestration should start successfully");
        secondCorrelationId.Should().BeNull("second orchestration should fail due to distributed lock being held");
    }
}