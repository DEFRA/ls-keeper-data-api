using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Exceptions;
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
        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new DomainException("EndDate for a mark cannot be before its StartDate.");
        }
        if (string.IsNullOrWhiteSpace(mark))
        {
            throw new DomainException("Mark value cannot be null or empty.");
        }

        Id = id;
        Mark = mark;
        Species = species;
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Marks Create(string mark, Species? species, DateTime startDate, DateTime? endDate = null)
    {
        return new Marks(
            Guid.NewGuid().ToString(),
            mark,
            species,
            startDate,
            endDate
        );
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {

        yield return Mark;
        yield return StartDate;
    }
}