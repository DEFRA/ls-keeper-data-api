namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamCphHolding
{
    public string CPH { get; set; } = string.Empty;

    public string? FEATURE_NAME { get; set; }
    public string? CPH_TYPE { get; set; }

    public string? SAON_START_NUMBER { get; set; }
    public string? STREET { get; set; }
    public string? TOWN { get; set; }
    public string? LOCALITY { get; set; }
    public string? POSTCODE { get; set; }
    public string? COUNTRY_CODE { get; set; }
    public string? UDPRN { get; set; }

    public double? EASTING { get; set; }
    public double? NORTHING { get; set; }
    public string? OS_MAP_REFERENCE { get; set; }

    public string? FEATURE_ADDRESS_FROM_DATE { get; set; }
    public string? FEATURE_ADDRESS_TO_DATE { get; set; }

    public string? FACILITY_BUSINESS_ACTIVITY_CODE { get; set; }
    public string? FACILITY_TYPE_CODE { get; set; }
}