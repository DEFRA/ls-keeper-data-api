using FluentAssertions;
using KeeperData.Application.MessageHandlers.Cts;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily;
using KeeperData.Application.Orchestration.Imports.Cts.Holdings;
using KeeperData.Application.Orchestration.Updates.Cts.Agents;
using KeeperData.Application.Orchestration.Updates.Cts.Holdings;
using KeeperData.Application.Orchestration.Updates.Cts.Keepers;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Tests.Common.Factories;
using MongoDB.Driver;
using Moq;
using System.Net;

namespace KeeperData.Application.Tests.Unit.MessageHandlers.Cts;

public class CtsHandlersTests
{
    private readonly DataBridgeScanConfiguration _config = new() { QueryPageSize = 100 };
    private readonly CancellationToken _ct = CancellationToken.None;

    [Fact]
    public async Task CtsBulkScanMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<CtsBulkScanOrchestrator>(Enumerable.Empty<Application.Orchestration.ChangeScanning.IScanStep<CtsBulkScanContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsBulkScanMessage>>();

        var message = new UnwrappedMessage { MessageId = "1", CorrelationId = "2" };
        var payload = new CtsBulkScanMessage { Identifier = "Test" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new CtsBulkScanMessageHandler(orchestrator.Object, serializer.Object, _config);

        await handler.Handle(message, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.IsAny<CtsBulkScanContext>(), _ct), Times.Once);
    }

    [Fact]
    public async Task CtsBulkScanMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<CtsBulkScanOrchestrator>(Enumerable.Empty<Application.Orchestration.ChangeScanning.IScanStep<CtsBulkScanContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsBulkScanMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };

        serializer.Setup(x => x.Deserialize(message)).Returns((CtsBulkScanMessage?)null);

        var handler = new CtsBulkScanMessageHandler(orchestrator.Object, serializer.Object, _config);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task CtsDailyScanMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<CtsDailyScanOrchestrator>(Enumerable.Empty<Application.Orchestration.ChangeScanning.IScanStep<CtsDailyScanContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsDailyScanMessage>>();

        var message = new UnwrappedMessage { MessageId = "1", CorrelationId = "2" };
        var payload = new CtsDailyScanMessage { Identifier = "Test" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new CtsDailyScanMessageHandler(orchestrator.Object, serializer.Object, _config);

        await handler.Handle(message, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.IsAny<CtsDailyScanContext>(), _ct), Times.Once);
    }

    [Fact]
    public async Task CtsDailyScanMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<CtsDailyScanOrchestrator>(Enumerable.Empty<Application.Orchestration.ChangeScanning.IScanStep<CtsDailyScanContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsDailyScanMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };

        serializer.Setup(x => x.Deserialize(message)).Returns((CtsDailyScanMessage?)null);

