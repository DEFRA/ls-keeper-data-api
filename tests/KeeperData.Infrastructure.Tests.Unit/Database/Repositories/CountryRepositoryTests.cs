using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Reflection;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class CountryRepositoryTests
{
    [Fact]
    public void GivenCountryRepository_ThenImplementsICountryRepository()
    {
        // Verify the repository implements the correct interface
        typeof(CountryRepository).Should()
            .Implement<ICountryRepository>();
    }

    [Fact]
    public void GivenCountryRepository_ThenExtendsReferenceDataRepository()
    {
        // Verify the repository extends the abstract base class
        var baseType = typeof(CountryRepository).BaseType;
        baseType.Should().NotBeNull();
        baseType!.IsGenericType.Should().BeTrue();
        baseType.GetGenericTypeDefinition().Should()
            .Be(typeof(ReferenceDataRepository<,>));
    }

    [Fact]
    public void GivenCountryRepository_ThenICountryRepositoryExtendsIReferenceDataRepository()
    {
        // Verify the interface extends the generic repository interface
        typeof(ICountryRepository).Should()
            .Implement<IReferenceDataRepository<CountryListDocument, CountryDocument>>();
    }
}
