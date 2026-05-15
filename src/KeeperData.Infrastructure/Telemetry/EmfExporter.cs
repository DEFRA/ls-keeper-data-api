using Amazon.CloudWatch;
using Amazon.CloudWatch.EMF.Logger;
using Amazon.CloudWatch.EMF.Model;
using Amazon.CloudWatch.Model;
using Humanizer;
using KeeperData.Infrastructure.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Unit = Amazon.CloudWatch.EMF.Model.Unit;
using StandardUnit = Amazon.CloudWatch.StandardUnit;

namespace KeeperData.Infrastructure.Telemetry;

public static class EmfExportExtensions
{
    public static IApplicationBuilder UseEmfExporter(this IApplicationBuilder builder)
    {
        var awsConfig = builder.ApplicationServices.GetRequiredService<IOptions<AwsConfig>>();
        var cloudWatchClient = builder.ApplicationServices.GetService<IAmazonCloudWatch>();

        EmfExporter.Init(
            builder.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(EmfExporter)),
            awsConfig.Value.EMF.Namespace,
            cloudWatchClient);

        return builder;
    }
}

public static class EmfExporter
{
    private static readonly MeterListener meterListener = new();
    private static ILogger log = null!;
    private static string awsNamespace = string.Empty;
    private static IAmazonCloudWatch? _cloudWatchClient; // For local grafana, null in other environments

    public static void Init(ILogger logger, string? awsNamespace, IAmazonCloudWatch? cloudWatchClient = null)
    {
        log = logger;
        EmfExporter.awsNamespace = awsNamespace ?? string.Empty;
        _cloudWatchClient = cloudWatchClient;

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is "KeeperData")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
        meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        meterListener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        meterListener.Start();
    }

    static void OnMeasurementRecorded<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        try
        {
            var value = Convert.ToDouble(measurement);
            var name = instrument.Name.Dehumanize().Camelize();

            // 1. Sanitize tags once to prevent AWS EMF crashes on empty dimensions
            var safeTags = new Dictionary<string, string>();
            foreach (var tag in tags)
            {
                var valStr = tag.Value?.ToString();
                safeTags[tag.Key] = string.IsNullOrWhiteSpace(valStr) ? "unknown" : valStr!;
            }

            // 2. Push standard EMF Metric
            var emfUnit = instrument.Unit == "ea" ? Unit.COUNT : Unit.MILLISECONDS;
            PushEmfMetric(name, value, emfUnit, safeTags);

            // 3. Push LocalStack Metric (if applicable)
            if (_cloudWatchClient != null)
            {
                var standardUnit = instrument.Unit == "ea" ? StandardUnit.Count : StandardUnit.Milliseconds;
                PushLocalStackMetric(name, value, standardUnit, safeTags);
            }
        }
        catch (Exception e)
        {
            log.LogError(e, "Failed to process metric measurement");
        }
    }

    private static void PushEmfMetric(string name, double value, Unit unit, Dictionary<string, string> safeTags)
    {
        using var metricsLogger = new MetricsLogger();
        metricsLogger.SetNamespace(awsNamespace);

        var dimensionSet = new DimensionSet();
        foreach (var tag in safeTags)
        {
            dimensionSet.AddDimension(tag.Key, tag.Value);
        }

        if (!string.IsNullOrEmpty(Activity.Current?.Id))
        {
            metricsLogger.PutProperty("TraceId", Activity.Current.Id);
        }

        metricsLogger.SetDimensions(dimensionSet);
        metricsLogger.PutMetric(name, value, unit);
        metricsLogger.Flush();
    }

    private static void PushLocalStackMetric(string name, double value, StandardUnit unit, Dictionary<string, string> safeTags)
    {
        var dimensions = safeTags.Select(t => new Dimension { Name = t.Key, Value = t.Value }).ToList();

        var request = new PutMetricDataRequest
        {
            Namespace = awsNamespace,
            MetricData =
            [
                new MetricDatum
                {
                    MetricName = name,
                    Value = value,
                    Dimensions = dimensions,
                    Unit = unit,
                    Timestamp = DateTime.UtcNow
                }
            ]
        };

        Task.Run(async () =>
        {
            try
            {
                var response = await _cloudWatchClient!.PutMetricDataAsync(request);
                if ((int)response.HttpStatusCode >= 400)
                {
                    log?.LogWarning("LocalStack CloudWatch rejected metric. Status: {Status}", response.HttpStatusCode);
                }
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Failed to push metric to LocalStack CloudWatch");
            }
        });
    }
}