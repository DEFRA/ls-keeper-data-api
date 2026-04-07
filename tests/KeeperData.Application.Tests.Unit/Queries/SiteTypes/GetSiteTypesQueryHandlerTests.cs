using FluentAssertions;
using KeeperData.Application.Queries.SiteTypes;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Services;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.SiteTypes;

public class GetSiteTypesQueryHandlerTests
{
    private readonly Mock<IReferenceDataCache> _cacheMock;
    private readonly GetSiteTypesQueryHandler _sut;
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    public GetSiteTypesQueryHandlerTests()
    {
        _cacheMock = new Mock<IReferenceDataCache>();
        _sut = new GetSiteTypesQueryHandler(_cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCacheHasSiteTypeMaps_ReturnsMappedDTOs()
    {
        // Arrange
        var query = new GetSiteTypesQuery();
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "market-id",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "MA",
                    Name = "Market"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "TRAD",
                        Name = "Trading"
                    },
                    new SiteTypeMapActivityInfo
                    {
                        Code = "SALE",
                        Name = "Sales"
                    }
                ]
            },
            new()
            {
                IdentifierId = "slaughterhouse-id",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "SR",
                    Name = "Slaughter House Red Meat"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "PROC",
                        Name = "Processing"
                    }
                ]
            }
        };

        _cacheMock.Setup(c => c.SiteTypeMaps).Returns(siteTypeMaps);

        // Act
        var result = await _sut.Handle(query, _cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var marketDto = result.First(r => r.Id == "market-id");
        marketDto.Type.Code.Should().Be("MA");
        marketDto.Type.Name.Should().Be("Market");
        marketDto.Activities.Should().HaveCount(2);
        marketDto.Activities.Should().Contain(a => a.Code == "TRAD" && a.Name == "Trading");
        marketDto.Activities.Should().Contain(a => a.Code == "SALE" && a.Name == "Sales");

        var slaughterhouseDto = result.First(r => r.Id == "slaughterhouse-id");
        slaughterhouseDto.Type.Code.Should().Be("SR");
        slaughterhouseDto.Type.Name.Should().Be("Slaughter House Red Meat");
        slaughterhouseDto.Activities.Should().HaveCount(1);
        slaughterhouseDto.Activities[0].Code.Should().Be("PROC");
        slaughterhouseDto.Activities[0].Name.Should().Be("Processing");
    }

    [Fact]
    public async Task Handle_WhenCacheIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetSiteTypesQuery();

        _cacheMock.Setup(c => c.SiteTypeMaps).Returns(new List<SiteTypeMapDocument>());

        // Act
        var result = await _sut.Handle(query, _cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSiteTypeMapHasNoActivities_ReturnsEmptyActivitiesList()
    {
        // Arrange
        var query = new GetSiteTypesQuery();
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "farm-id",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "AH",
                    Name = "Agricultural Holding"
                },
                Activities = []
            }
        };

        _cacheMock.Setup(c => c.SiteTypeMaps).Returns(siteTypeMaps);

        // Act
        var result = await _sut.Handle(query, _cancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("farm-id");
        result[0].Type.Code.Should().Be("AH");
        result[0].Type.Name.Should().Be("Agricultural Holding");
        result[0].Activities.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCacheHasMultipleSiteTypes_ReturnsAllMappedCorrectly()
    {
        // Arrange
        var query = new GetSiteTypesQuery();
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "AC",
                    Name = "Assembly Centre"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "ACT1",
                        Name = "Activity 1"
                    }
                ]
            },
            new()
            {
                IdentifierId = "id2",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "AP",
                    Name = "Airport"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "ACT2",
                        Name = "Activity 2"
                    }
                ]
            },
            new()
            {
                IdentifierId = "id3",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "CA",
                    Name = "Calf Collection Centre"
                },
                Activities = []
            }
        };

        _cacheMock.Setup(c => c.SiteTypeMaps).Returns(siteTypeMaps);

        // Act
        var result = await _sut.Handle(query, _cancellationToken);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(r => r.Id == "id1" && r.Type.Code == "AC");
        result.Should().Contain(r => r.Id == "id2" && r.Type.Code == "AP");
        result.Should().Contain(r => r.Id == "id3" && r.Type.Code == "CA");
    }

    [Fact]
    public async Task Handle_VerifiesReturnTypeIsCorrect()
    {
        // Arrange
        var query = new GetSiteTypesQuery();
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "test-id",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "TEST",
                    Name = "Test Type"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "TEST_ACT",
                        Name = "Test Activity"
                    }
                ]
            }
        };

        _cacheMock.Setup(c => c.SiteTypeMaps).Returns(siteTypeMaps);

        // Act
        var result = await _sut.Handle(query, _cancellationToken);

        // Assert
        result.Should().BeOfType<List<SiteTypeDTO>>();
        result[0].Should().BeOfType<SiteTypeDTO>();
        result[0].Type.Should().BeOfType<SiteTypeInfoDTO>();
        result[0].Activities.Should().AllBeOfType<SiteActivityInfoDTO>();
    }

    [Fact]
    public async Task Handle_WhenSiteTypeMapHasManyActivities_MapsAllCorrectly()
    {
        // Arrange
        var query = new GetSiteTypesQuery();
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "complex-id",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "MA",
                    Name = "Market"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "ACT1",
                        Name = "Activity 1"
                    },
                    new SiteTypeMapActivityInfo
                    {
                        Code = "ACT2",
                        Name = "Activity 2"
                    },
                    new SiteTypeMapActivityInfo
                    {
                        Code = "ACT3",
                        Name = "Activity 3"
                    },
                    new SiteTypeMapActivityInfo
                    {
                        Code = "ACT4",
                        Name = "Activity 4"
                    },
                    new SiteTypeMapActivityInfo
                    {
                        Code = "ACT5",
                        Name = "Activity 5"
                    }
                ]
            }
        };

        _cacheMock.Setup(c => c.SiteTypeMaps).Returns(siteTypeMaps);

        // Act
        var result = await _sut.Handle(query, _cancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result[0].Activities.Should().HaveCount(5);
        result[0].Activities.Should().Contain(a => a.Code == "ACT1");
        result[0].Activities.Should().Contain(a => a.Code == "ACT2");
        result[0].Activities.Should().Contain(a => a.Code == "ACT3");
        result[0].Activities.Should().Contain(a => a.Code == "ACT4");
        result[0].Activities.Should().Contain(a => a.Code == "ACT5");
    }

    [Fact]
    public async Task Handle_ShouldAccessCacheExactlyOnce()
    {
        // Arrange
        var query = new GetSiteTypesQuery();
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "id",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "CODE",
                    Name = "Name"
                },
                Activities = []
            }
        };

        _cacheMock.Setup(c => c.SiteTypeMaps).Returns(siteTypeMaps);

        // Act
        await _sut.Handle(query, _cancellationToken);

        // Assert
        _cacheMock.Verify(c => c.SiteTypeMaps, Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var query = new GetSiteTypesQuery();
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "id",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "TEST",
                    Name = "Test"
                },
                Activities = []
            }
        };

        _cacheMock.Setup(c => c.SiteTypeMaps).Returns(siteTypeMaps);

        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.Handle(query, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_EnsuresAllFieldsAreMappedCorrectly()
    {
        // Arrange
        var query = new GetSiteTypesQuery();
        var expectedId = "precise-id-123";
        var expectedTypeCode = "ZO";
        var expectedTypeName = "Zoo";
        var expectedActivityCode = "EXHIBIT";
        var expectedActivityName = "Exhibition";

        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = expectedId,
                Type = new SiteTypeMapTypeInfo
                {
                    Code = expectedTypeCode,
                    Name = expectedTypeName
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = expectedActivityCode,
                        Name = expectedActivityName
                    }
                ]
            }
        };

        _cacheMock.Setup(c => c.SiteTypeMaps).Returns(siteTypeMaps);

        // Act
        var result = await _sut.Handle(query, _cancellationToken);

        // Assert
        result.Should().HaveCount(1);

        var dto = result[0];
        dto.Id.Should().Be(expectedId);
        dto.Type.Code.Should().Be(expectedTypeCode);
        dto.Type.Name.Should().Be(expectedTypeName);
        dto.Activities.Should().HaveCount(1);
        dto.Activities[0].Code.Should().Be(expectedActivityCode);
        dto.Activities[0].Name.Should().Be(expectedActivityName);
    }
}