using KeeperData.Core.Documents;

namespace KeeperData.Tests.Common.TestData;

public static class RoleData
{
    private static readonly Dictionary<string, RoleDocument> s_roleByCode =
        new()
        {
            ["LIVESTOCKKEEPER"] = new RoleDocument
            {
                IdentifierId = "b2637b72-2196-4a19-bdf0-85c7ff66cf60",
                Code = "LIVESTOCKKEEPER",
                Name = "Livestock Keeper",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            },

            ["LIVESTOCKOWNER"] = new RoleDocument
            {
                IdentifierId = "2de15dc1-19b9-4372-9e81-a9a2f87fd197",
                Code = "LIVESTOCKOWNER",
                Name = "Livestock Owner",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            },

            ["AGENT"] = new RoleDocument
            {
                IdentifierId = "8184ae3d-c3c4-4904-b1b8-539eeadbf245",
                Code = "AGENT",
                Name = "Agent",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            },

            ["CPHHOLDER"] = new RoleDocument
            {
                IdentifierId = "5053be9f-685a-4779-a663-ce85df6e02e8",
                Code = "CPHHOLDER",
                Name = "CPH Holder",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            }
        };

    private static readonly Dictionary<string, RoleDocument> s_roleById =
        s_roleByCode.Values.ToDictionary(c => c.IdentifierId);

    public static (string? id, string? code, string? name) Find(string code)
    {
        var type = GetByCode(code);
        if (type == null) return (null, null, null);
        return (type.IdentifierId, type.Code, type.Name);
    }

    public static RoleDocument GetById(string id) => s_roleById[id];

    public static RoleDocument GetByCode(string code) => s_roleByCode[code];

    public static IEnumerable<RoleDocument> All => s_roleByCode.Values;
}