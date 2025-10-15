using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class SamHerdBuilder(
    string fixedChangeType,
    int batchId,
    string holdingIdentifier,
    List<string> partyIds) : ISpecimenBuilder
{
    private readonly Random _random = new();

    private readonly string _fixedChangeType = fixedChangeType;
    private readonly int _batchId = batchId;
    private readonly string _holdingIdentifier = holdingIdentifier;
    private readonly List<string> _partyIds = partyIds;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(SamHerd))
        {
            var ownerIds = _partyIds.OrderBy(_ => _random.Next()).Take(_random.Next(1, 3));
            var keeperIds = _partyIds.OrderBy(_ => _random.Next()).Take(_random.Next(1, 3));

            return new SamHerd
            {
                CPHH = _holdingIdentifier,

                OWNER_PARTY_IDS = string.Join(",", ownerIds),
                KEEPER_PARTY_IDS = string.Join(",", keeperIds),

                BATCH_ID = _batchId,
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}
