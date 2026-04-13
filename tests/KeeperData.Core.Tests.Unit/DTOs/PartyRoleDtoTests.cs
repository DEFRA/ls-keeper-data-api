using FluentAssertions;
using KeeperData.Core.DTOs;
using System.Text.Json;

namespace KeeperData.Core.Tests.Unit.DTOs;

public class PartyRoleDtoTests
{
    [Fact]
    public void PartyRoleDto_ShouldSetAllProperties()
    {
        // Arrange
        var identifierId = Guid.NewGuid().ToString();
        var lastUpdatedDate = DateTime.UtcNow;
        var role = new RoleDto
        {
            IdentifierId = Guid.NewGuid().ToString(),
            Code = "OWNER",
            Name = "Livestock Owner"
        };
        var species = new List<ManagedSpeciesDto>
        {
            new()
            {
                IdentifierId = Guid.NewGuid().ToString(),
                Code = "CTT",
                Name = "Cattle",
                StartDate = DateTime.UtcNow
            }
        };

        // Act
        var dto = new PartyRoleDto
        {
            IdentifierId = identifierId,
            Role = role,
            SpeciesManagedByRole = species,
            LastUpdatedDate = lastUpdatedDate
        };

        // Assert
        dto.IdentifierId.Should().Be(identifierId);
        dto.Role.Should().BeEquivalentTo(role);
        dto.SpeciesManagedByRole.Should().BeEquivalentTo(species);
        dto.LastUpdatedDate.Should().Be(lastUpdatedDate);
    }

    [Fact]
    public void PartyRoleDto_WithEmptySpeciesList_ShouldWork()
    {
        // Arrange & Act
        var dto = new PartyRoleDto
        {
            IdentifierId = "test-id",
            Role = new RoleDto
            {
                IdentifierId = "role-1",
                Code = "KEEPER",
                Name = "Livestock Keeper"
            },
            SpeciesManagedByRole = [],
            LastUpdatedDate = DateTime.UtcNow
        };

        // Assert
        dto.SpeciesManagedByRole.Should().NotBeNull();
        dto.SpeciesManagedByRole.Should().BeEmpty();
    }

    [Fact]
    public void PartyRoleDto_JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var dto = new PartyRoleDto
        {
            IdentifierId = "role-123",
            Role = new RoleDto
            {
                IdentifierId = "role-def-1",
                Code = "AGENT",
                Name = "Agent",
                LastUpdatedDate = DateTime.UtcNow
            },
            SpeciesManagedByRole = [],
            LastUpdatedDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"id\":\"role-123\"");
        json.Should().Contain("\"role\":");
        json.Should().Contain("\"speciesManagedByRole\":[]");
        json.Should().Contain("\"lastUpdatedDate\":\"2024-06-01T00:00:00Z\"");
    }

    [Fact]
    public void PartyRoleDto_JsonDeserialization_ShouldMapCorrectly()
    {
        // Arrange
        var json = """
        {
            "id": "role-456",
            "role": {
                "id": "role-def-2",
                "code": "OWNER",
                "name": "Livestock Owner",
                "lastUpdatedDate": "2024-01-01T00:00:00Z"
            },
            "speciesManagedByRole": [
                {
                    "id": "species-1",
                    "code": "SHP",
                    "name": "Sheep",
                    "startDate": "2020-01-01T00:00:00Z",
                    "endDate": null,
                    "lastUpdatedDate": "2024-01-01T00:00:00Z"
                }
            ],
            "lastUpdatedDate": "2024-06-01T00:00:00Z"
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<PartyRoleDto>(json);

        // Assert
        dto.Should().NotBeNull();
        dto!.IdentifierId.Should().Be("role-456");
        dto.Role.Code.Should().Be("OWNER");
        dto.SpeciesManagedByRole.Should().HaveCount(1);
        dto.SpeciesManagedByRole[0].Code.Should().Be("SHP");
    }
}