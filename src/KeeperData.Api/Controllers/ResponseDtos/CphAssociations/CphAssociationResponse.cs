using System.Text.Json.Serialization;

namespace KeeperData.Api.Controllers.ResponseDtos.CphAssociations;

public class CphAssociationResponse
{
    [JsonPropertyName("cph")]
    public required string Cph { get; set; }

    [JsonPropertyName("role")]
    public required string Role { get; set; }
}
