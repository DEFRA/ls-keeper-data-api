using KeeperData.Core.Messaging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace KeeperData.Application.Orchestration.Updates;

public abstract class UpdateStepBase<TContext>(ILogger logger) : IUpdateStep<TContext>
{
    private readonly ILogger _logger = logger;

    public async Task ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        var stepName = GetType().Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting update step: {StepName} correlationId: {CorrelationId}", stepName, CorrelationIdContext.Value);
            await ExecuteCoreAsync(context, cancellationToken);
            _logger.LogInformation("Completed update step: {StepName} correlationId: {CorrelationId} in {ElapsedMs}ms", stepName, CorrelationIdContext.Value, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in update step: {StepName}", stepName);
            throw;
        }
    }

    protected abstract Task ExecuteCoreAsync(TContext context, CancellationToken cancellationToken);
}