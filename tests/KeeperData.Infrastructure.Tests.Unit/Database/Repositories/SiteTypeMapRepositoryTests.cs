using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Repositories;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class SiteTypeMapRepositoryTests
{
    private readonly ReferenceRepositoryTestFixture<SiteTypeMapRepository, SiteTypeMapListDocument, SiteTypeMapDocument> _fixture;
    private readonly SiteTypeMapRepository _sut;

    public SiteTypeMapRepositoryTests()
    {
        _fixture = new ReferenceRepositoryTestFixture<SiteTypeMapRepository, SiteTypeMapListDocument, SiteTypeMapDocument>();
        _sut = _fixture.CreateSut((config, client, unitOfWork) => new SiteTypeMapRepository(config, client, unitOfWork));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidId_ReturnsMatchingSiteTypeMap()
    {
        var marketId = "6b4ca299-895d-4cdb-95dd-670de71ff328";
        var slaughterhouseId = "cb2fb3ee-6368-4125-a413-fc905fec51f0";
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = marketId,
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "MKT",
                    Name = "Market"
                },
                Activities =
                [
                    new SiteTypeMapActivityInfo
                    {
                        Code = "TRAD",
                        Name = "Trading"
                    }
                ]
            },
            new()
            {
                IdentifierId = slaughterhouseId,
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "SLTR",
                    Name = "Slaughterhouse"
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

        var listDocument = new SiteTypeMapListDocument
        {
            Id = "all-sitetypemaps",
            SiteTypeMaps = siteTypeMaps
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync(marketId);

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(marketId);
        result.Type.Code.Should().Be("MKT");
        result.Type.Name.Should().Be("Market");
        result.Activities.Should().HaveCount(1);
        result.Activities[0].Code.Should().Be("TRAD");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdDifferentCase_ReturnsMatchingSiteTypeMap()
    {
        var marketId = "6b4ca299-895d-4cdb-95dd-670de71ff328";
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = marketId,
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "MKT",
                    Name = "Market"
                },
                Activities = []
            }
        };

        var listDocument = new SiteTypeMapListDocument
        {
            Id = "all-sitetypemaps",
            SiteTypeMaps = siteTypeMaps
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync("6B4CA299-895D-4CDB-95DD-670DE71FF328");

        result.Should().NotBeNull();
        result!.Type.Code.Should().Be("MKT");
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdNotFound_ReturnsNull()
    {
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "6b4ca299-895d-4cdb-95dd-670de71ff328",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "MKT",
                    Name = "Market"
                },
                Activities = []
            }
        };

        var listDocument = new SiteTypeMapListDocument
        {
            Id = "all-sitetypemaps",
            SiteTypeMaps = siteTypeMaps
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync("non-existent-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsNull_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsWhitespace_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync("   ");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsEmptyString_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(string.Empty);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByTypeCodeAsync_WhenCalledWithMatchingCode_ReturnsSiteTypeMap()
    {
        var marketId = "6b4ca299-895d-4cdb-95dd-670de71ff328";
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = marketId,
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "MKT",
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
            }
        };

        var listDocument = new SiteTypeMapListDocument
        {
            Id = "all-sitetypemaps",
            SiteTypeMaps = siteTypeMaps
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindByTypeCodeAsync("MKT");

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(marketId);
        result.Type.Code.Should().Be("MKT");
        result.Type.Name.Should().Be("Market");
        result.Activities.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindByTypeCodeAsync_WhenCalledWithDifferentCase_ReturnsSiteTypeMap()
    {
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "cb2fb3ee-6368-4125-a413-fc905fec51f0",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "SLTR",
                    Name = "Slaughterhouse"
                },
                Activities = []
            }
        };

        var listDocument = new SiteTypeMapListDocument
        {
            Id = "all-sitetypemaps",
            SiteTypeMaps = siteTypeMaps
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindByTypeCodeAsync("sltr");

        result.Should().NotBeNull();
        result!.Type.Code.Should().Be("SLTR");
        result.Type.Name.Should().Be("Slaughterhouse");
    }

    [Fact]
    public async Task FindByTypeCodeAsync_WhenNoMatch_ReturnsNull()
    {
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "6b4ca299-895d-4cdb-95dd-670de71ff328",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "MKT",
                    Name = "Market"
                },
                Activities = []
            }
        };

        var listDocument = new SiteTypeMapListDocument
        {
            Id = "all-sitetypemaps",
            SiteTypeMaps = siteTypeMaps
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindByTypeCodeAsync("NONEXISTENT");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByTypeCodeAsync_WhenTypeCodeIsNull_ReturnsNull()
    {
        var result = await _sut.FindByTypeCodeAsync(null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByTypeCodeAsync_WhenTypeCodeIsWhitespace_ReturnsNull()
    {
        var result = await _sut.FindByTypeCodeAsync("   ");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByTypeCodeAsync_WhenTypeCodeIsEmptyString_ReturnsNull()
    {
        var result = await _sut.FindByTypeCodeAsync(string.Empty);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByTypeCodeAsync_WhenMultipleSiteTypeMapsExist_ReturnsCorrectOne()
    {
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "MKT",
                    Name = "Market"
                },
                Activities = []
            },
            new()
            {
                IdentifierId = "id2",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "SLTR",
                    Name = "Slaughterhouse"
                },
                Activities = []
            },
            new()
            {
                IdentifierId = "id3",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "FARM",
                    Name = "Farm"
                },
                Activities = []
            }
        };

        var listDocument = new SiteTypeMapListDocument
        {
            Id = "all-sitetypemaps",
            SiteTypeMaps = siteTypeMaps
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindByTypeCodeAsync("SLTR");

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be("id2");
        result.Type.Code.Should().Be("SLTR");
    }

    [Fact]
    public async Task FindByTypeCodeAsync_WhenSiteTypeMapHasNoActivities_ReturnsEmptyActivitiesList()
    {
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "FARM",
                    Name = "Farm"
                },
                Activities = []
            }
        };

        var listDocument = new SiteTypeMapListDocument
        {
            Id = "all-sitetypemaps",
            SiteTypeMaps = siteTypeMaps
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindByTypeCodeAsync("FARM");

        result.Should().NotBeNull();
        result!.Activities.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryIsEmpty_ReturnsNull()
    {
        _fixture.SetUpNoDocuments();

        var result = await _sut.GetByIdAsync("any-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByTypeCodeAsync_WhenRepositoryIsEmpty_ReturnsNull()
    {
        _fixture.SetUpNoDocuments();

        var result = await _sut.FindByTypeCodeAsync("MKT");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_PassesThroughCorrectly()
    {
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "MKT",
                    Name = "Market"
                },
                Activities = []
            }
        };

        var listDocument = new SiteTypeMapListDocument
        {
            Id = "all-sitetypemaps",
            SiteTypeMaps = siteTypeMaps
        };

        _fixture.SetUpDocuments(listDocument);

        using var cts = new CancellationTokenSource();
        var result = await _sut.GetByIdAsync("id1", cts.Token);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task FindByTypeCodeAsync_WithCancellationToken_PassesThroughCorrectly()
    {
        var siteTypeMaps = new List<SiteTypeMapDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Type = new SiteTypeMapTypeInfo
                {
                    Code = "MKT",
                    Name = "Market"
                },
                Activities = []
            }
        };

        var listDocument = new SiteTypeMapListDocument
        {
            Id = "all-sitetypemaps",
            SiteTypeMaps = siteTypeMaps
        };

        _fixture.SetUpDocuments(listDocument);

        using var cts = new CancellationTokenSource();
        var result = await _sut.FindByTypeCodeAsync("MKT", cts.Token);

        result.Should().NotBeNull();
    }
}