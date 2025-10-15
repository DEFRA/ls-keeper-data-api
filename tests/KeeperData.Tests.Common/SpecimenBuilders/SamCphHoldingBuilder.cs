using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class SamCphHoldingBuilder(
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
        if (request is Type type && type == typeof(SamCphHolding))
        {
            return new SamCphHolding
            {
                CPH = _holdingIdentifier,

                FEATURE_ADDRESS_TO_DATE = _fixedEndDate,

                BATCH_ID = _batchId,
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}
