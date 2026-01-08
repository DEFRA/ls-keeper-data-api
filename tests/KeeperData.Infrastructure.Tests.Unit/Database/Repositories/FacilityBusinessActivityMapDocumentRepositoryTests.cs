using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Reflection;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class FacilityBusinessActivityMapRepositoryTests
{
    private readonly Mock<IOptions<MongoConfig>> _configMock;
    private readonly Mock<IMongoClient> _mongoClientMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock;
    private readonly Mock<IMongoCollection<FacilityBusinessActivityMapListDocument>> _collectionMock;
    private readonly Mock<IAsyncCursor<FacilityBusinessActivityMapListDocument>> _asyncCursorMock;
    private readonly FacilityBusinessActivityMapRepository _sut;

    public FacilityBusinessActivityMapRepositoryTests()
    {
        _configMock = new Mock<IOptions<MongoConfig>>();
        _mongoClientMock = new Mock<IMongoClient>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clientSessionHandleMock = new Mock<IClientSessionHandle>();
        _collectionMock = new Mock<IMongoCollection<FacilityBusinessActivityMapListDocument>>();
        _asyncCursorMock = new Mock<IAsyncCursor<FacilityBusinessActivityMapListDocument>>();

        _configMock.Setup(c => c.Value).Returns(new MongoConfig { DatabaseName = "test" });

        var mockDatabase = new Mock<IMongoDatabase>();
        _mongoClientMock.Setup(c => c.GetDatabase(It.IsAny<string>(), null)).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.GetCollection<FacilityBusinessActivityMapListDocument>(It.IsAny<string>(), null))
            .Returns(_collectionMock.Object);

        _asyncCursorMock.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(() =>
            {
                _asyncCursorMock.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            });

        _collectionMock.Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle?>(),
                It.IsAny<FilterDefinition<FacilityBusinessActivityMapListDocument>>(),
                It.IsAny<FindOptions<FacilityBusinessActivityMapListDocument, FacilityBusinessActivityMapListDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _unitOfWorkMock.Setup(u => u.Session).Returns(_clientSessionHandleMock.Object);

        _sut = new FacilityBusinessActivityMapRepository(_configMock.Object, _mongoClientMock.Object, _unitOfWorkMock.Object);

        var collectionField = typeof(FacilityBusinessActivityMapRepository).BaseType!.BaseType!
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
        collectionField!.SetValue(_sut, _collectionMock.Object);
    }

    private List<FacilityBusinessActivityMapDocument> TestData = new List<FacilityBusinessActivityMapDocument>
        {
            new()
            {
                IdentifierId = "id1",
                FacilityActivityCode = "AB-EMB-ECT",
                AssociatedPremiseTypeCode = "AI",
                AssociatedPremiseActivityCode = "EMB",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = "id2",
                FacilityActivityCode = "AB-SEM-SCCDOM",
                AssociatedPremiseTypeCode = "AI",
                AssociatedPremiseActivityCode = "SEM",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }};

    [Theory]
    [InlineData("AB-EMB-ECT", "AI", "EMB")]
    [InlineData("AB-SEM-SCCDOM", "AI", "SEM")]
    [InlineData("invalid", null, null)]
    public async Task CanGetDocumentByFacilityActivityCode(string facilityActivityCode, string? associatedPremiseTypeCode, string? associatedPremiseActivityCode)
    {
        var listDocument = new FacilityBusinessActivityMapListDocument
        {
            Id = "all-premisesactivitytypes",
            FacilityBusinessActivityMaps = TestData
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        var result = await _sut.FindByActivityCodeAsync(facilityActivityCode);

        result?.AssociatedPremiseTypeCode.Should().Be(associatedPremiseTypeCode);
        result?.AssociatedPremiseActivityCode.Should().Be(associatedPremiseActivityCode);
    }
}