using Bogus;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace KeeperData.Infrastructure.Anonymization;

public static class PiiAnonymizerHelper
{
    /// <summary>
    /// Anonymizes personally identifiable information (PII) in the data contained within the specified response, if
    /// applicable for the data type.
    /// </summary>
    /// <remarks>Anonymization is applied only to specific data types known to contain PII. For types
    /// that do not contain PII, such as SamHerd or scan identifier types, no changes are made. The method modifies
    /// the data in place within the provided response object.</remarks>
    /// <typeparam name="T">The type of the data contained in the response. Anonymization is performed only for supported types that may
    /// contain PII.</typeparam>
    /// <param name="result">The response object whose data will be anonymized. If null, or if the data is null or empty, no action is
    /// taken.</param>
    /// <param name="logger"></param>
    public static void AnonymizeResponse<T>(DataBridgeResponse<T>? result, ILogger? logger = null)
    {
        if (result == null || result.Data == null || result.Data.Count == 0)
            return;

        var type = typeof(T);

        if (type == typeof(SamCphHolding))
        {
            AnonymizeAll(result.Data as List<SamCphHolding> ?? [], AnonymizeSamHolding, logger);
        }
        else if (type == typeof(SamCphHolder))
        {
            AnonymizeAll(result.Data as List<SamCphHolder> ?? [], AnonymizeSamHolder, logger);
        }
        else if (type == typeof(SamParty))
        {
            AnonymizeAll(result.Data as List<SamParty> ?? [], AnonymizeSamParty, logger);
        }
        else if (type == typeof(CtsCphHolding))
        {
            AnonymizeAll(result.Data as List<CtsCphHolding> ?? [], AnonymizeCtsHolding, logger);
        }
        else if (type == typeof(CtsAgentOrKeeper))
        {
            AnonymizeAll(result.Data as List<CtsAgentOrKeeper> ?? [], AnonymizeCtsAgentOrKeeper, logger);
        }
        else if (type == typeof(SamHerd))
        {
            return;
        }
    }

    /// <summary>
    /// Applies the specified anonymization action to each item in the provided list, logging any errors that occur
    /// during processing.
    /// </summary>
    /// <remarks>If an exception is thrown while anonymizing an item, the exception is caught and logged using
    /// the provided logger, and processing continues with the next item. The method does not stop or rethrow exceptions
    /// for individual failures.</remarks>
    /// <typeparam name="T">The type of items to be anonymized.</typeparam>
    /// <param name="items">The list of items to anonymize. Cannot be null.</param>
    /// <param name="anonymize">The action to perform on each item to anonymize it. Cannot be null.</param>
    /// <param name="logger">An optional logger used to record errors encountered during anonymization. If null, errors are not logged.</param>
    public static void AnonymizeAll<T>(List<T> items, Action<T> anonymize, ILogger? logger = null)
    {
        foreach (var item in items)
        {
            try
            {
                anonymize(item);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to anonymize {Type}", typeof(T).Name);
            }
        }
    }

    public static void AnonymizeSamHolding(SamCphHolding holding)
    {
        var seed = GetStableSeed(holding.CPH);
        var faker = CreateFaker(seed);

        if (holding.OS_MAP_REFERENCE is not null)
            holding.OS_MAP_REFERENCE = faker.Random.Replace("??########").ToUpperInvariant();

        if (holding.EASTING.HasValue)
            holding.EASTING = faker.Random.Int(100000, 999999);

        if (holding.NORTHING.HasValue)
            holding.NORTHING = faker.Random.Int(200000, 999999);

        if (holding.STREET is not null)
            holding.STREET = faker.Address.StreetAddress();

        if (holding.LOCALITY is not null)
            holding.LOCALITY = faker.Address.SecondaryAddress();

        if (holding.TOWN is not null)
            holding.TOWN = faker.Address.City();

        if (holding.POSTCODE is not null)
            holding.POSTCODE = faker.Address.ZipCode("??# #??");

        if (holding.PAON_DESCRIPTION is not null)
            holding.PAON_DESCRIPTION = faker.Address.BuildingNumber();

        if (holding.SAON_DESCRIPTION is not null)
            holding.SAON_DESCRIPTION = faker.Address.SecondaryAddress();
    }

