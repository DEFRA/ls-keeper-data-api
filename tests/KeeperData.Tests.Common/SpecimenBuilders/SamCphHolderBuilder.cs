using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class SamCphHolderBuilder(
    string fixedChangeType,
    int batchId,
    List<string> holdingIdentifiers,
    bool allowNulls = true) : ISpecimenBuilder
{
    private readonly Random _random = new();

    private readonly string _fixedChangeType = fixedChangeType;
    private readonly int _batchId = batchId;
    private readonly List<string> _holdingIdentifiers = holdingIdentifiers;
    private readonly bool _allowNulls = allowNulls;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(SamCphHolder))
        {
            var partyId = $"C{_random.Next(1, 9):D6}";

            var (
                saonStart,
                saonStartSuffix,
                saonEnd,
                saonEndSuffix,
                paonStart,
                paonStartSuffix,
                paonEnd,
                paonEndSuffix,
                street,
                town,
                locality,
                postcode,
                countryCode,
                ukInternalCode
            ) = AddressGenerator.GenerateSamAddress(_allowNulls);

            var (title, initials, forename, middlename, surname) = PersonGenerator.GeneratePerson(_allowNulls);

            return new SamCphHolder
            {
                PARTY_ID = partyId,

                PERSON_TITLE = title,
                PERSON_GIVEN_NAME = forename,
                PERSON_GIVEN_NAME2 = middlename,
                PERSON_INITIALS = initials,
                PERSON_FAMILY_NAME = surname,

                ORGANISATION_NAME = Guid.NewGuid().ToString(),
                TELEPHONE_NUMBER = CommunicationGenerator.GenerateTelephoneNumber(_allowNulls),
                MOBILE_NUMBER = CommunicationGenerator.GenerateMobileNumber(_allowNulls),
                INTERNET_EMAIL_ADDRESS = CommunicationGenerator.GenerateEmail(_allowNulls),

                SAON_START_NUMBER = saonStart,
                SAON_START_NUMBER_SUFFIX = saonStartSuffix,
                SAON_END_NUMBER = saonEnd,
                SAON_END_NUMBER_SUFFIX = saonEndSuffix,

                PAON_START_NUMBER = paonStart,
                PAON_START_NUMBER_SUFFIX = paonStartSuffix,
                PAON_END_NUMBER = paonEnd,
                PAON_END_NUMBER_SUFFIX = paonEndSuffix,

                STREET = street,
                TOWN = town,
                LOCALITY = locality,
                UK_INTERNAL_CODE = ukInternalCode,
                POSTCODE = postcode,
                COUNTRY_CODE = countryCode,
                UDPRN = _allowNulls && _random.Next(2) == 0 ? null : Guid.NewGuid().ToString(),
                PREFERRED_CONTACT_METHOD_IND = 'T',

                CPHS = string.Join(",", _holdingIdentifiers.Where(c => !string.IsNullOrWhiteSpace(c))),

                BATCH_ID = _batchId,
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}