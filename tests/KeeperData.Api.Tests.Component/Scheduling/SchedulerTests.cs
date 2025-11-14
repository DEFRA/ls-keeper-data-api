namespace KeeperData.Api.Tests.Component.Scheduling;

using System.Threading.Tasks;
using KeeperData.Api.Worker.Jobs;
using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Xunit;

public class SchedulerTests
{
    [Fact]
    public async Task Scheduler_Should_Execute_ScanCTSBulkFilesJob()
    {
        // Arrange
        var taskMock = new Mock<ITaskScanCTSBulkFiles>();
        var loggerMock = new Mock<ILogger<ScanCTSBulkFilesJob>>();
        var jobFactoryMock = new Mock<IJobFactory>();
        jobFactoryMock.Setup(f => f.NewJob(It.IsAny<TriggerFiredBundle>(), It.IsAny<IScheduler>()))
            .Returns(new ScanCTSBulkFilesJob(taskMock.Object, loggerMock.Object));

        var schedulerFactory = new StdSchedulerFactory();
        var scheduler = await schedulerFactory.GetScheduler();
        scheduler.JobFactory = jobFactoryMock.Object;

        var job = JobBuilder.Create<ScanCTSBulkFilesJob>()
            .WithIdentity("ScanCTSBulkFilesJob")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("ScanCTSBulkFilesJob-trigger")
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger);

        // Act
        await scheduler.Start();
        await Task.Delay(1000); // Wait for job to execute

        // Assert
        taskMock.Verify(t => t.RunAsync(It.IsAny<System.Threading.CancellationToken>()), Times.AtLeastOnce);
    }

    // Repeat similar tests for other jobs
    [Fact]
    public async Task Scheduler_Should_Execute_ScanSAMBulkFilesJob()
    {
        var taskMock = new Mock<ITaskScanSAMBulkFiles>();
        var loggerMock = new Mock<ILogger<ScanSAMBulkFilesJob>>();
        var jobFactoryMock = new Mock<IJobFactory>();
        jobFactoryMock.Setup(f => f.NewJob(It.IsAny<TriggerFiredBundle>(), It.IsAny<IScheduler>()))
            .Returns(new ScanSAMBulkFilesJob(taskMock.Object, loggerMock.Object));

        var schedulerFactory = new StdSchedulerFactory();
        var scheduler = await schedulerFactory.GetScheduler();
        scheduler.JobFactory = jobFactoryMock.Object;

        var job = JobBuilder.Create<ScanSAMBulkFilesJob>()
            .WithIdentity("ScanSAMBulkFilesJob")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("ScanSAMBulkFilesJob-trigger")
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger);

        await scheduler.Start();
        await Task.Delay(1000);

        taskMock.Verify(t => t.RunAsync(It.IsAny<System.Threading.CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Scheduler_Should_Execute_ScanCTSFilesJob()
    {
        var taskMock = new Mock<ITaskScanCTSFiles>();
        var loggerMock = new Mock<ILogger<ScanCTSFilesJob>>();
        var jobFactoryMock = new Mock<IJobFactory>();
        jobFactoryMock.Setup(f => f.NewJob(It.IsAny<TriggerFiredBundle>(), It.IsAny<IScheduler>()))
            .Returns(new ScanCTSFilesJob(taskMock.Object, loggerMock.Object));

        var schedulerFactory = new StdSchedulerFactory();
        var scheduler = await schedulerFactory.GetScheduler();
        scheduler.JobFactory = jobFactoryMock.Object;

        var job = JobBuilder.Create<ScanCTSFilesJob>()
            .WithIdentity("ScanCTSFilesJob")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("ScanCTSFilesJob-trigger")
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger);

        await scheduler.Start();
        await Task.Delay(1000);

        taskMock.Verify(t => t.RunAsync(It.IsAny<System.Threading.CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Scheduler_Should_Execute_ScanSAMFilesJob()
    {
        var taskMock = new Mock<ITaskScanSAMFiles>();
        var loggerMock = new Mock<ILogger<ScanSAMFilesJob>>();
        var jobFactoryMock = new Mock<IJobFactory>();
        jobFactoryMock.Setup(f => f.NewJob(It.IsAny<TriggerFiredBundle>(), It.IsAny<IScheduler>()))
            .Returns(new ScanSAMFilesJob(taskMock.Object, loggerMock.Object));

        var schedulerFactory = new StdSchedulerFactory();
        var scheduler = await schedulerFactory.GetScheduler();
        scheduler.JobFactory = jobFactoryMock.Object;

        var job = JobBuilder.Create<ScanSAMFilesJob>()
            .WithIdentity("ScanSAMFilesJob")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("ScanSAMFilesJob-trigger")
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger);

        await scheduler.Start();
        await Task.Delay(1000);

        taskMock.Verify(t => t.RunAsync(It.IsAny<System.Threading.CancellationToken>()), Times.AtLeastOnce);
    }
}
