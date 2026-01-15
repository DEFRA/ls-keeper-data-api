using KeeperData.Application.Commands;
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
    where TRequest : ICommand<TResponse>
{
    private readonly IOptions<MongoConfig> _mongoConfig = mongoConfig;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<UnitOfWorkTransactionBehavior<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdContext.Value ?? string.Empty;

        if (!_mongoConfig.Value.EnableTransactions)
            return await next(cancellationToken);

        if (request is not ITransactionalCommand)
            return await next(cancellationToken);

        if (_unitOfWork.Session is null)
            throw new InvalidOperationException(
                $"UnitOfWork session is not initialized for requestType: {typeof(TRequest).Name}, correlationId: {correlationId}");

        if (_unitOfWork.Session.IsInTransaction)
            return await next(cancellationToken);

        _logger.LogInformation("Starting MongoDB transaction for requestType: {requestType}, correlationId: {correlationId}",
                typeof(TRequest).Name, correlationId);

        _unitOfWork.Session.StartTransaction();

        try
        {
            var response = await next(cancellationToken);

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Committed MongoDB transaction for requestType: {requestType}, correlationId: {correlationId}",
                typeof(TRequest).Name, correlationId);

            return response;
        }
        catch (Exception ex)
        {
            if (_unitOfWork.Session.IsInTransaction)
            {
                await _unitOfWork.RollbackAsync();

                _logger.LogWarning(ex, "Rolled back MongoDB transaction due to exception for requestType: {requestType}, correlationId: {correlationId}",
                    typeof(TRequest).Name, correlationId);
            }

            throw;
        }
    }
}