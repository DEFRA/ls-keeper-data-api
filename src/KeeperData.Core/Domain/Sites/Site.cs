using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Domain.Sites.DomainEvents;

namespace KeeperData.Core.Domain.Sites;

public class Site : IAggregateRoot
{
    public string Id { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }
    public PremisesType? Type { get; private set; }
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

    private readonly List<GroupMark> _marks = [];
    public IReadOnlyCollection<GroupMark> Marks => _marks.AsReadOnly();

    private readonly List<SiteActivity> _activities = [];
    public IReadOnlyCollection<SiteActivity> Activities => _activities.AsReadOnly();

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Site(
        string id,
        DateTime createdDate,
        DateTime lastUpdatedDate,
        string name,
        DateTime startDate,
        DateTime? endDate,
        string? state,
        string? source,
        bool? destroyIdentityDocumentsFlag,
        bool deleted,
        PremisesType? type,
        Location? location)
    {
        Id = id;
        CreatedDate = createdDate;
        LastUpdatedDate = lastUpdatedDate;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        State = state;
        Source = source;
        DestroyIdentityDocumentsFlag = destroyIdentityDocumentsFlag;
        Deleted = deleted;
        Type = type;
        _location = location;
    }

    public static Site Create(
        string id,
        DateTime createdDate,
        DateTime lastUpdatedDate,
        string name,
        DateTime startDate,
        DateTime? endDate,
        string? state,
        string? source,
        bool? destroyIdentityDocumentsFlag,
        bool deleted,
        PremisesType? type = null,
        Location? location = null)
    {
        var site = new Site(
            id,
            createdDate,
            lastUpdatedDate,
            name,
            startDate,
            endDate,
            state,
            source,
            destroyIdentityDocumentsFlag,
            deleted,
            type,
            location);

        site._domainEvents.Add(new SiteCreatedDomainEvent(site.Id));
        return site;
    }

    public void Update(
        DateTime lastUpdatedDate,
        string name,
        DateTime startDate,
        DateTime? endDate,
        string? state,
        string? source,
        bool? destroyIdentityDocumentsFlag,
        bool deleted)
    {
        var changed = false;

        changed |= Change(Name, name, v => Name = v, lastUpdatedDate);
        changed |= Change(StartDate, startDate, v => StartDate = v, lastUpdatedDate);
        changed |= Change(EndDate, endDate, v => EndDate = v, lastUpdatedDate);
        changed |= Change(State, state, v => State = v, lastUpdatedDate);
        changed |= Change(Source, source, v => Source = v, lastUpdatedDate);
        changed |= Change(DestroyIdentityDocumentsFlag, destroyIdentityDocumentsFlag, v => DestroyIdentityDocumentsFlag = v, lastUpdatedDate);
        changed |= Change(Deleted, deleted, v => Deleted = v, lastUpdatedDate);

        if (changed)
        {
            _domainEvents.Add(new SiteUpdatedDomainEvent(Id));
        }
    }

    public void SetPremisesType(PremisesType? type, DateTime lastUpdatedDate)
    {
        if (Type == null && type == null) return;

        if (Type is null || !Type.Equals(type))
        {
            Type = type;
            UpdateLastUpdatedDate(lastUpdatedDate);
        }
    }

    public void SetLocation(Location location)
    {
        _location = location;
    }

    public void SetLocation(
        DateTime lastUpdatedDate,
        string? osMapReference,
        double? easting,
        double? northing,
        Address? address,
        IEnumerable<Communication>? communication)
    {
        if (_location is null)
        {
            _location = Location.Create(osMapReference, easting, northing, address, communication);
            UpdateLastUpdatedDate(lastUpdatedDate);
            return;
        }

        if (_location.ApplyChanges(lastUpdatedDate, osMapReference, easting, northing, address, communication))
        {
            UpdateLastUpdatedDate(lastUpdatedDate);
        }
    }

    public void SetSiteIdentifier(
        DateTime lastUpdatedDate,
        string identifier,
        SiteIdentifierType type,
        string? id = null)
    {
        var existing = _identifiers.FirstOrDefault(i => i.Type.Id == type.Id);

        if (existing is not null)
        {
            var changed = existing.ApplyChanges(lastUpdatedDate, identifier, type);
            if (changed)
            {
                UpdateLastUpdatedDate(DateTime.UtcNow);
            }
            return;
        }

        var siteIdentifier = id is null
            ? SiteIdentifier.Create(identifier, type)
            : new SiteIdentifier(id, lastUpdatedDate, identifier, type);

        _identifiers.Add(siteIdentifier);
        UpdateLastUpdatedDate(DateTime.UtcNow);
    }

    public void SetSpecies(IEnumerable<Species> incomingSpecies, DateTime lastUpdatedDate)
    {
        var incomingList = incomingSpecies.ToList();
        var changed = false;

        foreach (var incoming in incomingList)
        {
            var existing = _species.FirstOrDefault(s => s.Code == incoming.Code);

            if (existing is not null)
            {
                changed |= existing.ApplyChanges(lastUpdatedDate, incoming.Code, incoming.Name);
            }
            else
            {
                _species.Add(Shared.Species.Create(incoming.Id, lastUpdatedDate, incoming.Code, incoming.Name));
                changed = true;
            }
        }

        var orphaned = _species
            .Where(existing => incomingList.All(i => i.Code != existing.Code))
            .ToList();

        if (orphaned.Count != 0)
        {
            foreach (var orphan in orphaned)
            {
                _species.Remove(orphan);
            }
            changed = true;
        }

        if (changed)
        {
            UpdateLastUpdatedDate(lastUpdatedDate);
        }
    }

