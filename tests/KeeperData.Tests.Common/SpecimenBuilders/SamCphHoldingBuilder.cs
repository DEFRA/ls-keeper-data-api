using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class SamCphHoldingBuilder(
    string fixedChangeType,
    int batchId,
    string holdingIdentifier,
    DateTime? fixedEndDate = null,
    bool allowNulls = true) : ISpecimenBuilder
{
    private readonly Random _random = new();

    private readonly string _fixedChangeType = fixedChangeType;
    private readonly int _batchId = batchId;
    private readonly string _holdingIdentifier = holdingIdentifier;
    private readonly DateTime? _fixedEndDate = fixedEndDate;
    private readonly bool _allowNulls = allowNulls;

    private static readonly string[] s_cphType = ["PERMANENT", "EMERGENCY", "TEMPORARY"];

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(SamCphHolding))
        {
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

            return new SamCphHolding
            {
                CPH = _holdingIdentifier,

                FEATURE_NAME = Guid.NewGuid().ToString(),
                CPH_TYPE = s_cphType[_random.Next(s_cphType.Length)],

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

                EASTING = _allowNulls && _random.Next(2) == 0 ? null : _random.Next(100000, 999999),
                NORTHING = _allowNulls && _random.Next(2) == 0 ? null : _random.Next(100000, 999999),
                OS_MAP_REFERENCE = AddressGenerator.GenerateMapReference(_allowNulls),

                DISEASE_TYPE = _allowNulls && _random.Next(2) == 0 ? null : Guid.NewGuid().ToString(),
                INTERVAL = interval,
                INTERVAL_UNIT_OF_TIME = intervalUnit,

                FEATURE_ADDRESS_FROM_DATE = DateTime.Today.AddDays(-_random.Next(500)),
                FEATURE_ADDRESS_TO_DATE = _fixedEndDate,

                CPH_RELATIONSHIP_TYPE = _allowNulls && _random.Next(2) == 0 ? null : Guid.NewGuid().ToString(),
                SECONDARY_CPH = _allowNulls && _random.Next(2) == 0 ? null : CphGenerator.GenerateFormattedCph(),

                FACILITY_BUSINSS_ACTVTY_CODE = businessActivityCode,
                FACILITY_TYPE_CODE = facilityTypeCode,
                FCLTY_SUB_BSNSS_ACTVTY_CODE = businessSubActivityCode,

                MOVEMENT_RSTRCTN_RSN_CODE = movementRestrictionCode,

                ANIMAL_SPECIES_CODE = animalSpeciesCode,
                ANIMAL_PRODUCTION_USAGE_CODE = animalProductionUsageCode,

                BATCH_ID = _batchId,
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}