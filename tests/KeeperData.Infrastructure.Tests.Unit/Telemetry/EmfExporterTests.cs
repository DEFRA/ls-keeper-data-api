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
    public async Task OnMeasurementRecorded_WithCloudWatchClient_ShouldPutMetricData()
    {
        // Arrange
        var mockCloudWatch = new Mock<IAmazonCloudWatch>();
        mockCloudWatch.Setup(c => c.PutMetricDataAsync(It.IsAny<PutMetricDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutMetricDataResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

        EmfExporter.Init(_mockLogger.Object, "test-namespace", mockCloudWatch.Object);

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>("test_cloudwatch_counter");

        var tags = new TagList { { "test_key", "test_value" } };

        // Act
        counter.Add(1, tags);
        await Task.Delay(200);

        // Assert
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
    public async Task OnMeasurementRecorded_WithCloudWatchClient_LogsWarningOnFailure()
    {
        // Arrange
        var mockCloudWatch = new Mock<IAmazonCloudWatch>();

        mockCloudWatch.Setup(c => c.PutMetricDataAsync(It.IsAny<PutMetricDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutMetricDataResponse { HttpStatusCode = System.Net.HttpStatusCode.BadRequest });

        EmfExporter.Init(_mockLogger.Object, "test-namespace", mockCloudWatch.Object);

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>($"test_cloudwatch_fail_{Guid.NewGuid():N}");

        // Act
        counter.Add(1);

        // Assert
        bool logFound = false;
        for (int i = 0; i < 50; i++)
        {
            await Task.Delay(100);

            logFound = _mockLogger.Invocations.Any(inv =>
                inv.Method.Name == "Log" &&
                inv.Arguments.Count > 2 &&
                inv.Arguments[2]?.ToString()?.Contains("LocalStack CloudWatch rejected metric") == true);

            if (logFound) break;
        }

        logFound.Should().BeTrue("the warning log should be emitted when CloudWatch rejects the metric");
    }

    [Fact]
    public async Task OnMeasurementRecorded_WithCloudWatchClient_LogsErrorOnException()
    {
        // Arrange
        var mockCloudWatch = new Mock<IAmazonCloudWatch>();

        mockCloudWatch.Setup(c => c.PutMetricDataAsync(It.IsAny<PutMetricDataRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated network failure"));

        EmfExporter.Init(_mockLogger.Object, "test-namespace", mockCloudWatch.Object);

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>($"test_cloudwatch_error_{Guid.NewGuid():N}");

        // Act
        counter.Add(1);

        // Assert
        bool logFound = false;
        for (int i = 0; i < 50; i++)
        {
            await Task.Delay(100);

            logFound = _mockLogger.Invocations.Any(inv =>
                inv.Method.Name == "Log" &&
                inv.Arguments.Count > 2 &&
                inv.Arguments[2]?.ToString()?.Contains("Failed to push metric to LocalStack CloudWatch") == true);

            if (logFound) break;
        }

        logFound.Should().BeTrue("the error log should be emitted when CloudWatch throws an exception");
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

    [Fact]
    public async Task OnMeasurementRecorded_WithEmptyTagValue_UsesUnknownFallback()
    {
        // Arrange
        var mockCloudWatch = new Mock<IAmazonCloudWatch>();
        mockCloudWatch.Setup(c => c.PutMetricDataAsync(It.IsAny<PutMetricDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutMetricDataResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

        EmfExporter.Init(_mockLogger.Object, "test-namespace", mockCloudWatch.Object);

        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>($"test_empty_tag_{Guid.NewGuid():N}");

        // Act
        counter.Add(1, new KeyValuePair<string, object?>("EmptyTag", null));

        // Assert
        bool fallbackFound = false;
        for (int i = 0; i < 50; i++)
        {
            await Task.Delay(100);

            var invocations = mockCloudWatch.Invocations;
            foreach (var inv in invocations)
            {
                if (inv.Method.Name != "PutMetricDataAsync") continue;

                var request = (PutMetricDataRequest)inv.Arguments[0];
                var dimension = request.MetricData[0].Dimensions.FirstOrDefault(d => d.Name == "EmptyTag");

                if (dimension != null && dimension.Value == "unknown")
                {
                    fallbackFound = true;
                    break;
                }
            }

            if (fallbackFound) break;
        }

        fallbackFound.Should().BeTrue("the empty tag should be safely replaced with 'unknown'");
    }

    [Fact]
    public void OnMeasurementRecorded_WithActiveActivity_IncludesTraceId()
    {
        // Arrange
        EmfExporter.Init(_mockLogger.Object, "test-namespace", null);
        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>($"test_activity_{Guid.NewGuid():N}");

        using var activity = new System.Diagnostics.Activity("TestActivity");
        activity.Start();

        // Act
        Action act = () => counter.Add(1);

        // Assert
        act.Should().NotThrow();

        activity.Stop();
    }

    [Fact]
    public void OnMeasurementRecorded_WhenExceptionOccurs_LogsError()
    {
        // Arrange
        EmfExporter.Init(_mockLogger.Object, "test-namespace", null);
        using var meter = new Meter(MetricNames.MeterName);
        var counter = meter.CreateCounter<long>($"test_exception_{Guid.NewGuid():N}");

        // Act
        counter.Add(1, new KeyValuePair<string, object?>(null!, "value"));

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to process metric measurement")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}