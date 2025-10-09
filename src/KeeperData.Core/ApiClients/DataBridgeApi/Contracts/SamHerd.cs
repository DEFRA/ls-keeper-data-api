namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamHerd : BronzeBase
{
    public string HERDMARK { get; set; } = string.Empty;
    public string CPHH { get; set; } = string.Empty;

    public string? ANIMAL_SPECIES_CODE { get; set; }
    public string ANIMAL_PURPOSE_CODE { get; set; } = string.Empty;

    public string? DISEASE_TYPE { get; set; }
    public decimal? INTERVAL { get; set; }
    public string? INTERVAL_UNIT_OF_TIME { get; set; }
    public string? MOVEMENT_RSTRCTN_RSN_CODE { get; set; }

    public string? KEEPER_PARTY_IDS { get; set; }
    public string? OWNER_PARTY_IDS { get; set; }

    public DateTime ANIMAL_GROUP_ID_MCH_FRM_DAT { get; set; } = default;
    public DateTime? ANIMAL_GROUP_ID_MCH_TO_DAT { get; set; }

    public List<string> KeeperPartyIdList => SplitCommaSeparatedIds(KEEPER_PARTY_IDS ?? string.Empty);
    public List<string> OwnerPartyIdList => SplitCommaSeparatedIds(OWNER_PARTY_IDS ?? string.Empty);

    public string AnimalSpeciesCodeUnwrapped => UnwrapCoalesced(ANIMAL_SPECIES_CODE);
    public string AnimalPurposeCodeUnwrapped => UnwrapCoalesced(ANIMAL_PURPOSE_CODE);
}