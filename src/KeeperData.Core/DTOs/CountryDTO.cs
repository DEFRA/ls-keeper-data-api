using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs
{
    public class CountryDTO
    {
        [JsonPropertyName("id")]
        public required string IdentifierId { get; set; }

        [JsonPropertyName("code")]
        public required string Code { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("longName")]
        public string? LongName { get; set; }

        [JsonPropertyName("euTradeMemberFlag")]
        public bool EuTradeMemberFlag { get; set; }

        [JsonPropertyName("devolvedAuthorityFlag")]
        public bool DevolvedAuthorityFlag { get; set; }

        [JsonPropertyName("lastUpdatedDate")]
        public DateTime? LastUpdatedDate { get; set; }
    }
}