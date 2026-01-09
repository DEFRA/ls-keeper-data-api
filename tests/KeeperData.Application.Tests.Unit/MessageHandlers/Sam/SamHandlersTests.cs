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
using Moq;

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
        var payload = new SamBulkScanMessage();

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new SamBulkScanMessageHandler(orchestrator.Object, serializer.Object, _config);
        await handler.Handle(message, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), _ct), Times.Once);
    }

    [Fact]
    public async Task SamBulkScanMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<SamBulkScanOrchestrator>(Enumerable.Empty<Application.Orchestration.ChangeScanning.IScanStep<SamBulkScanContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamBulkScanMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };

        serializer.Setup(x => x.Deserialize(message)).Returns((SamBulkScanMessage?)null);

        var handler = new SamBulkScanMessageHandler(orchestrator.Object, serializer.Object, _config);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task SamDailyScanMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<SamDailyScanOrchestrator>(Enumerable.Empty<Application.Orchestration.ChangeScanning.IScanStep<SamDailyScanContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamDailyScanMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new SamDailyScanMessage();

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new SamDailyScanMessageHandler(orchestrator.Object, serializer.Object, _config);
        await handler.Handle(message, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.IsAny<SamDailyScanContext>(), _ct), Times.Once);
    }

    [Fact]
    public async Task SamDailyScanMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<SamDailyScanOrchestrator>(Enumerable.Empty<Application.Orchestration.ChangeScanning.IScanStep<SamDailyScanContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamDailyScanMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };

        serializer.Setup(x => x.Deserialize(message)).Returns((SamDailyScanMessage?)null);

        var handler = new SamDailyScanMessageHandler(orchestrator.Object, serializer.Object, _config);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task SamImportHoldingMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamImportHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new SamImportHoldingMessage { Identifier = "CPH" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new SamImportHoldingMessageHandler(orchestrator.Object, serializer.Object);
        await handler.Handle(message, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.Is<SamHoldingImportContext>(c => c.Cph == "CPH"), _ct), Times.Once);
    }

    [Fact]
    public async Task SamImportHoldingMessageHandler_Handle_DeserializationFailure_Throws()
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamImportHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        serializer.Setup(x => x.Deserialize(message)).Returns((SamImportHoldingMessage?)null);

        var handler = new SamImportHoldingMessageHandler(orchestrator.Object, serializer.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task SamImportHoldingMessageHandler_Handle_MongoException_Throws()
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamImportHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new SamImportHoldingMessage { Identifier = "CPH" };
        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var mongoException = MongoExceptionFactory.CreateMongoBulkWriteException();
        orchestrator.Setup(x => x.ExecuteAsync(It.IsAny<SamHoldingImportContext>(), _ct)).ThrowsAsync(mongoException);

        var handler = new SamImportHoldingMessageHandler(orchestrator.Object, serializer.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task SamUpdateHoldingMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamUpdateHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new SamUpdateHoldingMessage { Identifier = "CPH" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new SamUpdateHoldingMessageHandler(orchestrator.Object, serializer.Object);
        await handler.Handle(message, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.Is<SamHoldingImportContext>(c => c.Cph == "CPH"), _ct), Times.Once);
    }

    [Fact]
    public async Task SamUpdateHoldingMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamUpdateHoldingMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };

        serializer.Setup(x => x.Deserialize(message)).Returns((SamUpdateHoldingMessage?)null);

        var handler = new SamUpdateHoldingMessageHandler(orchestrator.Object, serializer.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task SamUpdateHoldingMessageHandler_Handle_GenericException_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamUpdateHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new SamUpdateHoldingMessage { Identifier = "CPH" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);
        orchestrator.Setup(x => x.ExecuteAsync(It.IsAny<SamHoldingImportContext>(), _ct)).ThrowsAsync(new Exception("Generic"));

        var handler = new SamUpdateHoldingMessageHandler(orchestrator.Object, serializer.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task SamUpdateHoldingMessageHandler_Handle_MongoException_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<SamHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<SamHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<SamUpdateHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new SamUpdateHoldingMessage { Identifier = "CPH" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var mongoException = MongoExceptionFactory.CreateMongoBulkWriteException();
        orchestrator.Setup(x => x.ExecuteAsync(It.IsAny<SamHoldingImportContext>(), _ct)).ThrowsAsync(mongoException);

        var handler = new SamUpdateHoldingMessageHandler(orchestrator.Object, serializer.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }
}