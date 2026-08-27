using FluentAssertions;
using KeeperData.Core.Documents;

namespace KeeperData.Core.Tests.Unit.Documents;

public class UserAccountDocumentTests
{
    [Fact]
    public void WhenConstructed_DefaultsShouldBeCorrect()
    {
        var sut = new UserAccountDocument
        {
            Id = "user-id",
            Email = "jane.farmer@example.com"
        };

        sut.Subject.Should().BeNull();
        sut.FirstName.Should().BeNull();
        sut.LastName.Should().BeNull();
        sut.DisplayName.Should().BeNull();
        sut.CphAssociations.Should().BeEmpty();
        sut.AssociationsRefreshedDate.Should().BeNull();
        sut.CreatedDate.Should().Be(default);
        sut.LastUpdatedDate.Should().Be(default);
        sut.Deleted.Should().BeFalse();
    }

    [Fact]
    public void WhenPropertiesAreSet_TheyShouldBeAssignedCorrectly()
    {
        var createdDate = new DateTime(2020, 1, 1);
        var lastUpdatedDate = new DateTime(2021, 1, 1);
        var associationsRefreshedDate = new DateTime(2022, 1, 1);

        var association = new CphAssociationDocument
        {
            IdentifierId = "assoc-id",
            CphNumber = "57/103/2335",
            Role = "owner",
            PartyId = "party-id",
            HoldingId = "holding-id",
            HoldingName = "Test Holding"
        };

        var sut = new UserAccountDocument
        {
            Id = "user-id",
            Subject = "9f3a1c2e-0b6d-4f4e-9d2a-7c8b1e5f0a3d",
            Email = "jane.farmer@example.com",
            FirstName = "Jane",
            LastName = "Farmer",
            DisplayName = "Jane Farmer",
            CphAssociations = [association],
            AssociationsRefreshedDate = associationsRefreshedDate,
            CreatedDate = createdDate,
            LastUpdatedDate = lastUpdatedDate,
            Deleted = true
        };

        sut.Id.Should().Be("user-id");
        sut.Subject.Should().Be("9f3a1c2e-0b6d-4f4e-9d2a-7c8b1e5f0a3d");
        sut.Email.Should().Be("jane.farmer@example.com");
        sut.FirstName.Should().Be("Jane");
        sut.LastName.Should().Be("Farmer");
        sut.DisplayName.Should().Be("Jane Farmer");
        sut.CphAssociations.Should().ContainSingle().Which.Should().Be(association);
        sut.AssociationsRefreshedDate.Should().Be(associationsRefreshedDate);
        sut.CreatedDate.Should().Be(createdDate);
        sut.LastUpdatedDate.Should().Be(lastUpdatedDate);
        sut.Deleted.Should().BeTrue();
    }

    [Fact]
    public void GetIndexModels_ShouldReturnExpectedIndexes()
    {
        var result = UserAccountDocument.GetIndexModels().ToList();

        var indexNames = result.Select(x => x.Options.Name).ToList();

        indexNames.Should().Contain("uidx_subject");
        indexNames.Should().Contain("uidx_email");
        indexNames.Should().Contain("idxv2_lastUpdatedDate");
        indexNames.Should().Contain("idxv2_deleted");
    }

    [Fact]
    public void GetIndexModels_SubjectIndex_ShouldBeUniqueAndSparse()
    {
        var result = UserAccountDocument.GetIndexModels().ToList();

        var subjectIndex = result.Single(x => x.Options.Name == "uidx_subject");

        subjectIndex.Options.Unique.Should().BeTrue();
        subjectIndex.Options.Sparse.Should().BeTrue();
    }

    [Fact]
    public void GetIndexModels_EmailIndex_ShouldBeUniqueWithCaseInsensitiveCollation()
    {
        var result = UserAccountDocument.GetIndexModels().ToList();

        var emailIndex = result.Single(x => x.Options.Name == "uidx_email");

        emailIndex.Options.Unique.Should().BeTrue();
        emailIndex.Options.Collation.Should().NotBeNull();
    }
}
