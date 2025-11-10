using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamHerd : BronzeBase
{
    [JsonPropertyName("HERDMARK")]
    public string HERDMARK { get; set; } = string.Empty;

    [JsonPropertyName("CPHH")]
    public string CPHH { get; set; } = string.Empty;

    [JsonPropertyName("ANIMAL_SPECIES_CODE")]
    public string? ANIMAL_SPECIES_CODE { get; set; }

    [JsonPropertyName("ANIMAL_PURPOSE_CODE")]
    public string ANIMAL_PURPOSE_CODE { get; set; } = string.Empty;

    [JsonPropertyName("DISEASE_TYPE")]
    public string? DISEASE_TYPE { get; set; }

    [JsonPropertyName("INTERVAL")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? INTERVAL { get; set; }

    [JsonPropertyName("INTERVAL_UNIT_OF_TIME")]
    public string? INTERVAL_UNIT_OF_TIME { get; set; }

    [JsonPropertyName("MOVEMENT_RSTRCTN_RSN_CODE")]
    public string? MOVEMENT_RSTRCTN_RSN_CODE { get; set; }

    [JsonPropertyName("KEEPER_PARTY_IDS")]
    public string? KEEPER_PARTY_IDS { get; set; }

    [JsonPropertyName("OWNER_PARTY_IDS")]
    public string? OWNER_PARTY_IDS { get; set; }

    [JsonPropertyName("ANIMAL_GROUP_ID_MCH_FRM_DAT")]
    [JsonConverter(typeof(SafeDateTimeConverter))]
    public DateTime ANIMAL_GROUP_ID_MCH_FRM_DAT { get; set; } = default;

    [JsonPropertyName("ANIMAL_GROUP_ID_MCH_TO_DAT")]
    [JsonConverter(typeof(SafeNullableDateTimeConverter))]
    public DateTime? ANIMAL_GROUP_ID_MCH_TO_DAT { get; set; }

    public List<string> KeeperPartyIdList => SplitCommaSeparatedIds(KEEPER_PARTY_IDS ?? string.Empty);
    public List<string> OwnerPartyIdList => SplitCommaSeparatedIds(OWNER_PARTY_IDS ?? string.Empty);

    public string AnimalSpeciesCodeUnwrapped => UnwrapCoalesced(ANIMAL_SPECIES_CODE);
    public string AnimalPurposeCodeUnwrapped => UnwrapCoalesced(ANIMAL_PURPOSE_CODE);
}