using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.UserAccounts;

public class GetUserAccountBySubjectQueryHandler(IUserAccountsRepository repository)
    : IQueryHandler<GetUserAccountBySubjectQuery, UserAccountDto>
{
    public async Task<UserAccountDto> Handle(GetUserAccountBySubjectQuery request, CancellationToken cancellationToken)
    {
        var document = await repository.FindBySubjectAsync(request.Subject, cancellationToken)
            ?? throw new NotFoundException($"User account with subject {request.Subject} not found.");

        return document.ToDto();
    }
}