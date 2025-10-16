using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class CtsCphHoldingBuilder(
    string fixedChangeType,
    int batchId,
    string holdingIdentifier,
    string? fixedLocType = null,
    DateTime? fixedEndDate = null,
    bool allowNulls = true) : ISpecimenBuilder
{
    private readonly Random _random = new();

    private readonly string[] _locationTypes = ["AG", "SH", "AH", "CL", "CC"];

    private readonly string _fixedChangeType = fixedChangeType;
    private readonly int _batchId = batchId;
    private readonly string _holdingIdentifier = holdingIdentifier;
    private readonly string? _fixedLocType = fixedLocType;
    private readonly DateTime? _fixedEndDate = fixedEndDate;
    private readonly bool _allowNulls = allowNulls;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(CtsCphHolding))
        {
            var (addressName, address2, address3, address4, address5, postCode) = AddressGenerator.GenerateCtsAddress(_allowNulls);

            return new CtsCphHolding
            {
                LID_FULL_IDENTIFIER = _holdingIdentifier,
                LTY_LOC_TYPE = _fixedLocType ?? _locationTypes[_random.Next(_locationTypes.Length)],

                ADR_NAME = addressName,
                ADR_ADDRESS_2 = address2,
                ADR_ADDRESS_3 = address3,
                ADR_ADDRESS_4 = address4,
                ADR_ADDRESS_5 = address5,
                ADR_POST_CODE = postCode,

                LOC_TEL_NUMBER = CommunicationGenerator.GenerateTelephoneNumber(_allowNulls),
                LOC_MOBILE_NUMBER = CommunicationGenerator.GenerateMobileNumber(_allowNulls),
                LOC_MAP_REFERENCE = AddressGenerator.GenerateMapReference(_allowNulls),

                LOC_EFFECTIVE_FROM = DateTime.Today.AddDays(-_random.Next(1000)),
                LOC_EFFECTIVE_TO = _fixedEndDate,

                BATCH_ID = _batchId,
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}