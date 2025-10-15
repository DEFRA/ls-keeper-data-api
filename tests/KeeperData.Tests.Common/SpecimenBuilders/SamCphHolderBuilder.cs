using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class SamCphHolderBuilder(
    string fixedChangeType,
    int batchId,
    string holdingIdentifier) : ISpecimenBuilder
{
    private readonly Random _random = new();

    private readonly string _fixedChangeType = fixedChangeType;
    private readonly int _batchId = batchId;
    private readonly string _holdingIdentifier = holdingIdentifier;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(SamCphHolder))
        {
            return new SamCphHolder
            {
                CPHS = $"{_holdingIdentifier}",

                BATCH_ID = _batchId,
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}
