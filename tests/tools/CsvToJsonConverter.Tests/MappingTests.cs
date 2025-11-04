using FluentAssertions;

namespace CsvToJsonConverter.Tests;

public class MappingTests
{
    [Fact]
    public void MapCountry_WithValidData_CreatesCorrectObject()
    {
        // Arrange
        var csvLine = "GB,,United Kingdom of Great Britain and Northern Ireland,United Kingdom,System,2023-01-01,false,,1900-01-01,false,true,System,2024-01-01,10";
        var parts = csvLine.Split(',');

        // Act
        var result = Program.MapCountry(parts);

        // Assert
        result.Code.Should().Be("GB");
        result.Name.Should().Be("United Kingdom");
        result.LongName.Should().Be("United Kingdom of Great Britain and Northern Ireland");
        result.IsActive.Should().BeTrue();
        result.SortOrder.Should().Be(10);
        result.CreatedBy.Should().Be("System");
        result.CreatedDate.Should().Be(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result.EffectiveStartDate.Should().Be(new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Guid.TryParse(result.Id, out _).Should().BeTrue();

        result.LastModifiedDate.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void MapCountry_WithEmptyLastModifiedDate_UsesCurrentTime()
    {
        // Arrange
        var csvLine = "GB,,United Kingdom,UK,System,2023-01-01,false,,1900-01-01,false,true,System,,10";
        var parts = csvLine.Split(',');

        // Act
        var result = Program.MapCountry(parts);

        // Assert
        result.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MapCountry_WithTooFewColumns_ThrowsInvalidDataException()
    {
        // Arrange
        var csvLine = "GB,United Kingdom"; // Not enough columns
        var parts = csvLine.Split(',');

        // Act
        Action act = () => Program.MapCountry(parts);

        // Assert
        act.Should().Throw<InvalidDataException>()
           .WithMessage("CSV line for country has fewer than 14 columns.");
    }

    [Fact]
    public void MapSpecies_WithValidData_CreatesCorrectObject()
    {
        // Arrange
        var csvLine = "CTT,NEWID(),Cattle,System,NEWDATE(),,NEWDATE(),True,10,System,NEWDATE()";
        var parts = csvLine.Split(',');

        // Act
        var result = Program.MapSpecies(parts);

        // Assert
        result.Code.Should().Be("CTT");
        result.Name.Should().Be("Cattle");
        result.IsActive.Should().BeTrue();
        result.SortOrder.Should().Be(10);
        result.CreatedBy.Should().Be("System");

        Guid.TryParse(result.Id, out _).Should().BeTrue();
        result.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.EffectiveStartDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MapSpecies_WithTooFewColumns_ThrowsInvalidDataException()
    {
        // Arrange
        var csvLine = "CTT,Cattle";
        var parts = csvLine.Split(',');

        // Act
        Action act = () => Program.MapSpecies(parts);

        // Assert
        act.Should().Throw<InvalidDataException>()
           .WithMessage("CSV line for species has fewer than 11 columns.");
    }

    [Fact]
    public void MapRole_WithValidData_CreatesCorrectObject()
    {
        // Arrange
        var csvLine = "LIVESTOCKKEEPER,NEWID(),Livestock Keeper,System,NEWDATE(),,NEWDATE(),True,,System,NEWDATE()";
        var parts = csvLine.Split(',');

        // Act
        var result = Program.MapRole(parts);

        // Assert
        result.Code.Should().Be("LIVESTOCKKEEPER");
        result.Name.Should().Be("Livestock Keeper");
        result.IsActive.Should().BeTrue();
        result.SortOrder.Should().Be(0);
        result.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void MapPremisesType_WithValidData_CreatesCorrectObject()
    {
        // Arrange
        var csvLine = "AH,NEWID(),Agricultural Holding,System,NEWDATE(),,NEWDATE(),True,,System,NEWDATE()";
        var parts = csvLine.Split(',');

        // Act
        var result = Program.MapPremisesType(parts);

        // Assert
        result.Code.Should().Be("AH");
        result.Name.Should().Be("Agricultural Holding");
        result.IsActive.Should().BeTrue();
        result.SortOrder.Should().Be(0);
        result.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void MapPremisesActivityType_WithValidData_CreatesCorrectObject()
    {
        // Arrange
        var csvLine = "AFU,NEWID(),Approved Finishing Unit,System,NEWDATE(),,NEWDATE(),True,150,System,NEWDATE()";
        var parts = csvLine.Split(',');

        // Act
        var result = Program.MapPremisesActivityType(parts);

        // Assert
        result.Code.Should().Be("AFU");
        result.Name.Should().Be("Approved Finishing Unit");
        result.IsActive.Should().BeTrue();
        result.PriorityOrder.Should().Be(150);
        result.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void MapSiteIdentifierType_WithValidData_CreatesCorrectObject()
    {
        // Arrange
        var csvLine = "CPHN,NEWID(),CPH Number,System,NEWDATE(),,NEWDATE(),True,System,NEWDATE()";
        var parts = csvLine.Split(',');

        // Act
        var result = Program.MapSiteIdentifierType(parts);

        // Assert
        result.Code.Should().Be("CPHN");
        result.Name.Should().Be("CPH Number");
        result.IsActive.Should().BeTrue();
        result.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void MapProductionUsage_WithValidData_CreatesCorrectObject()
    {
        // Arrange
        var csvLine = "BEEF,NEWID(),Beef,System,NEWDATE(),,NEWDATE(),True,,System,NEWDATE()";
        var parts = csvLine.Split(',');

        // Act
        var result = Program.MapProductionUsage(parts);

        // Assert
        result.Code.Should().Be("BEEF");
        result.Description.Should().Be("Beef");
        result.IsActive.Should().BeTrue();
        result.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }
}