namespace KeeperData.Core.Anonymization;

public interface ICphCommonPiiAddressData
{
    string? ADR_NAME { get; set; }
    string? ADR_ADDRESS_2 { get; set; }
    string? ADR_ADDRESS_3 { get; set; }
    string? ADR_ADDRESS_4 { get; set; }
    string? ADR_ADDRESS_5 { get; set; }
    string? ADR_POST_CODE { get; set; }
}