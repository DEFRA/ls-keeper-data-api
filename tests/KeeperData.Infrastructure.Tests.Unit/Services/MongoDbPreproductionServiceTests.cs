using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Infrastructure.Config;
using KeeperData.Infrastructure.Services;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Services
{
    public class MongoDbPreproductionServiceTests
    {
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoDbInitialiser> _mockInitialiser;
        private readonly MongoDbPreproductionService _sut;
        private readonly MongoDbPreproductionServiceConfig _config = new MongoDbPreproductionServiceConfig { Enabled = true, PermittedTables = new[] { "parties", "ctsHoldings" } };

        public MongoDbPreproductionServiceTests()
        {
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockInitialiser = new Mock<IMongoDbInitialiser>();
            _sut = new MongoDbPreproductionService(_mockDatabase.Object, _mockInitialiser.Object, _config);
        }

        [Theory]
        [InlineData("parties", typeof(PartyDocument))]
        [InlineData("ctsHoldings", typeof(CtsHoldingDocument))]
        public async Task CanDeleteCollection_AndItShouldBeReintialisedWithIndexes(string collectionName, Type documentType)
        {
            await _sut.WipeCollection(collectionName);
            _mockDatabase.Verify(db => db.DropCollectionAsync(collectionName, CancellationToken.None));
            _mockInitialiser.Verify(i => i.Initialise(documentType));
        }

        [Fact]
        public async Task CannotDeleteCollectionsThatAreNotConfiguredForDeletion()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await _sut.WipeCollection("refCountries"));
        }

        [Fact]
        public async Task FailsWithInvalidCollectionName()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await _sut.WipeCollection("not-a-collection"));
        }
    }
}