using KeeperData.Core.Telemetry;
using System.Diagnostics;

namespace KeeperData.Application.Orchestration.ChangeScanning;

public abstract class ScanOrchestrator<TContext>(IEnumerable<IScanStep<TContext>> steps, IApplicationMetrics metrics)
    where TContext : class
{
    private readonly IEnumerable<IScanStep<TContext>> _steps = steps;
    private readonly IApplicationMetrics _metrics = metrics;

    public virtual async Task ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        var orchestrationStopwatch = Stopwatch.StartNew();
        var contextType = typeof(TContext).Name;
        
        _metrics.RecordCount(MetricNames.Orchestrator, 1,
            (MetricNames.CommonTags.Operation, MetricNames.Operations.OrchestrationStarted),
            (MetricNames.CommonTags.UpdateType, contextType));

        try
        {
            var stepsList = _steps.ToList();
            var totalSteps = stepsList.Count;
            var completedSteps = 0;

            foreach (var step in stepsList)
            {
                await step.ExecuteAsync(context, cancellationToken);
                completedSteps++;
            }

            orchestrationStopwatch.Stop();
            
            _metrics.RecordValue(MetricNames.Orchestrator, orchestrationStopwatch.ElapsedMilliseconds,
                (MetricNames.CommonTags.Operation, MetricNames.Operations.OrchestrationDuration),
                (MetricNames.CommonTags.UpdateType, contextType));
                
            _metrics.RecordCount(MetricNames.Orchestrator, 1,
                (MetricNames.CommonTags.Operation, MetricNames.Operations.OrchestrationSuccess),
                (MetricNames.CommonTags.UpdateType, contextType),
                ("steps_completed", completedSteps.ToString()),
                ("total_steps", totalSteps.ToString()));
        }
        catch (Exception ex)
        {
            orchestrationStopwatch.Stop();
            
            _metrics.RecordCount(MetricNames.Orchestrator, 1,
                (MetricNames.CommonTags.Operation, MetricNames.Operations.OrchestrationFailed),
                (MetricNames.CommonTags.ErrorType, ex.GetType().Name),
                (MetricNames.CommonTags.UpdateType, contextType));
                
            throw;
        }
    }
}