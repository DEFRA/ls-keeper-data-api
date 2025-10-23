using CsvToJsonConverter.Logic;
using FluentAssertions;
using System.Text.Json;

namespace CsvToJsonConverter.Tests;

public class ConverterTests
{
    [Fact]
    public void ConvertCsvToJson_WithValidData_CreatesCorrectJson()
    {
        // Arrange
        var converter = new Converter();
        var csvLines = new[]
        {
            "Country Code,Country Identifier,Country Long Name,Country Short Name,Created By,Created Date,Devolved Authority,Effective End Date,Effective Start Date,EU Trade Member,Is Active,Last Modified By,Last Modified Date,Sort Order",
            "GB,,United Kingdom of Great Britain and Northern Ireland,United Kingdom,System,2023-01-01,false,,2023-01-01,false,true,,,10"
        };

        // Act
        var jsonString = converter.ConvertCsvToJson(csvLines);
        var result = JsonSerializer.Deserialize<List<CountryJson>>(jsonString);

        // Assert
        result.Should().HaveCount(1);
        var country = result.First();

        country.Code.Should().Be("GB");
        country.Name.Should().Be("United Kingdom");
        country.LongName.Should().Be("United Kingdom of Great Britain and Northern Ireland");
        country.SortOrder.Should().Be(10);
        Guid.TryParse(country.Id, out _).Should().BeTrue();
        country.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}