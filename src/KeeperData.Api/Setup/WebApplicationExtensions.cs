using KeeperData.Api.Middleware;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics.CodeAnalysis;

namespace KeeperData.Api.Setup;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Mongo-backed endpoints retired by LKPR-204. The controllers remain so the API can be
    /// reinstated via <c>LegacyEndpointsEnabled</c>, but they only ever serve stale data now
    /// that the ingest pipeline has moved to the data-bridge ETL.
    /// </summary>
    private static readonly PathString[] LegacyRoutePrefixes =
    [
        "/api/sites",
        "/api/parties",
        "/api/countries",
        "/api/sitetypes",
        "/api/species",
        "/api/reference"
    ];

    public static void ConfigureOpenApiGenerationPipeline(this WebApplication app)
    {
        app.MapControllers();
        app.MapOpenApi();
    }

    [ExcludeFromCodeCoverage]
    public static void ConfigureRequestPipeline(this WebApplication app)
    {
        var env = app.Services.GetRequiredService<IWebHostEnvironment>();
        var applicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        var healthcheckMaskingEnabled = configuration.GetValue<bool>("HealthcheckMaskingEnabled");
        var adminEndpointsEnabled = configuration.GetValue<bool>("AdminEndpointsEnabled");
        var legacyEndpointsEnabled = configuration.GetValue<bool>("LegacyEndpointsEnabled");

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
            options.SwaggerEndpoint("/swagger/v2/swagger.json", "V2 API");
            options.SwaggerEndpoint("/swagger/public/swagger.json", "Public API");
            options.SwaggerEndpoint("/swagger/internal/swagger.json", "Internal API");
            options.RoutePrefix = "swagger";
        });

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseHeaderPropagation();
        app.UseRouting();

        app.Use(async (context, next) =>
        {
            if (!adminEndpointsEnabled && context.Request.Path.Equals("/api/admin/sqlite-cache/refresh", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            if (!legacyEndpointsEnabled && LegacyRoutePrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix)))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            await next(context);
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapOpenApi();

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
    }
}