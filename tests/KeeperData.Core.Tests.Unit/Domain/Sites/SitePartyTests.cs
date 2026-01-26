using FluentAssertions;
using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Domain.Sites;
using System.Diagnostics;

namespace KeeperData.Core.Tests.Unit.Domain.Sites;

public class SitePartyTests
{
    public string _leftId = "id";
    public string _leftCustomerNumber = "cn";
    public DateTime _leftCreatedDate = DateTime.MinValue;
    public DateTime _leftLastUpdatedDate = DateTime.MaxValue;
    public string? _leftTitle = "ms";
    public string? _leftFirstName = "j";
    public string? _leftLastName = "smith";
    public string? _leftName = "jane";
    public string? _leftPartyType = "party-type";
    public string? _leftState = "state";
    public Address? _leftCorrespondanceAddress = null;
    public IEnumerable<Communication>? _leftCommunication = null;
    public IEnumerable<PartyRole>? _leftPartyRole = null;
    public string _rightId = "id";
    public string _rightCustomerNumber = "cn";
    public DateTime _rightCreatedDate = DateTime.MinValue;
    public DateTime _rightLastUpdatedDate = DateTime.MaxValue;
    public string? _rightTitle = "ms";
    public string? _rightFirstName = "j";
    public string? _rightLastName = "smith";
    public string? _rightName = "jane";
    public string? _rightPartyType = "party-type";
    public string? _rightState = "state";
    public Address? _rightCorrespondanceAddress = null;
    public IEnumerable<Communication>? _rightCommunication = null;
    public IEnumerable<PartyRole>? _rightPartyRole = null;

    [Fact]
    public void EqualityComparison_ShouldTestEqualForEmptyObjects()
    {
        var left = new SiteParty("", DateTime.MinValue, DateTime.MinValue, "", null, null, null, null, null, null, null, null, null);
        var right = new SiteParty("", DateTime.MinValue, DateTime.MinValue, "", null, null, null, null, null, null, null, null, null);
        Assert.Equal(left, right);
    }

    [Fact]
    public void EqualityComparison_ShouldTestEqualBetweenEmptyAndNullStrings()
    {
        var left = new SiteParty("", DateTime.MinValue, DateTime.MinValue, "", "", "", "", "", "", "", null, null, null);
        var right = new SiteParty("", DateTime.MinValue, DateTime.MinValue, "", null, null, null, null, null, null, null, null, null);
        Assert.Equal(left, right);
    }

    [Fact]
    public void EqualityComparison_ShouldTestEqualForFullObjects()
    {
        var (left, right) = CreateSitePartiesFromTestFields();
        Assert.Equal(left, right);
    }

    public static IEnumerable<object[]> EqualityCases
    {
        get
        {
            yield return ["ignore unequal ids", (SitePartyTests s) => { s._rightId = "different-id"; }];
            yield return ["ignore created date", (SitePartyTests s) => { s._rightCreatedDate = new DateTime(2202, 1, 1); }];
            yield return ["ignore update date", (SitePartyTests s) => { s._rightLastUpdatedDate = new DateTime(2202, 1, 1); }];
            yield return ["when comms are empty", (SitePartyTests s) => { s._leftCommunication = []; s._rightCommunication = []; }];
            yield return ["when comms properties are equal - 1",
                (SitePartyTests s) =>
                {
                    s._leftCommunication = [ new Communication(id: "ignore", new DateTime(1980,1,1), email:"Fred", null,null,null)];
                    s._rightCommunication = [ new Communication(id: "ignore-2", new DateTime(2020,1,1), email:"Fred", null,null,null)];
                }];
            yield return ["when comms properties are equal - 2",
                (SitePartyTests s) =>
                {
                    s._leftCommunication = [EqualityTestAddressHelper.CommunicationWithId("id-1-a"), EqualityTestAddressHelper.CommunicationWithId("id-2-a"), EqualityTestAddressHelper.CommunicationWithId("id-3-a")];
                    s._rightCommunication = [EqualityTestAddressHelper.CommunicationWithId("id-1-b"), EqualityTestAddressHelper.CommunicationWithId("id-2-b"), EqualityTestAddressHelper.CommunicationWithId("id-3-b")];
                }];
            yield return [ "when partyrole ids are equal",
                (SitePartyTests s) =>
                {
                    s._leftPartyRole = [ CreatePartyRole("partyrole-id-match") ];
                    s._rightPartyRole = [ CreatePartyRole("partyrole-id-match") ];
                }];
            yield return [ "when addresses are null or empty",
                (SitePartyTests s) =>
                {
                    s._leftCorrespondanceAddress = EqualityTestAddressHelper.EmptyAddress("id-1");
                    s._rightCorrespondanceAddress = EqualityTestAddressHelper.NullAddress("id-1");
                }];
        }
    }

    [Theory]
    [MemberData(nameof(EqualityCases))]
    public void EqualityComparison_ShouldTestEqualFor(string testcase, Action<SitePartyTests> givenTheseChanges)
    {
        Debug.WriteLine(testcase);
        givenTheseChanges(this);
        var (left, right) = CreateSitePartiesFromTestFields();
        Assert.Equal(left, right);
    }

