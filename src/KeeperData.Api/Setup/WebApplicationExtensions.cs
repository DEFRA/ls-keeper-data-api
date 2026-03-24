using KeeperData.Api.Controllers.ResponseDtos;
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
using KeeperData.Application;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.ScanStates;
using KeeperData.Core.DeadLetter;
using KeeperData.Core.Documents;
using Microsoft.AspNetCore.Mvc;
using KeeperData.Core.Repositories;

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
            logger.LogInformation("{ApplicationName} started", env.ApplicationName));
        applicationLifetime.ApplicationStopping.Register(() =>
            logger.LogInformation("{ApplicationName} stopping", env.ApplicationName));
        applicationLifetime.ApplicationStopped.Register(() =>
            logger.LogInformation("{ApplicationName} stopped", env.ApplicationName));

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
                .WithTags("import")
                .WithSummary("Start a CTS bulk scan")
                .WithDescription("Triggers a full bulk scan of CTS data. Only one scan may run at a time.")
                .Produces<StartScanResponse>(StatusCodes.Status202Accepted)
                .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = BasicAuthenticationHandler.SchemeName
                });
            RegisterBulkScanEndpoint<ISamBulkScanTask>(app, "/api/import/startSamBulkScan", "SAM bulk scan")
                .WithGroupName(InternalGroupName)
                .WithTags("import")
                .WithSummary("Start a SAM bulk scan")
                .WithDescription("Triggers a full bulk scan of SAM data. Only one scan may run at a time.")
                .Produces<StartScanResponse>(StatusCodes.Status202Accepted)
                .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = BasicAuthenticationHandler.SchemeName
                });
        }

        if (dailyScanEndpointsEnabled)
        {
            RegisterDailyScanEndpoint<ICtsDailyScanTask>(app, "/api/import/startCtsDailyScan", "CTS daily scan")
                .WithGroupName(InternalGroupName)
                .WithTags("import")
                .WithSummary("Start a CTS daily scan")
                .WithDescription("Triggers a daily incremental scan of CTS data. The sinceHours parameter controls how far back to scan.")
                .Produces<StartScanResponse>(StatusCodes.Status202Accepted)
                .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = BasicAuthenticationHandler.SchemeName
                });
            RegisterDailyScanEndpoint<ISamDailyScanTask>(app, "/api/import/startSamDailyScan", "SAM daily scan")
                .WithGroupName(InternalGroupName)
                .WithTags("import")
                .WithSummary("Start a SAM daily scan")
                .WithDescription("Triggers a daily incremental scan of SAM data. The sinceHours parameter controls how far back to scan.")
                .Produces<StartScanResponse>(StatusCodes.Status202Accepted)
                .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = BasicAuthenticationHandler.SchemeName
                });
        }

        if (adminEndpointsEnabled)
        {
            RegisterAdminDlqEndpoints(app);
            RegisterAdminScanStateEndpoints(app);
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
            .WithTags(DeadLetterQueueServiceConstants.Tags.DeadLetterQueue)
            .WithSummary("Get dead letter queue message count")
            .WithDescription("Returns the approximate number of messages in the dead letter queue")
            .Produces<QueueStats>(StatusCodes.Status200OK)
            .RequireAuthorization(adminAuth);

        app.MapGet("/api/admin/queues/deadletter/peek", GetDeadLetterMessagesHandler)
            .WithGroupName(InternalGroupName)
            .WithTags(DeadLetterQueueServiceConstants.Tags.DeadLetterQueue)
            .WithSummary("Peek at dead letter queue messages")
            .WithDescription("Retrieves up to 10 messages from the dead letter queue. Maximum allowed: 10 messages per request.")
            .WithMetadata(new ProducesResponseTypeAttribute(typeof(DeadLetterMessagesResult), StatusCodes.Status200OK))
            .RequireAuthorization(adminAuth);

        app.MapPost("/api/admin/queues/deadletter/redrive", RedriveDeadLetterMessagesHandler)
            .WithGroupName(InternalGroupName)
            .WithTags(DeadLetterQueueServiceConstants.Tags.DeadLetterQueue)
            .WithSummary("Redrive dead letter queue messages")
            .WithDescription("Moves up to 10 messages from the dead letter queue back to the main queue for reprocessing. Maximum allowed: 10 messages per request.")
            .WithMetadata(new ProducesResponseTypeAttribute(typeof(RedriveSummary), StatusCodes.Status200OK))
            .RequireAuthorization(adminAuth);

        app.MapPost("/api/admin/queues/deadletter/purge", PurgeDeadLetterQueueHandler)
            .WithGroupName(InternalGroupName)
            .WithTags(DeadLetterQueueServiceConstants.Tags.DeadLetterQueue)
            .WithSummary("Purge dead letter queue")
            .WithDescription("⚠️ DESTRUCTIVE: Permanently deletes all messages from the dead letter queue. This operation cannot be undone.")
            .WithMetadata(new ProducesResponseTypeAttribute(typeof(PurgeResult), StatusCodes.Status200OK))
            .RequireAuthorization(adminAuth);
    }

    private static void RegisterAdminScanStateEndpoints(WebApplication app)
    {
        var adminAuth = new AuthorizeAttribute
        {
            AuthenticationSchemes = BasicAuthenticationHandler.SchemeName
        };

        app.MapGet("/api/admin/scanstates", GetScanStatesHandler)
            .WithGroupName(InternalGroupName)
            .WithTags("scan-state")
            .WithSummary("Get scan states")
            .WithDescription("Retrieves all scan state records showing the history and status of scans")
            .WithMetadata(new ProducesResponseTypeAttribute(typeof(IEnumerable<ScanStateDocument>), StatusCodes.Status200OK))
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
        int? maxMessages,
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
            var result = await dlqService.PeekDeadLetterMessagesAsync(maxMessages ?? 10, ct);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to peek DLQ messages");
            return Results.Json(new { error = DeadLetterQueueServiceConstants.LogMessages.UnableToReachDeadLetterQueueError, detail = ex.Message }, statusCode: 503);
        }
    }

    internal static async Task<IResult> RedriveDeadLetterMessagesHandler(
        int? maxMessages,
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
            var summary = await dlqService.RedriveDeadLetterMessagesAsync(maxMessages ?? 10, ct);
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

    internal static async Task<IResult> GetScanStatesHandler(
        IRequestExecutor requestExecutor,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            var query = new GetScanStatesQuery();
            var result = await requestExecutor.ExecuteQuery(query, ct);

            return Results.Ok(result.ScanStates);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve scan states");
            return Results.Json(new { error = "Failed to retrieve scan states", detail = ex.Message }, statusCode: 500);
        }
    }

    private static async Task<IResult> ExecuteScanAsync(
        string route,
        string scanName,
        ILogger logger,
        Func<CancellationToken, Task<Guid?>> startScan,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to start {ScanName} at {RequestTime}", scanName, DateTime.UtcNow);

        try
        {
            var scanCorrelationId = await startScan(cancellationToken);

            if (scanCorrelationId == null)
            {
                logger.LogWarning("Failed to start {ScanName} - could not acquire lock", scanName);
                return Results.Conflict(new ErrorResponse
                {
                    Message = $"{scanName} is already running. Please wait for the current import to complete."
                });
            }

            logger.LogInformation("{ScanName} started successfully with scanCorrelationId: {ScanCorrelationId}", scanName, scanCorrelationId.Value);

            return Results.Accepted(route, new StartScanResponse
            {
                ScanCorrelationId = scanCorrelationId.Value,
                Message = $"{scanName} started successfully and is running in the background.",
                StartedAt = DateTime.UtcNow
            });
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("{ScanName} start request was cancelled", scanName);
            return Results.Json(new ErrorResponse { Message = DeadLetterQueueServiceConstants.LogMessages.RequestCancelledError }, statusCode: 499);
        }
    }
}