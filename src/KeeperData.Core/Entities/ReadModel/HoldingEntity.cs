namespace KeeperData.Core.Entities.ReadModel;

/// <summary>
/// A holding in the normalised SAM read model. The CPH is unique across the table.
/// </summary>
public class HoldingEntity
{
    public string Id { get; set; } = string.Empty;
    public string Cph { get; set; } = string.Empty;
    public string? FeatureName { get; set; }
    public string? CphType { get; set; }
}
