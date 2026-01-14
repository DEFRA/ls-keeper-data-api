using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Infrastructure.Config;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Services
{
    public class MongoDbPreproductionServiceTests
    {
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoDbInitialiser> _mockInitialiser;
        private readonly MongoDbPreproductionService _sut;
        private readonly MongoDbPreproductionServiceConfig _config = new MongoDbPreproductionServiceConfig { Enabled = true, PermittedTables = ["parties", "ctsHoldings"] };

        public MongoDbPreproductionServiceTests()
        {
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockInitialiser = new Mock<IMongoDbInitialiser>();
            var mockconfigOptions = new Mock<IOptions<MongoDbPreproductionServiceConfig>>();
            mockconfigOptions.Setup(x => x.Value).Returns(_config);
            var _mockMongoConfig = new Mock<IOptions<MongoConfig>>();
            _mockMongoConfig.Setup(c => c.Value).Returns(new MongoConfig { DatabaseName = "TestDb" });
            var mockClient = new Mock<IMongoClient>();
            mockClient.Setup(x => x.GetDatabase(It.IsAny<string>(), null)).Returns(_mockDatabase.Object);
            _sut = new MongoDbPreproductionService(mockClient.Object, _mockMongoConfig.Object, _mockInitialiser.Object, mockconfigOptions.Object);
        }

        [Theory]
        [InlineData("parties", typeof(PartyDocument))]
        [InlineData("ctsHoldings", typeof(CtsHoldingDocument))]
        public async Task WhenDeletingCollection_ItShouldBeReintialisedWithIndexes(string collectionName, Type documentType)
        {
            await _sut.DropCollection(collectionName);
            _mockDatabase.Verify(db => db.DropCollectionAsync(collectionName, CancellationToken.None));
            _mockInitialiser.Verify(i => i.Initialise(documentType));
        }

        [Fact]
        public async Task WhenDeletingCollectionThatIsNotPermitted_ItShouldFail()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await _sut.DropCollection("referenceData"));
        }

        [Fact]
        public async Task WhenDeletingInvalidCollectionName_ItShouldFail()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await _sut.DropCollection("not-a-collection"));
        }
    }
}