using FluentAssertions;
using KeeperData.Core.Domain.Shared;

namespace KeeperData.Core.Tests.Unit.Domain.Sites;

public class PartyRoleTests
{
    private PartyRole? _left = null;
    private PartyRole? _right = null;

    [Fact]
    public void EmptyPartyRolesShouldBeEqual()
    {
        _left = new PartyRole("", null, EmptyPartyRoleRole(""), [], null);
        _right = new PartyRole("", null, EmptyPartyRoleRole(""), [], null);
        Assert.Equal(_left, _right);
    }

    [Fact]
    public void PartyRolesShouldBeEqualIfIdsAreEqual()
    {
        _left = new PartyRole("id-match", null, EmptyPartyRoleRole(""), [], null);
        _right = new PartyRole("id-match", null, EmptyPartyRoleRole(""), [], null);
        Assert.Equal(_left, _right);
    }

    [Fact]
    public void PartyRolesShouldBeUnequalIfIdsAreUnequal()
    {
        _left = new PartyRole("id", null, EmptyPartyRoleRole(""), [], null);
        _right = new PartyRole("id-different", null, EmptyPartyRoleRole(""), [], null);
        Assert.NotEqual(_left, _right);
    }

    [Fact]
    public void ChangePartyRoleWithSameEmptyValuesShouldReturnNotChanged()
    {
        var orig = new PartyRole("id", null, EmptyPartyRoleRole(""), [], null);
        var result = orig.ApplyChanges(null, EmptyPartyRoleRole(""), [], DateTime.MaxValue);
        result.Should().BeFalse();
    }

    [Fact]
    public void ChangePartyRoleWithSameValuesShouldReturnNotChanged()
    {
        var orig = new PartyRole("id", EmptyPartyRoleSite("prs-id"), EmptyPartyRoleRole("prr-id"), [], null);
        var result = orig.ApplyChanges(EmptyPartyRoleSite("prs-id"), EmptyPartyRoleRole("prr-id"), [], DateTime.MaxValue);
        result.Should().BeFalse();
    }

    [Fact]
    public void ChangePartyRoleWithNewRoleSiteShouldReturnChanged()
    {
        var orig = new PartyRole("id", EmptyPartyRoleSite("prs-id-1"), EmptyPartyRoleRole("prr-id"), [], null);
        var result = orig.ApplyChanges(EmptyPartyRoleSite("prs-id-2"), EmptyPartyRoleRole("prr-id"), [], DateTime.MaxValue);
        result.Should().BeTrue();
        orig.Site!.Id.Should().Be("prs-id-2");
        orig.LastUpdatedDate.Should().Be(DateTime.MaxValue);
    }

    [Fact]
    public void ChangePartyRoleWithSameSpeciesShouldReturnUnchanged()
    {
        var orig = new PartyRole("id", EmptyPartyRoleSite("prs-id"), EmptyPartyRoleRole("prr-id"), [Species("code")], null);
        var result = orig.ApplyChanges(EmptyPartyRoleSite("prs-id"), EmptyPartyRoleRole("prr-id"), [Species("code")], DateTime.MaxValue);
        result.Should().BeFalse();
    }

    [Fact]
    public void ChangePartyRoleWithDifferentSpeciesShouldReturnChanged()
    {
        var orig = new PartyRole("id", EmptyPartyRoleSite("prs-id"), EmptyPartyRoleRole("prr-id"), [Species("code-1")], null);
        var result = orig.ApplyChanges(EmptyPartyRoleSite("prs-id"), EmptyPartyRoleRole("prr-id"), [Species("code-2")], DateTime.MaxValue);
        result.Should().BeTrue();
        orig.SpeciesManagedByRole.Count.Should().Be(1);
        orig.SpeciesManagedByRole.First().Code.Should().Be("code-2");
    }

    private static ManagedSpecies Species(string code)
    {
        return new ManagedSpecies("id", code, "", DateTime.MinValue, null, DateTime.MaxValue);
    }

    private static PartyRoleSite EmptyPartyRoleSite(string id)
    {
        return new PartyRoleSite(id, null, null, null, null);
    }

    private static PartyRoleRole EmptyPartyRoleRole(string id)
    {
        return new PartyRoleRole(id, null, null, null);
    }
}