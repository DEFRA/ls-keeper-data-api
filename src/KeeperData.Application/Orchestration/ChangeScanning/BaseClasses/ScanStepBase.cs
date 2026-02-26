using System.Diagnostics;
using KeeperData.Core.Messaging;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.BaseClasses;

public abstract class ScanStepBase<TContext>(ILogger logger) : IScanStep<TContext>
{
    public async Task ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        var stepName = GetType().Name;
        var correlationId = CorrelationIdContext.Value;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation("Starting scan step: {StepName} correlationId: {CorrelationId}", stepName, correlationId);
            await ExecuteCoreAsync(context, cancellationToken);
            logger.LogInformation("Completed scan step: {StepName} correlationId: {CorrelationId} in {ElapsedMs}ms", stepName, correlationId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in scan step: {StepName}", stepName);
            throw;
        }
    }

    protected abstract Task ExecuteCoreAsync(TContext context, CancellationToken cancellationToken);
}