using MediatR;

namespace KeeperData.Application.Commands;

public interface ICommand<out TResponse> : IRequest<TResponse> { }