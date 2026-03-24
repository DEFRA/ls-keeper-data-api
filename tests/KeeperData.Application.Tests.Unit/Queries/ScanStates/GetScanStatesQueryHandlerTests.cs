using FluentAssertions;
using KeeperData.Application.Queries.ScanStates;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.ScanStates;

public class GetScanStatesQueryHandlerTests
{
    private readonly Mock<IScanStateRepository> _mockRepository;
    private readonly GetScanStatesQueryHandler _sut;
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    public GetScanStatesQueryHandlerTests()
    {
        _mockRepository = new Mock<IScanStateRepository>();
        _sut = new GetScanStatesQueryHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllScanStates()
    {
        // Arrange
        var query = new GetScanStatesQuery();
        var scanStates = CreateScanStates();

        _mockRepository
            .Setup(r => r.GetAllAsync(0, 100, _cancellationToken))
            .ReturnsAsync(scanStates);

        // Act
        var result = await _sut.Handle(query, _cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.ScanStates.Should().HaveCount(4);
        result.ScanStates.Should().Contain(s => s.Id == "CTS-Bulk");
        result.ScanStates.Should().Contain(s => s.Id == "CTS-Daily");
        result.ScanStates.Should().Contain(s => s.Id == "SAM-Bulk");
        result.ScanStates.Should().Contain(s => s.Id == "SAM-Daily");

        _mockRepository.Verify(r => r.GetAllAsync(0, 100, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyCollection_ReturnsEmptyResult()
    {
        // Arrange
        var query = new GetScanStatesQuery();

        _mockRepository
            .Setup(r => r.GetAllAsync(0, 100, _cancellationToken))
            .ReturnsAsync(new List<ScanStateDocument>());

        // Act
        var result = await _sut.Handle(query, _cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.ScanStates.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsScanStatesWithCorrectData()
    {
        // Arrange
        var query = new GetScanStatesQuery();
        var scanCorrelationId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddHours(-2);
        var completedAt = DateTime.UtcNow.AddHours(-1);

        var scanStates = new List<ScanStateDocument>
        {
            new()
            {
                Id = "CTS-Bulk",
                LastSuccessfulScanStartedAt = startedAt,
                LastSuccessfulScanCompletedAt = completedAt,
                LastScanCorrelationId = scanCorrelationId,
                LastScanMode = "Bulk",
                LastScanItemCount = 1000
            }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(0, 100, _cancellationToken))
            .ReturnsAsync(scanStates);

        // Act
        var result = await _sut.Handle(query, _cancellationToken);

        // Assert
        var scanState = result.ScanStates.Single();
        scanState.Id.Should().Be("CTS-Bulk");
        scanState.LastSuccessfulScanStartedAt.Should().Be(startedAt);
        scanState.LastSuccessfulScanCompletedAt.Should().Be(completedAt);
        scanState.LastScanCorrelationId.Should().Be(scanCorrelationId);
        scanState.LastScanMode.Should().Be("Bulk");
        scanState.LastScanItemCount.Should().Be(1000);
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var query = new GetScanStatesQuery();

        _mockRepository
            .Setup(r => r.GetAllAsync(0, 100, _cancellationToken))
            .ReturnsAsync(new List<ScanStateDocument>());

        // Act
        await _sut.Handle(query, _cancellationToken);

        // Assert
        _mockRepository.Verify(
            r => r.GetAllAsync(0, 100, _cancellationToken),
            Times.Once,
            "Should fetch all scan states with skip=0 and limit=100");
    }

    private static List<ScanStateDocument> CreateScanStates()
    {
        return new List<ScanStateDocument>
        {
            new()
            {
                Id = "CTS-Bulk",
                LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-2),
                LastSuccessfulScanCompletedAt = DateTime.UtcNow.AddHours(-1),
                LastScanCorrelationId = Guid.NewGuid(),
                LastScanMode = "Bulk",
                LastScanItemCount = 5000
            },
            new()
            {
                Id = "CTS-Daily",
                LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-4),
                LastSuccessfulScanCompletedAt = DateTime.UtcNow.AddHours(-3),
                LastScanCorrelationId = Guid.NewGuid(),
                LastScanMode = "Daily",
                LastScanItemCount = 250
            },
            new()
            {
                Id = "SAM-Bulk",
                LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-6),
                LastSuccessfulScanCompletedAt = DateTime.UtcNow.AddHours(-5),
                LastScanCorrelationId = Guid.NewGuid(),
                LastScanMode = "Bulk",
                LastScanItemCount = 3000
            },
            new()
            {
                Id = "SAM-Daily",
                LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-8),
                LastSuccessfulScanCompletedAt = DateTime.UtcNow.AddHours(-7),
                LastScanCorrelationId = Guid.NewGuid(),
                LastScanMode = "Daily",
                LastScanItemCount = 150
            }
        };
    }
}