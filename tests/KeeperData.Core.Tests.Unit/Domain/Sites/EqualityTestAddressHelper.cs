using KeeperData.Core.Domain.Shared;

namespace KeeperData.Core.Tests.Unit.Domain.Sites;

public static class EqualityTestAddressHelper
{

    public static Address AddressWith(string id, int? propertyRef = null, string line1 = "", string? line2 = null, string? postTown = "", string? county = null, string postcode = "", Country? country = null, DateTime? updated = null)
    {
        return new Address(id, propertyRef, line1, line2, postTown, county, postcode, country, updated ?? DateTime.MinValue);
    }

    public static Address NullAddress(string id)
    {
        return AddressWith(id, null, "", null, "", null, "", null, DateTime.MinValue);
    }

    public static Address EmptyAddress(string id)
    {
        return AddressWith(id, 0, "", "", "", "", "", null, DateTime.MinValue);
    }

    public static Communication CommunicationWithId(string id, string email = "joe@google.com")
    {
        return new Communication(id: id, new DateTime(1980, 1, 1), email: email, null, null, null);
    }
}