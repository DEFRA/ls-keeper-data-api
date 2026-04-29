using KeeperData.Core.Documents.Silver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Legacy
{

    public class LegacyFieldDeserializationTests : IDisposable
    {
        private static bool s_mongoInitialized;
        private static readonly object s_lock = new();

        public LegacyFieldDeserializationTests()
        {
            InitializeMongo();
        }

        private static void InitializeMongo()
        {
            if (s_mongoInitialized) return;

            lock (s_lock)
            {
                if (s_mongoInitialized) return;

                // Register serializers (same as production code)
                try
                {
                    BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));
                }
                catch
                {
                    // Already registered
                }

                try
                {
                    var pack = new ConventionPack { new CamelCaseElementNameConvention() };
                    ConventionRegistry.Register("CamelCaseTests", pack, _ => true);
                }
                catch
                {
                    // Already registered
                }

                // Register legacy field support
                if (!BsonClassMap.IsClassMapRegistered(typeof(BaseHoldingDocument)))
                {
                    BsonClassMap.RegisterClassMap<BaseHoldingDocument>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true);
                        cm.SetIgnoreExtraElementsIsInherited(true);
                    });
                }

                if (!BsonClassMap.IsClassMapRegistered(typeof(SamHoldingDocument)))
                {
                    BsonClassMap.RegisterClassMap<SamHoldingDocument>(cm =>
                    {
                        cm.AutoMap();
                    });
                }

                s_mongoInitialized = true;
            }
        }

        [Fact]
        public void Deserialize_WithNewFieldNames_ShouldPopulateProperties()
        {
            // Arrange - Document with new field names
            var bson = new BsonDocument
        {
            { "_id", "test-id-1" },
            { "countyParishHoldingNumber", "12/345/6789" },
            { "siteActivityTypeId", "activity-123" },
            { "siteActivityTypeCode", "AFU" },
            { "siteTypeIdentifier", "type-456" },
            { "siteTypeCode", "AH" },
            { "createdDate", DateTime.UtcNow },
            { "lastUpdatedDate", DateTime.UtcNow }
        };

            // Act
            var result = BsonSerializer.Deserialize<SamHoldingDocument>(bson);

            // Assert
            result.Should().NotBeNull();
            result.CountyParishHoldingNumber.Should().Be("12/345/6789");
            result.SiteActivityTypeId.Should().Be("activity-123");
            result.SiteActivityTypeCode.Should().Be("AFU");
            result.SiteTypeIdentifier.Should().Be("type-456");
            result.SiteTypeCode.Should().Be("AH");
        }

        [Fact]
        public void Deserialize_WithLegacyFieldNames_ShouldIgnoreThemGracefully()
        {
            // Arrange - Document with OLD field names (should be ignored)
            var bson = new BsonDocument
        {
            { "_id", "test-id-2" },
            { "countyParishHoldingNumber", "12/345/6789" },
            { "premiseActivityTypeId", "old-activity-123" },
            { "premiseActivityTypeCode", "OLD-AFU" },
            { "premiseTypeIdentifier", "old-type-456" },
            { "premiseTypeCode", "OLD-AH" },
            { "createdDate", DateTime.UtcNow },
            { "lastUpdatedDate", DateTime.UtcNow }
        };

            // Act
            var result = BsonSerializer.Deserialize<SamHoldingDocument>(bson);

            // Assert - Should not throw, legacy fields should be ignored
            result.Should().NotBeNull();
            result.CountyParishHoldingNumber.Should().Be("12/345/6789");
            result.SiteActivityTypeId.Should().BeNull();
            result.SiteActivityTypeCode.Should().BeNull();
            result.SiteTypeIdentifier.Should().BeNull();
            result.SiteTypeCode.Should().BeNull();
        }

        [Fact]
        public void Deserialize_WithBothLegacyAndNewFieldNames_ShouldPreferNewFieldNames()
        {
            // Arrange - Document with BOTH old and new field names
            var bson = new BsonDocument
        {
            { "_id", "test-id-3" },
            { "countyParishHoldingNumber", "12/345/6789" },
            { "premiseActivityTypeId", "old-activity-123" },
            { "siteActivityTypeId", "new-activity-123" },
            { "premiseActivityTypeCode", "OLD-AFU" },
            { "siteActivityTypeCode", "NEW-AFU" },
            { "premiseTypeIdentifier", "old-type-456" },
            { "siteTypeIdentifier", "new-type-456" },
            { "premiseTypeCode", "OLD-AH" },
            { "siteTypeCode", "NEW-AH" },
            { "createdDate", DateTime.UtcNow },
            { "lastUpdatedDate", DateTime.UtcNow }
        };

            // Act
            var result = BsonSerializer.Deserialize<SamHoldingDocument>(bson);

            // Assert - Should use NEW field values
            result.Should().NotBeNull();
            result.CountyParishHoldingNumber.Should().Be("12/345/6789");
            result.SiteActivityTypeId.Should().Be("new-activity-123");
            result.SiteActivityTypeCode.Should().Be("NEW-AFU");
            result.SiteTypeIdentifier.Should().Be("new-type-456");
            result.SiteTypeCode.Should().Be("NEW-AH");
        }

        [Fact]
        public void Serialize_ShouldUseNewFieldNames()
        {
            // Arrange
            var document = new SamHoldingDocument
            {
                Id = "test-id-4",
                CountyParishHoldingNumber = "12/345/6789",
                SiteActivityTypeId = "activity-123",
                SiteActivityTypeCode = "AFU",
                SiteTypeIdentifier = "type-456",
                SiteTypeCode = "AH",
                CreatedDate = DateTime.UtcNow,
                LastUpdatedDate = DateTime.UtcNow
            };

            // Act
            var bson = document.ToBsonDocument();

            // Assert - Should only contain NEW field names
            bson.Names.Should().Contain("siteActivityTypeId");
            bson.Names.Should().Contain("siteActivityTypeCode");
            bson.Names.Should().Contain("siteTypeIdentifier");
            bson.Names.Should().Contain("siteTypeCode");

            bson.Names.Should().NotContain("premiseActivityTypeId");
            bson.Names.Should().NotContain("premiseActivityTypeCode");
            bson.Names.Should().NotContain("premiseTypeIdentifier");
            bson.Names.Should().NotContain("premiseTypeCode");

            bson["siteActivityTypeId"].AsString.Should().Be("activity-123");
            bson["siteActivityTypeCode"].AsString.Should().Be("AFU");
            bson["siteTypeIdentifier"].AsString.Should().Be("type-456");
            bson["siteTypeCode"].AsString.Should().Be("AH");
        }

        [Fact]
        public void Deserialize_WithExtraUnknownFields_ShouldIgnoreThem()
        {
            // Arrange - Document with completely unknown fields
            var bson = new BsonDocument
        {
            { "_id", "test-id-5" },
            { "countyParishHoldingNumber", "12/345/6789" },
            { "siteActivityTypeId", "activity-123" },
            { "someRandomField", "random-value" },
            { "anotherUnknownField", 12345 },
            { "createdDate", DateTime.UtcNow },
            { "lastUpdatedDate", DateTime.UtcNow }
        };

            // Act
            var result = BsonSerializer.Deserialize<SamHoldingDocument>(bson);

            // Assert - Should not throw
            result.Should().NotBeNull();
            result.CountyParishHoldingNumber.Should().Be("12/345/6789");
            result.SiteActivityTypeId.Should().Be("activity-123");
        }

        [Fact]
        public void RoundTrip_ShouldPreserveData()
        {
            // Arrange
            var original = new SamHoldingDocument
            {
                Id = "test-id-6",
                CountyParishHoldingNumber = "12/345/6789",
                SiteActivityTypeId = "activity-123",
                SiteActivityTypeCode = "AFU",
                SiteTypeIdentifier = "type-456",
                SiteTypeCode = "AH",
                LocationName = "Test Farm",
                CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LastUpdatedDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc)
            };

            // Act - Serialize then deserialize
            var bson = original.ToBsonDocument();
            var result = BsonSerializer.Deserialize<SamHoldingDocument>(bson);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(original.Id);
            result.CountyParishHoldingNumber.Should().Be(original.CountyParishHoldingNumber);
            result.SiteActivityTypeId.Should().Be(original.SiteActivityTypeId);
            result.SiteActivityTypeCode.Should().Be(original.SiteActivityTypeCode);
            result.SiteTypeIdentifier.Should().Be(original.SiteTypeIdentifier);
            result.SiteTypeCode.Should().Be(original.SiteTypeCode);
            result.LocationName.Should().Be(original.LocationName);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}