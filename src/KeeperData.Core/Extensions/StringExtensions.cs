namespace KeeperData.Core.Extensions;

public static class StringExtensions
{
    private const string PermanentLandHoldingRelationshipType = "PCPHLANDUSEDBYTCPH";

    public static bool IsPermanentLandHolding(this string? cphRelationshipType)
    {
        return string.Equals(cphRelationshipType, PermanentLandHoldingRelationshipType, StringComparison.OrdinalIgnoreCase);
    }
}