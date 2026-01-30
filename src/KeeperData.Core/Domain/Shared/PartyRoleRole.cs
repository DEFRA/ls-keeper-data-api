using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Shared;

public class PartyRoleRole : EntityObject
{
    private const string CphHolderRoleId = "5053be9f-685a-4779-a663-ce85df6e02e8";

    public string Id { get; private set; }
    public string? Code { get; private set; }
    public string? Name { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    public bool IsCphHolderRole => Id == CphHolderRoleId;

    public PartyRoleRole(
        string id,
        string? code,
        string? name,
        DateTime? lastUpdatedDate)
    {
        Id = id;
        Code = code;
        Name = name;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static PartyRoleRole Create(
        string roleId,
        string? code,
        string? name)
    {
        return new PartyRoleRole(
            roleId,
            code,
            name,
            DateTime.UtcNow);
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