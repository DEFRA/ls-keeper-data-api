using Bogus;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using System.Security.Cryptography;
using System.Text;
using KeeperData.Core.Anonymization;
using Microsoft.Extensions.Logging;

namespace KeeperData.Infrastructure.Anonymization;

public static class PiiAnonymizerHelper
{
    private const string MobileNumberFormat = "07#########";
    private const string TelephoneNumberFormat = "01### ######";
    private const string PostcodeFormat = "??# #??";
    private const string MapReferenceFormat = "??########";

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

        AnonymizeLocationInfo(holding, faker);
        AnonymizeAddressInfo(holding, faker);
    }

    public static void AnonymizeSamHolder(SamCphHolder holder)
    {
        var seed = GetStableSeed(holder.PARTY_ID);
        var faker = CreateFaker(seed);

        AnonymizePersonalInfo(holder, faker);
        AnonymizeContactInfo(holder, faker);
        AnonymizeAddressInfo(holder, faker);
    }

    public static void AnonymizeSamParty(SamParty party)
    {
        var seed = GetStableSeed(party.PARTY_ID);
        var faker = CreateFaker(seed);

        AnonymizePersonalInfo(party, faker);
        AnonymizeContactInfo(party, faker);
        AnonymizeAddressInfo(party, faker);
    }

    public static void AnonymizeCtsHolding(CtsCphHolding holding)
    {
        var seed = GetStableSeed(holding.LID_FULL_IDENTIFIER);
        var faker = CreateFaker(seed);

        AnonymizeLocationInfo(holding, faker);
        AnonymizeCtsAddressInfo(holding, faker);
        AnonymizeCtsContactInfo(holding, faker);
    }

    public static void AnonymizeCtsAgentOrKeeper(CtsAgentOrKeeper party)
    {
        var seed = GetStableSeed(party.PAR_ID);
        var faker = CreateFaker(seed);

        AnonymizeCtsPersonalInfo(party, faker);
        AnonymizeCtsContactInfo(party, faker);
        AnonymizeCtsAddressInfo(party, faker);
    }

    public static Faker CreateFaker(int seed) => new("en_GB") { Random = new Randomizer(seed) };

    private static int GetStableSeed(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return 0;

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(identifier));
        return BitConverter.ToInt32(hash, 0);
    }

    private static void AnonymizePersonalInfo<T>(T holder, Faker faker) where T : ISamCommonPiiData
    {
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
    }

    private static void AnonymizeContactInfo<T>(T entity, Faker faker) where T : ISamCommonPiiData
    {
        if (entity.INTERNET_EMAIL_ADDRESS is not null)
            entity.INTERNET_EMAIL_ADDRESS = faker.Internet.Email();

        if (entity.MOBILE_NUMBER is not null)
            entity.MOBILE_NUMBER = faker.Phone.PhoneNumber(MobileNumberFormat);

        if (entity.TELEPHONE_NUMBER is not null)
            entity.TELEPHONE_NUMBER = faker.Phone.PhoneNumber(TelephoneNumberFormat);
    }

    private static void AnonymizeAddressInfo<T>(T entity, Faker faker) where T : ISamCommonPiiAddressData
    {
        if (entity.STREET is not null)
            entity.STREET = faker.Address.StreetAddress();

        if (entity.LOCALITY is not null)
            entity.LOCALITY = faker.Address.SecondaryAddress();

        if (entity.TOWN is not null)
            entity.TOWN = faker.Address.City();

        if (entity.POSTCODE is not null)
            entity.POSTCODE = faker.Address.ZipCode(PostcodeFormat);

        if (entity.PAON_DESCRIPTION is not null)
            entity.PAON_DESCRIPTION = faker.Address.BuildingNumber();

        if (entity.SAON_DESCRIPTION is not null)
            entity.SAON_DESCRIPTION = faker.Address.SecondaryAddress();

        if (entity.UDPRN is not null)
            entity.UDPRN = faker.Random.Int(10_000_000, 99_999_999).ToString();
    }

    private static void AnonymizeLocationInfo(SamCphHolding holding, Faker faker)
    {
        if (holding.OS_MAP_REFERENCE is not null)
            holding.OS_MAP_REFERENCE = faker.Random.Replace(MapReferenceFormat).ToUpperInvariant();

        if (holding.EASTING.HasValue)
            holding.EASTING = faker.Random.Int(100000, 999999);

        if (holding.NORTHING.HasValue)
            holding.NORTHING = faker.Random.Int(200000, 999999);
    }

    private static void AnonymizeLocationInfo(CtsCphHolding holding, Faker faker)
    {
        if (holding.LOC_MAP_REFERENCE is not null)
            holding.LOC_MAP_REFERENCE = faker.Random.Replace(MapReferenceFormat).ToUpperInvariant();
    }

    private static void AnonymizeCtsAddressInfo<T>(T entity, Faker faker) where T : ICphCommonPiiData
    {
        if (entity.ADR_NAME is not null)
            entity.ADR_NAME = faker.Address.StreetAddress();

        if (entity.ADR_ADDRESS_2 is not null)
            entity.ADR_ADDRESS_2 = faker.Address.SecondaryAddress();

        if (entity.ADR_ADDRESS_3 is not null)
            entity.ADR_ADDRESS_3 = faker.Address.City();

        if (entity.ADR_ADDRESS_4 is not null)
            entity.ADR_ADDRESS_4 = faker.Address.County();

        if (entity.ADR_ADDRESS_5 is not null)
            entity.ADR_ADDRESS_5 = faker.Address.County();

        if (entity.ADR_POST_CODE is not null)
            entity.ADR_POST_CODE = faker.Address.ZipCode(PostcodeFormat);
    }

    private static void AnonymizeCtsPersonalInfo(CtsAgentOrKeeper party, Faker faker)
    {
        if (party.PAR_TITLE is not null)
            party.PAR_TITLE = faker.Name.Prefix();

        if (party.PAR_INITIALS is not null)
            party.PAR_INITIALS = faker.Name.FirstName()[..1];

        if (party.PAR_SURNAME is not null)
            party.PAR_SURNAME = faker.Name.LastName();
    }

    private static void AnonymizeCtsContactInfo(CtsAgentOrKeeper party, Faker faker)
    {
        if (party.PAR_EMAIL_ADDRESS is not null)
            party.PAR_EMAIL_ADDRESS = faker.Internet.Email();

        if (party.PAR_TEL_NUMBER is not null)
            party.PAR_TEL_NUMBER = faker.Phone.PhoneNumber(TelephoneNumberFormat);

        if (party.PAR_MOBILE_NUMBER is not null)
            party.PAR_MOBILE_NUMBER = faker.Phone.PhoneNumber(MobileNumberFormat);

        if (party.LOC_TEL_NUMBER is not null)
            party.LOC_TEL_NUMBER = faker.Phone.PhoneNumber(TelephoneNumberFormat);

        if (party.LOC_MOBILE_NUMBER is not null)
            party.LOC_MOBILE_NUMBER = faker.Phone.PhoneNumber(MobileNumberFormat);
    }

    private static void AnonymizeCtsContactInfo(CtsCphHolding holding, Faker faker)
    {
        if (holding.LOC_TEL_NUMBER is not null)
            holding.LOC_TEL_NUMBER = faker.Phone.PhoneNumber(TelephoneNumberFormat);

        if (holding.LOC_MOBILE_NUMBER is not null)
            holding.LOC_MOBILE_NUMBER = faker.Phone.PhoneNumber(MobileNumberFormat);
    }
}