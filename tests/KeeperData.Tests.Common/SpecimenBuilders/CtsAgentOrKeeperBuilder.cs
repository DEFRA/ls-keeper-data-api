using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class CtsAgentOrKeeperBuilder(
    string fixedChangeType,
    int batchId,
    string holdingIdentifier,
    DateTime? fixedEndDate = null) : ISpecimenBuilder
{
    private readonly Random _random = new();

    private readonly string _fixedChangeType = fixedChangeType;
    private readonly int _batchId = batchId;
    private readonly string _holdingIdentifier = holdingIdentifier;
    private readonly DateTime? _fixedEndDate = fixedEndDate;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(CtsAgentOrKeeper))
        {
            var (addressName, address2, address3, address4, address5, postCode) = AddressGenerator.GenerateCtsAddress();
            var (title, initials, _, surname) = PersonGenerator.GeneratePerson();

            return new CtsAgentOrKeeper
            {
                PAR_ID = _random.NextInt64(10000000000, 99999999999),
                LID_FULL_IDENTIFIER = _holdingIdentifier,

                PAR_TITLE = title,
                PAR_INITIALS = initials,
                PAR_SURNAME = surname,
                PAR_TEL_NUMBER = CommunicationGenerator.GenerateTelephoneNumber(),
                PAR_MOBILE_NUMBER = CommunicationGenerator.GenerateMobileNumber(),
                PAR_EMAIL_ADDRESS = CommunicationGenerator.GenerateEmail(),

                LOC_TEL_NUMBER = CommunicationGenerator.GenerateTelephoneNumber(),
                LOC_MOBILE_NUMBER = CommunicationGenerator.GenerateMobileNumber(),

                ADR_NAME = addressName,
                ADR_ADDRESS_2 = address2,
                ADR_ADDRESS_3 = address3,
                ADR_ADDRESS_4 = address4,
                ADR_ADDRESS_5 = address5,
                ADR_POST_CODE = postCode,

                LPR_EFFECTIVE_FROM_DATE = DateTime.Today.AddDays(-_random.Next(500)),
                LPR_EFFECTIVE_TO_DATE = _fixedEndDate,

                BATCH_ID = _batchId,
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}