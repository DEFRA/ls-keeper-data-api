using FluentAssertions;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using System.Diagnostics.Metrics;

namespace KeeperData.Infrastructure.Tests.Unit.Telemetry;

public class HealthCheckMetricsTests : IDisposable
{
    private readonly IMeterFactory _meterFactory;
    private readonly HealthCheckMetrics _sut;

    public HealthCheckMetricsTests()
    {
        _meterFactory = new TestMeterFactory();
        _sut = new HealthCheckMetrics(_meterFactory);
    }

    [Theory]
    [InlineData("database", HealthStatus.Healthy, 150.5)]
    [InlineData("redis", HealthStatus.Degraded, 250.0)]
    [InlineData("api", HealthStatus.Unhealthy, 500.75)]
    public void RecordHealthCheck_WhenCalledWithValidParameters_ShouldExecuteSuccessfully(
        string healthCheckName,
        HealthStatus status,
        double durationMs)
    {
        // Act
        var act = () => _sut.RecordHealthCheck(healthCheckName, status, durationMs);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHealthCheck_WhenHealthyStatus_ShouldRecordCorrectMetrics()
    {
        // Arrange
        const string healthCheckName = "test-service";
        const HealthStatus status = HealthStatus.Healthy;
        const double durationMs = 100.0;

        // Act
        var act = () => _sut.RecordHealthCheck(healthCheckName, status, durationMs);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHealthCheck_WhenUnhealthyStatus_ShouldRecordFailureMetrics()
    {
        // Arrange
        const string healthCheckName = "failing-service";
        const HealthStatus status = HealthStatus.Unhealthy;
        const double durationMs = 200.0;

        // Act
        var act = () => _sut.RecordHealthCheck(healthCheckName, status, durationMs);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHealthCheck_WhenDegradedStatus_ShouldRecordFailureMetrics()
    {
        // Arrange
        const string healthCheckName = "degraded-service";
        const HealthStatus status = HealthStatus.Degraded;
        const double durationMs = 300.0;

        // Act
        var act = () => _sut.RecordHealthCheck(healthCheckName, status, durationMs);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordHealthCheck_WhenEmptyHealthCheckName_ShouldExecuteSuccessfully(string healthCheckName)
    {
        // Arrange
        const HealthStatus status = HealthStatus.Healthy;
        const double durationMs = 100.0;

        // Act
        var act = () => _sut.RecordHealthCheck(healthCheckName, status, durationMs);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.MaxValue)]
    public void RecordHealthCheck_WhenExtremeValues_ShouldExecuteSuccessfully(double durationMs)
    {
        // Arrange
        const string healthCheckName = "test-service";
        const HealthStatus status = HealthStatus.Healthy;

        // Act
        var act = () => _sut.RecordHealthCheck(healthCheckName, status, durationMs);

        // Assert
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _meterFactory?.Dispose();
    }

    private class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = new();

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
            {
                meter?.Dispose();
            }
            _meters.Clear();
        }
    }
}