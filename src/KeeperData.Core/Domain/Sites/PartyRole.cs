using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class PartyRole : ValueObject
{
    public string Id { get; private set; }
    public Role Role { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }
    public IReadOnlyCollection<ManagedSpecies> SpeciesManagedByRole => _speciesManagedByRole.AsReadOnly();

    private readonly List<ManagedSpecies> _speciesManagedByRole;

    public PartyRole(
        string id,
        Role role,
        IEnumerable<ManagedSpecies> speciesManagedByRole,
        DateTime? lastUpdatedDate)
    {
        Id = id;
        Role = role;
        _speciesManagedByRole = [.. speciesManagedByRole];
        LastUpdatedDate = lastUpdatedDate;
    }

    public static PartyRole Create(
        Role role,
        IEnumerable<ManagedSpecies> speciesManagedByRole)
    {
        return new PartyRole(
            Guid.NewGuid().ToString(),
            role,
            speciesManagedByRole,
            DateTime.UtcNow);
    }

    public bool ApplyChanges(Role role, IEnumerable<ManagedSpecies> speciesManagedByRole, DateTime lastUpdatedDate)
    {
        var changed = false;

        changed |= Change(Role, role, v => Role = v);
        changed |= ChangeSpecies(speciesManagedByRole);

        if (changed)
        {
            LastUpdatedDate = lastUpdatedDate;
        }

        return changed;
    }

    private static bool Change<T>(T currentValue, T newValue, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) return false;
        setter(newValue);
        return true;
    }

    private bool ChangeSpecies(IEnumerable<ManagedSpecies> newSpecies)
    {
        var newList = newSpecies.ToList();
        if (_speciesManagedByRole.SequenceEqual(newList)) return false;

        _speciesManagedByRole.Clear();
        _speciesManagedByRole.AddRange(newList);
        return true;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}