        var handler = new CtsDailyScanMessageHandler(orchestrator.Object, serializer.Object, _config);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task CtsImportHoldingMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<CtsHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<CtsHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsImportHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new CtsImportHoldingMessage { Identifier = "CPH123" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new CtsImportHoldingMessageHandler(orchestrator.Object, serializer.Object);

        await handler.Handle(message, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.Is<CtsHoldingImportContext>(c => c.Cph == "CPH123"), _ct), Times.Once);
    }

    [Fact]
    public async Task CtsImportHoldingMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<CtsHoldingImportOrchestrator>(Enumerable.Empty<Application.Orchestration.Imports.IImportStep<CtsHoldingImportContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsImportHoldingMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };

        serializer.Setup(x => x.Deserialize(message)).Returns((CtsImportHoldingMessage?)null);

        var handler = new CtsImportHoldingMessageHandler(orchestrator.Object, serializer.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task CtsUpdateHoldingMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<CtsUpdateHoldingOrchestrator>(Enumerable.Empty<Application.Orchestration.Updates.IUpdateStep<CtsUpdateHoldingContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsUpdateHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new CtsUpdateHoldingMessage { Identifier = "CPH123" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new CtsUpdateHoldingMessageHandler(serializer.Object, orchestrator.Object);

        await handler.Handle(message, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.Is<CtsUpdateHoldingContext>(c => c.Cph == "CPH123"), _ct), Times.Once);
    }

    [Fact]
    public async Task CtsUpdateHoldingMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<CtsUpdateHoldingOrchestrator>(Enumerable.Empty<Application.Orchestration.Updates.IUpdateStep<CtsUpdateHoldingContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsUpdateHoldingMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };

        serializer.Setup(x => x.Deserialize(message)).Returns((CtsUpdateHoldingMessage?)null);

        var handler = new CtsUpdateHoldingMessageHandler(serializer.Object, orchestrator.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task CtsUpdateHoldingMessageHandler_Handle_MongoException_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<CtsUpdateHoldingOrchestrator>(Enumerable.Empty<Application.Orchestration.Updates.IUpdateStep<CtsUpdateHoldingContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsUpdateHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new CtsUpdateHoldingMessage { Identifier = "CPH123" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var mongoException = MongoExceptionFactory.CreateMongoBulkWriteException();
        orchestrator.Setup(x => x.ExecuteAsync(It.IsAny<CtsUpdateHoldingContext>(), _ct)).ThrowsAsync(mongoException);

        var handler = new CtsUpdateHoldingMessageHandler(serializer.Object, orchestrator.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task CtsUpdateHoldingMessageHandler_Handle_GenericException_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<CtsUpdateHoldingOrchestrator>(Enumerable.Empty<Application.Orchestration.Updates.IUpdateStep<CtsUpdateHoldingContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsUpdateHoldingMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new CtsUpdateHoldingMessage { Identifier = "CPH123" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);
        orchestrator.Setup(x => x.ExecuteAsync(It.IsAny<CtsUpdateHoldingContext>(), _ct)).ThrowsAsync(new Exception("Generic"));

        var handler = new CtsUpdateHoldingMessageHandler(serializer.Object, orchestrator.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task CtsUpdateAgentMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<CtsUpdateAgentOrchestrator>(Enumerable.Empty<Application.Orchestration.Updates.IUpdateStep<CtsUpdateAgentContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsUpdateAgentMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new CtsUpdateAgentMessage { Identifier = "P1" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new CtsUpdateAgentMessageHandler(serializer.Object, orchestrator.Object);

        await handler.Handle(message, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.Is<CtsUpdateAgentContext>(c => c.PartyId == "P1"), _ct), Times.Once);
    }

    [Fact]
    public async Task CtsUpdateAgentMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<CtsUpdateAgentOrchestrator>(Enumerable.Empty<Application.Orchestration.Updates.IUpdateStep<CtsUpdateAgentContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsUpdateAgentMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };

        serializer.Setup(x => x.Deserialize(message)).Returns((CtsUpdateAgentMessage?)null);

        var handler = new CtsUpdateAgentMessageHandler(serializer.Object, orchestrator.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task CtsUpdateKeeperMessageHandler_Handle_Success()
    {
        var orchestrator = new Mock<CtsUpdateKeeperOrchestrator>(Enumerable.Empty<Application.Orchestration.Updates.IUpdateStep<CtsUpdateKeeperContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsUpdateKeeperMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new CtsUpdateKeeperMessage { Identifier = "P1" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var handler = new CtsUpdateKeeperMessageHandler(serializer.Object, orchestrator.Object);

        await handler.Handle(message, _ct);

        orchestrator.Verify(x => x.ExecuteAsync(It.Is<CtsUpdateKeeperContext>(c => c.PartyId == "P1"), _ct), Times.Once);
    }

    [Fact]
    public async Task CtsUpdateKeeperMessageHandler_Handle_DeserializationReturnsNull_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<CtsUpdateKeeperOrchestrator>(Enumerable.Empty<Application.Orchestration.Updates.IUpdateStep<CtsUpdateKeeperContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsUpdateKeeperMessage>>();
        var message = new UnwrappedMessage { MessageId = "1" };

        serializer.Setup(x => x.Deserialize(message)).Returns((CtsUpdateKeeperMessage?)null);

        var handler = new CtsUpdateKeeperMessageHandler(serializer.Object, orchestrator.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }

    [Fact]
    public async Task CtsUpdateKeeperMessageHandler_Handle_MongoException_ThrowsNonRetryable()
    {
        var orchestrator = new Mock<CtsUpdateKeeperOrchestrator>(Enumerable.Empty<Application.Orchestration.Updates.IUpdateStep<CtsUpdateKeeperContext>>());
        var serializer = new Mock<IUnwrappedMessageSerializer<CtsUpdateKeeperMessage>>();

        var message = new UnwrappedMessage { MessageId = "1" };
        var payload = new CtsUpdateKeeperMessage { Identifier = "P1" };

        serializer.Setup(x => x.Deserialize(message)).Returns(payload);

        var mongoException = MongoExceptionFactory.CreateMongoBulkWriteException();
        orchestrator.Setup(x => x.ExecuteAsync(It.IsAny<CtsUpdateKeeperContext>(), _ct)).ThrowsAsync(mongoException);

        var handler = new CtsUpdateKeeperMessageHandler(serializer.Object, orchestrator.Object);

        await Assert.ThrowsAsync<NonRetryableException>(() => handler.Handle(message, _ct));
    }
}