using System.Diagnostics;
using FluentAssertions;
using KeeperData.Core.Domain.Shared;

namespace KeeperData.Core.Tests.Unit.Domain.Sites;

public class LocationTests
{
    public Location? Left = null;
    public Location? Right = null;

    [Fact]
    public void EmptyLocationsShouldBeEqual()
    {
        Left = new Location("", DateTime.MinValue, null, null, null, null, null);
        Right = new Location("", DateTime.MinValue, null, null, null, null, null);
        Assert.Equal(Left, Right);
    }

    [Fact]
    public void EmptyAndNullLocationsShouldBeEqual()
    {
        Left = new Location("", DateTime.MinValue, "", 0.0, 0.0, null, null);
        Right = new Location("", DateTime.MinValue, null, null, null, null, null);
        Assert.Equal(Left, Right);
    }

    public static IEnumerable<object[]> EqualityCases
    {
        get
        {
            yield return ["when properties are same",
                (LocationTests s) =>
                {
                    s.Left = new Location("", DateTime.MinValue, "A", 10.0, 20.0, null, null);
                    s.Right = new Location("", DateTime.MinValue, "A", 10.0, 20.0, null, null);
                }];
            yield return ["when addresses are same",
                (LocationTests s) =>
                {
                    s.Left = new Location("", DateTime.MinValue, "", null, null, EqualityTestAddressHelper.EmptyAddress("id"), null);
                    s.Right = new Location("", DateTime.MinValue, "", null, null, EqualityTestAddressHelper.EmptyAddress("id"), null);
                }];
            yield return ["when comms are same",
                (LocationTests s) =>
                {
                    s.Left = new Location("", DateTime.MinValue, "", null, null, null, [NullCommunication()]);
                    s.Right = new Location("", DateTime.MinValue, "", null, null, null, [NullCommunication()]);
                }];
        }
    }

    [Theory]
    [MemberData(nameof(EqualityCases))]
    public void LocationsWithEqualPropertiesShouldBeEqual(string testcase, Action<LocationTests> assignTestData)
    {
        Debug.WriteLine(testcase);
        assignTestData(this);
        Assert.Equal(Left, Right);
    }

    [Theory]
    [MemberData(nameof(EqualityCases))]
    public void ApplyChangesWithoutChangeShouldReturnNoChange(string testcase, Action<LocationTests> assignTestData)
    {
        Debug.WriteLine(testcase);
        assignTestData(this);
        var changed = Left!.ApplyChanges(
            Right!.LastUpdatedDate,
            Right.OsMapReference,
            Right.Easting,
            Right.Northing,
            Right.Address,
            Right.Communication);
        changed.Should().BeFalse();
    }

    public static IEnumerable<object[]> InequalityCases
    {
        get
        {
            yield return ["when OsMapReference is different",
                (LocationTests s) =>
                {
                    s.Left = new Location("", DateTime.MinValue, "A", 0.0, 0.0, null, null);
                    s.Right = new Location("", DateTime.MinValue, "B", 0.0, 0.0, null, null);
                }];
            yield return ["when Easting is different",
                (LocationTests s) =>
                {
                    s.Left = new Location("", DateTime.MinValue, "", 10.0, 0.0, null, null);
                    s.Right = new Location("", DateTime.MinValue, "", 20.0, 0.0, null, null);
                }];
            yield return ["when Northing is different",
                (LocationTests s) =>
                {
                    s.Left = new Location("", DateTime.MinValue, "", 0.0, 10.0, null, null);
                    s.Right = new Location("", DateTime.MinValue, "", 0.0, 20.0, null, null);
                }];
            yield return ["when Comms is different",
                (LocationTests s) =>
                {
                    s.Left = new Location("", DateTime.MinValue, "", 0.0, 0.0, null, [new Communication("id",  DateTime.MinValue, "", null, null, null)]);
                    s.Right = new Location("", DateTime.MinValue, "", 0.0, 0.0, null, [new Communication("id",  DateTime.MinValue, "different-email", null, null, null)]);
                }];
        }
    }

    [Theory]
    [MemberData(nameof(InequalityCases))]
    public void LocationsWithUnequalPropertiesShouldBeEqual(string testcase, Action<LocationTests> assignTestData)
    {
        Debug.WriteLine(testcase);
        assignTestData(this);
        Assert.NotEqual(Left, Right);
    }

    [Fact]
    public void LocationsWithDifferentAddressesAreNotEqual()
    {
        Left = new Location("", DateTime.MinValue, "", 0.0, 0.0, EqualityTestAddressHelper.AddressWith(id: "id", postcode: "code1"), null);
        Right = new Location("", DateTime.MinValue, "", 0.0, 0.0, EqualityTestAddressHelper.AddressWith(id: "id", postcode: "code2"), null);
        Assert.NotEqual(Left, Right);
    }

    // using referenceequals - so LEFT != RIGHT but when LEFT updated to RIGHT, return no change.
    // TODO fix - ULITP-4006 - should return a change if the address is updated.
    [Fact]
    public void LocationsWithDifferentAddressesReturnNoChange()
    {
        var originalAddress = EqualityTestAddressHelper.AddressWith(id: "id", postcode: "code1");
        var original = new Location("", DateTime.MinValue, "", 0.0, 0.0, originalAddress, null);

        var differentAddress = EqualityTestAddressHelper.AddressWith(id: "id", postcode: "NEW POSTCODE");
        var result = original.ApplyChanges(DateTime.MinValue, "", 0.0, 0.0, differentAddress, null);

        result.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(InequalityCases))]
    public void ApplyChangesWithDifferencesShouldReturnChanged(string testcase, Action<LocationTests> assignTestData)
    {
        Debug.WriteLine(testcase);
        assignTestData(this);
        var changed = Left!.ApplyChanges(
            Right!.LastUpdatedDate,
            Right.OsMapReference,
            Right.Easting,
            Right.Northing,
            Right.Address,
            Right.Communication);
        changed.Should().BeTrue();
        Assert.Equal(Left, Right);
    }

    private static Communication NullCommunication()
    {
        return new Communication("id", DateTime.MinValue, null, null, null, null);
    }
}