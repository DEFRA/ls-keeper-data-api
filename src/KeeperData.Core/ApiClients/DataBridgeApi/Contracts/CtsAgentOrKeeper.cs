namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class CtsAgentOrKeeper : BronzeBase
{
    public long PAR_ID { get; set; } = default;
    public string LID_FULL_IDENTIFIER { get; set; } = string.Empty;

    public string? PAR_TITLE { get; set; }
    public string? PAR_INITIALS { get; set; }
    public string? PAR_SURNAME { get; set; }
    public string? PAR_TEL_NUMBER { get; set; }
    public string? PAR_MOBILE_NUMBER { get; set; }
    public string? PAR_EMAIL_ADDRESS { get; set; }

    public string? LOC_TEL_NUMBER { get; set; }
    public string? LOC_MOBILE_NUMBER { get; set; }

    public string ADR_NAME { get; set; } = string.Empty;
    public string? ADR_ADDRESS_2 { get; set; }
    public string? ADR_ADDRESS_3 { get; set; }
    public string? ADR_ADDRESS_4 { get; set; }
    public string? ADR_ADDRESS_5 { get; set; }
    public string? ADR_POST_CODE { get; set; }

    public DateTime LPR_EFFECTIVE_FROM_DATE { get; set; } = default;
    public DateTime? LPR_EFFECTIVE_TO_DATE { get; set; }
}