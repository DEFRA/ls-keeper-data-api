using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Domain.Parties.DomainEvents;
using KeeperData.Core.Domain.Sites.DomainEvents;
using KeeperData.Core.Exceptions;

namespace KeeperData.Core.Domain.Sites;

public class Site : IAggregateRoot
{
    public string Id { get; private set; }
    public int LastUpdatedBatchId { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }
    public string Type { get; private set; }
    public string Name { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? State { get; private set; }
    public string? Source { get; private set; }
    public bool? DestroyIdentityDocumentsFlag { get; private set; }
    public bool Deleted { get; private set; }

    private readonly List<SiteIdentifier> _identifiers = [];
    public IReadOnlyCollection<SiteIdentifier> Identifiers => _identifiers.AsReadOnly();

    private Location? _location;
    public Location? Location => _location;

    private readonly List<SiteParty> _parties = [];
    public IReadOnlyCollection<SiteParty> Parties => _parties.AsReadOnly();

    private readonly List<Species> _species = [];
    public IReadOnlyCollection<Species> Species => _species.AsReadOnly();

    private readonly List<Marks> _marks = [];
    public IReadOnlyCollection<Marks> Marks => _marks.AsReadOnly();

    private readonly List<SiteActivity> _activities = [];
    public IReadOnlyCollection<SiteActivity> Activities => _activities.AsReadOnly();

    public Site(
        string id,
        int batchId,
        DateTime lastUpdatedDate,
        string type,
        string name,
        DateTime startDate,
        DateTime? endDate,
        string? state,
        string? source,
        bool? destroyIdentityDocumentsFlag,
        bool deleted,
        Location? location)
    {
        Id = id;
        LastUpdatedBatchId = batchId;
        LastUpdatedDate = lastUpdatedDate;
        Type = type;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        State = state;
        Source = source;
        DestroyIdentityDocumentsFlag = destroyIdentityDocumentsFlag;
        Deleted = deleted;

        _location = location;

        _domainEvents.Add(new SiteCreatedDomainEvent(Id));
    }

    public static Site Create(
        int batchId,
        DateTime lastUpdatedDate,
        string type,
        string name,
        DateTime startDate,
        DateTime? endDate,
        string? state,
        string? source,
        bool? destroyIdentityDocumentsFlag,
        bool deleted,
        Location? location = null)
    {
        var site = new Site(
            Guid.NewGuid().ToString(),
            batchId,
            lastUpdatedDate,
            type,
            name,
            startDate,
            endDate,
            state,
            source,
            destroyIdentityDocumentsFlag,
            deleted,
            location
        );

        site._domainEvents.Add(new SiteCreatedDomainEvent(site.Id));
        return site;
    }

    public void Delete(int batchId)
    {
        if (Deleted) return;

        Deleted = true;
        State = "Inactive";
        LastUpdatedBatchId = batchId;
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void UpdateLastUpdatedDate(DateTime lastUpdatedDate)
    {
        LastUpdatedDate = lastUpdatedDate;
    }

    public void AddSiteIdentifier(DateTime lastUpdatedDate, string identifier, string type, string? id = null)
    {
        if (_identifiers.Any(i => i.Identifier == identifier && i.Type == type))
        {
            throw new DomainException($"Site already contains an identifier with the same type and value.");
        }
        var siteIdentifier = id is null
            ? SiteIdentifier.Create(identifier, type)
            : new SiteIdentifier(id, lastUpdatedDate, identifier, type);

        _identifiers.Add(siteIdentifier);
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void RemoveSiteIdentifier(string identifier)
    {
        var existing = _identifiers.FirstOrDefault(x => x.Identifier == identifier);
        if (existing is not null)
        {
            _identifiers.Remove(existing);
            LastUpdatedDate = DateTime.UtcNow;
        }
    }

    public void SetLocation(DateTime lastUpdatedDate, string? osMapReference, double? easting, double? northing, Address? address, IEnumerable<Communication>? communication, string? id = null)
    {
        _location = id is null
            ? Location.Create(osMapReference, easting, northing, address, communication)
            : new Location(id, lastUpdatedDate, osMapReference, easting, northing, address, communication);
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void AddParty(SiteParty party)
    {
        if (_parties.Any(p => p.Id == party.Id))
        {
            throw new DomainException($"Party with ID '{party.Id}' already exists on this site.");
        }
        _parties.Add(party);
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void RemoveParty(string partyId)
    {
        var existing = _parties.FirstOrDefault(p => p.Id == partyId);
        if (existing is not null)
        {
            _parties.Remove(existing);
            LastUpdatedDate = DateTime.UtcNow;
        }
    }

    public void AddSpecies(Species species)
    {
        if (_species.Any(s => s.Code == species.Code))
        {
            throw new DomainException($"Species with code '{species.Code}' already exists on this site.");
        }
        _species.Add(species);
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void RemoveSpecies(string speciesCode)
    {
        var existing = _species.FirstOrDefault(s => s.Code == speciesCode);
        if (existing is not null)
        {
            _species.Remove(existing);
            LastUpdatedDate = DateTime.UtcNow;
        }
    }

    public void AddMark(Marks mark)
    {
        if (_marks.Any(m => m.Mark == mark.Mark && m.Species?.Code == mark.Species?.Code && m.StartDate == mark.StartDate))
        {
            throw new DomainException($"A mark with the same value, species, and start date already exists on this site.");
        }
        _marks.Add(mark);
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void RemoveMark(string markId)
    {
        var existing = _marks.FirstOrDefault(m => m.Id == markId);
        if (existing is not null)
        {
            _marks.Remove(existing);
            LastUpdatedDate = DateTime.UtcNow;
        }
    }

    public void AddActivity(string activity, string? description, DateTime startDate, DateTime? endDate)
    {
        var newActivity = SiteActivity.Create(activity, description, startDate, endDate);
        _activities.Add(newActivity);
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void RemoveActivity(string activityId)
    {
        var existing = _activities.FirstOrDefault(a => a.Id == activityId);
        if (existing is not null)
        {
            _activities.Remove(existing);
            LastUpdatedDate = DateTime.UtcNow;
        }
    }

    internal void LoadIdentifiers(IEnumerable<SiteIdentifier> identifiers)
    {
        _identifiers.Clear();
        _identifiers.AddRange(identifiers);
    }

    internal void LoadParties(IEnumerable<SiteParty> parties)
    {
        _parties.Clear();
        _parties.AddRange(parties);
    }

    internal void LoadSpecies(IEnumerable<Species> species)
    {
        _species.Clear();
        _species.AddRange(species);
    }

    internal void LoadMarks(IEnumerable<Marks> marks)
    {
        _marks.Clear();
        _marks.AddRange(marks);
    }

    internal void LoadActivities(IEnumerable<SiteActivity> activities)
    {
        _activities.Clear();
        _activities.AddRange(activities);
    }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
}