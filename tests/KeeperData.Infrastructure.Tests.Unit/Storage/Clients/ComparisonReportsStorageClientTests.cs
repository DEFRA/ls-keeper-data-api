using FluentAssertions;
using KeeperData.Infrastructure.Storage.Clients;

namespace KeeperData.Infrastructure.Tests.Unit.Storage.Clients;

public class ComparisonReportsStorageClientTests
{
    private ComparisonReportsStorageClient CreateSut()
    {
        return new ComparisonReportsStorageClient();
    }

    [Fact]
    public void ClientName_ShouldReturnTypeName_WhenCalled()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.ClientName;

        // Assert
        result.Should().Be("ComparisonReportsStorageClient");
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_WhenCalled()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        sut.Should().NotBeNull();
        sut.Should().BeOfType<ComparisonReportsStorageClient>();
    }
}