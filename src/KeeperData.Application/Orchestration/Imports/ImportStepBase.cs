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
            _logger.LogInformation("Starting import step: {StepName}", stepName);
            await ExecuteCoreAsync(context, cancellationToken);
            _logger.LogInformation("Completed import step: {StepName} in {ElapsedMs}ms", stepName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in import step: {StepName}", stepName);
            throw;
        }
    }

    protected abstract Task ExecuteCoreAsync(TContext context, CancellationToken cancellationToken);
}