    [Theory]
    [MemberData(nameof(EqualityCases))]
    public void ApplyChanges_ShouldReturnUnchanged(string testcase, Action<SitePartyTests> givenTheseChanges)
    {
        Debug.WriteLine(testcase);
        givenTheseChanges(this);
        var (left, right) = CreateSitePartiesFromTestFields();

        var changed = left.ApplyChanges(DateTime.Now, _rightCustomerNumber, _rightTitle, _rightFirstName, _rightLastName, _rightName, _rightPartyType, _rightState, _rightCorrespondanceAddress, _rightCommunication, _rightPartyRole);

        changed.Should().BeFalse();
    }

    public static IEnumerable<object[]> InequalityCases
    {
        get
        {
            yield return ["when customer number is different", (SitePartyTests s) => { s._rightCustomerNumber = "different-cn"; }];
            yield return ["when title is different", (SitePartyTests s) => { s._rightTitle = "different-title"; }];
            yield return ["when firstName is different", (SitePartyTests s) => { s._rightFirstName = "different-firstName"; }];
            yield return ["when lastName is different", (SitePartyTests s) => { s._rightLastName = "different-lastName"; }];
            yield return ["when name is different", (SitePartyTests s) => { s._rightName = "different-name"; }];
            yield return ["when partyType is different", (SitePartyTests s) => { s._rightPartyType = "different-partyType"; }];
            yield return ["when state is different", (SitePartyTests s) => { s._rightState = "different-state"; }];
            yield return ["when comms properties are different",
                (SitePartyTests s) =>
                {
                    s._leftCommunication = [ new Communication(id: "id", new DateTime(2001,1,1), email:"Fred", null,null,null)];
                    s._rightCommunication = [ new Communication(id: "id", new DateTime(2001,1,1), email:"Dean", null,null,null)];
                }];
            yield return ["when comms is missing",
                (SitePartyTests s) =>
                {
                    s._leftCommunication = [EqualityTestAddressHelper.CommunicationWithId("id-1-a", "1@google.com"), EqualityTestAddressHelper.CommunicationWithId("id-2-a", "2@google.com"), EqualityTestAddressHelper.CommunicationWithId("id-3-a", "3@google.com")];
                    s._rightCommunication = [EqualityTestAddressHelper.CommunicationWithId("id-1-b", "1@google.com"), EqualityTestAddressHelper.CommunicationWithId("id-3-b", "3@google.com")];
                }];
            yield return [ "when partyrole ids are different",
                (SitePartyTests s) =>
                {
                    s._leftPartyRole = [ CreatePartyRole("partyrole-id-1") ];
                    s._rightPartyRole = [ CreatePartyRole("partyrole-id-2") ];
                }];
            yield return [ "when address properties are different",
                (SitePartyTests s) =>
                {
                    s._leftCorrespondanceAddress = EqualityTestAddressHelper.AddressWith("id-1", postcode: "different");
                    s._rightCorrespondanceAddress = EqualityTestAddressHelper.AddressWith("id-1", postcode: "mismatch");
                }];
        }
    }

    [Theory]
    [MemberData(nameof(InequalityCases))]
    public void EqualityComparison_ShouldTestNotEqualFor(string testcase, Action<SitePartyTests> givenTheseChanges)
    {
        Debug.WriteLine(testcase);
        givenTheseChanges(this);
        var (left, right) = CreateSitePartiesFromTestFields();
        Assert.NotEqual(left, right);
    }

    [Theory]
    [MemberData(nameof(InequalityCases))]
    public void ApplyChanges_ShouldReturnChanged(string testcase, Action<SitePartyTests> givenTheseChanges)
    {
        Debug.WriteLine(testcase);
        givenTheseChanges(this);
        var (left, right) = CreateSitePartiesFromTestFields();

        var changed = left.ApplyChanges(DateTime.Now, _rightCustomerNumber, _rightTitle, _rightFirstName, _rightLastName, _rightName, _rightPartyType, _rightState, _rightCorrespondanceAddress, _rightCommunication, _rightPartyRole);

        changed.Should().BeTrue();
        left.Should().BeEquivalentTo(right);
    }

    private (SiteParty, SiteParty) CreateSitePartiesFromTestFields()
    {
        return (new SiteParty(_leftId,
            _leftCreatedDate,
            _leftLastUpdatedDate,
            _leftCustomerNumber,
            _leftTitle,
            _leftFirstName,
            _leftLastName,
            _leftName,
            _leftPartyType,
            _leftState,
            _leftCorrespondanceAddress,
            _leftCommunication,
            _leftPartyRole),
            new SiteParty(
            _rightId,
            _rightCreatedDate,
            _rightLastUpdatedDate,
            _rightCustomerNumber,
            _rightTitle,
            _rightFirstName,
            _rightLastName,
            _rightName,
            _rightPartyType,
            _rightState,
            _rightCorrespondanceAddress,
            _rightCommunication,
            _rightPartyRole));
    }

    private static PartyRole CreatePartyRole(string id)
    {
        return new PartyRole(
            id: id,
            null,
            new PartyRoleRole("ignored", null, null, null),
            [],
            DateTime.MinValue);
    }
}