using FluentAssertions;

namespace TsvToJsonConverter.Tests;

public class MappingTests
{
    [Fact]
    public void MapCountry_WithValidData_CreatesCorrectObject()
    {
        // Arrange
        var tsvLine = "GB\t\tUnited Kingdom of Great Britain and Northern Ireland\tUnited Kingdom\tSystem\t2023-01-01\tfalse\t\t1900-01-01\tfalse\ttrue\tSystem\t2024-01-01\t10";
        var parts = tsvLine.Split('\t');

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
        var tsvLine = "GB\t\tUnited Kingdom\tUK\tSystem\t2023-01-01\tfalse\t\t1900-01-01\tfalse\ttrue\tSystem\t\t10";
        var parts = tsvLine.Split('\t');

        // Act
        var result = Program.MapCountry(parts);

        // Assert
        result.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MapCountry_WithTooFewColumns_ThrowsInvalidDataException()
    {
        // Arrange
        var tsvLine = "GB\tUnited Kingdom"; // Not enough columns
        var parts = tsvLine.Split('\t');

        // Act
        Action act = () => Program.MapCountry(parts);

        // Assert
        act.Should().Throw<InvalidDataException>()
           .WithMessage("TSV line for country has fewer than 14 columns.");
    }

    [Fact]
    public void MapSpecies_WithValidData_CreatesCorrectObject()
    {
        // Arrange
        var tsvLine = "CTT\tNEWID()\tCattle\tSystem\tNEWDATE()\t\tNEWDATE()\tTrue\t10\tSystem\tNEWDATE()";
        var parts = tsvLine.Split('\t');

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
        var tsvLine = "CTT\tCattle";
        var parts = tsvLine.Split('\t');

        // Act
        Action act = () => Program.MapSpecies(parts);

        // Assert
        act.Should().Throw<InvalidDataException>()
           .WithMessage("TSV line for species has fewer than 11 columns.");
    }

    [Fact]
    public void MapRole_WithValidData_CreatesCorrectObject()
    {
        // Arrange
        var tsvLine = "LIVESTOCKKEEPER\tNEWID()\tLivestock Keeper\tSystem\tNEWDATE()\t\tNEWDATE()\tTrue\t\tSystem\tNEWDATE()";
        var parts = tsvLine.Split('\t');

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
        var tsvLine = "AH\tNEWID()\tAgricultural Holding\tSystem\tNEWDATE()\t\tNEWDATE()\tTrue\t\tSystem\tNEWDATE()";
        var parts = tsvLine.Split('\t');

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
        var tsvLine = "AFU\tNEWID()\tApproved Finishing Unit\tSystem\tNEWDATE()\t\tNEWDATE()\tTrue\t150\tSystem\tNEWDATE()";
        var parts = tsvLine.Split('\t');

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
        var tsvLine = "CPHN\tNEWID()\tCPH Number\tSystem\tNEWDATE()\t\tNEWDATE()\tTrue\tSystem\tNEWDATE()";
        var parts = tsvLine.Split('\t');

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
        var tsvLine = "BEEF\tNEWID()\tBeef\tSystem\tNEWDATE()\t\tNEWDATE()\tTrue\t\tSystem\tNEWDATE()";
        var parts = tsvLine.Split('\t');

        // Act
        var result = Program.MapProductionUsage(parts);

        // Assert
        result.Code.Should().Be("BEEF");
        result.Description.Should().Be("Beef");
        result.IsActive.Should().BeTrue();
        result.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void MapFacilityBusinessActivityMap_WithValidData_CreatesCorrectObject()
    {
        // Arrange
        var tsvLine = "AB-EMB-ESEC\tAI\tActivity123";
        var parts = tsvLine.Split('\t');

        // Act
        var result = Program.MapFacilityBusinessActivityMap(parts);

        result.FacilityActivityCode.Should().Be("AB-EMB-ESEC");
        result.AssociatedPremiseTypeCode.Should().Be("AI");
        result.AssociatedPremiseActivityCode.Should().Be("Activity123");

        // Assert
        Guid.TryParse(result.Id, out _).Should().BeTrue();
        result.IsActive.Should().BeTrue();

        result.CreatedBy.Should().Be("System");
        result.LastModifiedBy.Should().Be("System");

        result.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.EffectiveStartDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        result.EffectiveEndDate.Should().BeNull();
    }

    [Fact]
    public void MapFacilityBusinessActivityMap_WithEmptyOptionalColumns_MapsToNull()
    {
        // Arrange
        var tsvLine = "TB-AFU-\t\t   ";
        var parts = tsvLine.Split('\t');

        // Act
        var result = Program.MapFacilityBusinessActivityMap(parts);

        // Assert
        result.FacilityActivityCode.Should().Be("TB-AFU-");
        result.AssociatedPremiseTypeCode.Should().BeNull();
        result.AssociatedPremiseActivityCode.Should().BeNull();
    }

    [Fact]
    public void MapFacilityBusinessActivityMap_WithTooFewColumns_ThrowsInvalidDataException()
    {
        // Arrange
        var tsvLine = "Code\tType";
        var parts = tsvLine.Split('\t');

        // Act
        Action act = () => Program.MapFacilityBusinessActivityMap(parts);

        // Assert
        act.Should().Throw<InvalidDataException>()
           .WithMessage("TSV line has fewer than 3 columns*");
    }
}