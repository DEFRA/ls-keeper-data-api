namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamCphHolding : BronzeBase
{
    public string CPH { get; set; } = string.Empty;

    public string FEATURE_NAME { get; set; } = string.Empty;
    public string CPH_TYPE { get; set; } = string.Empty;

    public decimal? ADDRESS_PK { get; set; }

    public short? SAON_START_NUMBER { get; set; }
    public char? SAON_START_NUMBER_SUFFIX { get; set; }
    public short? SAON_END_NUMBER { get; set; }
    public char? SAON_END_NUMBER_SUFFIX { get; set; }

    public short? PAON_START_NUMBER { get; set; }
    public char? PAON_START_NUMBER_SUFFIX { get; set; }
    public short? PAON_END_NUMBER { get; set; }
    public char? PAON_END_NUMBER_SUFFIX { get; set; }

    public string? STREET { get; set; }
    public string? TOWN { get; set; }
    public string? LOCALITY { get; set; }
    public string? POSTCODE { get; set; }
    public string? UK_INTERNAL_CODE { get; set; }
    public string? COUNTRY_CODE { get; set; }
    public string? UDPRN { get; set; }

    public int? EASTING { get; set; }
    public int? NORTHING { get; set; }
    public string? OS_MAP_REFERENCE { get; set; }

    public string? DISEASE_TYPE { get; set; }
    public decimal? INTERVAL { get; set; }
    public string? INTERVAL_UNIT_OF_TIME { get; set; }

    public DateTime FEATURE_ADDRESS_FROM_DATE { get; set; } = default;
    public DateTime? FEATURE_ADDRESS_TO_DATE { get; set; }

    public string? CPH_RELATIONSHIP_TYPE { get; set; }
    public string? SECONDARY_CPH { get; set; } = string.Empty;

    public string? FACILITY_BUSINSS_ACTVTY_CODE { get; set; }
    public string? FACILITY_TYPE_CODE { get; set; }
    public string? FCLTY_SUB_BSNSS_ACTVTY_CODE { get; set; }

    public string? FEATURE_STATUS_CODE { get; set; }
    public string? MOVEMENT_RSTRCTN_RSN_CODE { get; set; }

    public string ANIMAL_SPECIES_CODE { get; set; } = string.Empty;
    public string ANIMAL_PRODUCTION_USAGE_CODE { get; set; } = string.Empty;

    public string SecondaryCphUnwrapped => UnwrapCoalesced(SECONDARY_CPH);
    public string AnimalSpeciesCodeUnwrapped => UnwrapCoalesced(ANIMAL_SPECIES_CODE);

    public List<string> AnimalProductionUsageCodeList => SplitCommaSeparatedIds(UnwrapCoalesced(ANIMAL_PRODUCTION_USAGE_CODE));
}