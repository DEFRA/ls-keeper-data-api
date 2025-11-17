using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class RoleTypeLookupService : IRoleTypeLookupService
{
    private readonly IRoleRepository _roleRepository;

    public RoleTypeLookupService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RoleDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        return await _roleRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<(string? roleTypeId, string? roleTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        return await _roleRepository.FindAsync(lookupValue, cancellationToken);
    }
}