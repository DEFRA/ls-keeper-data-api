using KeeperData.Core.Messaging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace KeeperData.Application.Orchestration.Imports;

public abstract class ImportStepBase<TContext>(ILogger logger) : IImportStep<TContext>
{
    private readonly ILogger _logger = logger;

    public async Task ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        var stepName = GetType().Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting import step: {StepName} correlationId: {CorrelationId}", stepName, CorrelationIdContext.Value);
            await ExecuteCoreAsync(context, cancellationToken);
            _logger.LogInformation("Completed import step: {StepName} correlationId: {CorrelationId} in {ElapsedMs}ms", stepName, CorrelationIdContext.Value, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in import step: {StepName}", stepName);
            throw;
        }
    }

    protected abstract Task ExecuteCoreAsync(TContext context, CancellationToken cancellationToken);
}