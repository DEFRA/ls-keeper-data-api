using MediatR;

namespace KeeperData.Application.Queries;

public interface IQuery<out TResponse> : IRequest<TResponse> { }