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
        _fixture.SetUpDocuments(new FacilityBusinessActivityMapListDocument
        {
            Id = "all-premisesactivitytypes",
            FacilityBusinessActivityMaps = TestData
        });

        var result = await _sut.FindByActivityCodeAsync(facilityActivityCode);

        result?.AssociatedPremiseTypeCode.Should().Be(associatedPremiseTypeCode);
        result?.AssociatedPremiseActivityCode.Should().Be(associatedPremiseActivityCode);
    }
}