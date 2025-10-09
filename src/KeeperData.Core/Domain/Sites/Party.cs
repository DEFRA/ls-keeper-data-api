using KeeperData.Core.Domain.BuildingBlocks;
using System;
using System.Collections.Generic;

namespace KeeperData.Core.Domain.Sites;

public class Party : ValueObject
{
    public string Id { get; }
    public string? Title { get; }
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? Name { get; }
    public string? CustomerNumber { get; }
    public string? PartyType { get; }
    public IReadOnlyCollection<Communication> Communication { get; }
    public Address? CorrespondanceAddress { get; }
    public IReadOnlyCollection<RolesToParty> PartyRoles { get; }
    public string? State { get; }
    public DateTime? LastUpdatedDate { get; }

    public Party(string id, string? title, string? firstName, string? lastName, string? name, string? customerNumber, string? partyType, IEnumerable<Communication> communication, Address? correspondanceAddress, IEnumerable<RolesToParty> partyRoles, string? state, DateTime? lastUpdatedDate)
    {
        Id = id;
        Title = title;
        FirstName = firstName;
        LastName = lastName;
        Name = name;
        CustomerNumber = customerNumber;
        PartyType = partyType;
        Communication = new List<Communication>(communication);
        CorrespondanceAddress = correspondanceAddress;
        PartyRoles = new List<RolesToParty>(partyRoles);
        State = state;
        LastUpdatedDate = lastUpdatedDate;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}