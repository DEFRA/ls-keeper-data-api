using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KeeperData.Core.Domain.Sites;

public class Party : ValueObject
{
    public string Id { get; }
    public string? Title { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Name { get; private set; }
    public string? CustomerNumber { get; private set; }
    public string? PartyType { get; private set; }
    public IReadOnlyCollection<Communication> Communication { get; private set; }
    public Address? CorrespondanceAddress { get; private set; }
    public IReadOnlyCollection<RolesToParty> PartyRoles { get; private set; }
    public string? State { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

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

    public static Party Create(string partyId, string? title, string? firstName, string? lastName, string? name, string? customerNumber, string? partyType, string? state)
    {
        return new Party(
            partyId,
            title,
            firstName,
            lastName,
            name,
            customerNumber,
            partyType,
            Enumerable.Empty<Communication>(),
            null,
            Enumerable.Empty<RolesToParty>(),
            state,
            DateTime.UtcNow
        );
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}