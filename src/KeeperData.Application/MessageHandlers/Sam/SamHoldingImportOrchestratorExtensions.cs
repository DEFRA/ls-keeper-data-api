using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.Exceptions;
using MongoDB.Driver;

namespace KeeperData.Application.MessageHandlers.Sam;

public static class SamHoldingImportOrchestratorExtensions
{
    public static async Task TryExecuteAndThrowRetryable(this SamHoldingImportOrchestrator orchestrator, SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        string FormatExceptionMessage(string msg) =>
            $"Exception Message: {msg}, Message Identifier: {context.Cph}";

        try
        {
            await orchestrator.ExecuteAsync(context, cancellationToken);
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