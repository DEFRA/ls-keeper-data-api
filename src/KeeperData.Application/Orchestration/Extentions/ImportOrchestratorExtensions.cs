using KeeperData.Application.Orchestration.Imports;
using KeeperData.Application.Orchestration.Updates;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Extentions;

public static class ImportOrchestratorExtensions
{
    public static Task TryExecuteAndThrowRetryable<TContext>(this ImportOrchestrator<TContext> orchestrator, TContext context, CancellationToken cancellationToken)
        => ExecuteWithMongoHandling(() => orchestrator.ExecuteAsync(context, cancellationToken));

    public static Task TryExecuteAndThrowRetryable<TContext>(this UpdateOrchestrator<TContext> orchestrator, TContext context, CancellationToken cancellationToken)
        => ExecuteWithMongoHandling(() => orchestrator.ExecuteAsync(context, cancellationToken));

    private static async Task ExecuteWithMongoHandling(Func<Task> action)
    {
        string FormatExceptionMessage(string msg) =>
            $"Exception Message: {msg}, CorrelationId: {CorrelationIdContext.Value}";

        try
        {
            await action();
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new RetryableException(FormatExceptionMessage(ex.Message), ex);
        }
        catch (MongoBulkWriteException ex) when (ex.WriteErrors.Any(e => e.Category == ServerErrorCategory.DuplicateKey))
        {
            throw new RetryableException(FormatExceptionMessage(ex.Message), ex);
        }
        catch (MongoBulkWriteException ex)
        {
            throw new NonRetryableException(FormatExceptionMessage(ex.Message), ex);
        }
        catch (MongoWriteException ex)
        {
            throw new NonRetryableException(FormatExceptionMessage(ex.Message), ex);
        }
        catch (Exception ex)
        {
            throw new NonRetryableException(ex.Message, ex);
        }
    }
}
