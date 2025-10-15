using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class CtsCphHoldingBuilder(
    string fixedChangeType,
    string? fixedLocType = null,
    DateTime? fixedEndDate = null) : ISpecimenBuilder
{
    private readonly Random _random = new();

    private readonly string[] _locationTypes = ["AG", "SH", "AH", "CL", "CC"];

    private readonly string _fixedChangeType = fixedChangeType;
    private readonly string? _fixedLocType = fixedLocType;
    private readonly DateTime? _fixedEndDate = fixedEndDate;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(CtsCphHolding))
        {
            var holdingIdentifier = CphGenerator.GenerateFormattedCph();
            var (addressName, address2, address3, address4, address5, postCode) = AddressGenerator.GenerateCtsAddress();

            return new CtsCphHolding
            {
                LID_FULL_IDENTIFIER = holdingIdentifier,
                LTY_LOC_TYPE = _fixedLocType ?? _locationTypes[_random.Next(_locationTypes.Length)],

                ADR_NAME = addressName,
                ADR_ADDRESS_2 = address2,
                ADR_ADDRESS_3 = address3,
                ADR_ADDRESS_4 = address4,
                ADR_ADDRESS_5 = address5,
                ADR_POST_CODE = postCode,

                LOC_TEL_NUMBER = CommunicationGenerator.GenerateTelephoneNumber(),
                LOC_MOBILE_NUMBER = CommunicationGenerator.GenerateMobileNumber(),
                LOC_MAP_REFERENCE = AddressGenerator.GenerateMapReference(),

                LOC_EFFECTIVE_FROM = DateTime.Today.AddDays(-_random.Next(1000)),
                LOC_EFFECTIVE_TO = _fixedEndDate,

                BATCH_ID = _random.Next(1000),
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}