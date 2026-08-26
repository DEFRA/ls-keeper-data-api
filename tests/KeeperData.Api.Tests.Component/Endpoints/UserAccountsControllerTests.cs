using FluentAssertions;
using KeeperData.Api.Controllers;
using KeeperData.Api.Controllers.RequestDtos.UserAccounts;
using KeeperData.Application;
using KeeperData.Application.Commands.UserAccounts;
using KeeperData.Application.Queries.UserAccounts;
using KeeperData.Core.DTOs;
using KeeperData.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class UserAccountsControllerTests
{
    private const string Subject = "9f3a1c2e-0b6d-4f4e-9d2a-7c8b1e5f0a3d";
    private const string Email = "jane.farmer@example.com";

    private readonly Mock<IRequestExecutor> _mockExecutor = new();
    private readonly Mock<IReadModelSqliteCacheService> _mockReadModelCache = new();
    private readonly UserAccountsController _controller;

    public UserAccountsControllerTests()
    {
        _controller = new UserAccountsController(_mockExecutor.Object, _mockReadModelCache.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task EnsureUserAccount_WhenReadModelCacheNotLoaded_Returns503AndDoesNotTouchTheAccount()
    {
        _mockReadModelCache.Setup(x => x.IsLoaded).Returns(false);

        var result = await _controller.EnsureUserAccount(ClaimsRequest(), CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);

        _mockExecutor.Verify(
            x => x.ExecuteCommand(It.IsAny<EnsureUserAccountCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnsureUserAccount_WhenAccountCreated_Returns201WithLocation()
    {
        SetupEnsureResult(created: true);

        var result = await _controller.EnsureUserAccount(ClaimsRequest(), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedResult>().Subject;
        created.Location.Should().Be($"/api/v2/user-accounts/{Uri.EscapeDataString(Subject)}");
        created.Value.Should().BeOfType<UserAccountDto>();
    }

    [Fact]
    public async Task EnsureUserAccount_WhenAccountRefreshed_Returns200WithTheSnapshot()
    {
        SetupEnsureResult(created: false);

        var result = await _controller.EnsureUserAccount(ClaimsRequest(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var account = ok.Value.Should().BeOfType<UserAccountDto>().Subject;
        account.CphAssociations.Should().HaveCount(1);
        account.CphAssociations[0].CphNumber.Should().Be("57/103/2335");
        account.CphAssociations[0].Role.Should().Be("owner");
    }

    [Fact]
    public async Task EnsureUserAccount_WhenLoaded_PassesTheClaimsThrough()
    {
        SetupEnsureResult(created: false);

        await _controller.EnsureUserAccount(ClaimsRequest(), CancellationToken.None);

        _mockExecutor.Verify(
            x => x.ExecuteCommand(
                It.Is<EnsureUserAccountCommand>(command =>
                    command.Subject == Subject &&
                    command.Email == Email &&
                    command.GivenName == "Jane" &&
                    command.FamilyName == "Farmer"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserAccountBySubject_Returns200AndDoesNotReadTheReadModel()
    {
        _mockExecutor
            .Setup(x => x.ExecuteQuery(It.IsAny<GetUserAccountBySubjectQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Account());

        var result = await _controller.GetUserAccountBySubject(Subject, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<UserAccountDto>().Subject.CphAssociations.Should().HaveCount(1);

        _mockReadModelCache.Verify(x => x.GetCurrentDbPath(), Times.Never);
    }

    private void SetupEnsureResult(bool created)
    {
        _mockReadModelCache.Setup(x => x.IsLoaded).Returns(true);
        _mockExecutor
            .Setup(x => x.ExecuteCommand(It.IsAny<EnsureUserAccountCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnsureUserAccountResult(Account(), created));
    }

    private static EnsureUserAccountRequest ClaimsRequest() => new()
    {
        Sub = Subject,
        Email = Email,
        GivenName = "Jane",
        FamilyName = "Farmer"
    };

    private static UserAccountDto Account() => new()
    {
        Id = "account-id",
        Subject = Subject,
        Email = Email,
        FirstName = "Jane",
        LastName = "Farmer",
        DisplayName = "Jane Farmer",
        CphAssociations =
        [
            new CphAssociationDto
            {
                IdentifierId = "party-role-id",
                CphNumber = "57/103/2335",
                Role = "owner",
                PartyId = "party-id",
                HoldingId = "holding-id",
                HoldingName = "Test Holding"
            }
        ]
    };
}