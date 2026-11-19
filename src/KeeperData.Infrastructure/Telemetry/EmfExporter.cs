using Amazon.CloudWatch.EMF.Logger;
using Amazon.CloudWatch.EMF.Model;
using Humanizer;
using KeeperData.Infrastructure.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Unit = Amazon.CloudWatch.EMF.Model.Unit;

namespace KeeperData.Infrastructure.Telemetry;

public static class EmfExportExtensions
{
    public static IApplicationBuilder UseEmfExporter(this IApplicationBuilder builder)
    {
        var awsConfig = builder.ApplicationServices.GetRequiredService<IOptions<AwsConfig>>();

        EmfExporter.Init(
            builder.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(EmfExporter)),
            awsConfig.Value.EMF.Namespace);

        return builder;
    }
}
public static class EmfExporter
{
    private static readonly MeterListener meterListener = new();
    private static ILogger log = null!;
    private static string awsNamespace = string.Empty;

    public static void Init(ILogger logger, string? awsNamespace)
    {
        log = logger;
        EmfExporter.awsNamespace = awsNamespace ?? string.Empty;

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
            var unit = instrument.Unit == "ea" ? Unit.COUNT : Unit.MILLISECONDS;

            using (var metricsLogger = new MetricsLogger())
            {
                metricsLogger.SetNamespace(awsNamespace);
                var dimensionSet = new DimensionSet();

                foreach (var tag in tags)
                {
                    dimensionSet.AddDimension(tag.Key, tag.Value?.ToString());
                }

                if (!string.IsNullOrEmpty(Activity.Current?.Id))
                {
                    metricsLogger.PutProperty("TraceId", Activity.Current.Id);
                }

                metricsLogger.SetDimensions(dimensionSet);
                metricsLogger.PutMetric(name, value, unit);
                metricsLogger.Flush();
            }
        }
        catch (Exception e)
        {
            log.LogError(e, "Failed to push EMF metric");
        }
    }
}