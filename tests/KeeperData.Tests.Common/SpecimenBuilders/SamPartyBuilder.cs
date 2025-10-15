using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class SamPartyBuilder(
    string fixedChangeType,
    int batchId,
    IEnumerable<string> partyIds) : ISpecimenBuilder
{
    private readonly Random _random = new();

    private readonly string _fixedChangeType = fixedChangeType;
    private readonly int _batchId = batchId;
    private readonly Queue<string> _partyIds = new(partyIds);

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(SamParty))
        {
            var partyId = _partyIds.Dequeue();

            return new SamParty
            {
                PARTY_ID = partyId,

                BATCH_ID = _batchId,
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}
