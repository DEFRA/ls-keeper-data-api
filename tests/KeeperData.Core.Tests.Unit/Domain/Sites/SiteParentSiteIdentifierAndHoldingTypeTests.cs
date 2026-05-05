using FluentAssertions;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;

namespace KeeperData.Core.Tests.Unit.Domain.Sites;

public class SiteParentSiteIdentifierAndHoldingTypeTests
{
    [Fact]
    public void Site_Create_WithParentSiteIdentifierAndHoldingType_SetsPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var createdDate = DateTime.UtcNow;
        var lastUpdatedDate = DateTime.UtcNow;
        var name = "Test Site";
        var startDate = DateTime.UtcNow.AddYears(-1);
        var parentSiteIdentifier = "12/345/9999";
        var holdingType = "PERMANENT";

        // Act
        var site = Site.Create(
            id,
            createdDate,
            lastUpdatedDate,
            name,
            startDate,
            null,
            "Active",
            "SAM",
            null,
            false,
            parentSiteIdentifier,
            holdingType);

        // Assert
        site.ParentSiteIdentifier.Should().Be(parentSiteIdentifier);
        site.HoldingType.Should().Be(holdingType);
    }

    [Fact]
    public void Site_Create_WithNullParentSiteIdentifierAndHoldingType_SetsPropertiesToNull()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var createdDate = DateTime.UtcNow;
        var lastUpdatedDate = DateTime.UtcNow;
        var name = "Test Site";
        var startDate = DateTime.UtcNow.AddYears(-1);

        // Act
        var site = Site.Create(
            id,
            createdDate,
            lastUpdatedDate,
            name,
            startDate,
            null,
            "Active",
            "SAM",
            null,
            false,
            null,
            null);

        // Assert
        site.ParentSiteIdentifier.Should().BeNull();
        site.HoldingType.Should().BeNull();
    }

    [Fact]
    public void Site_Update_ChangingParentSiteIdentifier_UpdatesProperty()
    {
        // Arrange
        var site = Site.Create(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            "12/345/9999",
            "PERMANENT");

        var newParentSiteIdentifier = "98/765/4321";
        var updateTime = DateTime.UtcNow.AddHours(1);

        // Act
        site.Update(
            updateTime,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            newParentSiteIdentifier,
            "PERMANENT");

        // Assert
        site.ParentSiteIdentifier.Should().Be(newParentSiteIdentifier);
        site.LastUpdatedDate.Should().Be(updateTime);
    }

    [Fact]
    public void Site_Update_ChangingHoldingType_UpdatesProperty()
    {
        // Arrange
        var site = Site.Create(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            "12/345/9999",
            "PERMANENT");

        var newHoldingType = "TEMPORARY";
        var updateTime = DateTime.UtcNow.AddHours(1);

        // Act
        site.Update(
            updateTime,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            "12/345/9999",
            newHoldingType);

        // Assert
        site.HoldingType.Should().Be(newHoldingType);
        site.LastUpdatedDate.Should().Be(updateTime);
    }

    [Fact]
    public void Site_Update_SettingParentSiteIdentifierToNull_ClearsProperty()
    {
        // Arrange
        var site = Site.Create(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            "12/345/9999",
            "PERMANENT");

        var updateTime = DateTime.UtcNow.AddHours(1);

        // Act
        site.Update(
            updateTime,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            null,
            "PERMANENT");

        // Assert
        site.ParentSiteIdentifier.Should().BeNull();
        site.LastUpdatedDate.Should().Be(updateTime);
    }

    [Fact]
    public void Site_Update_WithSameParentSiteIdentifierAndHoldingType_DoesNotUpdateLastUpdatedDate()
    {
        // Arrange
        var initialTime = DateTime.UtcNow;
        var startDate = initialTime.AddYears(-1);
        var site = Site.Create(
            Guid.NewGuid().ToString(),
            initialTime,
            initialTime,
            "Test Site",
             startDate,
            null,
            "Active",
            "SAM",
            null,
            false,
            "12/345/9999",
            "PERMANENT");

        var updateTime = DateTime.UtcNow.AddHours(1);

        // Act
        site.Update(
            updateTime,
            "Test Site",
            startDate,
            null,
            "Active",
            "SAM",
            null,
            false,
            "12/345/9999",
            "PERMANENT");

        // Assert - LastUpdatedDate should NOT change because values are the same
        site.ParentSiteIdentifier.Should().Be("12/345/9999");
        site.HoldingType.Should().Be("PERMANENT");
        site.LastUpdatedDate.Should().Be(initialTime);
    }

    [Fact]
    public void SiteDocument_FromDomain_MapsParentSiteIdentifierAndHoldingType()
    {
        // Arrange
        var site = Site.Create(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            "12/345/9999",
            "PERMANENT");

        // Act
        var document = SiteDocument.FromDomain(site);

        // Assert
        document.ParentSiteIdentifier.Should().Be("12/345/9999");
        document.HoldingType.Should().Be("PERMANENT");
    }

    [Fact]
    public void SiteDocument_FromDomain_WithNullValues_MapsCorrectly()
    {
        // Arrange
        var site = Site.Create(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            null,
            null);

        // Act
        var document = SiteDocument.FromDomain(site);

        // Assert
        document.ParentSiteIdentifier.Should().BeNull();
        document.HoldingType.Should().BeNull();
    }

    [Fact]
    public void SiteDocument_ToDomain_MapsParentSiteIdentifierAndHoldingType()
    {
        // Arrange
        var document = new SiteDocument
        {
            Id = Guid.NewGuid().ToString(),
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Name = "Test Site",
            StartDate = DateTime.UtcNow.AddYears(-1),
            State = "Active",
            Source = "SAM",
            Deleted = false,
            ParentSiteIdentifier = "12/345/9999",
            HoldingType = "PERMANENT"
        };

        // Act
        var site = document.ToDomain();

        // Assert
        site.ParentSiteIdentifier.Should().Be("12/345/9999");
        site.HoldingType.Should().Be("PERMANENT");
    }

    [Fact]
    public void SiteDocument_ToDomain_WithNullValues_MapsCorrectly()
    {
        // Arrange
        var document = new SiteDocument
        {
            Id = Guid.NewGuid().ToString(),
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Name = "Test Site",
            StartDate = DateTime.UtcNow.AddYears(-1),
            State = "Active",
            Source = "SAM",
            Deleted = false,
            ParentSiteIdentifier = null,
            HoldingType = null
        };

        // Act
        var site = document.ToDomain();

        // Assert
        site.ParentSiteIdentifier.Should().BeNull();
        site.HoldingType.Should().BeNull();
    }

    [Fact]
    public void SiteDocument_ToDto_MapsParentSiteIdentifierAndHoldingType()
    {
        // Arrange
        var document = new SiteDocument
        {
            Id = Guid.NewGuid().ToString(),
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Name = "Test Site",
            StartDate = DateTime.UtcNow.AddYears(-1),
            State = "Active",
            Source = "SAM",
            Deleted = false,
            ParentSiteIdentifier = "12/345/9999",
            HoldingType = "PERMANENT"
        };

        // Act
        var dto = document.ToDto();

        // Assert
        dto.ParentSiteIdentifier.Should().Be("12/345/9999");
        dto.HoldingType.Should().Be("PERMANENT");
    }

    [Fact]
    public void SiteDocument_ToDto_WithNullValues_MapsCorrectly()
    {
        // Arrange
        var document = new SiteDocument
        {
            Id = Guid.NewGuid().ToString(),
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Name = "Test Site",
            StartDate = DateTime.UtcNow.AddYears(-1),
            State = "Active",
            Source = "SAM",
            Deleted = false,
            ParentSiteIdentifier = null,
            HoldingType = null
        };

        // Act
        var dto = document.ToDto();

        // Assert
        dto.ParentSiteIdentifier.Should().BeNull();
        dto.HoldingType.Should().BeNull();
    }

    [Fact]
    public void Site_RoundTrip_DomainToDocumentToDomain_PreservesParentSiteIdentifierAndHoldingType()
    {
        // Arrange
        var originalSite = Site.Create(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            "12/345/9999",
            "PERMANENT");

        // Act
        var document = SiteDocument.FromDomain(originalSite);
        var reconstructedSite = document.ToDomain();

        // Assert
        reconstructedSite.ParentSiteIdentifier.Should().Be(originalSite.ParentSiteIdentifier);
        reconstructedSite.HoldingType.Should().Be(originalSite.HoldingType);
    }

    [Theory]
    [InlineData("12/345/6789", "PERMANENT")]
    [InlineData("98/765/4321", "TEMPORARY")]
    [InlineData("11/111/1111", "SEASONAL")]
    [InlineData(null, "PERMANENT")]
    [InlineData("12/345/6789", null)]
    [InlineData(null, null)]
    public void Site_Create_WithVariousParentSiteIdentifierAndHoldingTypeCombinations_SetsCorrectly(
        string? parentSiteIdentifier,
        string? holdingType)
    {
        // Act
        var site = Site.Create(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            parentSiteIdentifier,
            holdingType);

        // Assert
        site.ParentSiteIdentifier.Should().Be(parentSiteIdentifier);
        site.HoldingType.Should().Be(holdingType);
    }

    [Fact]
    public void Site_Update_OnlyParentSiteIdentifierChanges_TriggersUpdateEvent()
    {
        // Arrange
        var site = Site.Create(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            "12/345/9999",
            "PERMANENT");

        site.ClearDomainEvents(); // Clear creation event

        // Act
        site.Update(
            DateTime.UtcNow.AddHours(1),
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            "98/765/4321", // Changed
            "PERMANENT");

        // Assert
        site.DomainEvents.Should().HaveCount(1);
        site.DomainEvents.First().Should().BeOfType<KeeperData.Core.Domain.Sites.DomainEvents.SiteUpdatedDomainEvent>();
    }

    [Fact]
    public void Site_Update_OnlyHoldingTypeChanges_TriggersUpdateEvent()
    {
        // Arrange
        var site = Site.Create(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            "12/345/9999",
            "PERMANENT");

        site.ClearDomainEvents(); // Clear creation event

        // Act
        site.Update(
            DateTime.UtcNow.AddHours(1),
            "Test Site",
            DateTime.UtcNow.AddYears(-1),
            null,
            "Active",
            "SAM",
            null,
            false,
            "12/345/9999",
            "TEMPORARY"); // Changed

        // Assert
        site.DomainEvents.Should().HaveCount(1);
        site.DomainEvents.First().Should().BeOfType<KeeperData.Core.Domain.Sites.DomainEvents.SiteUpdatedDomainEvent>();
    }
}