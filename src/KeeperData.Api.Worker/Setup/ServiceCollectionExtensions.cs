using KeeperData.Api.Worker.Configuration;
using KeeperData.Api.Worker.Jobs;
using KeeperData.Api.Worker.Tasks;
using KeeperData.Api.Worker.Tasks.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System.Diagnostics.CodeAnalysis;

namespace KeeperData.Api.Worker.Setup;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static void AddBackgroundJobDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddQuartz(configuration)
            .AddJobs()
            .AddTasks();
    }

    private static IServiceCollection AddQuartz(this IServiceCollection services, IConfiguration configuration)
    {
        var scheduledJobConfiguration = configuration.GetRequiredSection("Quartz:Jobs").Get<List<ScheduledJobConfiguration>>() ?? [];

        services.AddQuartz(q =>
        {
            var ctsScanJobConfig = scheduledJobConfiguration.FirstOrDefault(x => x.JobType == nameof(CtsScanJob));
            if (ctsScanJobConfig?.Enabled == true && ctsScanJobConfig?.CronSchedule != null)
            {
                q.AddJob<CtsScanJob>(opts => opts.WithIdentity(ctsScanJobConfig.JobType));

                q.AddTrigger(opts => opts
                    .ForJob(ctsScanJobConfig.JobType)
                    .StartAt(ctsScanJobConfig.EnabledFrom)
                    .EndAt(ctsScanJobConfig.EnabledTo)
                    .WithIdentity($"{ctsScanJobConfig.JobType}-trigger")
                    .WithCronSchedule(ctsScanJobConfig.CronSchedule));
            }

            var samScanJobConfig = scheduledJobConfiguration.FirstOrDefault(x => x.JobType == nameof(SamScanJob));
            if (samScanJobConfig?.Enabled == true && samScanJobConfig?.CronSchedule != null)
            {
                q.AddJob<SamScanJob>(opts => opts.WithIdentity(samScanJobConfig.JobType));

                q.AddTrigger(opts => opts
                    .ForJob(samScanJobConfig.JobType)
                    .StartAt(samScanJobConfig.EnabledFrom)
                    .EndAt(samScanJobConfig.EnabledTo)
                    .WithIdentity($"{samScanJobConfig.JobType}-trigger")
                    .WithCronSchedule(samScanJobConfig.CronSchedule));
            }
        });

        services.AddQuartzHostedService(q =>
        {
            q.WaitForJobsToComplete = false;
        });

        return services;
    }

    private static IServiceCollection AddJobs(this IServiceCollection services)
    {
        services.AddScoped<CtsScanJob>();
        services.AddScoped<SamScanJob>();

        return services;
    }

    private static IServiceCollection AddTasks(this IServiceCollection services)
    {
        services.AddScoped<ICtsScanTask, CtsScanTask>();
        services.AddScoped<ISamScanTask, SamScanTask>();

        return services;
    }
}