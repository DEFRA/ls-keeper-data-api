namespace KeeperData.Tests.Common.Generators;

public class FacilityGenerator
{
    private static readonly Random s_random = new();

    private static readonly string[] s_businessActivities = ["AA", "AB", "AC", "AD", "AE"];
    private static readonly string[] s_facilityTypes = ["BA", "BB", "BC", "BD", "BE"];
    private static readonly string[] s_speciesCodes = ["CA", "CB", "CC", "CD", "CE"];
    private static readonly string[] s_usageCodes = ["DA", "DB", "DC", "DD", "DE"];

    public static (
        string? businessActivityCode,
        string? facilityTypeCode,
        string? businessSubActivityCode,
        string? statusCode,
        string? movementRestrictionCode,
        string animalSpeciesCode,
        string animalProductionUsageCode
    ) GenerateFacility(bool allowNulls = false)
    {
        var businessActivityCode = allowNulls && s_random.Next(2) == 0 ? null : s_businessActivities[s_random.Next(s_businessActivities.Length)];
        var facilityTypeCode = allowNulls && s_random.Next(2) == 0 ? null : s_facilityTypes[s_random.Next(s_facilityTypes.Length)];
        var businessSubActivityCode = businessActivityCode + "-" + facilityTypeCode;// TODO validate
        var statusCode = allowNulls && s_random.Next(2) == 0 ? null : Guid.NewGuid().ToString();
        var movementRestrictionCode = allowNulls && s_random.Next(2) == 0 ? null : Guid.NewGuid().ToString();
        var animalSpeciesCode = allowNulls && s_random.Next(2) == 0 ? string.Empty : s_speciesCodes[s_random.Next(s_speciesCodes.Length)];
        var animalProductionUsageCode = allowNulls && s_random.Next(2) == 0 ? string.Empty : s_usageCodes[s_random.Next(s_usageCodes.Length)];

        return (
            businessActivityCode,
            facilityTypeCode,
            businessSubActivityCode,
            statusCode,
            movementRestrictionCode,
            animalSpeciesCode,
            animalProductionUsageCode);
    }

    public static (
        string animalSpeciesCode,
        string animalProductionUsageCode
    ) GenerateAnimalSpeciesAndProductionUsageCodes(bool allowNulls = true)
    {
        var animalSpeciesCode = allowNulls && s_random.Next(2) == 0 ? string.Empty : s_speciesCodes[s_random.Next(s_speciesCodes.Length)];
        var animalProductionUsageCode = allowNulls && s_random.Next(2) == 0 ? string.Empty : s_usageCodes[s_random.Next(s_usageCodes.Length)];

        return (
            animalSpeciesCode,
            animalProductionUsageCode);
    }
}