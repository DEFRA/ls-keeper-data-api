using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class SiteTypeDerivedCodeLookupServiceTests
{
    private readonly Mock<IReferenceDataCache> _mockCache;
    private readonly Mock<ILogger<SiteTypeDerivedCodeLookupService>> _mockLogger;
    private readonly SiteTypeDerivedCodeLookupService _sut;

    public SiteTypeDerivedCodeLookupServiceTests()
    {
        _mockCache = new Mock<IReferenceDataCache>();
        _mockLogger = new Mock<ILogger<SiteTypeDerivedCodeLookupService>>();
        _sut = new SiteTypeDerivedCodeLookupService(_mockCache.Object, _mockLogger.Object);

        SetupMockData();
    }

    private void SetupMockData()
    {
        _mockCache.Setup(c => c.ActivityMaps).Returns(new List<FacilityBusinessActivityMapDocument>
        {
            new()
            {
                IdentifierId  = "1",
                FacilityActivityCode = "AG-MARP-SL",
                AssociatedSiteTypeCode = "AH",
                AssociatedSiteActivityCode = "SLM",
                IsActive = true
            },
            new()
            {
                IdentifierId = "2",
                FacilityActivityCode = "AG-MARP-SLSL",
                AssociatedSiteTypeCode = "AH",
                AssociatedSiteActivityCode = "STM",
                IsActive = true
            },
            new()
            {
                IdentifierId = "3",
                FacilityActivityCode = "TB-AFU",
                AssociatedSiteTypeCode = "AH",
                AssociatedSiteActivityCode = "AFU",
                IsActive = true
            },
            new()
            {
                IdentifierId = "4",
                FacilityActivityCode = "AI-CENTRE",
                AssociatedSiteTypeCode = "AI",
                AssociatedSiteActivityCode = null,
                IsActive = true
            },
            new()
            {
                IdentifierId = "5",
                FacilityActivityCode = "LAF-HAT-NA",
                AssociatedSiteTypeCode = "AH",
                AssociatedSiteActivityCode = "HATCH",
                IsActive = true
            },
            new()
            {
                IdentifierId = "6",
                FacilityActivityCode = "LAF-HAT-NAIT",
                AssociatedSiteTypeCode = "AH",
                AssociatedSiteActivityCode = "HATCH-IT",
                IsActive = true
            }
        });

        _mockCache.Setup(c => c.SiteTypes).Returns(new List<SiteTypeDocument>
        {
            new()
            {
                IdentifierId = "ah-id",
                Code = "AH",
                Name = "Agricultural Holding"
            },
            new()
            {
                IdentifierId = "ai-id",
                Code = "AI",
                Name = "Artificial Insemination Centre"
            }
        });

        _mockCache.Setup(c => c.SiteActivityTypes).Returns(new List<SiteActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "slm-id",
                Code = "SLM",
                Name = "Semen Collection from Livestock Male"
            },
            new()
            {
                IdentifierId = "stm-id",
                Code = "STM",
                Name = "Storage of Embryos/Semen from Livestock"
            },
            new()
            {
                IdentifierId = "afu-id",
                Code = "AFU",
                Name = "Approved Finishing Unit"
            },
            new()
            {
                IdentifierId = "hatch-id",
                Code = "HATCH",
                Name = "Hatchery"
            },
            new()
            {
                IdentifierId = "hatch-it-id",
                Code = "HATCH-IT",
                Name = "Hatchery IT"
            }
        });
    }

    [Fact]
    public void Resolve_WithNullInput_ReturnsDefaultSiteTypeAH()
    {
        // Act
        var result = _sut.Resolve(null);

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.SiteTypeName.Should().Be("Agricultural Holding");
        result.Activities.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WithEmptyString_ReturnsDefaultSiteTypeAH()
    {
        // Act
        var result = _sut.Resolve("");

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.SiteTypeName.Should().Be("Agricultural Holding");
        result.Activities.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WithWhitespace_ReturnsDefaultSiteTypeAH()
    {
        // Act
        var result = _sut.Resolve("   ");

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.SiteTypeName.Should().Be("Agricultural Holding");
        result.Activities.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WithNullInput_WhenSiteTypeNotInCache_ReturnsDefaultCodeWithFallbackName()
    {
        // Arrange - Remove AH from site types
        _mockCache.Setup(c => c.SiteTypes).Returns(new List<SiteTypeDocument>
        {
            new()
            {
                IdentifierId = "ai-id",
                Code = "AI",
                Name = "Artificial Insemination Centre"
            }
        });

        // Act
        var result = _sut.Resolve(null);

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.SiteTypeName.Should().Be("AH"); // Fallback to code when not in cache
        result.Activities.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WithExactMatch_ReturnsSingleActivity()
    {
        // Act
        var result = _sut.Resolve("AG-MARP-SL");

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.SiteTypeName.Should().Be("Agricultural Holding");
        result.Activities.Should().HaveCount(1);
        result.Activities[0].Code.Should().Be("SLM");
        result.Activities[0].Name.Should().Be("Semen Collection from Livestock Male");
    }

    [Fact]
    public void Resolve_WithLongerCodeContainingShorterCode_ReturnsOnlyLongerMatch()
    {
        // Arrange - This is the key test case for the bug
        // "AG-MARP-SLSL" contains "AG-MARP-SL" as substring but should only match "AG-MARP-SLSL"

        // Act
        var result = _sut.Resolve("AG-MARP-SLSL");

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.Activities.Should().HaveCount(1);
        result.Activities[0].Code.Should().Be("STM");
        result.Activities[0].Name.Should().Be("Storage of Embryos/Semen from Livestock");
    }

    [Fact]
    public void Resolve_WithSharedPrefixCodes_ShorterCode_ReturnsOnlyShorterMatch()
    {
        // Arrange - Test the LAF-HAT-NA scenario
        // Both "LAF-HAT-NA" and "LAF-HAT-NAIT" exist in the maps
        // When raw code contains only "LAF-HAT-NA", should only return HATCH

        // Act
        var result = _sut.Resolve("LAF-HAT-NA");

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.Activities.Should().HaveCount(1);
        result.Activities[0].Code.Should().Be("HATCH");
        result.Activities[0].Name.Should().Be("Hatchery");
    }

    [Fact]
    public void Resolve_WithSharedPrefixCodes_LongerCode_ReturnsOnlyLongerMatch()
    {
        // Arrange - Test the LAF-HAT-NAIT scenario
        // Both "LAF-HAT-NA" and "LAF-HAT-NAIT" exist in the maps
        // When raw code contains "LAF-HAT-NAIT", should only return HATCH-IT (not HATCH)

        // Act
        var result = _sut.Resolve("LAF-HAT-NAIT");

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.Activities.Should().HaveCount(1);
        result.Activities[0].Code.Should().Be("HATCH-IT");
        result.Activities[0].Name.Should().Be("Hatchery IT");
        result.Activities.Should().NotContain(a => a.Code == "HATCH");
    }

    [Fact]
    public void Resolve_WithSharedPrefixCodes_BothCodesInString_ReturnsOnlyLongerMatch()
    {
        // Arrange - Both codes present in the raw string
        // Should filter out the shorter one and return only the longer match

        // Act
        var result = _sut.Resolve("LAF-HAT-NA LAF-HAT-NAIT");

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.Activities.Should().HaveCount(1);
        result.Activities[0].Code.Should().Be("HATCH-IT");
        result.Activities.Should().NotContain(a => a.Code == "HATCH");
    }

    [Fact]
    public void Resolve_WithMultipleDistinctCodes_ReturnsAllActivities()
    {
        // Arrange - Simulating comma-separated or space-separated codes
        var rawCode = "AG-MARP-SL,TB-AFU";

        // Act
        var result = _sut.Resolve(rawCode);

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.Activities.Should().HaveCount(2);
        result.Activities.Should().Contain(a => a.Code == "SLM");
        result.Activities.Should().Contain(a => a.Code == "AFU");
    }

    [Fact]
    public void Resolve_WithSpaceSeparatedCodes_ReturnsAllActivities()
    {
        // Arrange
        var rawCode = "AG-MARP-SL TB-AFU";

        // Act
        var result = _sut.Resolve(rawCode);

        // Assert
        result.Should().NotBeNull();
        result.Activities.Should().HaveCount(2);
        result.Activities.Should().Contain(a => a.Code == "SLM");
        result.Activities.Should().Contain(a => a.Code == "AFU");
    }

    [Fact]
    public void Resolve_WithBothShortAndLongCodeInString_FiltersOutPartialMatch()
    {
        // Arrange - String contains both "AG-MARP-SL" and "AG-MARP-SLSL"
        var rawCode = "AG-MARP-SL AG-MARP-SLSL";

        // Act
        var result = _sut.Resolve(rawCode);

        // Assert - Should only return STM (from AG-MARP-SLSL), not SLM (from AG-MARP-SL)
        result.Should().NotBeNull();
        result!.Activities.Should().HaveCount(1);
        result.Activities[0].Code.Should().Be("STM");
        result.Activities.Should().NotContain(a => a.Code == "SLM");
    }

    [Fact]
    public void Resolve_WithNoActivityCode_ReturnsOnlySiteType()
    {
        // Act
        var result = _sut.Resolve("AI-CENTRE");

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AI");
        result.SiteTypeName.Should().Be("Artificial Insemination Centre");
        result.Activities.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WithUnknownCode_ReturnsNull()
    {
        // Act
        var result = _sut.Resolve("UNKNOWN-CODE");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WithMultipleMatchesAndDifferentSiteTypes_LogsWarning()
    {
        // Arrange - Add a conflicting mapping
        var activityMapsWithConflict = _mockCache.Object.ActivityMaps.ToList();
        activityMapsWithConflict.Add(new FacilityBusinessActivityMapDocument
        {
            IdentifierId = "10",
            FacilityActivityCode = "CONFLICT",
            AssociatedSiteTypeCode = "MARKET",
            AssociatedSiteActivityCode = "TRADING",
            IsActive = true
        });
        _mockCache.Setup(c => c.ActivityMaps).Returns(activityMapsWithConflict);

        var rawCode = "AG-MARP-SL CONFLICT";

        // Act
        var result = _sut.Resolve(rawCode);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to resolve facility code")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WithInactiveMapping_ReturnsNull()
    {
        // Arrange - Add inactive mapping
        var activityMapsWithInactive = _mockCache.Object.ActivityMaps.ToList();
        activityMapsWithInactive.Add(new FacilityBusinessActivityMapDocument
        {
            IdentifierId = "10",
            FacilityActivityCode = "INACTIVE-CODE",
            AssociatedSiteTypeCode = "AH",
            AssociatedSiteActivityCode = "INACTIVE-ACT",
            IsActive = false
        });
        _mockCache.Setup(c => c.ActivityMaps).Returns(activityMapsWithInactive);

        // Act
        var result = _sut.Resolve("INACTIVE-CODE");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WithMappingButNoSiteType_ReturnsNull()
    {
        // Arrange
        var activityMapsWithNoSiteType = new List<FacilityBusinessActivityMapDocument>
        {
            new()
            {
                IdentifierId = "10",
                FacilityActivityCode = "NO-SITE-TYPE",
                AssociatedSiteTypeCode = null,
                AssociatedSiteActivityCode = "SOME-ACT",
                IsActive = true
            }
        };
        _mockCache.Setup(c => c.ActivityMaps).Returns(activityMapsWithNoSiteType);

        // Act
        var result = _sut.Resolve("NO-SITE-TYPE");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WithCaseInsensitiveMatch_ReturnsResult()
    {
        // Act
        var result = _sut.Resolve("ag-marp-sl"); // lowercase

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.Activities.Should().HaveCount(1);
        result.Activities[0].Code.Should().Be("SLM");
    }

    [Fact]
    public void Resolve_WithDuplicateActivityCodes_ReturnsDeduplicated()
    {
        // Arrange - Add duplicate mapping with same activity
        var activityMapsWithDuplicate = _mockCache.Object.ActivityMaps.ToList();
        activityMapsWithDuplicate.Add(new FacilityBusinessActivityMapDocument
        {
            IdentifierId = "10",
            FacilityActivityCode = "DUPLICATE-SLM",
            AssociatedSiteTypeCode = "AH",
            AssociatedSiteActivityCode = "SLM",
            IsActive = true
        });
        _mockCache.Setup(c => c.ActivityMaps).Returns(activityMapsWithDuplicate);

        var rawCode = "AG-MARP-SL DUPLICATE-SLM";

        // Act
        var result = _sut.Resolve(rawCode);

        // Assert
        result.Should().NotBeNull();
        result!.SiteTypeCode.Should().Be("AH");
        result.Activities.Should().HaveCount(1); // Deduplicated
        result.Activities[0].Code.Should().Be("SLM");
        result.Activities[0].Name.Should().Be("Semen Collection from Livestock Male");
    }
}