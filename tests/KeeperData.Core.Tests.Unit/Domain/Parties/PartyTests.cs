using FluentAssertions;
using KeeperData.Core.Domain.Parties;
using KeeperData.Core.Domain.Shared;
using Microsoft.IdentityModel.Tokens;

namespace KeeperData.Core.Tests.Unit.Domain.Parties;

public class PartyTests
{
    [Fact]
    public void UpdateAddressFromNull_ShouldUpdateLastUpdatedDate()
    {
        DateTime lastUpdatedDate = new DateTime(2020,1,1);
        DateTime addressLastUpdatedDate = new DateTime(2025,1,1);
        var sut = CreateParty(lastUpdatedDate, address: null);
        
        sut.SetAddress(addressLastUpdatedDate, CreateAddress("line-1"));
        
        sut.LastUpdatedDate.Should().Be(addressLastUpdatedDate);
        sut.Address!.AddressLine1.Should().Be("line-1");
    }

    [Fact]
    public void UpdateAddress_ShouldUpdateLastUpdatedDate()
    {
        DateTime lastUpdatedDate = new DateTime(2020,1,1);
        DateTime addressLastUpdatedDate = new DateTime(2025,1,1);
        var sut = CreateParty(lastUpdatedDate, address: CreateAddress("line-1"));
        
        sut.SetAddress(addressLastUpdatedDate, CreateAddress("new-line-1"));
        
        sut.LastUpdatedDate.Should().Be(addressLastUpdatedDate);
        sut.Address!.AddressLine1.Should().Be("new-line-1");
    }
    
    [Fact]
    public void UpdateCommsFromNull_ShouldUpdateLastUpdatedDate()
    {
        DateTime lastUpdatedDate = new DateTime(2020,1,1);
        DateTime newLastUpdatedDate = new DateTime(2025,1,1);
        var sut = CreateParty(lastUpdatedDate);
        
        sut.AddOrUpdatePrimaryCommunication(newLastUpdatedDate, CreateComms("email"));
        
        sut.Communications.First().Email.Should().Be("email");
        sut.LastUpdatedDate.Should().Be(newLastUpdatedDate);
    }
    
    [Fact]
    public void UpdateComms_ShouldUpdateLastUpdatedDate()
    {
        DateTime oldLastUpdatedDate = new DateTime(2020,1,1);
        DateTime newLastUpdatedDate = new DateTime(2025,1,1);
        var sut = CreateParty(oldLastUpdatedDate);
        sut.AddOrUpdatePrimaryCommunication(oldLastUpdatedDate, CreateComms("old-email"));
            
        sut.AddOrUpdatePrimaryCommunication(newLastUpdatedDate, CreateComms("new-email"));
        
        sut.Communications.First().Email.Should().Be("new-email");
        sut.LastUpdatedDate.Should().Be(newLastUpdatedDate);
    }
    
    [Fact]
    public void UpdateRolesFromNull_ShouldUpdateLastUpdatedDate()
    {
        DateTime lastUpdatedDate = new DateTime(2020,1,1);
        DateTime newLastUpdatedDate = new DateTime(2025,1,1);
        var sut = CreateParty(lastUpdatedDate);
        
        sut.AddOrUpdateRole(newLastUpdatedDate, CreateRole("prr-Code", null));
        
        sut.Roles.First().Role.Code.Should().Be("prr-Code");
        sut.LastUpdatedDate.Should().Be(newLastUpdatedDate);
    }
    
    [Fact]
    public void UpdateRolesWhenRoleAlreadyExists_ShouldUpdateLastUpdatedDate()
    {
        DateTime oldLastUpdatedDate = new DateTime(2020,1,1);
        DateTime newLastUpdatedDate = new DateTime(2025,1,1);
        var sut = CreateParty(oldLastUpdatedDate);
        sut.AddOrUpdateRole(oldLastUpdatedDate, CreateRole("pr-id", null));
        
        sut.AddOrUpdateRole(newLastUpdatedDate, CreateRole("pr-id", [new ManagedSpecies("spec-id", "spec-code", "spec-name", DateTime.MinValue, null, DateTime.MinValue )]));
        
        sut.Roles.First().SpeciesManagedByRole.First().Id.Should().Be("spec-id");
        sut.LastUpdatedDate.Should().Be(newLastUpdatedDate);
    }

    private static Address CreateAddress(string addressLine1)
    {
        return new Address("address-id", null, addressLine1, null,null,null,"",null, DateTime.MinValue);
    }

    private Communication CreateComms(string email)
    {
        return new Communication("comm-id", DateTime.MinValue, email, null, null, null);
    }

    private PartyRole CreateRole(string? prrCode = null, IEnumerable<ManagedSpecies>? speciesManagedByRole = null)
    {
        return new PartyRole("prId", null, new PartyRoleRole("prr-id", prrCode,null,null), speciesManagedByRole ?? [], DateTime.MinValue);
    }

    private static Party CreateParty(DateTime? lastUpdatedDate = null, Address? address = null)
    {
        return new Party("id", DateTime.MinValue, lastUpdatedDate ?? DateTime.MinValue, null,null,null,null,null,null,null,false,address);
    }
}