using FluentAssertions;
using KeeperData.Application.Queries.Sites;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.Sites.Builders;

public class GetSiteByIdQueryHandlerTests
{
    private readonly CancellationToken Token = new CancellationToken();
    private const string SiteIdThatExists = "site-id";
    private const string SiteIdThatDoesNotExist = "bad-id";
    private Mock<IGenericRepository<SiteDocument>> mockRepo;
    private GetSiteByIdQueryHandler sut;

    public GetSiteByIdQueryHandlerTests()
    {
        mockRepo = new Mock<IGenericRepository<SiteDocument>>();
        sut = new GetSiteByIdQueryHandler(mockRepo.Object);
    }

    [Fact]
    public async Task GivenSiteDoesNotExistWhenGettingDocumentItShouldThrowNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(new GetSiteByIdQuery(SiteIdThatDoesNotExist), Token));
    }

    [Fact]
    public async Task GivenSiteDoesExistWhenGettingDocumentItShouldReturnRequestedDocument()
    {
        var siteDocument = new SiteDocument
        {
            Id = SiteIdThatExists,
            Name = "Test Site",
            StartDate = DateTime.UtcNow
        };
        mockRepo.Setup(x => x.GetByIdAsync(SiteIdThatExists, Token)).ReturnsAsync(siteDocument);

        var result = await sut.Handle(new GetSiteByIdQuery(SiteIdThatExists), Token);

        result.Should().NotBeNull();
        result.Id.Should().Be(SiteIdThatExists);
        result.Name.Should().Be("Test Site");
        result.StartDate.Should().Be(siteDocument.StartDate);
    }
}