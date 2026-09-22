using FluentAssertions;
using KeeperData.Application.Queries.Holdings;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.Holdings;

public class GetHoldingsQueryHandlerTests
{
    private readonly Mock<IHoldingDetailRepository> _repository = new();
    private readonly CancellationToken _token = CancellationToken.None;

    [Fact]
    public async Task GivenHoldingsExist_WhenHandling_ThenReturnsPaginatedResult()
    {
        var items = new List<HoldingDetail>
        {
            new(
                Identifier: "13/169/0001",
                HoldingType: "permanent",
                Name: "Farm 1",
                StartDate: DateTimeOffset.UtcNow,
                EndDate: null,
                Location: new HoldingLocation(null, 100, 200, new HoldingAddress(null, "Line 1", null, "Town", "Locality", "AB1 2CD", "England")),
                Associations: [],
                AllowedSpecies: ["CTT"],
                Marks: []),
            new(
                Identifier: "13/169/0002",
                HoldingType: "temporary",
                Name: "Farm 2",
                StartDate: DateTimeOffset.UtcNow,
                EndDate: null,
                Location: new HoldingLocation(null, 100, 200, new HoldingAddress(null, "Line 2", null, "Town", "Locality", "AB1 2CE", "England")),
                Associations: [],
                AllowedSpecies: ["SHP"],
                Marks: [])
        };

        _repository
            .Setup(r => r.GetPagedHoldingsAsync(1, 10, "asc", "cph", _token))
            .ReturnsAsync((items, 25));

        var handler = new GetHoldingsQueryHandler(_repository.Object);
        var query = new GetHoldingsQuery
        {
            Page = 1,
            PageSize = 10,
            Sort = "asc"
        };

        var result = await handler.Handle(query, _token);

        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result.Values.Should().BeEquivalentTo(items);

        _repository.Verify(r => r.GetPagedHoldingsAsync(1, 10, "asc", "cph", _token), Times.Once);
    }

    [Fact]
    public async Task GivenCustomPageAndDescSort_WhenHandling_ThenForwardsToRepository()
    {
        _repository
            .Setup(r => r.GetPagedHoldingsAsync(2, 5, "desc", "name", _token))
            .ReturnsAsync(([], 10));

        var handler = new GetHoldingsQueryHandler(_repository.Object);
        var query = new GetHoldingsQuery
        {
            Page = 2,
            PageSize = 5,
            Sort = "desc",
            Order = "name"
        };

        var result = await handler.Handle(query, _token);

        result.Count.Should().Be(0);
        result.TotalCount.Should().Be(10);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();

        _repository.Verify(r => r.GetPagedHoldingsAsync(2, 5, "desc", "name", _token), Times.Once);
    }

    [Theory]
    [InlineData(1, 10, "asc", true)]
    [InlineData(1, 10, "desc", true)]
    [InlineData(1, 10, "ASC", true)]
    [InlineData(1, 10, "DESC", true)]
    [InlineData(1, 10, null, true)]
    [InlineData(1, 10, "", true)]
    [InlineData(0, 10, "asc", false)]
    [InlineData(-1, 10, "asc", false)]
    [InlineData(1, 0, "asc", false)]
    [InlineData(1, 101, "asc", false)]
    [InlineData(1, 10, "invalid", false)]
    public void Validator_ValidatesInputsCorrectly(int page, int pageSize, string? sort, bool expectedValid)
    {
        var validator = new GetHoldingsQueryValidator();
        var query = new GetHoldingsQuery
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort
        };

        var validationResult = validator.Validate(query);

        validationResult.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("cph", true)]
    [InlineData("identifier", true)]
    [InlineData("name", true)]
    [InlineData("holdingType", true)]
    [InlineData("startDate", true)]
    [InlineData("endDate", true)]
    [InlineData("NAME", true)]
    [InlineData("unsupported", false)]
    public void Validator_ValidatesOrder(string order, bool expectedValid)
    {
        var result = new GetHoldingsQueryValidator().Validate(new GetHoldingsQuery { Order = order });

        result.IsValid.Should().Be(expectedValid);
    }
}