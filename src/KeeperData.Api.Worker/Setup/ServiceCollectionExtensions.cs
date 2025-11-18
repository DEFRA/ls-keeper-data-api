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
            var ctsBulkScanJobConfig = scheduledJobConfiguration.FirstOrDefault(x => x.JobType == nameof(CtsBulkScanJob));
            if (ctsBulkScanJobConfig?.CronSchedule != null)
            {
                q.AddJob<CtsBulkScanJob>(opts => opts.WithIdentity(ctsBulkScanJobConfig.JobType));

                q.AddTrigger(opts => opts
                    .ForJob(ctsBulkScanJobConfig.JobType)
                    .WithIdentity($"{ctsBulkScanJobConfig.JobType}-trigger")
                    .WithCronSchedule(ctsBulkScanJobConfig.CronSchedule));
            }

            var samBulkScanJobConfig = scheduledJobConfiguration.FirstOrDefault(x => x.JobType == nameof(SamBulkScanJob));
            if (samBulkScanJobConfig?.CronSchedule != null)
            {
                q.AddJob<CtsBulkScanJob>(opts => opts.WithIdentity(samBulkScanJobConfig.JobType));

                q.AddTrigger(opts => opts
                    .ForJob(samBulkScanJobConfig.JobType)
                    .WithIdentity($"{samBulkScanJobConfig.JobType}-trigger")
                    .WithCronSchedule(samBulkScanJobConfig.CronSchedule));
            }

            var ctsDailyScanJobConfig = scheduledJobConfiguration.FirstOrDefault(x => x.JobType == nameof(CtsDailyScanJob));
            if (ctsDailyScanJobConfig?.CronSchedule != null)
            {
                q.AddJob<CtsDailyScanJob>(opts => opts.WithIdentity(ctsDailyScanJobConfig.JobType));

                q.AddTrigger(opts => opts
                    .ForJob(ctsDailyScanJobConfig.JobType)
                    .WithIdentity($"{ctsDailyScanJobConfig.JobType}-trigger")
                    .WithCronSchedule(ctsDailyScanJobConfig.CronSchedule));
            }

            var samDailyScanJobConfig = scheduledJobConfiguration.FirstOrDefault(x => x.JobType == nameof(SamDailyScanJob));
            if (samDailyScanJobConfig?.CronSchedule != null)
            {
                q.AddJob<SamDailyScanJob>(opts => opts.WithIdentity(samDailyScanJobConfig.JobType));

                q.AddTrigger(opts => opts
                    .ForJob(samDailyScanJobConfig.JobType)
                    .WithIdentity($"{samDailyScanJobConfig.JobType}-trigger")
                    .WithCronSchedule(samDailyScanJobConfig.CronSchedule));
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
        services.AddScoped<CtsBulkScanJob>();
        services.AddScoped<SamBulkScanJob>();
        services.AddScoped<CtsDailyScanJob>();
        services.AddScoped<SamDailyScanJob>();

        return services;
    }

    private static IServiceCollection AddTasks(this IServiceCollection services)
    {
        services.AddScoped<ICtsBulkScanTask, CtsBulkScanTask>();
        services.AddScoped<ISamBulkScanTask, SamBulkScanTask>();
        services.AddScoped<ICtsDailyScanTask, CtsDailyScanTask>();
        services.AddScoped<ISamDailyScanTask, SamDailyScanTask>();

        return services;
    }
}