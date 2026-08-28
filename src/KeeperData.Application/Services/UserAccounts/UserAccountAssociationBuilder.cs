using KeeperData.Application.Configuration;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Options;

namespace KeeperData.Application.Services.UserAccounts;

public class UserAccountAssociationBuilder(
    ICphAssociationsRepository associationsRepository,
    IOptions<UserAccountAssociationConfig> config) : IUserAccountAssociationBuilder
{
    private readonly UserAccountAssociationConfig _config = config.Value;

    public async Task<List<CphAssociationDocument>> BuildForEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var associations = await associationsRepository.FindByEmailAsync(
            email,
            _config.Roles,
            cancellationToken);

        return associations
            .Select(association => new CphAssociationDocument
            {
                IdentifierId = association.PartyRoleId,
                CphNumber = association.CphNumber,
                Role = association.Role,
                PartyId = association.PartyId,
                HoldingId = association.HoldingId,
                HoldingName = association.HoldingName
            })
            .ToList();
    }
}