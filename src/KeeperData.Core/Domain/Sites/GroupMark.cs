using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class GroupMark : ValueObject
{
    public string Id { get; }
    public string Mark { get; }
    public Species? Species { get; }
    public DateTime StartDate { get; }
    public DateTime? EndDate { get; }

    public GroupMark(string id, string mark, Species? species, DateTime startDate, DateTime? endDate)
    {
        Id = id;
        Mark = mark;
        Species = species;
        StartDate = startDate;
        EndDate = endDate;
    }

    public static GroupMark Create(string mark, Species? species, DateTime startDate, DateTime? endDate = null)
    {
        return new GroupMark(
            Guid.NewGuid().ToString(),
            mark,
            species,
            startDate,
            endDate
        );
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Mark;
        yield return StartDate;
        yield return EndDate ?? default;
    }
}