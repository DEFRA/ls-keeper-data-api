using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Shared;
using AddressDocument = KeeperData.Core.Documents.Silver.AddressDocument;
using CommunicationDocument = KeeperData.Core.Documents.Silver.CommunicationDocument;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class LocationMapper
{
    public static async Task<Address> AddressToGold(
        AddressDocument? incomingAddress,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        CancellationToken cancellationToken)
    {
        int? uprn = int.TryParse(incomingAddress?.UniquePropertyReferenceNumber, out var value) ? value : null;

        var country = await GetCountryAsync(
            incomingAddress?.CountryIdentifier,
            getCountryById,
            cancellationToken);

        var updatedAddress = Address.Create(
            uprn,
            incomingAddress?.AddressLine ?? string.Empty,
            incomingAddress?.AddressStreet,
            incomingAddress?.AddressTown,
            incomingAddress?.AddressLocality,
            incomingAddress?.AddressPostCode ?? string.Empty,
            country);
        return updatedAddress;
    }

    private static async Task<Country?> GetCountryAsync(
        string? countryIdentifier,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        CancellationToken cancellationToken)
    {
        if (countryIdentifier == null) return null;

        var countryDocument = await getCountryById(countryIdentifier, cancellationToken);

        if (countryDocument == null)
            return null;

        return countryDocument.ToDomain();
    }

    public static Communication CommunicationToGold(CommunicationDocument? incomingCommunication)
    {
        var updatedCommunication = Communication.Create(
            incomingCommunication?.Email,
            incomingCommunication?.Mobile,
            incomingCommunication?.Landline,
            false);
        return updatedCommunication;
    }
}