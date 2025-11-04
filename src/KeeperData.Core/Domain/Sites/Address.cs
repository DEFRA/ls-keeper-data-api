using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class Address : ValueObject
{
    public string Id { get; private set; }
    public int? Uprn { get; private set; }
    public string AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? PostTown { get; private set; }
    public string? County { get; private set; }
    public string PostCode { get; private set; }
    public Country? Country { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    public Address(
        string id,
        int? uprn,
        string addressLine1,
        string? addressLine2,
        string? postTown,
        string? county,
        string postCode,
        Country? country,
        DateTime? lastUpdatedDate)
    {
        Id = id;
        Uprn = uprn;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        PostTown = postTown;
        County = county;
        PostCode = postCode;
        Country = country;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static Address Create(
        int? uprn,
        string addressLine1,
        string? addressLine2,
        string? postTown,
        string? county,
        string postCode,
        Country? country)
    {
        return new Address(
            Guid.NewGuid().ToString(),
            uprn,
            addressLine1,
            addressLine2,
            postTown,
            county,
            postCode,
            country,
            DateTime.UtcNow);
    }

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        int? uprn,
        string addressLine1,
        string? addressLine2,
        string? postTown,
        string? county,
        string postCode,
        Country? country)
    {
        var changed = false;

        changed |= Change(Uprn, uprn, v => Uprn = v, lastUpdatedDate);
        changed |= Change(AddressLine1, addressLine1, v => AddressLine1 = v, lastUpdatedDate);
        changed |= Change(AddressLine2, addressLine2, v => AddressLine2 = v, lastUpdatedDate);
        changed |= Change(PostTown, postTown, v => PostTown = v, lastUpdatedDate);
        changed |= Change(County, county, v => County = v, lastUpdatedDate);
        changed |= Change(PostCode, postCode, v => PostCode = v, lastUpdatedDate);
        changed |= Change(Country, country, v => Country = v, lastUpdatedDate);

        return changed;
    }

    private bool Change<T>(T currentValue, T newValue, Action<T> setter, DateTime lastUpdatedAt)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) return false;
        setter(newValue);
        LastUpdatedDate = lastUpdatedAt;
        return true;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Uprn ?? default;
        yield return AddressLine1;
        yield return AddressLine2 ?? string.Empty;
        yield return PostTown ?? string.Empty;
        yield return County ?? string.Empty;
        yield return PostCode;
        yield return Country ?? default!;
    }
}