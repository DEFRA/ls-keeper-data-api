using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class SamHerdBuilder(
    string fixedChangeType,
    int batchId,
    string holdingIdentifier,
    List<string> partyIds,
    bool allowNulls = true) : ISpecimenBuilder
{
    private readonly Random _random = new();

    private readonly string _fixedChangeType = fixedChangeType;
    private readonly int _batchId = batchId;
    private readonly string _holdingIdentifier = holdingIdentifier;
    private readonly List<string> _partyIds = partyIds;
    private readonly bool _allowNulls = allowNulls;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(SamHerd))
        {
            var ownerIds = _partyIds.OrderBy(_ => _random.Next()).Take(_random.Next(1, 3));
            var keeperIds = _partyIds.OrderBy(_ => _random.Next()).Take(_random.Next(1, 3));

            var (interval, intervalUnit) = IntervalGenerator.GenerateInterval(_allowNulls);

            var (
                businessActivityCode,
                facilityTypeCode,
                businessSubActivityCode,
                statusCode,
                movementRestrictionCode,
                animalSpeciesCode,
                animalProductionUsageCode
            ) = FacilityGenerator.GenerateFacility(_allowNulls);

            return new SamHerd
            {
                HERDMARK = "",
                CPHH = _holdingIdentifier,

                ANIMAL_SPECIES_CODE = animalSpeciesCode,
                ANIMAL_PURPOSE_CODE = animalProductionUsageCode,

                DISEASE_TYPE = _allowNulls && _random.Next(2) == 0 ? null : Guid.NewGuid().ToString(),
                INTERVAL = interval,
                INTERVAL_UNIT_OF_TIME = intervalUnit,
                MOVEMENT_RSTRCTN_RSN_CODE = movementRestrictionCode,

                OWNER_PARTY_IDS = string.Join(",", ownerIds),
                KEEPER_PARTY_IDS = string.Join(",", keeperIds),

                ANIMAL_GROUP_ID_MCH_FRM_DAT = DateTime.Today.AddDays(-_random.Next(500)),
                ANIMAL_GROUP_ID_MCH_TO_DAT = _allowNulls && _random.Next(2) == 0 ? null : DateTime.Today.AddDays(-_random.Next(50)),

                BATCH_ID = _batchId,
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}
