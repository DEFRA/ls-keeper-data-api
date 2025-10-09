using KeeperData.Core.Domain.BuildingBlocks;
using System;
using System.Collections.Generic;

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
        Id = id;
        Code = code;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        LastUpdatedDate = lastUpdatedDate;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
        yield return StartDate;
    }
}