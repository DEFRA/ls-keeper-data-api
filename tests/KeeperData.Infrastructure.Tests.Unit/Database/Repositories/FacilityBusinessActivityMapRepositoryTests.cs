using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Repositories;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class FacilityBusinessActivityMapRepositoryTests
{
    private readonly ReferenceRepositoryTestFixture<FacilityBusinessActivityMapRepository, FacilityBusinessActivityMapListDocument, FacilityBusinessActivityMapDocument> _fixture;
    private readonly FacilityBusinessActivityMapRepository _sut;

    public FacilityBusinessActivityMapRepositoryTests()
    {
        _fixture = new ReferenceRepositoryTestFixture<FacilityBusinessActivityMapRepository, FacilityBusinessActivityMapListDocument, FacilityBusinessActivityMapDocument>();
        _sut = _fixture.CreateSut((config, client, unitOfWork) => new FacilityBusinessActivityMapRepository(config, client, unitOfWork));
    }

    private List<FacilityBusinessActivityMapDocument> TestData = new List<FacilityBusinessActivityMapDocument>
        {
            new()
            {
                IdentifierId = "id1",
                FacilityActivityCode = "AB-EMB-ECT",
                AssociatedSiteTypeCode = "AI",
                AssociatedSiteActivityCode = "EMB",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = "id2",
                FacilityActivityCode = "AB-SEM-SCCDOM",
                AssociatedSiteTypeCode = "AI",
                AssociatedSiteActivityCode = "SEM",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }};

    [Theory]
    [InlineData("AB-EMB-ECT", "AI", "EMB")]
    [InlineData("AB-SEM-SCCDOM", "AI", "SEM")]
    [InlineData("invalid", null, null)]
    public async Task CanGetDocumentByFacilityActivityCode(string facilityActivityCode, string? associatedSiteTypeCode, string? associatedSiteActivityCode)
    {
        _fixture.SetUpDocuments(new FacilityBusinessActivityMapListDocument
        {
            Id = "all-siteactivitytypes",
            FacilityBusinessActivityMaps = TestData
        });

        var result = await _sut.FindByActivityCodeAsync(facilityActivityCode);

        result?.AssociatedSiteTypeCode.Should().Be(associatedSiteTypeCode);
        result?.AssociatedSiteActivityCode.Should().Be(associatedSiteActivityCode);
    }
}