    public static void AnonymizeSamHolder(SamCphHolder holder)
    {
        var seed = GetStableSeed(holder.PARTY_ID);
        var faker = CreateFaker(seed);

        if (holder.PERSON_TITLE is not null)
            holder.PERSON_TITLE = faker.Name.Prefix();

        if (holder.PERSON_GIVEN_NAME is not null)
            holder.PERSON_GIVEN_NAME = faker.Name.FirstName();

        if (holder.PERSON_GIVEN_NAME2 is not null)
            holder.PERSON_GIVEN_NAME2 = faker.Name.FirstName();

        if (holder.PERSON_INITIALS is not null)
            holder.PERSON_INITIALS = holder.PERSON_GIVEN_NAME?[..1];

        if (holder.PERSON_FAMILY_NAME is not null)
            holder.PERSON_FAMILY_NAME = faker.Name.LastName();

        if (holder.ORGANISATION_NAME is not null)
            holder.ORGANISATION_NAME = faker.Company.CompanyName();

        if (holder.INTERNET_EMAIL_ADDRESS is not null)
            holder.INTERNET_EMAIL_ADDRESS = faker.Internet.Email();

        if (holder.MOBILE_NUMBER is not null)
            holder.MOBILE_NUMBER = faker.Phone.PhoneNumber("07#########");

        if (holder.TELEPHONE_NUMBER is not null)
            holder.TELEPHONE_NUMBER = faker.Phone.PhoneNumber("01### ######");

        if (holder.STREET is not null)
            holder.STREET = faker.Address.StreetAddress();

        if (holder.LOCALITY is not null)
            holder.LOCALITY = faker.Address.SecondaryAddress();

        if (holder.TOWN is not null)
            holder.TOWN = faker.Address.City();

        if (holder.POSTCODE is not null)
            holder.POSTCODE = faker.Address.ZipCode("??# #??");

        if (holder.PAON_DESCRIPTION is not null)
            holder.PAON_DESCRIPTION = faker.Address.BuildingNumber();

        if (holder.SAON_DESCRIPTION is not null)
            holder.SAON_DESCRIPTION = faker.Address.SecondaryAddress();
    }

    public static void AnonymizeSamParty(SamParty party)
    {
        var seed = GetStableSeed(party.PARTY_ID);
        var faker = CreateFaker(seed);

        if (party.PERSON_TITLE is not null)
            party.PERSON_TITLE = faker.Name.Prefix();

        if (party.PERSON_GIVEN_NAME is not null)
            party.PERSON_GIVEN_NAME = faker.Name.FirstName();

        if (party.PERSON_GIVEN_NAME2 is not null)
            party.PERSON_GIVEN_NAME2 = faker.Name.FirstName();

        if (party.PERSON_INITIALS is not null)
            party.PERSON_INITIALS = party.PERSON_GIVEN_NAME?[..1];

        if (party.PERSON_FAMILY_NAME is not null)
            party.PERSON_FAMILY_NAME = faker.Name.LastName();

        if (party.ORGANISATION_NAME is not null)
            party.ORGANISATION_NAME = faker.Company.CompanyName();

        if (party.INTERNET_EMAIL_ADDRESS is not null)
            party.INTERNET_EMAIL_ADDRESS = faker.Internet.Email();

        if (party.MOBILE_NUMBER is not null)
            party.MOBILE_NUMBER = faker.Phone.PhoneNumber("07#########");

        if (party.TELEPHONE_NUMBER is not null)
            party.TELEPHONE_NUMBER = faker.Phone.PhoneNumber("01### ######");

        if (party.STREET is not null)
            party.STREET = faker.Address.StreetAddress();

        if (party.LOCALITY is not null)
            party.LOCALITY = faker.Address.SecondaryAddress();

        if (party.TOWN is not null)
            party.TOWN = faker.Address.City();

        if (party.POSTCODE is not null)
            party.POSTCODE = faker.Address.ZipCode("??# #??");

        if (party.PAON_DESCRIPTION is not null)
            party.PAON_DESCRIPTION = faker.Address.BuildingNumber();

        if (party.SAON_DESCRIPTION is not null)
            party.SAON_DESCRIPTION = faker.Address.SecondaryAddress();
    }

