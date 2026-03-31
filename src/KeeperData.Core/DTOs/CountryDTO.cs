using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs
{
    /// <summary>
    /// An ISO country reference record.
    /// </summary>
    public class CountryDTO
    {
        /// <summary>
        /// This is an immutable field which represents the golden key of the reference object.
        /// </summary>
        [JsonPropertyName("id")]
        public required string IdentifierId { get; set; }

        /// <summary>
        /// This the business code/key of the Country object. This is a mutable field, where this field data could possibly change.
        /// </summary>
        /// <example>GB-ENG</example>
        [JsonPropertyName("code")]
        public required string Code { get; set; }

        /// <summary>
        /// This the business name of the Country object. This is a mutable field, where this field data could possibly change.
        /// </summary>
        /// <example>England</example>
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        /// <summary>
        /// The long name of the country.
        /// </summary>
        /// <example>England - United Kingdom</example>
        [JsonPropertyName("longName")]
        public string? LongName { get; set; }

        /// <summary>
        /// Indicates whether the country is an EU trade member.
        /// </summary>
        /// <example>true</example>
        [JsonPropertyName("euTradeMemberFlag")]
        public bool EuTradeMemberFlag { get; set; }

        /// <summary>
        /// Indicates whether the country is a devolved authority.
        /// </summary>
        /// <example>true</example>
        [JsonPropertyName("devolvedAuthorityFlag")]
        public bool DevolvedAuthorityFlag { get; set; }

        /// <summary>
        /// The timestamp of the last time the Country record was updated.
        /// </summary>
        [JsonPropertyName("lastUpdatedDate")]
        public DateTime? LastUpdatedDate { get; set; }
    }
}