using FluentAssertions;
using KeeperData.Application.Configuration;
using KeeperData.Application.Services.UserAccounts;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Options;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services.UserAccounts;

public class UserAccountAssociationBuilderTests
{
    private const string Email = "jane.farmer@example.com";

    private readonly CancellationToken _token = CancellationToken.None;
    private readonly Mock<ICphAssociationsRepository> _associationsRepository = new();
    private readonly UserAccountAssociationBuilder _sut;

    public UserAccountAssociationBuilderTests()
    {
        _sut = new UserAccountAssociationBuilder(
            _associationsRepository.Object,
            Options.Create(new UserAccountAssociationConfig()));
    }

    [Fact]
    public async Task GivenNoAssociationsInTheReadModel_WhenBuilding_ThenReturnsEmptyGraph()
    {
        SetupAssociations([]);

        var result = await _sut.BuildForEmailAsync(Email, _token);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenAnAssociationInTheReadModel_WhenBuilding_ThenTheSnapshotCarriesTheReadModelKeys()
    {
        var association = CreateAssociation("57/103/2335", "owner");
        SetupAssociations([association]);

        var result = await _sut.BuildForEmailAsync(Email, _token);

        result.Should().HaveCount(1);
        result[0].IdentifierId.Should().Be(association.PartyRoleId);
        result[0].CphNumber.Should().Be("57/103/2335");
        result[0].Role.Should().Be("owner");
        result[0].PartyId.Should().Be(association.PartyId);
        result[0].HoldingId.Should().Be(association.HoldingId);
        result[0].HoldingName.Should().Be("Test Holding");
    }

    [Fact]
    public async Task GivenDefaultConfiguration_WhenBuilding_ThenOnlyOwnerRolesAreRequested()
    {
        SetupAssociations([]);

        await _sut.BuildForEmailAsync(Email, _token);

        _associationsRepository.Verify(
            x => x.FindByEmailAsync(Email, It.Is<IReadOnlyCollection<string>>(roles => roles.SequenceEqual(new[] { "owner" })), _token),
            Times.Once);
    }

    [Fact]
    public async Task GivenConfiguredRoles_WhenBuilding_ThenScopeFollowsConfiguration()
    {
        SetupAssociations([]);

        var sut = new UserAccountAssociationBuilder(
            _associationsRepository.Object,
            Options.Create(new UserAccountAssociationConfig { Roles = ["owner", "keeper"] }));

        await sut.BuildForEmailAsync(Email, _token);

        _associationsRepository.Verify(
            x => x.FindByEmailAsync(Email, It.Is<IReadOnlyCollection<string>>(roles => roles.SequenceEqual(new[] { "owner", "keeper" })), _token),
            Times.Once);
    }

    private void SetupAssociations(List<CphAssociationSourceDto> associations) =>
        _associationsRepository
            .Setup(x => x.FindByEmailAsync(Email, It.IsAny<IReadOnlyCollection<string>>(), _token))
            .ReturnsAsync(associations);

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
