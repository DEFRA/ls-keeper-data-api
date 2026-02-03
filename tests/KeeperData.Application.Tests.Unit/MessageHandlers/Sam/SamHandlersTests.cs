using FluentAssertions;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Application.MessageHandlers.Sam;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Tests.Common.Factories;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System.Reflection;

namespace KeeperData.Application.Tests.Unit.MessageHandlers.Sam;

public class SamHandlersTests
{
    private readonly DataBridgeScanConfiguration _config = new() { QueryPageSize = 100 };
    private readonly CancellationToken _ct = CancellationToken.None;

    [Fact]
    public async Task SamBulkScanMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<SamBulkScanOrchestrator>(Enumerable.Empty<Application.Orchestration.ChangeScanning.IScanStep<SamBulkScanContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamBulkScanMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var command = new ProcessSamBulkScanMessageCommand(message);
        var payload = new SamBulkScanMessage();

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new SamBulkScanMessageHandler(orchestrator.Object, serializer.Object, _config);
        await handler.Handle(command, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), _ct), Times.Once);
    }

    [Fact]
    public async Task SamBulkScanMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<SamBulkScanOrchestrator>(Enumerable.Empty<Application.Orchestration.ChangeScanning.IScanStep<SamBulkScanContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamBulkScanMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };
        var command = new ProcessSamBulkScanMessageCommand(message);

        serializer.Setup(x => x.Deserialize(message)).Returns((SamBulkScanMessage?)null);

        var handler = new SamBulkScanMessageHandler(orchestrator.Object, serializer.Object, _config);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(command, _ct));
    }

    [Fact]
    public async Task SamDailyScanMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<SamDailyScanOrchestrator>(Enumerable.Empty<Application.Orchestration.ChangeScanning.IScanStep<SamDailyScanContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamDailyScanMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var command = new ProcessSamDailyScanMessageCommand(message);
        var payload = new SamDailyScanMessage();

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new SamDailyScanMessageHandler(orchestrator.Object, serializer.Object, _config);
        await handler.Handle(command, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.IsAny<SamDailyScanContext>(), _ct), Times.Once);
    }

    [Fact]
    public async Task SamDailyScanMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<SamDailyScanOrchestrator>(Enumerable.Empty<Application.Orchestration.ChangeScanning.IScanStep<SamDailyScanContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamDailyScanMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };
        var command = new ProcessSamDailyScanMessageCommand(message);

        serializer.Setup(x => x.Deserialize(message)).Returns((SamDailyScanMessage?)null);

        var handler = new SamDailyScanMessageHandler(orchestrator.Object, serializer.Object, _config);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(command, _ct));
    }

    [Fact]
    public async Task SamImportHoldingMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamImportHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var command = new ProcessSamImportHoldingMessageCommand(message);
        var payload = new SamImportHoldingMessage { Identifier = "CPH" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new SamImportHoldingMessageHandler(orchestrator.Object, serializer.Object);
        await handler.Handle(command, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.Is<SamHoldingImportContext>(c => c.Cph == "CPH"), _ct), Times.Once);
    }

    [Fact]
    public async Task SamImportHoldingMessageHandler_Handle_DeserializationFailure_Throws()
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamImportHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var command = new ProcessSamImportHoldingMessageCommand(message);

        serializer.Setup(x => x.Deserialize(message)).Returns((SamImportHoldingMessage?)null);

        var handler = new SamImportHoldingMessageHandler(orchestrator.Object, serializer.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(command, _ct));
    }

    public static IEnumerable<object[]> MongoExceptions
    {
        get
        {
            yield return [MongoExceptionFactory.CreateMongoBulkWriteException([GetBulkWriteError(ServerErrorCategory.DuplicateKey)]), typeof(RetryableException)];
            yield return [MongoExceptionFactory.CreateMongoBulkWriteException(), typeof(NonRetryableException)];
            yield return [GetMongoWriteException(ServerErrorCategory.DuplicateKey), typeof(RetryableException)];
            yield return [GetMongoWriteException(ServerErrorCategory.Uncategorized), typeof(NonRetryableException)];
            yield return [new Exception(), typeof(NonRetryableException)];
        }
    }

    public static BulkWriteError GetBulkWriteError(ServerErrorCategory category)
    {
        var ctor = typeof(BulkWriteError).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, [typeof(int), typeof(ServerErrorCategory), typeof(int), typeof(string), typeof(BsonDocument)]);
        var bwe = ctor!.Invoke(new object[] { 0, category, 1, "", new BsonDocument() });
        return (BulkWriteError)bwe;
    }

    public static MongoWriteException GetMongoWriteException(ServerErrorCategory category)
    {
        var t = typeof(MongoWriteException);
        var fromBulk = t.GetMethod("FromBulkWriteException", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        var we = fromBulk!.Invoke(null, new object[] { MongoExceptionFactory.CreateMongoBulkWriteException([GetBulkWriteError(category)]) });
        return (MongoWriteException)we!;
    }

    [Theory]
    [MemberData(nameof(MongoExceptions))]
    public async Task SamImportHoldingMessageHandler_Handle_MongoExceptions(Exception thrown, Type expectedExceptionType)
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamImportHoldingMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };
        var command = new ProcessSamImportHoldingMessageCommand(message);
        var payload = new SamImportHoldingMessage { Identifier = "CPH" };
        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        orchestrator.Setup(x => x.ExecuteAsync(It.IsAny<SamHoldingImportContext>(), _ct)).ThrowsAsync(thrown);

        var handler = new SamImportHoldingMessageHandler(orchestrator.Object, serializer.Object);

        var actualException = await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(command, _ct));
        actualException.GetType().Should().Be(expectedExceptionType);
    }

    [Fact]
    public async Task SamUpdateHoldingMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamUpdateHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var command = new ProcessSamUpdateHoldingMessageCommand(message);
        var payload = new SamUpdateHoldingMessage { Identifier = "CPH" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new SamUpdateHoldingMessageHandler(orchestrator.Object, serializer.Object);
        await handler.Handle(command, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.Is<SamHoldingImportContext>(c => c.Cph == "CPH"), _ct), Times.Once);
    }

    [Fact]
    public async Task SamUpdateHoldingMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamUpdateHoldingMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };
        var command = new ProcessSamUpdateHoldingMessageCommand(message);

        serializer.Setup(x => x.Deserialize(message)).Returns((SamUpdateHoldingMessage?)null);

        var handler = new SamUpdateHoldingMessageHandler(orchestrator.Object, serializer.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(command, _ct));
    }

    [Theory]
    [MemberData(nameof(MongoExceptions))]
    public async Task SamUpdateHoldingMessageHandler_Handle_MongoExceptions(Exception thrown, Type expectedExceptionType)
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamUpdateHoldingMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };
        var command = new ProcessSamUpdateHoldingMessageCommand(message);
        var payload = new SamUpdateHoldingMessage { Identifier = "CPH" };
        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        orchestrator.Setup(x => x.ExecuteAsync(It.IsAny<SamHoldingImportContext>(), _ct)).ThrowsAsync(thrown);

        var handler = new SamUpdateHoldingMessageHandler(orchestrator.Object, serializer.Object);

        var actualException = await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(command, _ct));
        actualException.GetType().Should().Be(expectedExceptionType);
    }

}