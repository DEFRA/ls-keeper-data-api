using FluentAssertions;
using KeeperData.Application.Configuration;
using KeeperData.Application.Queries.CphAssociations;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Options;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.CphAssociations;

public class GetCphAssociationsQueryHandlerTests
{
    private const string Email = "test@example.com";
    private readonly CancellationToken _token = CancellationToken.None;
    private readonly Mock<ICphAssociationsRepository> _repository = new();

    [Fact]
    public async Task GivenDefaultConfig_WhenHandling_ThenUsesOwnerRoleAndMapsResults()
    {
        // Arrange
        var config = Options.Create(new UserAccountAssociationConfig { Roles = null! });
        var sut = new GetCphAssociationsQueryHandler(_repository.Object, config);

        var sourceDtos = new List<CphAssociationSourceDto>
        {
            CreateAssociation("12/345/6789", "owner")
        };

        _repository
            .Setup(r => r.FindByEmailAsync(Email, It.Is<IReadOnlyCollection<string>>(roles => roles.SequenceEqual(new[] { "owner" })), _token))
            .ReturnsAsync(sourceDtos);

        // Act
        var result = await sut.Handle(new GetCphAssociationsQuery { Email = Email }, _token);

        // Assert
        result.Should().HaveCount(1);
        result[0].Cph.Should().Be("12/345/6789");
        result[0].Role.Should().Be("owner");
    }

    [Fact]
    public async Task GivenConfiguredRoles_WhenHandling_ThenPassesConfiguredRolesAndMapsMultipleResults()
    {
        // Arrange
        var config = Options.Create(new UserAccountAssociationConfig { Roles = ["owner", "agent"] });
        var sut = new GetCphAssociationsQueryHandler(_repository.Object, config);

        var sourceDtos = new List<CphAssociationSourceDto>
        {
            CreateAssociation("12/345/6789", "owner"),
            CreateAssociation("98/765/4321", "agent")
        };

        _repository
            .Setup(r => r.FindByEmailAsync(Email, It.Is<IReadOnlyCollection<string>>(roles => roles.SequenceEqual(new[] { "owner", "agent" })), _token))
            .ReturnsAsync(sourceDtos);

        // Act
        var result = await sut.Handle(new GetCphAssociationsQuery { Email = Email }, _token);

        // Assert
        result.Should().HaveCount(2);
        result[0].Cph.Should().Be("12/345/6789");
        result[0].Role.Should().Be("owner");
        result[1].Cph.Should().Be("98/765/4321");
        result[1].Role.Should().Be("agent");
    }

    [Fact]
    public async Task GivenNoAssociationsFound_WhenHandling_ThenReturnsEmptyList()
    {
        // Arrange
        var config = Options.Create(new UserAccountAssociationConfig());
        var sut = new GetCphAssociationsQueryHandler(_repository.Object, config);

        _repository
            .Setup(r => r.FindByEmailAsync(Email, It.IsAny<IReadOnlyCollection<string>>(), _token))
            .ReturnsAsync([]);

        // Act
        var result = await sut.Handle(new GetCphAssociationsQuery { Email = Email }, _token);

        // Assert
        result.Should().BeEmpty();
    }

    private static CphAssociationSourceDto CreateAssociation(string cphNumber, string role) => new()
    {
        PartyRoleId = Guid.NewGuid().ToString(),
        CphNumber = cphNumber,
        Role = role,
        PartyId = Guid.NewGuid().ToString(),
        HoldingId = Guid.NewGuid().ToString(),
        HoldingName = "Test Holding"
    };
}