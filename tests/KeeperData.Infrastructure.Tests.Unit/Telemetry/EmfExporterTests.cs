using FluentAssertions;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace KeeperData.Infrastructure.Tests.Unit.Telemetry;

public class EmfExporterTests
{
    private readonly Mock<ILogger> _mockLogger;

    public EmfExporterTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void Init_WhenCalled_ShouldInitializeMeterListenerSuccessfully()
    {
        // Act
        var act = () => EmfExporter.Init(_mockLogger.Object, "test-namespace");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Init_WhenCalledMultipleTimes_ShouldNotThrowException()
    {
        // Act
        var act1 = () => EmfExporter.Init(_mockLogger.Object, "test-namespace");
        var act2 = () => EmfExporter.Init(_mockLogger.Object, "test-namespace");

        // Assert
        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    [Fact]
    public void Init_WhenLoggerIsNull_ShouldHandleGracefully()
    {
        // Act
        var act = () => EmfExporter.Init(null!, "test-namespace");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Init_WhenNamespaceIsNull_ShouldHandleGracefully()
    {
        // Act
        var act = () => EmfExporter.Init(_mockLogger.Object, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Init_WhenNamespaceIsEmpty_ShouldHandleGracefully()
    {
        // Act
        var act = () => EmfExporter.Init(_mockLogger.Object, string.Empty);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void OnMeasurementRecorded_WhenEmfExporterInitialized_ShouldAcceptMeasurements()
    {
        // Arrange
        EmfExporter.Init(_mockLogger.Object, "test-namespace");

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>("test-counter");

        // Act
        var act = () => counter.Add(1);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void OnMeasurementRecorded_WhenCalledWithTaggedMeasurement_ShouldNotThrow()
    {
        // Arrange
        EmfExporter.Init(_mockLogger.Object, "test-namespace");

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>("test-counter");

        var tags = new TagList
        {
            { "service", "keeper-data-api" },
            { "environment", "test" }
        };

        // Act
        var act = () => counter.Add(1, tags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void OnMeasurementRecorded_WhenCalledWithHistogram_ShouldNotThrow()
    {
        // Arrange
        EmfExporter.Init(_mockLogger.Object, "test-namespace");

        using var meter = new Meter(MetricNames.MeterName);
        var histogram = meter.CreateHistogram<double>("test-histogram", "ms", "Test histogram");

        // Act
        var act = () => histogram.Record(123.45);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(999999)]
    [InlineData(-1)]
    public void OnMeasurementRecorded_WhenCalledWithVariousValues_ShouldNotThrow(long value)
    {
        // Arrange
        EmfExporter.Init(_mockLogger.Object, "test-namespace");

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>("test-counter");

        // Act
        var act = () => counter.Add(value);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void OnMeasurementRecorded_WhenCalledWithComplexTags_ShouldNotThrow()
    {
        // Arrange
        EmfExporter.Init(_mockLogger.Object, "test-namespace");

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>("test-counter");

        var tags = new TagList
        {
            { "operation", "get-keeper" },
            { "status", "success" },
            { "endpoint", "/api/v1/keeper" },
            { "method", "GET" }
        };

        // Act
        var act = () => counter.Add(1, tags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void OnMeasurementRecorded_WhenMeterNameDoesNotMatch_ShouldStillNotThrow()
    {
        // Arrange
        EmfExporter.Init(_mockLogger.Object, "test-namespace");

        using var meter = new Meter("DifferentMeterName");
        var counter = meter.CreateCounter<long>("test-counter");

        // Act
        var act = () => counter.Add(1);

        // Assert
        act.Should().NotThrow();
    }
}