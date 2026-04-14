using FluentAssertions;
using KeeperData.Core.DTOs;
using System.Text.Json;

namespace KeeperData.Core.Tests.Unit.DTOs;

public class GroupMarkDtoTests
{
    [Fact]
    public void GroupMarkDto_ShouldSetAllProperties()
    {
        // Arrange
        var identifierId = Guid.NewGuid().ToString();
        var mark = "H12345";
        var startDate = DateTime.UtcNow.AddYears(-1);
        var endDate = DateTime.UtcNow;
        var lastUpdatedDate = DateTime.UtcNow;
        var species = new List<SpeciesSummaryDto>
        {
            new() { IdentifierId = Guid.NewGuid().ToString(), Code = "CTT", Name = "Cattle" }
        };

        // Act
        var dto = new GroupMarkDto
        {
            IdentifierId = identifierId,
            Mark = mark,
            StartDate = startDate,
            EndDate = endDate,
            LastUpdatedDate = lastUpdatedDate,
            Species = species
        };

        // Assert
        dto.IdentifierId.Should().Be(identifierId);
        dto.Mark.Should().Be(mark);
        dto.StartDate.Should().Be(startDate);
        dto.EndDate.Should().Be(endDate);
        dto.LastUpdatedDate.Should().Be(lastUpdatedDate);
        dto.Species.Should().BeEquivalentTo(species);
    }

    [Fact]
    public void GroupMarkDto_WithNullEndDate_ShouldAllowNull()
    {
        // Arrange & Act
        var dto = new GroupMarkDto
        {
            IdentifierId = "test-id",
            Mark = "H12345",
            StartDate = DateTime.UtcNow,
            EndDate = null,
            Species = [],
            LastUpdatedDate = DateTime.UtcNow
        };

        // Assert
        dto.EndDate.Should().BeNull();
    }

    [Fact]
    public void GroupMarkDto_WithEmptySpeciesList_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var dto = new GroupMarkDto
        {
            IdentifierId = "test-id",
            Mark = "H12345",
            StartDate = DateTime.UtcNow,
            Species = [],
            LastUpdatedDate = DateTime.UtcNow
        };

        // Assert
        dto.Species.Should().NotBeNull();
        dto.Species.Should().BeEmpty();
    }

    [Fact]
    public void GroupMarkDto_JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var dto = new GroupMarkDto
        {
            IdentifierId = "test-id-123",
            Mark = "H99999",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastUpdatedDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Species = []
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"id\":\"test-id-123\"");
        json.Should().Contain("\"mark\":\"H99999\"");
        json.Should().Contain("\"startDate\":\"2023-01-01T00:00:00Z\"");
        json.Should().Contain("\"endDate\":\"2024-01-01T00:00:00Z\"");
        json.Should().Contain("\"lastUpdatedDate\":\"2024-06-01T00:00:00Z\"");
        json.Should().Contain("\"species\":[]");
    }

    [Fact]
    public void GroupMarkDto_JsonDeserialization_ShouldMapCorrectly()
    {
        // Arrange
        var json = """
        {
            "id": "test-id-456",
            "mark": "H88888",
            "startDate": "2022-01-01T00:00:00Z",
            "endDate": null,
            "lastUpdatedDate": "2024-06-01T00:00:00Z",
            "species": [
                {
                    "id": "species-1",
                    "code": "SHP",
                    "name": "Sheep",
                    "lastModifiedDate": "2024-01-01T00:00:00Z"
                }
            ]
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<GroupMarkDto>(json);

        // Assert
        dto.Should().NotBeNull();
        dto!.IdentifierId.Should().Be("test-id-456");
        dto.Mark.Should().Be("H88888");
        dto.EndDate.Should().BeNull();
        dto.Species.Should().HaveCount(1);
        dto.Species[0].Code.Should().Be("SHP");
    }
}