    public static void AnonymizeCtsHolding(CtsCphHolding holding)
    {
        var seed = GetStableSeed(holding.LID_FULL_IDENTIFIER);
        var faker = CreateFaker(seed);

        if (holding.LOC_MAP_REFERENCE is not null)
            holding.LOC_MAP_REFERENCE = faker.Random.Replace("??########").ToUpperInvariant();

        if (holding.ADR_NAME is not null)
            holding.ADR_NAME = faker.Address.StreetAddress();

        if (holding.ADR_ADDRESS_2 is not null)
            holding.ADR_ADDRESS_2 = faker.Address.SecondaryAddress();

        if (holding.ADR_ADDRESS_3 is not null)
            holding.ADR_ADDRESS_3 = faker.Address.City();

        if (holding.ADR_ADDRESS_4 is not null)
            holding.ADR_ADDRESS_4 = faker.Address.County();

        if (holding.ADR_ADDRESS_5 is not null)
            holding.ADR_ADDRESS_5 = faker.Address.County();

        if (holding.ADR_POST_CODE is not null)
            holding.ADR_POST_CODE = faker.Address.ZipCode("??# #??");

        if (holding.LOC_TEL_NUMBER is not null)
            holding.LOC_TEL_NUMBER = faker.Phone.PhoneNumber("01### ######");

        if (holding.LOC_MOBILE_NUMBER is not null)
            holding.LOC_MOBILE_NUMBER = faker.Phone.PhoneNumber("07#########");
    }

    public static void AnonymizeCtsAgentOrKeeper(CtsAgentOrKeeper party)
    {
        var seed = GetStableSeed(party.PAR_ID);
        var faker = CreateFaker(seed);

        if (party.PAR_TITLE is not null)
            party.PAR_TITLE = faker.Name.Prefix();

        if (party.PAR_INITIALS is not null)
            party.PAR_INITIALS = faker.Name.FirstName()[..1];

        if (party.PAR_SURNAME is not null)
            party.PAR_SURNAME = faker.Name.LastName();

        if (party.PAR_EMAIL_ADDRESS is not null)
            party.PAR_EMAIL_ADDRESS = faker.Internet.Email();

        if (party.PAR_TEL_NUMBER is not null)
            party.PAR_TEL_NUMBER = faker.Phone.PhoneNumber("01### ######");

        if (party.PAR_MOBILE_NUMBER is not null)
            party.PAR_MOBILE_NUMBER = faker.Phone.PhoneNumber("07#########");

        if (party.LOC_TEL_NUMBER is not null)
            party.LOC_TEL_NUMBER = faker.Phone.PhoneNumber("01### ######");

        if (party.LOC_MOBILE_NUMBER is not null)
            party.LOC_MOBILE_NUMBER = faker.Phone.PhoneNumber("07#########");

        if (party.ADR_NAME is not null)
            party.ADR_NAME = faker.Address.StreetAddress();

        if (party.ADR_ADDRESS_2 is not null)
            party.ADR_ADDRESS_2 = faker.Address.SecondaryAddress();

        if (party.ADR_ADDRESS_3 is not null)
            party.ADR_ADDRESS_3 = faker.Address.City();

        if (party.ADR_ADDRESS_4 is not null)
            party.ADR_ADDRESS_4 = faker.Address.County();

        if (party.ADR_ADDRESS_5 is not null)
            party.ADR_ADDRESS_5 = faker.Address.County();

        if (party.ADR_POST_CODE is not null)
            party.ADR_POST_CODE = faker.Address.ZipCode("??# #??");
    }

    public static Faker CreateFaker(int seed) => new("en_GB") { Random = new Randomizer(seed) };

    private static int GetStableSeed(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return 0;

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(identifier));
        return BitConverter.ToInt32(hash, 0);
    }
}