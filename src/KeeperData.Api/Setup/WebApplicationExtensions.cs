using KeeperData.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

        applicationLifetime.ApplicationStarted.Register(() =>
            logger.LogInformation("{applicationName} started", env.ApplicationName));
        applicationLifetime.ApplicationStopping.Register(() =>
            logger.LogInformation("{applicationName} stopping", env.ApplicationName));
        applicationLifetime.ApplicationStopped.Register(() =>
            logger.LogInformation("{applicationName} stopped", env.ApplicationName));

        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1.0))
            .ReportApiVersions()
            .Build();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseHeaderPropagation();
        app.UseRouting();

        app.MapHealthChecks("/health", new HealthCheckOptions()
        {
            Predicate = _ => true,
            ResponseWriter = (context, healthReport) =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                return context.Response.WriteAsync(HealthCheckWriter.WriteHealthStatusAsJson(healthReport, excludeHealthy: false, indented: true));
            },
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        }).WithApiVersionSet(versionSet).IsApiVersionNeutral();

        app.MapGet("/", () => "Alive!").WithApiVersionSet(versionSet).IsApiVersionNeutral();

        app.MapControllers();
    }
}