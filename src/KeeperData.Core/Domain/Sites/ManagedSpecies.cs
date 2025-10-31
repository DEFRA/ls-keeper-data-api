using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Exceptions;

namespace KeeperData.Core.Domain.Sites;

public class ManagedSpecies : ValueObject
{
    public string Id { get; }
    public string Code { get; }
    public string Name { get; }
    public DateTime StartDate { get; }
    public DateTime? EndDate { get; }
    public DateTime? LastUpdatedDate { get; }

    public ManagedSpecies(string id, string code, string name, DateTime startDate, DateTime? endDate, DateTime? lastUpdatedDate)
    {
        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new DomainException("EndDate for a managed species cannot be before its StartDate.");
        }

        Id = id;
        Code = code;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static ManagedSpecies Create(string code, string name, DateTime startDate, DateTime? endDate = null)
    {
        return new ManagedSpecies(
            Guid.NewGuid().ToString(),
            code,
            name,
            startDate,
            endDate,
            DateTime.UtcNow
        );
    }


    public override IEnumerable<object> GetEqualityComponents()
    {

        yield return Code;
        yield return StartDate;
        yield return EndDate ?? default;
    }
}