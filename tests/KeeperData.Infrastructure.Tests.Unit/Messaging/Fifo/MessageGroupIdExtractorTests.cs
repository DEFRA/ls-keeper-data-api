using FluentAssertions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Infrastructure.Messaging.Fifo;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Fifo;

public class MessageGroupIdExtractorTests
{
    [Theory]
    [InlineData("12/345/6789", "CPH_12_345_6789")]
    [InlineData("12-345-6789", "CPH_12_345_6789")]
    [InlineData("123456789", "CPH_123456789")]
    [InlineData("AB/123/45678", "CPH_AB_123_45678")]
    public void ExtractGroupId_WithSamImportHoldingMessage_ReturnsCorrectCphGroupId(string cph, string expectedGroupId)
    {
        // Arrange
        var message = new SamImportHoldingMessage { Identifier = cph };

        // Act
        var result = MessageGroupIdExtractor.ExtractGroupId(message);

        // Assert
        result.Should().Be(expectedGroupId);
    }

    [Theory]
    [InlineData("12/345/6789", "CPH_12_345_6789")]
    [InlineData("AB/123/45678", "CPH_AB_123_45678")]
    public void ExtractGroupId_WithSamUpdateHoldingMessage_ReturnsCorrectCphGroupId(string cph, string expectedGroupId)
    {
        // Arrange
        var message = new SamUpdateHoldingMessage { Identifier = cph };

        // Act
        var result = MessageGroupIdExtractor.ExtractGroupId(message);

        // Assert
        result.Should().Be(expectedGroupId);
    }

    [Theory]
    [InlineData("AG-12/345/6789", "CPH_12_345_6789")]
    [InlineData("AH-AB/CDE/FGHI", "CPH_AB_CDE_FGHI")]
    [InlineData("XX-3000123", "CPH_3000123")]
    public void ExtractGroupId_WithCtsImportHoldingMessage_ReturnsCorrectCphGroupId(string lidIdentifier, string expectedGroupId)
    {
        // Arrange
        var message = new CtsImportHoldingMessage { Identifier = lidIdentifier };

        // Act
        var result = MessageGroupIdExtractor.ExtractGroupId(message);

        // Assert
        result.Should().Be(expectedGroupId);
    }

    [Theory]
    [InlineData("AG-12/345/6789", "CPH_12_345_6789")]
    [InlineData("AH-AB/CDE/FGHI", "CPH_AB_CDE_FGHI")]
    [InlineData("XX-3000123", "CPH_3000123")]
    public void ExtractGroupId_WithCtsUpdateHoldingMessage_ReturnsCorrectCphGroupId(string lidIdentifier, string expectedGroupId)
    {
        // Arrange
        var message = new CtsUpdateHoldingMessage { Identifier = lidIdentifier };

        // Act
        var result = MessageGroupIdExtractor.ExtractGroupId(message);

        // Assert
        result.Should().Be(expectedGroupId);
    }

    [Theory]
    [InlineData("PARTY123456", "PARTY_PARTY123456")]
    [InlineData("AGT789012", "PARTY_AGT789012")]
    [InlineData("12345", "PARTY_12345")]
    public void ExtractGroupId_WithCtsUpdateAgentMessage_ReturnsCorrectPartyGroupId(string partyId, string expectedGroupId)
    {
        // Arrange
        var message = new CtsUpdateAgentMessage { Identifier = partyId };

        // Act
        var result = MessageGroupIdExtractor.ExtractGroupId(message);

        // Assert
        result.Should().Be(expectedGroupId);
    }

    [Theory]
    [InlineData("PARTY123456", "PARTY_PARTY123456")]
    [InlineData("KEEPER789012", "PARTY_KEEPER789012")]
    [InlineData("67890", "PARTY_67890")]
    public void ExtractGroupId_WithCtsUpdateKeeperMessage_ReturnsCorrectPartyGroupId(string partyId, string expectedGroupId)
    {
        // Arrange
        var message = new CtsUpdateKeeperMessage { Identifier = partyId };

        // Act
        var result = MessageGroupIdExtractor.ExtractGroupId(message);

        // Assert
        result.Should().Be(expectedGroupId);
    }

    [Fact]
    public void ExtractGroupId_WithSamBulkScanMessage_ReturnsSystemGroupId()
    {
        // Arrange
        var message = new SamBulkScanMessage { Identifier = "" };

        // Act
        var result = MessageGroupIdExtractor.ExtractGroupId(message);

        // Assert
        result.Should().Be("SYSTEM_SAM_BULK_SCAN");
    }

    [Fact]
    public void ExtractGroupId_WithCtsBulkScanMessage_ReturnsSystemGroupId()
    {
        // Arrange
        var message = new CtsBulkScanMessage { Identifier = "" };

        // Act
        var result = MessageGroupIdExtractor.ExtractGroupId(message);

        // Assert
        result.Should().Be("SYSTEM_CTS_BULK_SCAN");
    }

    [Fact]
    public void ExtractGroupId_WithSamDailyScanMessage_ReturnsSystemGroupId()
    {
        // Arrange
        var message = new SamDailyScanMessage { Identifier = "" };

        // Act
        var result = MessageGroupIdExtractor.ExtractGroupId(message);

        // Assert
        result.Should().Be("SYSTEM_SAM_DAILY_SCAN");
    }

    [Fact]
    public void ExtractGroupId_WithCtsDailyScanMessage_ReturnsSystemGroupId()
    {
        // Arrange
        var message = new CtsDailyScanMessage { Identifier = "" };

        // Act
        var result = MessageGroupIdExtractor.ExtractGroupId(message);

        // Assert
        result.Should().Be("SYSTEM_CTS_DAILY_SCAN");
    }

    [Theory]
    [InlineData("CPH_12_345_6789")]
    [InlineData("PARTY_AGENT123456")]
    [InlineData("SYSTEM_SAM_BULK_SCAN")]
    public void ExtractGroupId_ResultsAreSqsCompatible_ShouldOnlyContainValidCharacters(string expectedGroupId)
    {
        // Assert
        expectedGroupId.Should().MatchRegex(@"^[A-Za-z0-9\-_]+$",
            "SQS FIFO MessageGroupId must contain only alphanumeric characters, hyphens, and underscores");

        expectedGroupId.Length.Should().BeLessThanOrEqualTo(128,
            "SQS FIFO MessageGroupId must be 128 characters or less");
    }

    [Fact]
    public void ExtractGroupId_WithUnsupportedMessageType_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedMessage = new UnsupportedMessage();

        // Act
        var act = () => MessageGroupIdExtractor.ExtractGroupId(unsupportedMessage);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Message type UnsupportedMessage not supported for FIFO grouping");
    }

    private class UnsupportedMessage : MessageType
    {
        public string Identifier { get; set; } = string.Empty;
    }
}