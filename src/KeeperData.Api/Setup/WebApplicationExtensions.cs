using Amazon.Runtime.Internal;
using KeeperData.Api.Controllers.ResponseDtos.Scans;
using KeeperData.Api.Middleware;
using KeeperData.Api.Worker.Tasks;
using KeeperData.Infrastructure.Config;
using KeeperData.Infrastructure.Services;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics.CodeAnalysis;

namespace KeeperData.Api.Setup;

public static class WebApplicationExtensions
{
    [ExcludeFromCodeCoverage]
    public static void ConfigureRequestPipeline(this WebApplication app)
    {
        var env = app.Services.GetRequiredService<IWebHostEnvironment>();
        var applicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        var healthcheckMaskingEnabled = configuration.GetValue<bool>("HealthcheckMaskingEnabled");
        var bulkScanEndpointsEnabled = configuration.GetValue<bool>("BulkScanEndpointsEnabled");
        var dailyScanEndpointsEnabled = configuration.GetValue<bool>("DailyScanEndpointsEnabled");

        applicationLifetime.ApplicationStarted.Register(() =>
            logger.LogInformation("{applicationName} started", env.ApplicationName));
        applicationLifetime.ApplicationStopping.Register(() =>
            logger.LogInformation("{applicationName} stopping", env.ApplicationName));
        applicationLifetime.ApplicationStopped.Register(() =>
            logger.LogInformation("{applicationName} stopped", env.ApplicationName));

        app.UseEmfExporter();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseHeaderPropagation();
        app.UseRouting();

        app.MapHealthChecks("/health", new HealthCheckOptions()
        {
            Predicate = _ => true,
            ResponseWriter = (context, healthReport) =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                return context.Response.WriteAsync(HealthCheckWriter.WriteHealthStatusAsJson(healthReport, healthcheckMaskingEnabled: healthcheckMaskingEnabled, excludeHealthy: false, indented: true));
            },
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        app.MapGet("/", () => "Alive!");

        if (bulkScanEndpointsEnabled)
        {
            RegisterScanEndpoint<ICtsBulkScanTask>(app, "/api/import/startCtsBulkScan", "CTS bulk scan");
            RegisterScanEndpoint<ISamBulkScanTask>(app, "/api/import/startSamBulkScan", "SAM bulk scan");
        }

        if (dailyScanEndpointsEnabled)
        {
            RegisterScanEndpoint<ICtsDailyScanTask>(app, "/api/import/startCtsDailyScan", "CTS daily scan");
            RegisterScanEndpoint<ISamDailyScanTask>(app, "/api/import/startSamDailyScan", "SAM daily scan");
        }

        app.MapControllers();
    }

    private static void RegisterScanEndpoint<TTask>(
        WebApplication app,
        string route,
        string scanName)
        where TTask : IScanTask
    {
        app.MapPost(route, async (
            TTask scanTask,
            ILogger<IScanTask> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("Received request to start {scanName} at {requestTime}", scanName, DateTime.UtcNow);

            try
            {
                var scanCorrelationId = await scanTask.StartAsync(cancellationToken);

                if (scanCorrelationId == null)
                {
                    logger.LogWarning("Failed to start {scanName} - could not acquire lock", scanName);
                    return Results.Conflict(new ErrorResponse
                    {
                        Message = $"{scanName} is already running. Please wait for the current import to complete."
                    });
                }

                logger.LogInformation("{scanName} started successfully with scanCorrelationId: {scanCorrelationId}", scanName, scanCorrelationId.Value);

                return Results.Accepted(route, new StartScanResponse
                {
                    ScanCorrelationId = scanCorrelationId.Value,
                    Message = $"{scanName} started successfully and is running in the background.",
                    StartedAt = DateTime.UtcNow
                });
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("{scanName} start request was cancelled", scanName);
                return Results.Json(
                    new ErrorResponse { Message = "Request was cancelled." },
                    statusCode: 499
                );
            }
        });
    }
}