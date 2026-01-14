using FluentAssertions;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class ReferenceDataRepositoryTests
{
    private readonly ReferenceRepositoryTestFixture<TestReferenceRepository, TestReferenceListDocument, TestReferenceDocument> _fixture;
    private readonly TestReferenceRepository _sut;

    public ReferenceDataRepositoryTests()
    {
        _fixture = new ReferenceRepositoryTestFixture<TestReferenceRepository, TestReferenceListDocument, TestReferenceDocument>();
        _sut = _fixture.CreateSut((config, client, unitOfWork) => new TestReferenceRepository(config, client, unitOfWork));
    }

    [Fact]
    public async Task GetAllAsync_LoadsItemsOnFirstCall()
    {
        var items = new List<TestReferenceDocument>
        {
            new() { IdentifierId = "1", Name = "Item 1" },
            new() { IdentifierId = "2", Name = "Item 2" }
        };

        var listDocument = new TestReferenceListDocument
        {
            Id = "test-list",
            TestItems = items
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCachedItemsOnSubsequentCalls()
    {
        var items = new List<TestReferenceDocument>
        {
            new() { IdentifierId = "1", Name = "Item 1" }
        };

        var listDocument = new TestReferenceListDocument
        {
            Id = "test-list",
            TestItems = items
        };

        _fixture.SetUpDocuments(listDocument);

        var findCallCount = 0;
        _fixture._collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TestReferenceListDocument>>(),
                It.IsAny<FindOptions<TestReferenceListDocument, TestReferenceListDocument>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => findCallCount++)
            .ReturnsAsync(_fixture._asyncCursorMock.Object);

        await _sut.GetAllAsync(CancellationToken.None);
        await _sut.GetAllAsync(CancellationToken.None);

        findCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyCollection_WhenNoDocumentFound()
    {
        _fixture._asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.GetAllAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }
}

// Test implementation classes
public class TestReferenceDocument : INestedEntity
{
    public string IdentifierId { get; set; } = default!;
    public string Name { get; set; } = default!;
}

[CollectionName("testReferenceData")]
public class TestReferenceListDocument : IReferenceListDocument<TestReferenceDocument>, IListDocument
{
    public static string DocumentId => "test-list";

    public string Id { get; set; } = DocumentId;
    public int? LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public List<TestReferenceDocument> TestItems { get; set; } = [];
    public IReadOnlyCollection<TestReferenceDocument> Items => TestItems.AsReadOnly();
}

public class TestReferenceRepository : ReferenceDataRepository<TestReferenceListDocument, TestReferenceDocument>
{
    public TestReferenceRepository(
        IOptions<MongoConfig> mongoConfig,
        IMongoClient client,
        IUnitOfWork unitOfWork)
        : base(mongoConfig, client, unitOfWork)
    {
    }
}