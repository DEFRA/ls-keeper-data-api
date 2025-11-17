using KeeperData.Core.Messaging;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Behaviors;

public class UnitOfWorkTransactionBehavior<TRequest, TResponse>(
    IOptions<MongoConfig> mongoConfig,
    IUnitOfWork unitOfWork,
    ILogger<UnitOfWorkTransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IOptions<MongoConfig> _mongoConfig = mongoConfig;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<UnitOfWorkTransactionBehavior<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdContext.Value ?? string.Empty;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestType"] = typeof(TRequest).Name
        }))
        {
            var transactionsEnabled = _mongoConfig.Value.EnableTransactions;
            var transactionStarted = false;

            if (transactionsEnabled && _unitOfWork.Session?.IsInTransaction == false)
            {
                _logger.LogInformation("Starting MongoDB transaction for {RequestType}", typeof(TRequest).Name);
                _unitOfWork.Session.StartTransaction();
                transactionStarted = true;
            }

            try
            {
                var response = await next(cancellationToken);

                if (transactionStarted)
                {
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation("Committed MongoDB transaction");
                }

                return response;
            }
            catch (Exception ex)
            {
                if (transactionStarted && _unitOfWork.Session != null && _unitOfWork.Session.IsInTransaction)
                {
                    await _unitOfWork.RollbackAsync();
                    _logger.LogWarning(ex, "Rolled back MongoDB transaction due to exception");
                }

                throw;
            }
        }
    }
}