using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Tests.Common.TestData.Cts;

public static class CtsTestScenarios
{
    /// <summary>
    /// Default CTS holding import scenario with Agent and Keeper
    /// </summary>
    public static CtsTestScenarioData DefaultScenario => new()
    {
        Cph = "AH-123456-01",
        RawHoldings = [
            new CtsCphHolding
            {
                LID_FULL_IDENTIFIER = "AH-123456-01",
                LTY_LOC_TYPE = "Farm",
                ADR_NAME = "Test Farm CTS",
                ADR_ADDRESS_2 = "123 Farm Road",
                ADR_ADDRESS_3 = "Rural Area",
                ADR_ADDRESS_4 = "Farmville",
                ADR_ADDRESS_5 = "Countryside",
                ADR_POST_CODE = "AB12 3CD",
                LOC_EFFECTIVE_FROM = DateTime.Parse("2024-01-15T10:30:00Z"),
                CHANGE_TYPE = "I",
                BATCH_ID = 1001,
                UpdatedAtUtc = DateTime.Parse("2024-01-15T10:30:00Z")
            }
        ],
        RawAgents = [
            new CtsAgentOrKeeper
            {
                PAR_ID = "P12345",
                LID_FULL_IDENTIFIER = "AH-123456-01",
                PAR_SURNAME = "Keaves",
                ADR_NAME = "Reanu Keaves",
                ADR_ADDRESS_2 = "456 Agent Street",
                ADR_ADDRESS_4 = "Agentville",
                ADR_ADDRESS_5 = "Agentshire",
                ADR_POST_CODE = "EF34 5GH",
                CHANGE_TYPE = "I",
                BATCH_ID = 1001,
                UpdatedAtUtc = DateTime.Parse("2024-01-15T10:30:00Z")
            }
        ],
        RawKeepers = [
            new CtsAgentOrKeeper
            {
                PAR_ID = "P67890",
                LID_FULL_IDENTIFIER = "AH-123456-01",
                PAR_SURNAME = "Chonk",
                ADR_NAME = "Donut Anne Chonk",
                ADR_ADDRESS_2 = "789 Keeper Lane",
                ADR_ADDRESS_3 = "Farm District",
                ADR_ADDRESS_4 = "Keeperville",
                ADR_ADDRESS_5 = "Keepshire",
                ADR_POST_CODE = "IJ56 7KL",
                CHANGE_TYPE = "I",
                BATCH_ID = 1001,
                UpdatedAtUtc = DateTime.Parse("2024-01-15T10:30:00Z")
            }
        ]
    };

    /// <summary>
    /// Scenario for updating existing CTS holding with new agent information
    /// </summary>
    public static CtsTestScenarioData Scenario_UpdatedAgentAndKeepers() => new()
    {
        Cph = "AH-123456-01",
        RawHoldings = [
            new CtsCphHolding
            {
                LID_FULL_IDENTIFIER = "AH-123456-01",
                LTY_LOC_TYPE = "Farm",
                ADR_NAME = "Test Farm CTS - Updated",
                ADR_ADDRESS_2 = "123 Farm Road",
                ADR_ADDRESS_3 = "Rural Area",
                ADR_ADDRESS_4 = "Farmville",
                ADR_ADDRESS_5 = "Countryside",
                ADR_POST_CODE = "AB12 3CD",
                LOC_EFFECTIVE_FROM = DateTime.Parse("2024-02-15T11:00:00Z"),
                CHANGE_TYPE = "U",
                BATCH_ID = 1002,
                UpdatedAtUtc = DateTime.Parse("2024-02-15T11:00:00Z")
            }
        ],
        RawAgents = [
            new CtsAgentOrKeeper
            {
                PAR_ID = "P54321", // New agent
                LID_FULL_IDENTIFIER = "AH-123456-01",
                PAR_SURNAME = "Hyrule",
                ADR_NAME = "Zelda Hyrule",
                ADR_ADDRESS_2 = "789 New Agent Road",
                ADR_ADDRESS_3 = "Business District",
                ADR_ADDRESS_4 = "New Agentville",
                ADR_ADDRESS_5 = "Newshire",
                ADR_POST_CODE = "XY12 3ZA",
                CHANGE_TYPE = "I",
                BATCH_ID = 1002,
                UpdatedAtUtc = DateTime.Parse("2024-02-15T11:00:00Z")
            }
        ],
        RawKeepers = [
            new CtsAgentOrKeeper
            {
                PAR_ID = "P67890", // Same keeper, updated info
                LID_FULL_IDENTIFIER = "AH-123456-01",
                PAR_SURNAME = "Cramwell", // Name change
                ADR_NAME = "Loafus Cramwell",
                ADR_ADDRESS_2 = "789 Keeper Lane",
                ADR_ADDRESS_3 = "Farm District",
                ADR_ADDRESS_4 = "Keeperville",
                ADR_ADDRESS_5 = "Keepshire",
                ADR_POST_CODE = "IJ56 7KL",
                CHANGE_TYPE = "U",
                BATCH_ID = 1002,
                UpdatedAtUtc = DateTime.Parse("2024-02-15T11:00:00Z")
            }
        ]
    };
}

public record CtsTestScenarioData
{
    public string Cph { get; init; } = string.Empty;
    public List<CtsCphHolding> RawHoldings { get; init; } = [];
    public List<CtsAgentOrKeeper> RawAgents { get; init; } = [];
    public List<CtsAgentOrKeeper> RawKeepers { get; init; } = [];
}