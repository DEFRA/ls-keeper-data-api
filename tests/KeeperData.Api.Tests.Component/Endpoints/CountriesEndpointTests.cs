using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Xunit.Sdk;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class CountriesEndpointTests
{
    private readonly HttpClient _client;
    private Mock<ICountryRepository> _countryRepoMock;

    public CountriesEndpointTests()
    {
        _countryRepoMock = new Mock<ICountryRepository>();
        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsScoped(_countryRepoMock.Object);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WhenEndpointHitWithNoParams_AllCountriesShouldBeReturned()
    {
        var response = await _client.GetAsync("api/countries?");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}