    public void SetActivities(IEnumerable<SiteActivity> incomingActivities, DateTime lastUpdatedDate)
    {
        var incomingList = incomingActivities.ToList();
        var changed = false;

        foreach (var incoming in incomingList)
        {
            var existing = _activities.FirstOrDefault(a => a.Id == incoming.Id);

            if (existing is not null)
            {
                changed |= existing.ApplyChanges(
                    lastUpdatedDate,
                    incoming.Type,
                    incoming.StartDate,
                    incoming.EndDate);
            }
            else
            {
                _activities.Add(SiteActivity.Create(
                    incoming.Id,
                    incoming.Type,
                    incoming.StartDate,
                    incoming.EndDate,
                    lastUpdatedDate));
                changed = true;
            }
        }

        var incomingIds = incomingList.Select(i => i.Id).ToHashSet();
        var orphaned = _activities.Where(a => !incomingIds.Contains(a.Id)).ToList();

        if (orphaned.Count > 0)
        {
            foreach (var orphan in orphaned)
            {
                _activities.Remove(orphan);
            }
            changed = true;
        }

        if (changed)
        {
            UpdateLastUpdatedDate(lastUpdatedDate);
        }
    }

    public void SetGroupMarks(IEnumerable<GroupMark> incomingMarks, DateTime lastUpdatedDate)
    {
        var incomingList = incomingMarks.ToList();
        var changed = false;

        foreach (var incoming in incomingList)
        {
            var existing = _marks.FirstOrDefault(m =>
                m.Mark == incoming.Mark &&
                m.Species?.Id == incoming.Species?.Id);

            if (existing is not null)
            {
                changed |= existing.ApplyChanges(
                    lastUpdatedDate,
                    incoming.Mark,
                    incoming.StartDate,
                    incoming.EndDate,
                    incoming.Species);
            }
            else
            {
                _marks.Add(new GroupMark(
                    incoming.Id,
                    lastUpdatedDate,
                    incoming.Mark,
                    incoming.StartDate,
                    incoming.EndDate,
                    incoming.Species));
                changed = true;
            }
        }

        var orphaned = _marks
            .Where(existing => incomingList.All(i =>
                i.Mark != existing.Mark ||
                i.Species?.Id != existing.Species?.Id))
            .ToList();

        if (orphaned.Count != 0)
        {
            foreach (var orphan in orphaned)
            {
                _marks.Remove(orphan);
            }
            changed = true;
        }

        if (changed)
        {
            UpdateLastUpdatedDate(lastUpdatedDate);
        }
    }

    public void SetSiteParties(string goldSiteId, IEnumerable<SiteParty> incomingParties, DateTime lastUpdatedDate)
    {
        var incomingList = incomingParties.ToList();
        var changed = false;

        foreach (var incoming in incomingList)
        {
            var existing = _parties.FirstOrDefault(p => p.CustomerNumber == incoming.CustomerNumber);
            var siteSpecificPartyRoles = incoming.PartyRoles?.Where(x => x.Site != null && x.Site.Id == goldSiteId).ToList() ?? [];

            if (existing is not null)
            {
                changed |= existing.ApplyChanges(
                    incoming.LastUpdatedDate,
                    incoming.CustomerNumber,
                    incoming.Title,
                    incoming.FirstName,
                    incoming.LastName,
                    incoming.Name,
                    incoming.PartyType,
                    incoming.State,
                    incoming.CorrespondanceAddress,
                    incoming.Communication,
                    siteSpecificPartyRoles);
            }
            else
            {
                _parties.Add(new SiteParty(
                    incoming.Id,
                    incoming.CreatedDate,
                    incoming.LastUpdatedDate,
                    incoming.CustomerNumber,
                    incoming.Title,
                    incoming.FirstName,
                    incoming.LastName,
                    incoming.Name,
                    incoming.PartyType,
                    incoming.State,
                    incoming.CorrespondanceAddress,
                    incoming.Communication,
                    siteSpecificPartyRoles));
                changed = true;
            }
        }

        var orphaned = _parties
            .Where(existing => incomingList.All(i => i.CustomerNumber != existing.CustomerNumber))
            .ToList();

        if (orphaned.Count != 0)
        {
            foreach (var orphan in orphaned)
            {
                _parties.Remove(orphan);
            }
            changed = true;
        }

        if (changed)
        {
            UpdateLastUpdatedDate(lastUpdatedDate);
        }
    }

    private bool Change<T>(T currentValue, T newValue, Action<T> setter, DateTime lastUpdatedAt)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) return false;
        setter(newValue);
        UpdateLastUpdatedDate(lastUpdatedAt);
        return true;
    }

    private void UpdateLastUpdatedDate(DateTime lastUpdatedDate)
    {
        LastUpdatedDate = lastUpdatedDate;
    }
}