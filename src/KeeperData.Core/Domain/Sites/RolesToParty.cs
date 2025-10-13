using KeeperData.Core.Domain.BuildingBlocks;
using System;
using System.Collections.Generic;

namespace KeeperData.Core.Domain.Sites;

public class RolesToParty : ValueObject
{
    public string Id { get; }
    public Role? Role { get; }
    public IReadOnlyCollection<ManagedSpecies> SpeciesManagedByRole { get; }
    public DateTime? LastUpdatedDate { get; }

    public RolesToParty(string id, Role? role, IEnumerable<ManagedSpecies> speciesManagedByRole, DateTime? lastUpdatedDate)
    {
        Id = id;
        Role = role;
        SpeciesManagedByRole = new List<ManagedSpecies>(speciesManagedByRole);
        LastUpdatedDate = lastUpdatedDate;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}