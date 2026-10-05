using FluentAssertions;
using KeeperData.Core.Telemetry;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace KeeperData.Infrastructure.Tests.Unit.Telemetry;

public class EmfExporterTests
{
    private readonly Mock<ILogger> _mockLogger;

    public EmfExporterTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(10);

    // The exporter pushes to CloudWatch on a background task, so wait for the observable side effect.
    private ManualResetEventSlim SignalOnLog(LogLevel level, string messageFragment)
    {
        var signal = new ManualResetEventSlim();

        _mockLogger
            .Setup(x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                if (invocation.Arguments[2]?.ToString()?.Contains(messageFragment, StringComparison.Ordinal) == true)
                {
                    signal.Set();
                }
            }));

        return signal;
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
    [Fact]
    public void OnMeasurementRecorded_WithCloudWatchClient_ShouldPutMetricData()
    {
        // Arrange
        using var metricPut = new ManualResetEventSlim();
        var mockCloudWatch = new Mock<IAmazonCloudWatch>();
        mockCloudWatch.Setup(c => c.PutMetricDataAsync(It.IsAny<PutMetricDataRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutMetricDataRequest, CancellationToken>((request, _) =>
            {
                if (request.MetricData.Any(d => d.Dimensions.Any(dim => dim.Name == "test_key")))
                {
                    metricPut.Set();
                }
            })
            .ReturnsAsync(new PutMetricDataResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

        EmfExporter.Init(_mockLogger.Object, "test-namespace", mockCloudWatch.Object);

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>("test_cloudwatch_counter");

        var tags = new TagList { { "test_key", "test_value" } };

        // Act
        counter.Add(1, tags);

        // Assert
        metricPut.Wait(SignalTimeout).Should().BeTrue("the metric should be pushed to CloudWatch");

        mockCloudWatch.Verify(c => c.PutMetricDataAsync(
            It.Is<PutMetricDataRequest>(r =>
                r.Namespace == "test-namespace" &&
                r.MetricData.Count == 1 &&
                !string.IsNullOrEmpty(r.MetricData[0].MetricName) &&
                r.MetricData[0].Value == 1.0 &&
                r.MetricData[0].Dimensions.Any(d => d.Name == "test_key" && d.Value == "test_value")
            ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void OnMeasurementRecorded_WithCloudWatchClient_LogsWarningOnFailure()
    {
        // Arrange
        var mockCloudWatch = new Mock<IAmazonCloudWatch>();
        mockCloudWatch.Setup(c => c.PutMetricDataAsync(It.IsAny<PutMetricDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutMetricDataResponse { HttpStatusCode = System.Net.HttpStatusCode.BadRequest });

        using var warningLogged = SignalOnLog(LogLevel.Warning, "LocalStack CloudWatch rejected metric");

        EmfExporter.Init(_mockLogger.Object, "test-namespace", mockCloudWatch.Object);

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>("test_cloudwatch_fail");

        // Act
        counter.Add(1);

        // Assert
        warningLogged.Wait(SignalTimeout).Should().BeTrue("the CloudWatch rejection should be logged as a warning");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LocalStack CloudWatch rejected metric")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void OnMeasurementRecorded_WithCloudWatchClient_LogsErrorOnException()
    {
        // Arrange
        var mockCloudWatch = new Mock<IAmazonCloudWatch>();
        mockCloudWatch.Setup(c => c.PutMetricDataAsync(It.IsAny<PutMetricDataRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        using var errorLogged = SignalOnLog(LogLevel.Error, "Failed to push metric to LocalStack CloudWatch");

        EmfExporter.Init(_mockLogger.Object, "test-namespace", mockCloudWatch.Object);

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>("test_cloudwatch_exception");

        // Act
        counter.Add(1);

        // Assert
        errorLogged.Wait(SignalTimeout).Should().BeTrue("the CloudWatch failure should be logged as an error");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to push metric to LocalStack CloudWatch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void OnMeasurementRecorded_WithActiveActivity_ShouldIncludeTraceId()
    {
        // Arrange
        EmfExporter.Init(_mockLogger.Object, "test-namespace");

        using var activity = new Activity("test-activity").Start();

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>("test_activity_counter");

        // Act
        var act = () => counter.Add(1);

        // Assert
        act.Should().NotThrow();
    }
}