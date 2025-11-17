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
            var scanCTSBulkFilesConfig = scheduledJobConfiguration.FirstOrDefault(x => x.JobType == nameof(ScanCTSBulkFilesJob));
            if (scanCTSBulkFilesConfig?.CronSchedule != null)
            {
                q.AddJob<ScanCTSBulkFilesJob>(opts => opts.WithIdentity(scanCTSBulkFilesConfig.JobType));

                q.AddTrigger(opts => opts
                    .ForJob(scanCTSBulkFilesConfig.JobType)
                    .WithIdentity($"{scanCTSBulkFilesConfig.JobType}-trigger")
                    .WithCronSchedule(scanCTSBulkFilesConfig.CronSchedule));
            }

            var scanSAMBulkFilesConfig = scheduledJobConfiguration.FirstOrDefault(x => x.JobType == nameof(ScanSAMBulkFilesJob));
            if (scanSAMBulkFilesConfig?.CronSchedule != null)
            {
                q.AddJob<ScanCTSBulkFilesJob>(opts => opts.WithIdentity(scanSAMBulkFilesConfig.JobType));

                q.AddTrigger(opts => opts
                    .ForJob(scanSAMBulkFilesConfig.JobType)
                    .WithIdentity($"{scanSAMBulkFilesConfig.JobType}-trigger")
                    .WithCronSchedule(scanSAMBulkFilesConfig.CronSchedule));
            }

            var scanCTSFilesConfig = scheduledJobConfiguration.FirstOrDefault(x => x.JobType == nameof(ScanCTSFilesJob));
            if (scanCTSFilesConfig?.CronSchedule != null)
            {
                q.AddJob<ScanCTSFilesJob>(opts => opts.WithIdentity(scanCTSFilesConfig.JobType));

                q.AddTrigger(opts => opts
                    .ForJob(scanCTSFilesConfig.JobType)
                    .WithIdentity($"{scanCTSFilesConfig.JobType}-trigger")
                    .WithCronSchedule(scanCTSFilesConfig.CronSchedule));
            }

            var scanSAMFilesConfig = scheduledJobConfiguration.FirstOrDefault(x => x.JobType == nameof(ScanSAMFilesJob));
            if (scanSAMFilesConfig?.CronSchedule != null)
            {
                q.AddJob<ScanSAMFilesJob>(opts => opts.WithIdentity(scanSAMFilesConfig.JobType));

                q.AddTrigger(opts => opts
                    .ForJob(scanSAMFilesConfig.JobType)
                    .WithIdentity($"{scanSAMFilesConfig.JobType}-trigger")
                    .WithCronSchedule(scanSAMFilesConfig.CronSchedule));
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
        services.AddScoped<ScanCTSBulkFilesJob>();
        services.AddScoped<ScanSAMBulkFilesJob>();
        services.AddScoped<ScanCTSFilesJob>();
        services.AddScoped<ScanSAMFilesJob>();

        return services;
    }

    private static IServiceCollection AddTasks(this IServiceCollection services)
    {
        services.AddScoped<ITaskScanCTSBulkFiles, TaskScanCTSBulkFiles>();
        services.AddScoped<ITaskScanSAMBulkFiles, TaskScanSAMBulkFiles>();
        services.AddScoped<ITaskScanCTSFiles, TaskScanCTSFiles>();
        services.AddScoped<ITaskScanSAMFiles, TaskScanSAMFiles>();

        return services;
    }
}