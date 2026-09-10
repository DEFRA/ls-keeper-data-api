using FluentAssertions;
using KeeperData.Application.Queries.Holdings;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.Holdings;

public class GetHoldingDetailQueryHandlerTests
{
    private readonly Mock<IHoldingDetailRepository> _repository = new();
    private readonly CancellationToken _token = CancellationToken.None;

    [Fact]
    public async Task GivenExistingHolding_WhenHandling_ThenReturnsHoldingDetail()
    {
        var expected = new HoldingDetail(
            Identifier: "13/169/0007",
            HoldingType: "permanent",
            Name: "Test Farm",
            StartDate: DateTimeOffset.UtcNow,
            EndDate: null,
            Location: new HoldingLocation(null, 100, 200, new HoldingAddress(null, "Line 1", null, "Town", "Locality", "AB1 2CD", "England")),
            Associations: [],
            AllowedSpecies: ["CTT"],
            Marks: []);

        _repository
            .Setup(r => r.GetHoldingDetailByCphAsync("13/169/0007", _token))
            .ReturnsAsync(expected);

        var handler = new GetHoldingDetailQueryHandler(_repository.Object);
        var query = new GetHoldingDetailQuery
        {
            County = "13",
            Parish = "169",
            Holding = "0007"
        };

        var result = await handler.Handle(query, _token);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GivenNonExistentHolding_WhenHandling_ThenThrowsNotFoundException()
    {
        _repository
            .Setup(r => r.GetHoldingDetailByCphAsync("99/999/9999", _token))
            .ReturnsAsync((HoldingDetail?)null);

        var handler = new GetHoldingDetailQueryHandler(_repository.Object);
        var query = new GetHoldingDetailQuery
        {
            County = "99",
            Parish = "999",
            Holding = "9999"
        };

        var act = async () => await handler.Handle(query, _token);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Holding with CPH '99/999/9999' was not found.");
    }

    [Fact]
    public void GivenQuerySegments_WhenEvaluatingCph_ThenFormatsProperly()
    {
        var query = new GetHoldingDetailQuery
        {
            County = "10",
            Parish = "024",
            Holding = "0247"
        };

        query.Cph.Should().Be("10/024/0247");
    }
}
