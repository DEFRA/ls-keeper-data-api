using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class SamPartyBuilder(
    string fixedChangeType,
    int batchId,
    IEnumerable<string> partyIds,
    DateTime? fixedEndDate = null,
    bool allowNulls = true) : ISpecimenBuilder
{
    private readonly Random _random = new();

    private readonly string _fixedChangeType = fixedChangeType;
    private readonly int _batchId = batchId;
    private readonly Queue<string> _partyIds = new(partyIds);
    private readonly DateTime? _fixedEndDate = fixedEndDate;
    private readonly bool _allowNulls = allowNulls;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(SamParty))
        {
            var partyId = _partyIds.Dequeue();

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

            var partyRoles = RoleGenerator.GenerateRoles(2, false, _allowNulls);

            return new SamParty
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

                ROLES = string.Join(",", partyRoles),

                PARTY_ROLE_FROM_DATE = DateTime.Today.AddDays(-_random.Next(500)),
                PARTY_ROLE_TO_DATE = _fixedEndDate,

                BATCH_ID = _batchId,
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}