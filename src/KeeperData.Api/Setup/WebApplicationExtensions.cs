using Amazon.Runtime.Internal;
using KeeperData.Api.Controllers.ResponseDtos.Scans;
using KeeperData.Api.Middleware;
using KeeperData.Api.Worker.Tasks;
using KeeperData.Infrastructure.Authentication.Handlers;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Services;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics.CodeAnalysis;
using KeeperData.Core.DeadLetter;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Setup;

public static class WebApplicationExtensions
{
    private const string InternalGroupName = "internal";

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
        var adminEndpointsEnabled = configuration.GetValue<bool>("AdminEndpointsEnabled");

        applicationLifetime.ApplicationStarted.Register(() =>
            logger.LogInformation("{applicationName} started", env.ApplicationName));
        applicationLifetime.ApplicationStopping.Register(() =>
            logger.LogInformation("{applicationName} stopping", env.ApplicationName));
        applicationLifetime.ApplicationStopped.Register(() =>
            logger.LogInformation("{applicationName} stopped", env.ApplicationName));

        app.UseEmfExporter();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/public/swagger.json", "Public API");
            options.SwaggerEndpoint("/swagger/internal/swagger.json", "Internal API");
            options.RoutePrefix = "swagger";
        });

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseHeaderPropagation();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

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

        app.MapGet("/", () => "Alive!").ExcludeFromDescription();

        if (bulkScanEndpointsEnabled)
        {
            RegisterBulkScanEndpoint<ICtsBulkScanTask>(app, "/api/import/startCtsBulkScan", "CTS bulk scan")
                .WithGroupName(InternalGroupName)
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = BasicAuthenticationHandler.SchemeName
                });
            RegisterBulkScanEndpoint<ISamBulkScanTask>(app, "/api/import/startSamBulkScan", "SAM bulk scan")
                .WithGroupName(InternalGroupName)
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = BasicAuthenticationHandler.SchemeName
                });
        }

        if (dailyScanEndpointsEnabled)
        {
            RegisterDailyScanEndpoint<ICtsDailyScanTask>(app, "/api/import/startCtsDailyScan", "CTS daily scan")
                .WithGroupName(InternalGroupName)
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = BasicAuthenticationHandler.SchemeName
                });
            RegisterDailyScanEndpoint<ISamDailyScanTask>(app, "/api/import/startSamDailyScan", "SAM daily scan")
                .WithGroupName(InternalGroupName)
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = BasicAuthenticationHandler.SchemeName
                });
        }

        if (adminEndpointsEnabled)
        {
            RegisterAdminDlqEndpoints(app);
        }
    }

    private static RouteHandlerBuilder RegisterBulkScanEndpoint<TTask>(
        WebApplication app,
        string route,
        string scanName)
        where TTask : IScanTask
    {
        return app.MapPost(route, async (
            TTask scanTask,
            ILogger<IScanTask> logger,
            CancellationToken cancellationToken) =>
            await ExecuteScanAsync(route, scanName, logger, ct => scanTask.StartAsync(ct), cancellationToken));
    }

    private static RouteHandlerBuilder RegisterDailyScanEndpoint<TTask>(
        WebApplication app,
        string route,
        string scanName)
        where TTask : IDailyScanTask
    {
        return app.MapPost(route, async (
            [FromQuery] int? sinceHours,
            TTask scanTask,
            ILogger<IScanTask> logger,
            CancellationToken cancellationToken) =>
            await ExecuteScanAsync(route, scanName, logger, ct => scanTask.StartAsync(sinceHours, ct), cancellationToken));
    }

    private static void RegisterAdminDlqEndpoints(WebApplication app)
    {
        var adminAuth = new AuthorizeAttribute
        {
            AuthenticationSchemes = BasicAuthenticationHandler.SchemeName
        };

        app.MapGet("/api/admin/queues/deadletter/count", GetDeadLetterQueueCountHandler)
            .WithGroupName(InternalGroupName)
            .RequireAuthorization(adminAuth);

        app.MapGet("/api/admin/queues/deadletter/messages", GetDeadLetterMessagesHandler)
            .WithGroupName(InternalGroupName)
            .RequireAuthorization(adminAuth);

        app.MapPost("/api/admin/queues/deadletter/redrive", RedriveDeadLetterMessagesHandler)
            .WithGroupName(InternalGroupName)
            .RequireAuthorization(adminAuth);

        app.MapPost("/api/admin/queues/deadletter/purge", PurgeDeadLetterQueueHandler)
            .WithGroupName(InternalGroupName)
            .RequireAuthorization(adminAuth);
    }

    internal static async Task<IResult> GetDeadLetterQueueCountHandler(
        IDeadLetterQueueService dlqService,
        IOptions<IntakeEventQueueOptions> queueOptions,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var dlqUrl = queueOptions.Value.DeadLetterQueueUrl;
        if (string.IsNullOrWhiteSpace(dlqUrl))
            return Results.BadRequest(new { error = DeadLetterQueueServiceConstants.LogMessages.DeadLetterQueueUrlNotConfiguredError });

        try
        {
            var stats = await dlqService.GetQueueStatsAsync(dlqUrl, ct);
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get DLQ stats");
            return Results.Json(new { error = DeadLetterQueueServiceConstants.LogMessages.UnableToReachDeadLetterQueueError, detail = ex.Message }, statusCode: 503);
        }
    }

    internal static async Task<IResult> GetDeadLetterMessagesHandler(
        [FromQuery] int? maxMessages,
        IDeadLetterQueueService dlqService,
        IOptions<IntakeEventQueueOptions> queueOptions,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var dlqUrl = queueOptions.Value.DeadLetterQueueUrl;
        if (string.IsNullOrWhiteSpace(dlqUrl))
            return Results.BadRequest(new { error = DeadLetterQueueServiceConstants.LogMessages.DeadLetterQueueUrlNotConfiguredError });

        var max = Math.Clamp(maxMessages ?? 5, 1, 10);
        try
        {
            var result = await dlqService.PeekDeadLetterMessagesAsync(max, ct);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to peek DLQ messages");
            return Results.Json(new { error = DeadLetterQueueServiceConstants.LogMessages.UnableToReachDeadLetterQueueError, detail = ex.Message }, statusCode: 503);
        }
    }

    internal static async Task<IResult> RedriveDeadLetterMessagesHandler(
        [FromQuery] int? maxMessages,
        IDeadLetterQueueService dlqService,
        IOptions<IntakeEventQueueOptions> queueOptions,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var dlqUrl = queueOptions.Value.DeadLetterQueueUrl;
        if (string.IsNullOrWhiteSpace(dlqUrl))
            return Results.BadRequest(new { error = DeadLetterQueueServiceConstants.LogMessages.DeadLetterQueueUrlNotConfiguredError });

        var max = Math.Clamp(maxMessages ?? 10, 1, 100);
        try
        {
            var summary = await dlqService.RedriveDeadLetterMessagesAsync(max, ct);
            return Results.Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to redrive DLQ messages");
            return Results.Json(new { error = DeadLetterQueueServiceConstants.LogMessages.UnableToReachDeadLetterQueueError, detail = ex.Message }, statusCode: 503);
        }
    }

    internal static async Task<IResult> PurgeDeadLetterQueueHandler(
        IDeadLetterQueueService dlqService,
        IOptions<IntakeEventQueueOptions> queueOptions,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var dlqUrl = queueOptions.Value.DeadLetterQueueUrl;
        if (string.IsNullOrWhiteSpace(dlqUrl))
            return Results.BadRequest(new { error = DeadLetterQueueServiceConstants.LogMessages.DeadLetterQueueUrlNotConfiguredError });

        try
        {
            var result = await dlqService.PurgeDeadLetterQueueAsync(ct);
            return Results.Ok(result);
        }
        catch (Amazon.SQS.Model.PurgeQueueInProgressException)
        {
            return Results.Json(new { error = DeadLetterQueueServiceConstants.LogMessages.PurgeInProgressError }, statusCode: 429);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to purge DLQ");
            return Results.Json(new { error = DeadLetterQueueServiceConstants.LogMessages.UnableToReachDeadLetterQueueError, detail = ex.Message }, statusCode: 503);
        }
    }

    private static async Task<IResult> ExecuteScanAsync(
        string route,
        string scanName,
        ILogger logger,
        Func<CancellationToken, Task<Guid?>> startScan,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to start {scanName} at {requestTime}", scanName, DateTime.UtcNow);

        try
        {
            var scanCorrelationId = await startScan(cancellationToken);

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
            return Results.Json(new ErrorResponse { Message = DeadLetterQueueServiceConstants.LogMessages.RequestCancelledError }, statusCode: 499);
        }
    }
}