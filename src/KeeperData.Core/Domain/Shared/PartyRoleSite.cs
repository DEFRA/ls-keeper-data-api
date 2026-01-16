using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.Sites;

namespace KeeperData.Core.Domain.Shared;

public class PartyRoleSite : ValueObject
{
    public string Id { get; private set; }
    public string? Name { get; private set; }
    public PremisesType? Type { get; private set; }
    public string? State { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    private readonly List<SiteIdentifier> _identifiers = [];
    public IReadOnlyCollection<SiteIdentifier> Identifiers => _identifiers.AsReadOnly();

    public PartyRoleSite(
        string id,
        string? name,
        PremisesType? type,
        string? state,
        DateTime? lastUpdatedDate)
    {
        Id = id;
        Name = name;
        Type = type;
        State = state;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static PartyRoleSite Create(
        string siteId,
        string? name,
        PremisesType? type,
        string? state = null)
    {
        return new PartyRoleSite(
            siteId,
            name,
            type,
            state,
            DateTime.UtcNow);
    }

    public void SetIdentifiers(IEnumerable<SiteIdentifier> identifiers)
    {
        _identifiers.Clear();
        _identifiers.AddRange(identifiers);
    }

    public bool ApplyChanges(string? name, PremisesType? type, string? state, DateTime lastUpdatedDate)
    {
        var changed = false;

        changed |= Change(Name, name, v => Name = v);
        changed |= Change(Type, type, v => Type = v);
        changed |= Change(State, state, v => State = v);

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

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}