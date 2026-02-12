using FluentAssertions;
using KeeperData.Core.Telemetry;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.Metrics;

namespace KeeperData.Infrastructure.Tests.Unit.Telemetry;

public class HealthCheckMetricsPublisherTests
{
    private readonly HealthCheckMetrics _healthMetrics;
    private readonly Mock<IApplicationMetrics> _mockApplicationMetrics;
    private readonly Mock<ILogger<HealthCheckMetricsPublisher>> _mockLogger;
    private readonly HealthCheckMetricsPublisher _sut;

    public HealthCheckMetricsPublisherTests()
    {
        var testMeterFactory = new TestMeterFactory();
        _healthMetrics = new HealthCheckMetrics(testMeterFactory);
        _mockApplicationMetrics = new Mock<IApplicationMetrics>();
        _mockLogger = new Mock<ILogger<HealthCheckMetricsPublisher>>();

        _sut = new HealthCheckMetricsPublisher(
            _healthMetrics,
            _mockApplicationMetrics.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task PublishAsync_WhenCalledWithHealthyReport_ShouldRecordApplicationMetrics()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await _sut.PublishAsync(healthReport, CancellationToken.None);

        // Assert
        _mockApplicationMetrics.Verify(x => x.RecordRequest(
            "health_check",
            "healthy"), Times.AtLeastOnce);

        _mockApplicationMetrics.Verify(x => x.RecordDuration(
            "health_check",
            It.IsAny<double>()), Times.AtLeastOnce);

        _mockApplicationMetrics.Verify(x => x.RecordRequest(
            "health_check_overall",
            "healthy"), Times.Once);

        _mockApplicationMetrics.Verify(x => x.RecordDuration(
            "health_check_overall",
            It.IsAny<double>()), Times.Once);

        _mockApplicationMetrics.Verify(x => x.RecordCount(
            "health_checks_executed",
            It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenCalledWithUnhealthyReport_ShouldRecordFailureMetrics()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Unhealthy);

        // Act
        await _sut.PublishAsync(healthReport, CancellationToken.None);

        // Assert
        _mockApplicationMetrics.Verify(x => x.RecordRequest(
            "health_check",
            "unhealthy"), Times.AtLeastOnce);

        _mockApplicationMetrics.Verify(x => x.RecordRequest(
            "health_check_overall",
            "unhealthy"), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenCalledWithDegradedReport_ShouldRecordDegradedMetrics()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Degraded);

        // Act
        await _sut.PublishAsync(healthReport, CancellationToken.None);

        // Assert
        _mockApplicationMetrics.Verify(x => x.RecordRequest(
            "health_check",
            "degraded"), Times.AtLeastOnce);

        _mockApplicationMetrics.Verify(x => x.RecordRequest(
            "health_check_overall",
            "degraded"), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenCalledWithMultipleEntries_ShouldProcessEachEntry()
    {
        // Arrange
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["database"] = CreateHealthReportEntry(HealthStatus.Healthy, TimeSpan.FromMilliseconds(100)),
            ["redis"] = CreateHealthReportEntry(HealthStatus.Degraded, TimeSpan.FromMilliseconds(200)),
            ["api"] = CreateHealthReportEntry(HealthStatus.Unhealthy, TimeSpan.FromMilliseconds(300))
        };

        var healthReport = new HealthReport(entries, HealthStatus.Unhealthy, TimeSpan.FromMilliseconds(600));

        // Act
        await _sut.PublishAsync(healthReport, CancellationToken.None);

        // Assert
        _mockApplicationMetrics.Verify(x => x.RecordRequest("health_check", "healthy"), Times.Once);
        _mockApplicationMetrics.Verify(x => x.RecordRequest("health_check", "degraded"), Times.Once);
        _mockApplicationMetrics.Verify(x => x.RecordRequest("health_check", "unhealthy"), Times.Once);
        _mockApplicationMetrics.Verify(x => x.RecordRequest("health_check_overall", "unhealthy"), Times.Once);
        _mockApplicationMetrics.Verify(x => x.RecordCount("health_checks_executed", 3), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenCalledWithEmptyReport_ShouldRecordOverallMetricsOnly()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(50));

        // Act
        await _sut.PublishAsync(healthReport, CancellationToken.None);

        // Assert
        _mockApplicationMetrics.Verify(x => x.RecordRequest("health_check_overall", "healthy"), Times.Once);
        _mockApplicationMetrics.Verify(x => x.RecordDuration("health_check_overall", 50.0), Times.Once);
        _mockApplicationMetrics.Verify(x => x.RecordCount("health_checks_executed", 0), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenCancellationRequested_ShouldCompleteSuccessfully()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _sut.PublishAsync(healthReport, cts.Token);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WhenCalled_ShouldLogDebugMessages()
    {
        // Arrange
        var healthReport = CreateHealthReport(HealthStatus.Healthy);

        // Act
        await _sut.PublishAsync(healthReport, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Published health check metric")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private static HealthReport CreateHealthReport(HealthStatus status)
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["test-service"] = CreateHealthReportEntry(status, TimeSpan.FromMilliseconds(150))
        };

        return new HealthReport(entries, status, TimeSpan.FromMilliseconds(150));
    }

    private static HealthReportEntry CreateHealthReportEntry(HealthStatus status, TimeSpan duration)
    {
        return new HealthReportEntry(
            status,
            "Test health check description",
            duration,
            exception: null,
            data: new Dictionary<string, object>());
    }

    private class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }
}