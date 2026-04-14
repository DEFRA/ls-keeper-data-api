using FluentAssertions;
using KeeperData.Core.DTOs;
using System.Text.Json;

namespace KeeperData.Core.Tests.Unit.DTOs;

public class ManagedSpeciesDtoTests
{
    [Fact]
    public void ManagedSpeciesDto_ShouldSetAllProperties()
    {
        // Arrange
        var identifierId = Guid.NewGuid().ToString();
        var code = "CTT";
        var name = "Cattle";
        var startDate = DateTime.UtcNow.AddYears(-1);
        var endDate = DateTime.UtcNow;
        var lastUpdatedDate = DateTime.UtcNow;

        // Act
        var dto = new ManagedSpeciesDto
        {
            IdentifierId = identifierId,
            Code = code,
            Name = name,
            StartDate = startDate,
            EndDate = endDate,
            LastUpdatedDate = lastUpdatedDate
        };

        // Assert
        dto.IdentifierId.Should().Be(identifierId);
        dto.Code.Should().Be(code);
        dto.Name.Should().Be(name);
        dto.StartDate.Should().Be(startDate);
        dto.EndDate.Should().Be(endDate);
        dto.LastUpdatedDate.Should().Be(lastUpdatedDate);
    }

    [Fact]
    public void ManagedSpeciesDto_WithNullEndDate_ShouldAllowNull()
    {
        // Arrange & Act
        var dto = new ManagedSpeciesDto
        {
            IdentifierId = "test-id",
            Code = "SHP",
            Name = "Sheep",
            StartDate = DateTime.UtcNow,
            EndDate = null,
            LastUpdatedDate = DateTime.UtcNow
        };

        // Assert
        dto.EndDate.Should().BeNull();
    }

    [Fact]
    public void ManagedSpeciesDto_JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var dto = new ManagedSpeciesDto
        {
            IdentifierId = "species-123",
            Code = "PIG",
            Name = "Pigs",
            StartDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastUpdatedDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"id\":\"species-123\"");
        json.Should().Contain("\"code\":\"PIG\"");
        json.Should().Contain("\"name\":\"Pigs\"");
        json.Should().Contain("\"startDate\":\"2020-01-01T00:00:00Z\"");
        json.Should().Contain("\"endDate\":\"2024-01-01T00:00:00Z\"");
        json.Should().Contain("\"lastUpdatedDate\":\"2024-06-01T00:00:00Z\"");
    }

    [Fact]
    public void ManagedSpeciesDto_JsonDeserialization_ShouldMapCorrectly()
    {
        // Arrange
        var json = """
        {
            "id": "species-456",
            "code": "GTT",
            "name": "Goats",
            "startDate": "2019-06-15T00:00:00Z",
            "endDate": null,
            "lastUpdatedDate": "2024-06-01T00:00:00Z"
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<ManagedSpeciesDto>(json);

        // Assert
        dto.Should().NotBeNull();
        dto!.IdentifierId.Should().Be("species-456");
        dto.Code.Should().Be("GTT");
        dto.Name.Should().Be("Goats");
        dto.StartDate.Year.Should().Be(2019);
        dto.EndDate.Should().BeNull();
    }
}