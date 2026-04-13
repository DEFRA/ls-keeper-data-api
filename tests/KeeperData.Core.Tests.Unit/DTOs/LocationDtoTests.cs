using FluentAssertions;
using KeeperData.Core.DTOs;
using System.Text.Json;

namespace KeeperData.Core.Tests.Unit.DTOs;

public class LocationDtoTests
{
    [Fact]
    public void LocationDto_ShouldSetAllProperties()
    {
        // Arrange
        var identifierId = Guid.NewGuid().ToString();
        var osMapRef = "ND2150071600";
        var easting = 399568;
        var northing = 579087;
        var lastUpdatedDate = DateTime.UtcNow;
        var address = new AddressDto
        {
            IdentifierId = Guid.NewGuid()
                .ToString(),
            Postcode = "SW1A 1AA",
            AddressLine1 = null
        };
        var communication = new List<CommunicationDto>
        {
            new() { IdentifierId = Guid.NewGuid().ToString(), Email = "test@example.com" }
        };

        // Act
        var dto = new LocationDto
        {
            IdentifierId = identifierId,
            OsMapReference = osMapRef,
            Easting = easting,
            Northing = northing,
            Address = address,
            Communication = communication,
            LastUpdatedDate = lastUpdatedDate
        };

        // Assert
        dto.IdentifierId.Should().Be(identifierId);
        dto.OsMapReference.Should().Be(osMapRef);
        dto.Easting.Should().Be(easting);
        dto.Northing.Should().Be(northing);
        dto.Address.Should().BeEquivalentTo(address);
        dto.Communication.Should().BeEquivalentTo(communication);
        dto.LastUpdatedDate.Should().Be(lastUpdatedDate);
    }

    [Fact]
    public void LocationDto_WithNullableProperties_ShouldAllowNulls()
    {
        // Arrange & Act
        var dto = new LocationDto
        {
            IdentifierId = "test-id",
            OsMapReference = null,
            Easting = null,
            Northing = null,
            Address = null,
            Communication = [],
            LastUpdatedDate = DateTime.UtcNow
        };

        // Assert
        dto.OsMapReference.Should().BeNull();
        dto.Easting.Should().BeNull();
        dto.Northing.Should().BeNull();
        dto.Address.Should().BeNull();
    }

    [Fact]
    public void LocationDto_JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var dto = new LocationDto
        {
            IdentifierId = "loc-123",
            OsMapReference = "SK123456",
            Easting = 123456,
            Northing = 654321,
            Address = null,
            Communication = [],
            LastUpdatedDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"id\":\"loc-123\"");
        json.Should().Contain("\"osMapReference\":\"SK123456\"");
        json.Should().Contain("\"easting\":123456");
        json.Should().Contain("\"northing\":654321");
        json.Should().Contain("\"lastUpdatedDate\":\"2024-06-01T00:00:00Z\"");
    }

    [Fact]
    public void LocationDto_JsonDeserialization_ShouldMapCorrectly()
    {
        // Arrange
        var json = """
        {
            "id": "loc-456",
            "osMapReference": "NT987654",
            "easting": 987654,
            "northing": 456789,
            "address": {
                "id": "addr-1",
                "addressLine1": "addressline1",
                "postcode": "EH1 1AA"
            },
            "communication": [],
            "lastUpdatedDate": "2024-06-01T00:00:00Z"
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<LocationDto>(json);

        // Assert
        dto.Should().NotBeNull();
        dto!.IdentifierId.Should().Be("loc-456");
        dto.OsMapReference.Should().Be("NT987654");
        dto.Easting.Should().Be(987654);
        dto.Northing.Should().Be(456789);
        dto.Address.Should().NotBeNull();
        dto.Address!.Postcode.Should().Be("EH1 1AA");
    }
}