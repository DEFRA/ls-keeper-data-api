using KeeperData.Core.Domain.BuildingBlocks;
using System;
using System.Collections.Generic;

namespace KeeperData.Core.Domain.Sites;

public class Marks : ValueObject
{
    public string Id { get; }
    public string Mark { get; }
    public Species? Species { get; }
    public DateTime StartDate { get; }
    public DateTime? EndDate { get; }

public Marks(string id, string mark, Species? species, DateTime startDate, DateTime? endDate)
    {
        Id = id;
        Mark = mark;
        Species = species;
        StartDate = startDate;
        EndDate = endDate;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Mark;
        yield return StartDate;
    }
}