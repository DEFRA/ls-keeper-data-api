using FluentAssertions;
using KeeperData.Core.Repositories;
using Moq;
using System.Diagnostics;
using System.Net;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class SimpleTest
{
    private readonly Mock<ISitesRepository> _sitesRepositoryMock = new();
    private readonly HttpClient _client;

    public SimpleTest()
    {
        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsScoped(_sitesRepositoryMock.Object);
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("5")]
    [InlineData("6")]
    [InlineData("7")]
    [InlineData("8")]
    [InlineData("9")]
    [InlineData("10")]
    [InlineData("11")]
    [InlineData("12")]
    [InlineData("13")]
    [InlineData("14")]
    [InlineData("15")]
    [InlineData("16")]
    [InlineData("17")]
    [InlineData("18")]
    [InlineData("19")]
    [InlineData("20")]
    public async Task WhenUserSearchesAppropriateCountriesShouldBeReturned(string scenario)
    {
        Debug.WriteLine(scenario);
        var result = await _client.GetAsync("/api/site");
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}