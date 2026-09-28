using FluentAssertions;
using KeeperData.Api.Controllers.RequestDtos.UserAccounts;
using KeeperData.Application.Commands.UserAccounts;
using KeeperData.Application.Queries.UserAccounts;
using KeeperData.Core.DTOs;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Utilities;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class UserAccountsEndpointTests : IClassFixture<AppTestFixture>
{
    private readonly Mock<IReadModelSqliteCacheService> _mockCache;
    private readonly Mock<KeeperData.Application.IRequestExecutor> _mockExecutor;
    private readonly HttpClient _client;

    public UserAccountsEndpointTests(AppTestFixture appTestFixture)
    {
        _mockCache = new Mock<IReadModelSqliteCacheService>();
        _mockExecutor = new Mock<KeeperData.Application.IRequestExecutor>();

        _client = appTestFixture.AppWebApplicationFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_mockCache.Object);
                services.AddSingleton(_mockExecutor.Object);
            });
        }).CreateClient();

        _client.AddBasicApiKey("ApiKey", "integration-test-secret");
    }

    [Theory]
    [InlineData("/api/v2/user-accounts/provider%2Fuser%2F987654")]
    [InlineData("/api/v2/user-accounts/provider/user/987654")]
    public async Task GetUserAccountBySubject_WhenSubjectContainsSlashes_Returns200AndResolvesTheSubject(string url)
    {
        _mockExecutor
            .Setup(x => x.ExecuteQuery(It.IsAny<GetUserAccountBySubjectQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserAccountDto
            {
                Id = "account-id",
                Subject = "provider/user/987654",
                Email = "federated.user@example.test"
            });

        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        _mockExecutor.Verify(
            x => x.ExecuteQuery(
                It.Is<GetUserAccountBySubjectQuery>(query => query.Subject == "provider/user/987654"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureUserAccount_WhenEmailIsPaddedWithWhitespace_TrimsItBeforeDispatch()
    {
        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        _mockExecutor
            .Setup(x => x.ExecuteCommand(It.IsAny<EnsureUserAccountCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnsureUserAccountResult(new UserAccountDto
            {
                Id = "account-id",
                Subject = "subject",
                Email = "jane.farmer@example.com"
            }, true));

        var response = await _client.PostAsJsonAsync("/api/v2/user-accounts", new EnsureUserAccountRequest
        {
            Sub = "subject",
            Email = "  jane.farmer@example.com  ",
            GivenName = "Jane",
            FamilyName = "Farmer"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        _mockExecutor.Verify(
            x => x.ExecuteCommand(
                It.Is<EnsureUserAccountCommand>(command => command.Email == "jane.farmer@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
